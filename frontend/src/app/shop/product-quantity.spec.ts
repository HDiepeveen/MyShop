import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from '../app.routes';
import { ShopDetail } from './shop-detail';
import { Cart } from './cart';
const productId = '10000000-0000-0000-0000-000000000001';
const variantId = '20000000-0000-0000-0000-000000000001';
const secondId = '20000000-0000-0000-0000-000000000002';
const product = {
  id: productId,
  name: 'Shirt',
  description: '',
  categories: [],
  imageUrl: null,
  imageAlt: '',
  variants: [
    { id: variantId, name: 'Small', isAvailable: true },
    { id: secondId, name: 'Large', isAvailable: true },
  ],
};
const prices = {
  at: '2026-10-07T10:00:00Z',
  variants: [
    { variantId, amount: '1.00', currency: 'EUR' },
    { variantId: secondId, amount: '2.00', currency: 'EUR' },
  ],
};

describe('Product quantity selection', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    localStorage.removeItem('myshop.cart.v1');
    TestBed.configureTestingModule({
      providers: [provideRouter(routes), provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => {
    http.verify();
    localStorage.removeItem('myshop.cart.v1');
    vi.restoreAllMocks();
  });
  async function setup(available = true) {
    const harness = await RouterTestingHarness.create('/winkel/' + productId);
    http
      .expectOne('/api/shop/products/' + productId)
      .flush({
        ...product,
        variants: product.variants.map((v) => ({ ...v, isAvailable: available })),
      });
    http.expectOne('/api/shop/products/' + productId + '/prices').flush(prices);
    harness.detectChanges();
    return {
      harness,
      detail: harness.routeDebugElement!.componentInstance as ShopDetail,
      cart: TestBed.inject(Cart),
    };
  }
  it('chooses an amount, adds it in one write and shows the current cart quantity', async () => {
    const { harness, detail, cart } = await setup();
    const input = harness.routeNativeElement!.querySelector(
      'input[name="quantity"]',
    ) as HTMLInputElement;
    input.value = '4';
    input.dispatchEvent(new Event('input'));
    await harness.fixture.whenStable();
    const persist = vi.spyOn(Storage.prototype, 'setItem');
    detail.addToCart();
    harness.detectChanges();
    expect(cart.lines()).toEqual([{ productId, variantId, quantity: 4 }]);
    expect(persist).toHaveBeenCalledOnce();
    expect(detail.cartMessage()).toBe('Toegevoegd aan je winkelmand.');
    expect(detail.cartError()).toBe('');
    expect(harness.routeNativeElement!.textContent).toContain('4 stuks van deze variant');
    expect(new Cart().lines()).toEqual(cart.lines());
  });
  it('increases and decreases within bounds using the visible controls', async () => {
    const { harness, detail } = await setup();
    const minus = harness.routeNativeElement!.querySelector(
      '[aria-label="Aantal verlagen"]',
    ) as HTMLButtonElement;
    const plus = harness.routeNativeElement!.querySelector(
      '[aria-label="Aantal verhogen"]',
    ) as HTMLButtonElement;
    expect(minus.disabled).toBe(true);
    plus.click();
    harness.detectChanges();
    expect(detail.quantity).toBe(2);
    minus.click();
    expect(detail.quantity).toBe(1);
    detail.quantity = 99;
    harness.detectChanges();
    expect(plus.disabled).toBe(true);
    detail.changeQuantity(1);
    expect(detail.quantity).toBe(99);
    detail.changeQuantity(2);
    expect(detail.quantity).toBe(99);
  });
  it.each([null, 0, -1, 100, 1.5, NaN, Infinity])(
    'rejects invalid amount %s without changing the cart',
    async (quantity) => {
      const { detail, cart } = await setup();
      detail.quantity = quantity;
      detail.addToCart();
      expect(cart.lines()).toEqual([]);
      expect(detail.cartMessage()).toBe('');
      expect(detail.cartError()).toContain('heel aantal');
    },
  );
  it('merges with existing quantities and leaves everything intact on an overflow', async () => {
    const { detail, cart } = await setup();
    cart.add(productId, variantId, 98);
    const before = cart.lines();
    const persist = vi.spyOn(Storage.prototype, 'setItem');
    detail.quantity = 2;
    detail.addToCart();
    expect(cart.lines()).toBe(before);
    expect(persist).not.toHaveBeenCalled();
    expect(detail.cartMessage()).toBe('');
    expect(detail.cartError()).toContain('99');
    detail.quantity = 1;
    detail.addToCart();
    expect(cart.count()).toBe(99);
    expect(detail.cartError()).toBe('');
  });
  it('blocks unavailable variants even when the handler is invoked directly', async () => {
    const { detail, cart } = await setup(false);
    detail.quantity = 5;
    detail.addToCart();
    expect(cart.lines()).toEqual([]);
    expect(detail.cartMessage()).toBe('');
    detail.selectVariant(secondId);
    expect(detail.selectedId()).toBe(variantId);
  });
  it('clears success and error feedback when changing variants or amounts', async () => {
    const { detail, cart } = await setup();
    detail.addToCart();
    detail.selectVariant(secondId);
    expect(detail.cartMessage()).toBe('');
    expect(detail.cartQuantity()).toBe(0);
    detail.quantity = 100;
    detail.addToCart();
    expect(detail.cartError()).not.toBe('');
    detail.quantity = 2;
    detail.changeQuantity(1);
    expect(detail.cartError()).toBe('');
    detail.selectVariant('missing');
    expect(detail.selectedId()).toBe(secondId);
    expect(cart.count()).toBe(1);
  });
  it('retains selection and quantity on refreshing the same product', async () => {
    const { harness, detail } = await setup();
    detail.selectVariant(secondId);
    detail.quantity = 7;
    detail.retry();
    http.expectOne('/api/shop/products/' + productId).flush(product);
    harness.detectChanges();
    expect(detail.selectedId()).toBe(secondId);
    expect(detail.quantity).toBe(7);
    detail.refreshPrices();
    http.expectOne('/api/shop/products/' + productId + '/prices').flush(prices);
    harness.detectChanges();
    expect(detail.selectedId()).toBe(secondId);
    expect(detail.quantity).toBe(7);
  });
  it('falls back to an available variant if the selected one becomes unavailable', async () => {
    const { harness, detail } = await setup();
    detail.selectVariant(secondId);
    detail.retry();
    http
      .expectOne('/api/shop/products/' + productId)
      .flush({
        ...product,
        variants: [product.variants[0], { ...product.variants[1], isAvailable: false }],
      });
    harness.detectChanges();
    expect(detail.selectedId()).toBe(variantId);
  });
  it('starts another product at one and clears prior feedback', async () => {
    const { harness, detail } = await setup();
    detail.quantity = 4;
    detail.addToCart();
    const nextId = '10000000-0000-0000-0000-000000000002';
    await harness.navigateByUrl('/winkel/' + nextId);
    http.expectOne('/api/shop/products/' + nextId).flush({ ...product, id: nextId });
    http.expectOne('/api/shop/products/' + nextId + '/prices').flush(prices);
    harness.detectChanges();
    expect(detail.quantity).toBe(1);
    expect(detail.selectedId()).toBe(variantId);
    expect(detail.cartMessage()).toBe('');
    expect(detail.cartError()).toBe('');
  });
  it('does not add while prices are being refreshed or after a price read failure', async () => {
    const { detail, cart } = await setup();
    detail.refreshPrices();
    detail.addToCart();
    expect(cart.count()).toBe(0);
    http
      .expectOne('/api/shop/products/' + productId + '/prices')
      .flush({}, { status: 500, statusText: 'Failed' });
    detail.addToCart();
    expect(cart.count()).toBe(0);
  });
});
