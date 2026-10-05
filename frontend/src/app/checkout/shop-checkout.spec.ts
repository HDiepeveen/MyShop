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
    fixture.componentRef.setInput('deliveryMethodId', '40000000-0000-0000-0000-000000000001');
    const page = fixture.componentInstance;
    page.customerName = 'Ada Lovelace';
    page.email = 'ada@example.com';
    page.addressLine = 'Main street 1';
    page.postalCode = '1234 AB';
    page.city = 'Amsterdam';
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
      deliveryMethodId: '40000000-0000-0000-0000-000000000001',
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
    fixture.componentRef.setInput('deliveryMethodId', '40000000-0000-0000-0000-000000000001');
    const page = fixture.componentInstance;
    page.customerName = 'Ada Lovelace';
    page.email = 'ada@example.com';
    page.addressLine = 'Main street 1';
    page.postalCode = '1234 AB';
    page.city = 'Amsterdam';

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

  it('prepares an online payment without placing the order yet', () => {
    const fixture = TestBed.createComponent(ShopCheckout);
    fixture.componentRef.setInput('lines', [line]);
    fixture.componentRef.setInput('paymentMethod', 'online');
    fixture.componentRef.setInput('deliveryMethodId', '40000000-0000-0000-0000-000000000001');
    const page = fixture.componentInstance;
    page.customerName = 'Ada Lovelace';
    page.email = 'ada@example.com';
    page.addressLine = 'Main street 1';
    page.postalCode = '1234 AB';
    page.city = 'Amsterdam';
    const placed = vi.fn();
    page.placed.subscribe(placed);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Online betaling voorbereiden');

    page.submit();
    http.expectOne('/api/auth/csrf').flush(null);
    const request = http.expectOne('/api/shop/online-payments');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      checkoutToken: expect.stringMatching(/^[0-9a-f-]{36}$/),
      deliveryMethodId: '40000000-0000-0000-0000-000000000001',
      customerName: 'Ada Lovelace',
      email: 'ada@example.com',
      addressLine: 'Main street 1',
      postalCode: '1234 AB',
      city: 'Amsterdam',
      countryCode: 'NL',
      lines: [line],
    });
    request.flush({
      checkoutToken: request.request.body.checkoutToken,
      providerName: 'Mollie',
      paymentReference: 'OP-123',
      providerPaymentId: 'test_123',
      checkoutUrl: 'https://payments.example.test/test_123',
      message: 'De online betaalprovider is klaar om gekoppeld te worden.',
      totals: [{ currency: 'EUR', amount: '29.95' }],
      deliveryMethod: {
        id: '40000000-0000-0000-0000-000000000001',
        name: 'Pakketdienst',
        description: null,
        amount: '4.95',
        currency: 'EUR',
      },
    });
    expect(page.notice()).toBe('De online betaalprovider is klaar om gekoppeld te worden.');
    fixture.detectChanges();
    const content = fixture.nativeElement.textContent;
    expect(content).toContain('De online betaalprovider is klaar om gekoppeld te worden.');
    expect(content).toContain('Betaalprovider: Mollie');
    expect(content).toContain('Betalingskenmerk: OP-123');
    expect(content).toContain('Providerbetaling: test_123');
    const link = fixture.nativeElement.querySelector('a');
    expect(link.getAttribute('href')).toBe('https://payments.example.test/test_123');
    expect(content).toContain('Bezorging: Pakketdienst');
    expect(content).toContain('Totaal EUR 29,95');

    const buttons = fixture.nativeElement.querySelectorAll(
      'button',
    ) as NodeListOf<HTMLButtonElement>;
    const button = Array.from(buttons).find((item) =>
      item.textContent?.includes('Testbetaling afronden'),
    )!;
    button.click();
    http.expectOne('/api/auth/csrf').flush(null);
    const complete = http.expectOne(
      `/api/shop/online-payments/${request.request.body.checkoutToken}/complete?providerPaymentId=test_123`,
    );
    expect(complete.request.method).toBe('GET');
    complete.flush({
      id: '30000000-0000-0000-0000-000000000001',
      number: 'MS-3000',
      placedAt: '2026-09-30T12:00:00Z',
      paymentInstructions: null,
      totals: [{ currency: 'EUR', amount: '29.95' }],
      deliveryMethod: {
        id: '40000000-0000-0000-0000-000000000001',
        name: 'Pakketdienst',
        description: null,
        amount: '4.95',
        currency: 'EUR',
      },
    });
    expect(placed).toHaveBeenCalledWith(expect.objectContaining({ number: 'MS-3000' }));
  });
});
