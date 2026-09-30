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
  function reply(
    amount: number | null = 12.5,
    currency = 'EUR',
    secondAmount = 0,
    secondCurrency = 'USD',
  ) {
    http.expectOne(`/api/shop/products/${productId}`).flush(product);
    http.expectOne(`/api/shop/products/${productId}/prices`).flush({
      at: '2026-09-30T12:00:00Z',
      variants: [
        { variantId, amount, currency },
        { variantId: secondId, amount: secondAmount, currency: secondCurrency },
      ],
    });
  }
  it('opens an empty public cart without requests', async () => {
    const harness = await RouterTestingHarness.create('/winkel/winkelmand');
    expect(harness.routeNativeElement!.textContent).toContain('Je winkelmand is leeg');
    http.expectNone(() => true);
  });
  it('adds the selected variant from the product page and shows confirmation', async () => {
    const harness = await RouterTestingHarness.create(`/winkel/${productId}`);
    reply();
    await harness.fixture.whenStable();
    const button = Array.from(harness.routeNativeElement!.querySelectorAll('button')).find((b) =>
      b.textContent?.includes('In winkelmand'),
    )!;
    button.click();
    await harness.fixture.whenStable();
    expect(TestBed.inject(Cart).lines()).toEqual([line]);
    expect(harness.routeNativeElement!.textContent).toContain('Toegevoegd aan je winkelmand');
    expect(
      harness.routeNativeElement!.querySelector('a[href="/winkel/winkelmand"]'),
    ).not.toBeNull();
  });
  it('cannot add an unpriced variant', async () => {
    const harness = await RouterTestingHarness.create(`/winkel/${productId}`);
    reply(null);
    await harness.fixture.whenStable();
    const button = Array.from(harness.routeNativeElement!.querySelectorAll('button')).find((b) =>
      b.textContent?.includes('In winkelmand'),
    )!;
    expect(button.disabled).toBe(true);
    (harness.routeDebugElement!.componentInstance as ShopDetail).addToCart();
    expect(TestBed.inject(Cart).count()).toBe(0);
  });
  it('groups reads by product and keeps currencies separate, including zero prices', async () => {
    const cart = TestBed.inject(Cart);
    cart.add(productId, variantId);
    cart.add(productId, secondId);
    const harness = await RouterTestingHarness.create('/winkel/winkelmand');
    reply();
    await harness.fixture.whenStable();
    const page = harness.routeDebugElement!.componentInstance as ShopCart;
    expect(page.totals()).toEqual([
      { currency: 'EUR', amount: 12.5 },
      { currency: 'USD', amount: 0 },
    ]);
    expect(page.complete()).toBe(true);
    expect(harness.routeNativeElement!.textContent).toContain('USD0.00');
  });
  it('rechecks quantity changes, cancels old requests and removes old totals on failure', async () => {
    const cart = TestBed.inject(Cart);
    cart.add(productId, variantId);
    const harness = await RouterTestingHarness.create('/winkel/winkelmand');
    reply();
    await harness.fixture.whenStable();
    const page = harness.routeDebugElement!.componentInstance as ShopCart;
    cart.setQuantity(line, 2);
    expect(page.complete()).toBe(false);
    await harness.fixture.whenStable();
    const obsoleteProduct = http.expectOne(`/api/shop/products/${productId}`);
    const obsoletePrice = http.expectOne(`/api/shop/products/${productId}/prices`);
    cart.setQuantity(line, 3);
    await harness.fixture.whenStable();
    expect(obsoleteProduct.cancelled).toBe(true);
    expect(obsoletePrice.cancelled).toBe(true);
    reply(10);
    await harness.fixture.whenStable();
    expect(page.totals()).toEqual([{ currency: 'EUR', amount: 30 }]);
    page.refresh();
    http.expectOne(`/api/shop/products/${productId}`).flush(product);
    http
      .expectOne(`/api/shop/products/${productId}/prices`)
      .flush({}, { status: 500, statusText: 'Error' });
    await harness.fixture.whenStable();
    expect(page.complete()).toBe(false);
    expect(page.totals()).toEqual([]);
    expect(harness.routeNativeElement!.textContent).toContain('kon niet worden gecontroleerd');
    page.refresh();
    reply(15);
    await harness.fixture.whenStable();
    expect(page.totals()).toEqual([{ currency: 'EUR', amount: 45 }]);
    page.remove(line);
    await harness.fixture.whenStable();
    expect(harness.routeNativeElement!.textContent).toContain('Je winkelmand is leeg');
  });
  it('submits quantity edits through the form and removes a deleted variant', async () => {
    TestBed.inject(Cart).add(productId, variantId);
    const harness = await RouterTestingHarness.create('/winkel/winkelmand');
    reply();
    await harness.fixture.whenStable();
    const input = harness.routeNativeElement!.querySelector('input')!;
    input.value = '4';
    harness.routeNativeElement!.querySelector<HTMLButtonElement>('button[type=submit]')!.click();
    await harness.fixture.whenStable();
    http.expectOne(`/api/shop/products/${productId}`).flush({ ...product, variants: [] });
    http
      .expectOne(`/api/shop/products/${productId}/prices`)
      .flush({ at: '2026-09-30T12:00:00Z', variants: [] });
    await harness.fixture.whenStable();
    expect(TestBed.inject(Cart).count()).toBe(4);
    expect((harness.routeDebugElement!.componentInstance as ShopCart).complete()).toBe(false);
    expect(harness.routeNativeElement!.textContent).toContain('niet meer beschikbaar');
    Array.from(harness.routeNativeElement!.querySelectorAll('button'))
      .find((button) => button.textContent?.includes('Verwijderen'))!
      .click();
    await harness.fixture.whenStable();
    expect(harness.routeNativeElement!.textContent).toContain('Je winkelmand is leeg');
  });
  it('hides totals for withdrawn products and permits removal', async () => {
    TestBed.inject(Cart).add(productId, variantId);
    const harness = await RouterTestingHarness.create('/winkel/winkelmand');
    const prices = http.expectOne(`/api/shop/products/${productId}/prices`);
    http
      .expectOne(`/api/shop/products/${productId}`)
      .flush({}, { status: 404, statusText: 'Not Found' });
    expect(prices.cancelled).toBe(true);
    await harness.fixture.whenStable();
    const page = harness.routeDebugElement!.componentInstance as ShopCart;
    expect(page.complete()).toBe(false);
    expect(harness.routeNativeElement!.textContent).toContain('niet meer beschikbaar');
  });
  it.each([null, Number.MAX_SAFE_INTEGER])(
    'does not total missing or unsafe amounts: %s',
    async (amount) => {
      TestBed.inject(Cart).add(productId, variantId);
      const harness = await RouterTestingHarness.create('/winkel/winkelmand');
      reply(amount);
      await harness.fixture.whenStable();
      expect((harness.routeDebugElement!.componentInstance as ShopCart).complete()).toBe(false);
    },
  );
});
