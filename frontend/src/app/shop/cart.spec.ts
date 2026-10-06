import { By } from '@angular/platform-browser';
import { ShopCheckout } from '../checkout/shop-checkout';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { Cart } from './cart';
import { ShopCart } from './shop-cart';
import { routes } from '../app.routes';
import { ShopDetail } from './shop-detail';

const productId = '10000000-0000-0000-0000-000000000001';
const variantId = '20000000-0000-0000-0000-000000000001';
const secondId = '20000000-0000-0000-0000-000000000002';
const line = { productId, variantId, quantity: 1 };
const product = {
  id: productId,
  name: 'Shirt',
  description: 'Linen',
  imageUrl: null,
  imageAlt: '',
  categories: [],
  variants: [
    { id: variantId, name: 'Small' },
    { id: secondId, name: 'Large' },
  ],
};
const key = 'myshop.cart.v1';

beforeEach(() => localStorage.removeItem(key));
afterEach(() => {
  localStorage.removeItem(key);
  vi.restoreAllMocks();
});

describe('Cart storage', () => {
  it('does not persist actions on missing cart lines or clear a storage warning', () => {
    const cart = new Cart();
    cart.add(productId, variantId);
    const stored = cart.lines();
    cart.warning.set('Storage unavailable');
    const setItem = vi.spyOn(Storage.prototype, 'setItem');
    const missing = { ...line, variantId: secondId };
    expect(cart.setQuantity(missing, 2)).toContain('niet meer');
    cart.remove(missing);
    expect(cart.lines()).toBe(stored);
    expect(cart.warning()).toBe('Storage unavailable');
    expect(setItem).not.toHaveBeenCalled();
    cart.setQuantity(line, 2);
    expect(cart.count()).toBe(2);
    cart.remove(line);
    expect(cart.count()).toBe(0);
  });
  it('merges variants, preserves only identifiers and quantities, and restores after reload', () => {
    const cart = new Cart();
    expect(cart.add(productId, variantId)).toBe('');
    cart.add(productId, variantId);
    cart.add(productId, secondId);
    expect(cart.lines()).toEqual([
      { ...line, quantity: 2 },
      { ...line, variantId: secondId },
    ]);
    expect(new Cart().count()).toBe(3);
    expect(JSON.parse(localStorage.getItem(key)!)).toEqual(cart.lines());
    cart.remove(cart.lines()[0]);
    expect(new Cart().lines()).toEqual([{ ...line, variantId: secondId }]);
    cart.clear();
    expect(new Cart().lines()).toEqual([]);
  });
  it('validates quantities and capacity without changing the cart on failure', () => {
    const cart = new Cart();
    cart.add(productId, variantId);
    for (const quantity of [0, -1, 100, 1.5, NaN, Infinity])
      expect(cart.setQuantity(line, quantity)).not.toBe('');
    expect(cart.count()).toBe(1);
    cart.setQuantity(line, 99);
    expect(cart.add(productId, variantId)).not.toBe('');
    expect(cart.count()).toBe(99);
    for (let i = 2; i <= 20; i++)
      cart.add(productId, `20000000-0000-0000-0000-${String(i).padStart(12, '0')}`);
    expect(cart.lines()).toHaveLength(20);
    expect(cart.add(productId, '20000000-0000-0000-0000-000000000021')).not.toBe('');
  });
  it.each([
    'bad json',
    '{}',
    '[null]',
    JSON.stringify([{ ...line, quantity: '2' }]),
    JSON.stringify([{ ...line, productId: 'bad' }]),
    JSON.stringify([line, line]),
  ])('rejects invalid persisted data: %s', (raw) => {
    localStorage.setItem(key, raw);
    const cart = new Cart();
    expect(cart.lines()).toEqual([]);
    expect(cart.warning()).not.toBe('');
    expect(cart.add(productId, variantId)).toBe('');
  });
  it('keeps working in memory when browser storage is unavailable', () => {
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error();
    });
    const cart = new Cart();
    cart.add(productId, variantId);
    expect(cart.count()).toBe(1);
    expect(cart.warning()).toContain('niet in deze browser');
  });
});

