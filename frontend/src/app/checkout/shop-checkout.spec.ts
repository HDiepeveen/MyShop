import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { MollieNavigation } from './mollie-navigation';
import { PendingPayment } from './pending-payment';
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
    localStorage.removeItem('HDWebFramework.pending-payment.v1');
    TestBed.configureTestingModule({
      imports: [ShopCheckout],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: MollieNavigation,
          useValue: {
            open: vi.fn(() => {
              throw new Error('Blocked navigation');
            }),
          },
        },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    localStorage.removeItem('HDWebFramework.pending-payment.v1');
  });

  it('saves the payment before redirecting directly and blocks duplicate clicks during navigation', () => {
    const fixture = TestBed.createComponent(ShopCheckout);
    fixture.componentRef.setInput('lines', [line]);
    fixture.componentRef.setInput('paymentMethod', 'online');
    fixture.componentRef.setInput('deliveryMethodId', '40000000-0000-0000-0000-000000000001');
    const page = fixture.componentInstance;
    page.customerName = 'Ada';
    page.email = 'ada@example.test';
    page.addressLine = 'Street 1';
    page.postalCode = '1234 AB';
    page.city = 'Utrecht';
    page.phoneNumber = '0301234567';
    page.countryCode = 'Nederland';
    const placed = vi.fn();
    page.placed.subscribe(placed);
    const navigation = vi.spyOn(TestBed.inject(MollieNavigation), 'open').mockImplementation(() => {
      expect(TestBed.inject(PendingPayment).token()).not.toBeNull();
      expect(
        TestBed.inject(PendingPayment).matches(TestBed.inject(PendingPayment).token()!, [line]),
      ).toBe(true);
    });
    page.submit();
    page.submit();
    http.expectOne('/api/auth/csrf').flush(null);
    const start = http.expectOne('/api/shop/online-payments');
    start.flush({
      checkoutToken: start.request.body.checkoutToken,
      providerName: 'Mollie',
      checkoutUrl: 'https://www.mollie.com/checkout/test',
      paymentReference: 'OP-TEST',
      providerPaymentId: 'tr_test',
      totals: [],
      deliveryMethod: {},
    });
    expect(navigation).toHaveBeenCalledWith('https://www.mollie.com/checkout/test');
    expect(page.redirecting()).toBe(true);
    expect(page.busy()).toBe(true);
    expect(placed).not.toHaveBeenCalled();
    page.submit();
    http.expectNone('/api/auth/csrf');
    http.expectNone('/api/shop/online-payments');
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[aria-label="Online betaling"]')).toBeNull();
  });

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
    page.phoneNumber = '0301234567';
    page.countryCode = 'Nederland';
    page.customerName = 'Ada Lovelace';
    page.email = 'ada@example.com';
    page.addressLine = 'Main street 1';
    page.postalCode = '1234 AB';
    page.city = 'Amsterdam';
    page.phoneNumber = '0301234567';
    page.countryCode = 'Nederland';
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
      countryCode: 'Nederland',
      phoneNumber: '0301234567',
      notes: '',
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
    page.phoneNumber = '0301234567';
    page.countryCode = 'Nederland';

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
    page.phoneNumber = '0301234567';
    page.countryCode = 'Nederland';
    const placed = vi.fn();
    page.placed.subscribe(placed);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Betalen met iDEAL');

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
      countryCode: 'Nederland',
      phoneNumber: '0301234567',
      notes: '',
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
    expect(TestBed.inject(MollieNavigation).open).toHaveBeenCalledWith(
      'https://payments.example.test/test_123',
    );
    expect(page.notice()).toContain('Automatisch doorsturen lukte niet');
    fixture.detectChanges();
    const content = fixture.nativeElement.textContent;
    expect(content).toContain('Automatisch doorsturen lukte niet');
    expect(content).toContain('Betaalprovider: Mollie');
    expect(content).toContain('Betalingskenmerk: OP-123');
    expect(content).not.toContain('Providerbetaling:');
    expect(content).toContain('Testmodus: er wordt geen echt geld afgeschreven.');
    const link = fixture.nativeElement.querySelector('a');
    expect(link.getAttribute('href')).toBe('https://payments.example.test/test_123');
    expect(link.textContent).toContain('iDEAL-betaalpagina openen');
    expect(content).toContain('iDEAL via Mollie');
    expect(content).toContain('Bezorging: Pakketdienst');
    expect(content).toContain('Totaal € 29,95');

    const buttons = fixture.nativeElement.querySelectorAll(
      'button',
    ) as NodeListOf<HTMLButtonElement>;
    const button = Array.from(buttons).find((item) =>
      item.textContent?.includes('Betaalstatus controleren'),
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
