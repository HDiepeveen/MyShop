import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, Subject } from 'rxjs';
import { CustomerProfile } from './customer-profile';
import { CustomerAccountApi } from './customer-account.api';
import { CustomerManagementList } from './customer-management-list';
import { CustomerManagementApi, ManagedCustomerPage } from './customer-management.api';

const profile = {
  email: 'ada@example.test',
  name: 'Ada',
  addressLine: 'Street 1',
  postalCode: '1234 AB',
  city: 'Utrecht',
  countryCode: 'NL',
  revision: 'r',
};
describe('Profile input validation', () => {
  function setup() {
    const api = { profile: vi.fn(() => of(profile)), update: vi.fn(() => new Subject()) };
    TestBed.configureTestingModule({
      imports: [CustomerProfile],
      providers: [provideRouter([]), { provide: CustomerAccountApi, useValue: api }],
    });
    const fixture = TestBed.createComponent(CustomerProfile);
    return { page: fixture.componentInstance, api };
  }
  for (const [field, maximum] of [
    ['name', 200],
    ['addressLine', 200],
    ['postalCode', 32],
    ['city', 100],
  ] as const) {
    it.each(['blank', 'long'])(
      'rejects ' + field + ' %s and preserves draft and revision',
      (invalid) => {
        const { page, api } = setup();
        page[field] = invalid === 'blank' ? '  ' : 'x'.repeat(maximum + 1);
        const draft = page[field];
        page.message.set('Saved before');
        page.save();
        expect(api.update).not.toHaveBeenCalled();
        expect(page.failure()).toBeTruthy();
        expect(page.message()).toBe('');
        expect(page.busy()).toBe(false);
        expect(page[field]).toBe(draft);
        expect(page.revision).toBe('r');
      },
    );
  }
  it.each(['', 'N', 'NLD', '12'])('rejects country code %s', (country) => {
    const { page, api } = setup();
    page.countryCode = country;
    page.save();
    expect(api.update).not.toHaveBeenCalled();
    expect(page.failure()).toContain('landcode');
  });
  it('trims names and address details and normalizes country without changing the revision', () => {
    const { page, api } = setup();
    page.name = ' Ada ';
    page.addressLine = ' Street 1 ';
    page.postalCode = ' 1234 AB ';
    page.city = ' Utrecht ';
    page.countryCode = ' be ';
    page.save();
    expect(api.update).toHaveBeenCalledWith({
      name: 'Ada',
      addressLine: 'Street 1',
      postalCode: '1234 AB',
      city: 'Utrecht',
      countryCode: 'BE',
      revision: 'r',
    });
  });
  it('accepts maximum field lengths', () => {
    const { page, api } = setup();
    page.name = 'x'.repeat(200);
    page.addressLine = 'x'.repeat(200);
    page.postalCode = 'x'.repeat(32);
    page.city = 'x'.repeat(100);
    page.save();
    expect(api.update).toHaveBeenCalledOnce();
  });
});
describe('Customer management paging boundaries', () => {
  function setup() {
    const response = new Subject<ManagedCustomerPage>();
    const api = { list: vi.fn(() => response) };
    TestBed.configureTestingModule({
      imports: [CustomerManagementList],
      providers: [provideRouter([]), { provide: CustomerManagementApi, useValue: api }],
    });
    return {
      page: TestBed.createComponent(CustomerManagementList).componentInstance,
      api,
      response,
    };
  }
  it('does not reload the first page through previous or advance an empty page', () => {
    const { page, api, response } = setup();
    response.next({ items: [], offset: 0, limit: 20, totalCount: 0 });
    page.previous();
    page.next();
    expect(api.list).toHaveBeenCalledTimes(1);
    response.next({ items: [], offset: 20, limit: 20, totalCount: 40 });
    page.next();
    expect(api.list).toHaveBeenCalledTimes(1);
    page.previous();
    expect(api.list).toHaveBeenLastCalledWith(0, '');
  });
  it('stops at the last nonempty page and advances when more customers exist', () => {
    const { page, api, response } = setup();
    const item = {
      id: 'c',
      email: 'ada@example.test',
      name: 'Ada',
      isLocked: false,
      lockedUntil: null,
      orderCount: 0,
    };
    response.next({ items: [item], offset: 20, limit: 20, totalCount: 21 });
    page.next();
    expect(api.list).toHaveBeenCalledTimes(1);
    response.next({ items: [item], offset: 20, limit: 20, totalCount: 22 });
    page.next();
    expect(api.list).toHaveBeenLastCalledWith(40, '');
  });
});