describe('Cart page', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter(routes), provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => {
    http.match('/api/shop/delivery-methods').forEach((request) => request.flush([]));
    http.verify();
  });
  function priceReply(amount: string | null = '12.50') {
    http.expectOne(`/api/shop/products/${productId}`).flush(product);
    http
      .expectOne(`/api/shop/products/${productId}/prices`)
      .flush({ at: '2026-09-30T12:00:00Z', variants: [{ variantId, amount, currency: 'EUR' }] });
  }
  function request() {
    return http.expectOne((r) => r.url === '/api/shop/cart/quote');
  }
  function paymentReply(
    items: { code: string; name: string; instructions: string | null }[] = [
      { code: 'payLater', name: 'Later betalen', instructions: 'Betaal binnen 14 dagen.' },
    ],
  ) {
    http.expectOne('/api/shop/payment-options').flush({ items });
  }
  function quote(amount = '12.50', quantity = 1, total = '12.50') {
    return {
      at: '2026-09-30T12:00:00Z',
      lines: [
        {
          ...line,
          quantity,
          name: 'Shirt',
          variant: 'Small',
          amount,
          currency: 'EUR',
          total,
          failure: null,
        },
      ],
      totals: [{ currency: 'EUR', amount: total }],
    };
  }
  it.each(['current', 'unavailable'])(
    'rechecks repeated order articles with %s server data before checkout',
    async (result) => {
      const historical = {
        ...line,
        quantity: 2,
        unitAmount: '1.00',
        productName: 'Old product name',
      };
      expect(TestBed.inject(Cart).addLines([historical])).toBe('');
      const harness = await RouterTestingHarness.create('/winkel/winkelmand');
      paymentReply();
      const pending = request();
      expect(pending.request.params.getAll('lines')).toEqual([productId + ':' + variantId + ':2']);
      const response = quote('20.00', 2, '40.00');
      if (result === 'current') pending.flush(response);
      else
        pending.flush({
          ...response,
          lines: response.lines.map((line) => ({
            ...line,
            amount: null,
            currency: null,
            total: null,
            failure: 'unavailable',
          })),
          totals: [],
        });
      await harness.fixture.whenStable();
      harness.detectChanges();
      const text = harness.routeNativeElement!.textContent;
      expect(text).not.toContain('Old product name');
      if (result === 'current') expect(text).toContain('EUR 40,00');
      else expect(text).toContain('niet meer beschikbaar');
      http.expectNone((request) => request.method === 'POST');
    },
  );
  it('locks cart controls during the actual checkout request and restores them after failure', async () => {
    TestBed.inject(Cart).add(productId, variantId);
    const harness = await RouterTestingHarness.create('/winkel/winkelmand');
    paymentReply();
    http
      .expectOne('/api/shop/delivery-methods')
      .flush([
        {
          id: '40000000-0000-0000-0000-000000000001',
          name: 'Post',
          amount: '0.00',
          currency: 'EUR',
          description: null,
        },
      ]);
    request().flush(quote());
    await harness.fixture.whenStable();
    harness.detectChanges();
    const page = harness.routeDebugElement!.componentInstance as ShopCart;
    const checkout = harness.routeDebugElement!.query(By.directive(ShopCheckout))
      .componentInstance as ShopCheckout;
    checkout.customerName = 'Ada';
    checkout.email = 'ada@example.test';
    checkout.addressLine = 'Street 1';
    checkout.postalCode = '1234 AB';
    checkout.city = 'Utrecht';
    checkout.submit();
    expect(page.checkoutBusy()).toBe(true);
    const before = TestBed.inject(Cart).lines();
    const confirm = vi.spyOn(window, 'confirm').mockReturnValue(true);
    page.clearCart();
    page.remove(line);
    page.update(line, '4');
    page.undoRemoval();
    page.refresh();
    expect(TestBed.inject(Cart).lines()).toBe(before);
    expect(confirm).not.toHaveBeenCalled();
    http.expectOne('/api/auth/csrf').flush(null);
    const order = http.expectOne('/api/shop/orders');
    expect(order.request.body.lines[0].quantity).toBe(1);
    order.flush({ message: 'Retry checkout' }, { status: 409, statusText: 'Conflict' });
    expect(page.checkoutBusy()).toBe(false);
    page.remove(line);
    await harness.fixture.whenStable();
    expect(TestBed.inject(Cart).lines()).toEqual([]);
    expect(page.undoItems()).toEqual([line]);
  });
  it('opens an empty public cart and loads the available payment methods', async () => {
    const harness = await RouterTestingHarness.create('/winkel/winkelmand');
    paymentReply();
    await harness.fixture.whenStable();
    expect(harness.routeNativeElement!.textContent).toContain('Je winkelmand is leeg');
    (harness.routeDebugElement!.componentInstance as ShopCart).orderPlaced({
      id: '30000000-0000-0000-0000-000000000001',
      number: 'MS-3000',
      placedAt: '2026-09-30T12:00:00Z',
      paymentInstructions: 'Betaal binnen 14 dagen.',
      totals: [{ currency: 'EUR', amount: '17.45' }],
      deliveryMethod: {
        id: '40000000-0000-0000-0000-000000000001',
        name: 'Pakketdienst',
        description: 'Binnen twee werkdagen.',
        amount: '4.95',
        currency: 'EUR',
      },
    });
    await harness.fixture.whenStable();
    expect(harness.routeNativeElement!.textContent).toContain('Je bestelnummer is MS-3000');
    expect(harness.routeNativeElement!.textContent).toContain('Pakketdienst');
    expect(harness.routeNativeElement!.textContent).toContain('Binnen twee werkdagen.');
    expect(harness.routeNativeElement!.textContent).toContain('EUR 17,45');
    expect(harness.routeNativeElement!.textContent).toContain('Betaal binnen 14 dagen.');
  });
  it('adds the selected variant from the product page', async () => {
    const harness = await RouterTestingHarness.create(`/winkel/${productId}`);
    priceReply();
    await harness.fixture.whenStable();
    const button = Array.from(harness.routeNativeElement!.querySelectorAll('button')).find((b) =>
      b.textContent?.includes('In winkelmand'),
    )!;
    button.click();
    await harness.fixture.whenStable();
    expect(TestBed.inject(Cart).lines()).toEqual([line]);
    expect(harness.routeNativeElement!.textContent).toContain('Toegevoegd aan je winkelmand');
  });
  it('cannot add an unpriced variant', async () => {
    const harness = await RouterTestingHarness.create(`/winkel/${productId}`);
    priceReply(null);
    await harness.fixture.whenStable();
    const button = Array.from(harness.routeNativeElement!.querySelectorAll('button')).find((b) =>
      b.textContent?.includes('In winkelmand'),
    )!;
    expect(button.disabled).toBe(true);
    (harness.routeDebugElement!.componentInstance as ShopDetail).addToCart();
    expect(TestBed.inject(Cart).count()).toBe(0);
  });
  it('sends only identifiers and quantities in one request and shows exact server amounts', async () => {
    const cart = TestBed.inject(Cart);
    cart.add(productId, variantId);
    cart.add(productId, secondId);
    const harness = await RouterTestingHarness.create('/winkel/winkelmand');
    paymentReply([
      { code: 'payLater', name: 'Later betalen', instructions: 'Betaal binnen 14 dagen.' },
      { code: 'online', name: 'Direct online betalen', instructions: null },
    ]);
    const pending = request();
    expect(pending.request.method).toBe('GET');
    expect(pending.request.params.keys()).toEqual(['lines']);
    expect(pending.request.params.getAll('lines')).toEqual([
      `${productId}:${variantId}:1`,
      `${productId}:${secondId}:1`,
    ]);
    const response = quote('9999999999999999.99', 1, '9999999999999999.99');
    response.lines.push({
      ...response.lines[0],
      variantId: secondId,
      currency: 'USD',
      amount: '0.00',
      total: '0.00',
    });
    response.totals.push({ currency: 'USD', amount: '0.00' });
    pending.flush(response);
    await harness.fixture.whenStable();
    const page = harness.routeDebugElement!.componentInstance as ShopCart;
    expect(page.totals()).toEqual(response.totals);
    expect(page.complete()).toBe(true);
    expect(page.selectedPayment()).toBe('payLater');
    expect(harness.routeNativeElement!.textContent).toContain('Betaal binnen 14 dagen.');
    expect(harness.routeNativeElement!.textContent).toContain('9999999999999999,99');
    expect(harness.routeNativeElement!.textContent).toContain('USD 0,00');
    expect(harness.routeNativeElement!.querySelectorAll('input[type=radio]')).toHaveLength(2);
  });
  it('cancels obsolete quotes and clears old totals during changes and errors', async () => {
    const cart = TestBed.inject(Cart);
    cart.add(productId, variantId);
    const harness = await RouterTestingHarness.create('/winkel/winkelmand');
    paymentReply();
    request().flush(quote());
    await harness.fixture.whenStable();
    const page = harness.routeDebugElement!.componentInstance as ShopCart;
    cart.setQuantity(line, 2);
    expect(page.complete()).toBe(false);
    await harness.fixture.whenStable();
    const obsolete = request();
    cart.setQuantity(line, 3);
    await harness.fixture.whenStable();
    expect(obsolete.cancelled).toBe(true);
    request().flush(quote('10.00', 3, '30.00'));
    await harness.fixture.whenStable();
    expect(page.totals()).toEqual([{ currency: 'EUR', amount: '30.00' }]);
    page.refresh();
    expect(page.totals()).toEqual([]);
    request().flush({}, { status: 500, statusText: 'Error' });
    await harness.fixture.whenStable();
    expect(page.complete()).toBe(false);
    expect(harness.routeNativeElement!.textContent).toContain('kon niet worden gecontroleerd');
    page.refresh();
    request().flush(quote('15.00', 3, '45.00'));
    await harness.fixture.whenStable();
    expect(page.totals()).toEqual([{ currency: 'EUR', amount: '45.00' }]);
  });
  it.each(['unavailable', 'priceMissing', 'outOfStock'] as const)(
    'displays unavailable lines without totals: %s',
    async (failure) => {
      TestBed.inject(Cart).add(productId, variantId);
      const harness = await RouterTestingHarness.create('/winkel/winkelmand');
      paymentReply();
      request().flush({
        at: '2026-09-30T12:00:00Z',
        lines: [
          {
            ...line,
            name: null,
            variant: null,
            amount: null,
            currency: null,
            total: null,
            failure,
          },
        ],
        totals: [],
      });
      await harness.fixture.whenStable();
      const page = harness.routeDebugElement!.componentInstance as ShopCart;
      expect(page.complete()).toBe(false);
      expect(harness.routeNativeElement!.textContent).toContain('Geen subtotaal beschikbaar');
      page.remove(line);
      await harness.fixture.whenStable();
      expect(harness.routeNativeElement!.textContent).toContain('Je winkelmand is leeg');
    },
  );
  it('submits quantity edits through the form and can remove the last line', async () => {
    TestBed.inject(Cart).add(productId, variantId);
    const harness = await RouterTestingHarness.create('/winkel/winkelmand');
    paymentReply();
    request().flush(quote());
    await harness.fixture.whenStable();
    harness.routeNativeElement!.querySelector('input')!.value = '4';
    harness.routeNativeElement!.querySelector<HTMLButtonElement>('button[type=submit]')!.click();
    await harness.fixture.whenStable();
    const pending = request();
    expect(pending.request.params.get('lines')).toBe(`${productId}:${variantId}:4`);
    pending.flush(quote('12.50', 4, '50.00'));
    await harness.fixture.whenStable();
    expect(harness.routeNativeElement!.textContent).toContain('50,00');
    Array.from(harness.routeNativeElement!.querySelectorAll('button'))
      .find((button) => button.textContent?.includes('Verwijderen'))!
      .click();
    await harness.fixture.whenStable();
    expect(harness.routeNativeElement!.textContent).toContain('Je winkelmand is leeg');
  });
});
