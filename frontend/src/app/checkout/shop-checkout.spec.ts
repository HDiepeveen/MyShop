import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ShopCheckout } from './shop-checkout';

const line = {
  productId: '10000000-0000-0000-0000-000000000001',
  variantId: '20000000-0000-0000-0000-000000000001',
  quantity: 2,
  expectedAmount: '12.50',
  expectedCurrency: 'EUR',
};

describe('Shop checkout', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ShopCheckout],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('prepares CSRF and submits only customer input, the choice and cart identifiers', () => {
    const fixture = TestBed.createComponent(ShopCheckout);
    fixture.componentRef.setInput('lines', [line]);
    fixture.componentRef.setInput('paymentMethod', 'payLater');
    const page = fixture.componentInstance;
    page.customerName = 'Ada Lovelace';
    page.email = 'ada@example.com';
    page.addressLine = 'Main street 1';
    page.postalCode = '1234 AB';
    page.city = 'Amsterdam';
    const placed = vi.fn();
    page.placed.subscribe(placed);

    page.submit();
    page.submit();
    http.expectOne('/api/auth/csrf').flush(null);
    const request = http.expectOne('/api/shop/orders');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      checkoutToken: expect.stringMatching(/^[0-9a-f-]{36}$/),
      paymentMethod: 'payLater',
      customerName: 'Ada Lovelace',
      email: 'ada@example.com',
      addressLine: 'Main street 1',
      postalCode: '1234 AB',
      city: 'Amsterdam',
      countryCode: 'NL',
      lines: [line],
    });
    request.flush({
      id: '30000000-0000-0000-0000-000000000001',
      number: 'MS-3000',
      placedAt: '2026-09-30T12:00:00Z',
      paymentInstructions: 'Betaal binnen 14 dagen.',
    });
    expect(placed).toHaveBeenCalledWith(expect.objectContaining({ number: 'MS-3000' }));
    expect(page.busy()).toBe(false);
  });

  it('shows the safe server conflict and permits a retry with the same checkout token', () => {
    const fixture = TestBed.createComponent(ShopCheckout);
    fixture.componentRef.setInput('lines', [line]);
    fixture.componentRef.setInput('paymentMethod', 'payLater');
    const page = fixture.componentInstance;

    page.submit();
    http.expectOne('/api/auth/csrf').flush(null);
    const first = http.expectOne('/api/shop/orders');
    const token = first.request.body.checkoutToken;
    first.flush(
      { code: 'cartChanged', message: 'Controleer de winkelmand opnieuw.' },
      { status: 409, statusText: 'Conflict' },
    );
    expect(page.failure()).toBe('Controleer de winkelmand opnieuw.');

    page.submit();
    http.expectOne('/api/auth/csrf').flush(null);
    const retry = http.expectOne('/api/shop/orders');
    expect(retry.request.body.checkoutToken).toBe(token);
    retry.flush({
      id: '30000000-0000-0000-0000-000000000001',
      number: 'MS-3000',
      placedAt: '2026-09-30T12:00:00Z',
      paymentInstructions: 'Betaal binnen 14 dagen.',
    });
  });
});
