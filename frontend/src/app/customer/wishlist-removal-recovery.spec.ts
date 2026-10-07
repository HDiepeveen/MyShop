import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, Subject } from 'rxjs';
import { CustomerWishlist } from './customer-wishlist';
import { CustomerWishlistApi, WishlistItem } from './customer-wishlist.api';

describe('Wishlist removal recovery', () => {
  const item: WishlistItem = {
    productId: 'id',
    name: 'Product',
    imageUrl: null,
    imageAlt: '',
    isAvailable: true,
    addedAt: '',
  };
  function setup(available = true) {
    const saved = { ...item, isAvailable: available };
    const add = new Subject<void>();
    const remove = new Subject<void>();
    const api = {
      list: vi.fn(() => of({ items: [saved], offset: 0, limit: 20, totalCount: 1 })),
      add: vi.fn(() => add),
      remove: vi.fn(() => remove),
    };
    TestBed.configureTestingModule({
      imports: [CustomerWishlist],
      providers: [provideRouter([]), { provide: CustomerWishlistApi, useValue: api }],
    });
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    const fixture = TestBed.createComponent(CustomerWishlist);
    return { fixture, component: fixture.componentInstance, api, add, remove, saved };
  }
  afterEach(() => vi.restoreAllMocks());

  it('offers recovery only after successful removal and reloads current filters after restoration', () => {
    const { component, api, saved, remove, add } = setup();
    component.searchText = 'Product';
    component.applySearch();
    component.changeSort('name');
    component.remove(saved);
    expect(component.removedItem()).toBeNull();
    remove.next();
    expect(component.removedItem()).toEqual(saved);
    component.undoRemoval();
    component.undoRemoval();
    component.clearSearch();
    component.load(20);
    expect(api.add).toHaveBeenCalledTimes(1);
    expect(component.search).toBe('Product');
    add.next();
    expect(component.removedItem()).toBeNull();
    expect(api.list).toHaveBeenLastCalledWith(0, 'Product', 'name');
    expect(component.notice()).toBe('Teruggezet op je verlanglijst.');
  });

  it('keeps recovery available after a failed restoration', () => {
    const { component, saved, remove, add } = setup();
    component.remove(saved);
    remove.next();
    component.undoRemoval();
    add.error(new Error('unavailable'));
    expect(component.removing()).toBe('');
    expect(component.removedItem()).toEqual(saved);
    expect(component.removeError()).toContain('niet meer beschikbaar');
  });

  it('does not offer restoration of an already withdrawn product', () => {
    const { component, api, saved, remove } = setup(false);
    component.remove(saved);
    remove.next();
    component.undoRemoval();
    expect(component.removedItem()).toBeNull();
    expect(api.add).not.toHaveBeenCalled();
  });

  it('does not offer restoration when removal fails or is cancelled', () => {
    const { component, saved, remove } = setup();
    vi.mocked(window.confirm).mockReturnValue(false);
    component.remove(saved);
    expect(component.removing()).toBe('');
    vi.mocked(window.confirm).mockReturnValue(true);
    component.remove(saved);
    remove.error(new Error('failed'));
    expect(component.removedItem()).toBeNull();
    expect(component.items()).toEqual([saved]);
  });

  it('ignores restoration completion after leaving the page', () => {
    const { fixture, component, api, saved, remove, add } = setup();
    component.remove(saved);
    remove.next();
    component.undoRemoval();
    const calls = api.list.mock.calls.length;
    fixture.destroy();
    add.next();
    expect(api.list).toHaveBeenCalledTimes(calls);
  });
  it('replaces the recovery choice only after another successful removal', () => {
    const { component, api, saved, remove } = setup();
    component.remove(saved);
    remove.next();
    const nextItem = { ...saved, productId: 'second', name: 'Tweede' };
    component.items.set([nextItem]);
    const secondRemoval = new Subject<void>();
    api.remove.mockReturnValue(secondRemoval);
    component.remove(nextItem);
    expect(component.removedItem()).toEqual(saved);
    secondRemoval.next();
    expect(component.removedItem()).toEqual(nextItem);
  });

  it('renders the recovery action and disables it during restoration', () => {
    const { fixture, component, saved, remove } = setup();
    component.remove(saved);
    remove.next();
    fixture.detectChanges();
    const button = Array.from(
      fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
    ).find((value) => value.textContent?.includes('ongedaan maken'))!;
    expect(button).toBeDefined();
    button.click();
    fixture.detectChanges();
    expect(button.disabled).toBe(true);
  });
});
