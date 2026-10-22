import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { CheckoutEnabledSettings } from './checkout-enabled-settings';

describe('Checkout switch in website settings', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [CheckoutEnabledSettings], providers: [provideHttpClient(), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  const options = { checkoutEnabled: true, payLaterEnabled: true, onlinePaymentEnabled: false,
    payLaterInstructions: 'Betaal bij ophalen', revision: 'revision-1', onlinePaymentConfigured: false, onlinePaymentProvider: null };
  it('changes checkout while preserving payment methods, instructions and concurrency revision', () => {
    const page = TestBed.createComponent(CheckoutEnabledSettings).componentInstance;
    http.expectOne('/api/payment-options').flush(options);
    page.enabled = false;
    page.save();
    const request = http.expectOne('/api/payment-options');
    expect(request.request.body).toEqual({ checkoutEnabled: false, payLaterEnabled: true, onlinePaymentEnabled: false,
      payLaterInstructions: 'Betaal bij ophalen', revision: 'revision-1' });
    request.flush({ ...options, checkoutEnabled: false, revision: 'revision-2' });
    expect(page.options()?.revision).toBe('revision-2');
    expect(page.notice()).toBe('Bestelinstelling opgeslagen.');
  });
  it('does not enable ordering without a payment method', () => {
    const page = TestBed.createComponent(CheckoutEnabledSettings).componentInstance;
    http.expectOne('/api/payment-options').flush({ ...options, checkoutEnabled: false, payLaterEnabled: false });
    page.enabled = true;
    page.save();
    http.expectNone(request => request.method === 'PUT');
    expect(page.error()).toContain('Betaalopties');
  });
});
