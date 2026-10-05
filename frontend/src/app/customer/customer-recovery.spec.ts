import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject, Subject } from 'rxjs';
import { CustomerProfile } from './customer-profile';
import { CustomerAccountApi, CustomerProfile as Profile } from './customer-account.api';
import { CustomerWishlist } from './customer-wishlist';
import { CustomerWishlistApi, WishlistPage } from './customer-wishlist.api';
import { CustomerOrderDetail } from './customer-order-detail';
import { CustomerOrderApi, CustomerOrderDetail as Detail } from './customer-order.api';

const profile: Profile = {
  email: 'ada@example.test', name: 'Ada', addressLine: 'Straat 1',
  postalCode: '1234 AB', city: 'Utrecht', countryCode: 'NL', revision: null,
};
const product = {
  productId: 'product-1', name: 'Shirt', imageUrl: null, imageAlt: '',
  isAvailable: true, addedAt: '2026-10-01T08:00:00Z',
};

describe('Customer page recovery', () => {
  afterEach(() => vi.restoreAllMocks());

  function profileFixture() {
    const response = new Subject<Profile>();
    const api = { profile: vi.fn(() => response), update: vi.fn(() => new Subject<Profile>()) };
    TestBed.configureTestingModule({
      imports: [CustomerProfile],
      providers: [provideRouter([]), { provide: CustomerAccountApi, useValue: api }],
    });
    return { fixture: TestBed.createComponent(CustomerProfile), response, api };
  }

  it('hides the profile form after a load failure and allows retry', () => {
    const { fixture, response, api } = profileFixture();
    response.error(new Error('Offline'));
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('form')).toBeNull();
    const retry = new Subject<Profile>();
    api.profile.mockReturnValue(retry);
    (fixture.nativeElement.querySelector('button') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeNull();
    retry.next(profile);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('form')).not.toBeNull();
  });

  it('cannot save a profile before its current values have loaded', () => {
    const { fixture, api } = profileFixture();
    fixture.componentInstance.save();
    expect(api.update).not.toHaveBeenCalled();
  });

  it('protects profile edits while saving and preserves them after a save failure', async () => {
    const { fixture, response, api } = profileFixture();
    response.next(profile);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    page.name = 'Ada Byron';
    const saved = new Subject<Profile>();
    api.update.mockReturnValue(saved);
    page.save();
    fixture.detectChanges();
    await fixture.whenStable();
    const inputs = Array.from(fixture.nativeElement.querySelectorAll('input')) as HTMLInputElement[];
    expect(inputs.length).toBe(6);
    expect(inputs.every((input) => input.disabled)).toBe(true);
    expect(api.update).toHaveBeenCalledWith({
      name: 'Ada Byron', addressLine: profile.addressLine, postalCode: profile.postalCode,
      city: profile.city, countryCode: profile.countryCode, revision: profile.revision,
    });
    saved.error(new Error('Offline'));
    fixture.detectChanges();
    await fixture.whenStable();
    expect(inputs[0].disabled).toBe(true);
    expect(inputs.slice(1).every((input) => !input.disabled)).toBe(true);
    expect(page.name).toBe('Ada Byron');
    expect(page.busy()).toBe(false);
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
  });

  it('unsubscribes profile loading and saving when leaving the page', () => {
    const { fixture, response, api } = profileFixture();
    response.next(profile);
    const saved = new Subject<Profile>();
    api.update.mockReturnValue(saved);
    fixture.componentInstance.save();
    fixture.destroy();
    expect(response.observed).toBe(false);
    expect(saved.observed).toBe(false);
  });

  function wishlistFixture() {
    const response = new Subject<WishlistPage>();
    const removal = new Subject<void>();
    const api = { list: vi.fn(() => response), remove: vi.fn(() => removal) };
    TestBed.configureTestingModule({
      imports: [CustomerWishlist],
      providers: [provideRouter([]), { provide: CustomerWishlistApi, useValue: api }],
    });
    const fixture = TestBed.createComponent(CustomerWishlist);
    return { fixture, response, removal, api };
  }

  it('ignores duplicate wishlist loads while the request is in progress', () => {
    const { fixture, api } = wishlistFixture();
    fixture.componentInstance.load(20);
    expect(api.list).toHaveBeenCalledTimes(1);
  });

  it('retries the failed wishlist page at the same offset without old products', () => {
    const { fixture, response, api } = wishlistFixture();
    response.next({ items: [product], offset: 0, limit: 20, totalCount: 21 });
    const next = new Subject<WishlistPage>();
    api.list.mockReturnValue(next);
    fixture.componentInstance.load(20);
    expect(fixture.componentInstance.items()).toEqual([]);
    next.error(new Error('Offline'));
    const retry = new Subject<WishlistPage>();
    api.list.mockReturnValue(retry);
    fixture.detectChanges();
    (fixture.nativeElement.querySelector('button') as HTMLButtonElement).click();
    expect(api.list).toHaveBeenLastCalledWith(20);
  });

  it('requires confirmation before removing a saved product', () => {
    const { fixture, response, api } = wishlistFixture();
    response.next({ items: [product], offset: 0, limit: 20, totalCount: 1 });
    vi.spyOn(window, 'confirm').mockReturnValue(false);
    fixture.componentInstance.remove(product);
    expect(api.remove).not.toHaveBeenCalled();
    expect(fixture.componentInstance.items()).toEqual([product]);
  });

  it('blocks other removals and page loads until removal has completed', () => {
    const { fixture, response, removal, api } = wishlistFixture();
    response.next({ items: [product], offset: 0, limit: 20, totalCount: 21 });
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    fixture.componentInstance.remove(product);
    fixture.componentInstance.remove({ ...product, productId: 'product-2' });
    fixture.componentInstance.load(20);
    expect(api.remove).toHaveBeenCalledTimes(1);
    expect(api.list).toHaveBeenCalledTimes(1);
    removal.next();
    fixture.componentInstance.load(20);
    expect(api.list).toHaveBeenLastCalledWith(20);
  });

  it('keeps a product after removal failure and retries removal directly', () => {
    const { fixture, response, removal, api } = wishlistFixture();
    response.next({ items: [product], offset: 0, limit: 20, totalCount: 1 });
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    fixture.componentInstance.remove(product);
    removal.error(new Error('Offline'));
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Het product kon niet worden verwijderd.');
    expect(fixture.nativeElement.textContent).not.toContain('Opnieuw proberen');
    expect(fixture.componentInstance.items()).toEqual([product]);
    api.remove.mockReturnValue(new Subject<void>());
    fixture.componentInstance.remove(product);
    expect(fixture.componentInstance.removeError()).toBe('');
    expect(api.remove).toHaveBeenCalledTimes(2);
  });

  it('offers a previous page when the last product on a later page is removed', () => {
    const { fixture, response, removal, api } = wishlistFixture();
    response.next({ items: [product], offset: 20, limit: 20, totalCount: 21 });
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    fixture.componentInstance.remove(product);
    removal.next();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain('Je verlanglijst is nog leeg.');
    const previous = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
    previous.find((button) => button.textContent?.includes('Vorige pagina'))!.click();
    expect(api.list).toHaveBeenLastCalledWith(0);
  });

  it.each(['order-2', 'order-1'])('does not replace a newly loaded %s with a late cancellation response', (nextId) => {
    const params = new BehaviorSubject(convertToParamMap({ id: 'order-1' }));
    const first = new Subject<Detail>();
    const second = new Subject<Detail>();
    const cancellation = new Subject<{ status: 'cancelled'; cancelledAt: string; revision: string }>();
    const api = { get: vi.fn((id: string) => id === 'order-1' ? first : second), cancel: vi.fn(() => cancellation) };
    TestBed.configureTestingModule({
      imports: [CustomerOrderDetail],
      providers: [provideRouter([]), { provide: ActivatedRoute, useValue: { paramMap: params } }, { provide: CustomerOrderApi, useValue: api }],
    });
    const fixture = TestBed.createComponent(CustomerOrderDetail);
    const order = { id: 'order-1', status: 'awaitingPayment', revision: 'revision-1' } as Detail;
    first.next(order);
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    fixture.componentInstance.cancel(order);
    params.next(convertToParamMap({ id: nextId }));
    const other = { ...order, id: nextId, revision: 'new-revision' };
    (nextId === 'order-1' ? first : second).next(other);
    cancellation.next({ status: 'cancelled', cancelledAt: '2026-10-01T09:00:00Z', revision: 'revision-2' });
    expect(fixture.componentInstance.order()).toEqual(other);
    expect(fixture.componentInstance.notice()).toBe('');
    expect(fixture.componentInstance.cancelling()).toBe(false);
  });
});
