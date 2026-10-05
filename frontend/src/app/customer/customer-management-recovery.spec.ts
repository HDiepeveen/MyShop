import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject, Subject } from 'rxjs';
import { CustomerManagementApi, ManagedCustomerDetail, ManagedCustomerPage } from './customer-management.api';
import { CustomerManagementList } from './customer-management-list';
import { CustomerManagementDetail } from './customer-management-detail';

const customer: ManagedCustomerDetail = {
  id: 'customer-1', email: 'ada@example.test', name: 'Ada', isLocked: false,
  lockedUntil: null, orderCount: 0, addressLine: null, postalCode: null,
  city: null, countryCode: null, lastOrderAt: null, revision: 'revision-1',
};
const page: ManagedCustomerPage = { items: [customer], offset: 0, limit: 20, totalCount: 21 };

describe('Customer management recovery', () => {
  afterEach(() => vi.restoreAllMocks());

  function listFixture() {
    const response = new Subject<ManagedCustomerPage>();
    const api = { list: vi.fn(() => response) };
    TestBed.configureTestingModule({
      imports: [CustomerManagementList],
      providers: [provideRouter([]), { provide: CustomerManagementApi, useValue: api }],
    });
    return { fixture: TestBed.createComponent(CustomerManagementList), response, api };
  }

  it('removes the old results while loading and after a failed page request', () => {
    const { fixture, response, api } = listFixture();
    response.next(page);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(customer.email);
    const next = new Subject<ManagedCustomerPage>();
    api.list.mockReturnValue(next);
    fixture.componentInstance.next();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain(customer.email);
    next.error(new Error('Offline'));
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain(customer.email);
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
  });

  it('keeps the applied search unchanged during a pending request', () => {
    const { fixture, response, api } = listFixture();
    response.next(page);
    fixture.componentInstance.searchText = 'Ada';
    fixture.componentInstance.applySearch();
    fixture.componentInstance.searchText = 'Grace';
    fixture.componentInstance.applySearch();
    fixture.componentInstance.clearSearch();
    expect(fixture.componentInstance.search).toBe('Ada');
    expect(fixture.componentInstance.searchText).toBe('Grace');
    expect(api.list).toHaveBeenCalledTimes(2);
    expect(api.list).toHaveBeenLastCalledWith(0, 'Ada');
  });

  it('keeps the requested offset stable during a pending page request', () => {
    const { fixture, response, api } = listFixture();
    response.next(page);
    fixture.componentInstance.next();
    fixture.componentInstance.next();
    fixture.componentInstance.previous();
    expect(fixture.componentInstance.offset()).toBe(20);
    expect(api.list).toHaveBeenCalledTimes(2);
    response.next({ ...page, offset: 20 });
    fixture.componentInstance.previous();
    expect(api.list).toHaveBeenLastCalledWith(0, '');
  });

  it('offers a previous page without showing an inverted range for an empty page', () => {
    const { fixture, response, api } = listFixture();
    response.next({ items: [], offset: 20, limit: 20, totalCount: 20 });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Deze pagina bevat geen klanten meer.');
    expect(fixture.nativeElement.textContent).not.toContain('21–20');
    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
    buttons.find((button) => button.textContent?.includes('Vorige pagina'))!.click();
    expect(api.list).toHaveBeenLastCalledWith(0, '');
  });

  function detailFixture() {
    const params = new BehaviorSubject(convertToParamMap({ id: customer.id }));
    const response = new Subject<ManagedCustomerDetail>();
    const action = new Subject<{ locked: boolean; revision: string }>();
    const api = { get: vi.fn(() => response), setLocked: vi.fn(() => action) };
    TestBed.configureTestingModule({
      imports: [CustomerManagementDetail],
      providers: [provideRouter([]), { provide: ActivatedRoute, useValue: { paramMap: params } }, { provide: CustomerManagementApi, useValue: api }],
    });
    return { fixture: TestBed.createComponent(CustomerManagementDetail), params, response, action, api };
  }

  it('loads a different customer after a failed request', () => {
    const { fixture, response, params, api } = detailFixture();
    response.error(new Error('Offline'));
    const next = new Subject<ManagedCustomerDetail>();
    api.get.mockReturnValue(next);
    params.next(convertToParamMap({ id: 'customer-2' }));
    expect(api.get).toHaveBeenLastCalledWith('customer-2');
    expect(fixture.componentInstance.failure()).toBe('');
    next.next({ ...customer, id: 'customer-2', name: 'Grace' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Grace');
  });

  it('clears previous customer details and action messages when navigating', () => {
    const { fixture, response, params, api } = detailFixture();
    response.next(customer);
    fixture.componentInstance.notice.set('Old notice');
    fixture.componentInstance.actionFailure.set('Old error');
    api.get.mockReturnValue(new Subject<ManagedCustomerDetail>());
    params.next(convertToParamMap({ id: 'customer-2' }));
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain(customer.email);
    expect(fixture.nativeElement.textContent).not.toContain('Old notice');
    expect(fixture.nativeElement.textContent).not.toContain('Old error');
    expect(fixture.nativeElement.textContent).toContain('Klant ophalen');
  });

  it('retries the same customer from the error screen without duplicate requests', () => {
    const { fixture, response, api } = detailFixture();
    response.error(new Error('Offline'));
    const next = new Subject<ManagedCustomerDetail>();
    api.get.mockReturnValue(next);
    fixture.detectChanges();
    (fixture.nativeElement.querySelector('button') as HTMLButtonElement).click();
    fixture.componentInstance.reload();
    expect(api.get).toHaveBeenCalledTimes(2);
    expect(api.get).toHaveBeenLastCalledWith(customer.id);
    next.next(customer);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain(customer.email);
  });

  it.each(['customer-1', 'customer-2'])('ignores late access results after loading %s', (nextId) => {
    const { fixture, response, action, params, api } = detailFixture();
    response.next(customer);
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    fixture.componentInstance.setLocked(customer, true);
    const next = new Subject<ManagedCustomerDetail>();
    api.get.mockReturnValue(next);
    params.next(convertToParamMap({ id: nextId }));
    const current = { ...customer, id: nextId, revision: 'fresh' };
    next.next(current);
    action.next({ locked: true, revision: 'changed' });
    expect(fixture.componentInstance.customer()).toEqual(current);
    expect(fixture.componentInstance.notice()).toBe('');
    expect(fixture.componentInstance.saving()).toBe(false);
  });

  it('does not show another customer access error on the current customer', () => {
    const { fixture, response, action, params, api } = detailFixture();
    response.next(customer);
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    fixture.componentInstance.setLocked(customer, true);
    const next = new Subject<ManagedCustomerDetail>();
    api.get.mockReturnValue(next);
    params.next(convertToParamMap({ id: 'customer-2' }));
    next.next({ ...customer, id: 'customer-2' });
    action.error(new Error('Offline'));
    expect(fixture.componentInstance.actionFailure()).toBe('');
    expect(fixture.componentInstance.saving()).toBe(false);
  });

  it('rejects stale or unchanged access actions without asking for confirmation', () => {
    const { fixture, response, api } = detailFixture();
    response.next(customer);
    const confirm = vi.spyOn(window, 'confirm').mockReturnValue(true);
    fixture.componentInstance.setLocked(customer, false);
    fixture.componentInstance.setLocked({ ...customer, revision: 'old' }, true);
    expect(confirm).not.toHaveBeenCalled();
    expect(api.setLocked).not.toHaveBeenCalled();
  });
});
