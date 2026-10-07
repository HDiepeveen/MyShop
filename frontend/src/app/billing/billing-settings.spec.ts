import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { BillingSettings } from './billing-settings';
const company = {
  name: 'Seller BV',
  addressLine: 'Street 1',
  postalCode: '1234 AB',
  city: 'Utrecht',
  vatId: 'NL123456789B01',
  kvkNumber: '12345678',
  invoicePrefix: 'INV-',
  revision: 'current',
};
describe('Billing settings', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [BillingSettings],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  function setup() {
    const fixture = TestBed.createComponent(BillingSettings);
    http.expectOne('/api/billing/company').flush(company);
    http.expectOne('/api/billing/vat-rates').flush([]);
    fixture.detectChanges();
    return { fixture, page: fixture.componentInstance };
  }
  it('saves company data with the loaded revision once', () => {
    const { page } = setup();
    page.draft.name = 'New seller';
    page.saveCompany();
    page.saveCompany();
    page.load();
    http.expectOne('/api/auth/csrf').flush(null);
    const request = http.expectOne('/api/billing/company');
    expect(request.request.body.revision).toBe('current');
    expect(request.request.body.name).toBe('New seller');
    request.flush({ ...company, name: 'New seller', revision: 'new' });
    expect(page.company()!.revision).toBe('new');
    expect(page.message()).toContain('opgeslagen');
  });
  it('accepts a custom decimal percentage and preserves drafts after a conflict', () => {
    const { page } = setup();
    page.rateName = 'Custom';
    page.percentage = '12,5';
    page.saveRate();
    http.expectOne('/api/auth/csrf').flush(null);
    const request = http.expectOne('/api/billing/vat-rates');
    expect(request.request.body.percentage).toBe(12.5);
    request.flush({}, { status: 409, statusText: 'Conflict' });
    expect(page.percentage).toBe('12,5');
    expect(page.busy()).toBe(false);
  });
  it.each(['101', '-1', '12.555', 'Infinity'])('rejects invalid percentage %s', (value) => {
    const { page } = setup();
    page.rateName = 'Custom';
    page.percentage = value;
    page.saveRate();
    http.expectNone('/api/auth/csrf');
    expect(page.error()).not.toBe('');
  });
});
