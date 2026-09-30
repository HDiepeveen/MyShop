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
  afterEach(() => http.verify());
  function priceReply(amount: number | null = 12.5) {
    http.expectOne(`/api/shop/products/${productId}`).flush(product);
    http
      .expectOne(`/api/shop/products/${productId}/prices`)
      .flush({ at: '2026-09-30T12:00:00Z', variants: [{ variantId, amount, currency: 'EUR' }] });
  }
  function request() {
    return http.expectOne((r) => r.url === '/api/shop/cart/quote');
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
  it('opens an empty public cart without requests', async () => {
    const harness = await RouterTestingHarness.create('/winkel/winkelmand');
    expect(harness.routeNativeElement!.textContent).toContain('Je winkelmand is leeg');
    http.expectNone(() => true);
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
    expect(harness.routeNativeElement!.textContent).toContain('9999999999999999,99');
    expect(harness.routeNativeElement!.textContent).toContain('USD 0,00');
  });
  it('cancels obsolete quotes and clears old totals during changes and errors', async () => {
    const cart = TestBed.inject(Cart);
    cart.add(productId, variantId);
    const harness = await RouterTestingHarness.create('/winkel/winkelmand');
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
  it.each(['unavailable', 'priceMissing'])(
    'displays unavailable lines without totals: %s',
    async (failure) => {
      TestBed.inject(Cart).add(productId, variantId);
      const harness = await RouterTestingHarness.create('/winkel/winkelmand');
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
