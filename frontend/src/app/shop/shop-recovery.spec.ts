import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject, Subject, of } from 'rxjs';
import { Auth } from '../auth/auth';
import { CustomerWishlistApi } from '../customer/customer-wishlist.api';
import { CustomerAccountApi, CustomerProfile } from '../customer/customer-account.api';
import { ShopCheckout } from '../checkout/shop-checkout';
import { OrderApi } from '../checkout/order.api';
import { PaymentOptionsApi } from '../checkout/payment-options.api';
import { DeliveryMethodsApi, PublicDeliveryMethod } from '../checkout/delivery-methods.api';
import { ShopDetail } from './shop-detail';
import { ShopCart } from './shop-cart';
import { Cart } from './cart';
import { CartQuote, ShopApi, ShopProduct } from './shop.api';

const product: ShopProduct = { id: 'p1', name: 'Shirt', imageUrl: null, imageAlt: '', description: '', categories: [], variants: [] };

describe('Storefront recovery', () => {
  function detailFixture() {
    const params = new BehaviorSubject(convertToParamMap({ id: 'p1' }));
    const products = new Subject<ShopProduct>();
    const first = new Subject<{ saved: boolean }>();
    const second = new Subject<{ saved: boolean }>();
    const operation = new Subject<void>();
    const wishlist = { state: vi.fn().mockReturnValueOnce(first).mockReturnValue(second), add: vi.fn(() => operation), remove: vi.fn(() => operation) };
    TestBed.configureTestingModule({
      imports: [ShopDetail], providers: [provideRouter([]),
        { provide: ActivatedRoute, useValue: { paramMap: params, queryParamMap: of(convertToParamMap({})) } },
        { provide: Auth, useValue: { session: signal({ customer: true }) } },
        { provide: Cart, useValue: { add: vi.fn(), warning: signal('') } },
        { provide: ShopApi, useValue: { product: vi.fn(() => products), prices: vi.fn(() => of({ at: '', variants: [] })) } },
        { provide: CustomerWishlistApi, useValue: wishlist },
      ],
    });
    const fixture = TestBed.createComponent(ShopDetail);
    products.next(product);
    fixture.detectChanges();
    return { fixture, params, products, first, second, operation, wishlist };
  }

  it('resets wishlist state and messages when moving to another product', () => {
    const { fixture, params, first } = detailFixture();
    first.next({ saved: true });
    fixture.componentInstance.wishlistMessage.set('Old message');
    params.next(convertToParamMap({ id: 'p2' }));
    fixture.detectChanges();
    expect(fixture.componentInstance.wishlistSaved()).toBe(false);
    expect(fixture.componentInstance.wishlistMessage()).toBe('');
  });

  it.each(['success', 'error'])('ignores a late wishlist read %s from another product', (result) => {
    const { fixture, params, products, first, second } = detailFixture();
    params.next(convertToParamMap({ id: 'p2' }));
    products.next({ ...product, id: 'p2' });
    fixture.detectChanges();
    second.next({ saved: false });
    if (result === 'success') first.next({ saved: true });
    else first.error(new Error('Offline'));
    expect(fixture.componentInstance.wishlistSaved()).toBe(false);
    expect(fixture.componentInstance.wishlistMessage()).toBe('');
  });

  it('blocks wishlist changes while reading the state or already updating it', () => {
    const { fixture, first, wishlist } = detailFixture();
    fixture.componentInstance.toggleWishlist('p1');
    expect(wishlist.add).not.toHaveBeenCalled();
    first.next({ saved: false });
    fixture.componentInstance.toggleWishlist('p1');
    fixture.componentInstance.toggleWishlist('p1');
    expect(wishlist.add).toHaveBeenCalledTimes(1);
  });

  it.each(['success', 'error'])('ignores a late wishlist update %s after navigating', (result) => {
    const { fixture, params, products, first, second, operation } = detailFixture();
    first.next({ saved: false });
    fixture.componentInstance.toggleWishlist('p1');
    params.next(convertToParamMap({ id: 'p2' }));
    products.next({ ...product, id: 'p2' });
    fixture.detectChanges();
    second.next({ saved: false });
    if (result === 'success') operation.next();
    else operation.error(new Error('Offline'));
    expect(fixture.componentInstance.wishlistSaved()).toBe(false);
    expect(fixture.componentInstance.wishlistMessage()).toBe('');
    expect(fixture.componentInstance.wishlistBusy()).toBe(false);
  });

  function cartFixture() {
    const quote = new Subject<CartQuote>();
    const payment = new Subject<{ items: { code: string; name: string; instructions: string | null }[] }>();
    const delivery = new Subject<PublicDeliveryMethod[]>();
    const paymentApi = { publicOptions: vi.fn(() => payment) };
    const deliveryApi = { publicMethods: vi.fn(() => delivery) };
    const shopApi = { quote: vi.fn(() => quote) };
    TestBed.configureTestingModule({ imports: [ShopCart], providers: [provideRouter([]),
      { provide: Auth, useValue: { session: signal(null) } },
      { provide: Cart, useValue: { lines: signal([{ productId: 'p1', variantId: 'v1', quantity: 1 }]), warning: signal('') } },
      { provide: ShopApi, useValue: shopApi }, { provide: PaymentOptionsApi, useValue: paymentApi },
      { provide: DeliveryMethodsApi, useValue: deliveryApi },
    ] });
    const fixture = TestBed.createComponent(ShopCart);
    fixture.detectChanges();
    return { fixture, quote, payment, delivery, paymentApi, deliveryApi, shopApi };
  }

  it('retries payment options after failure without duplicate requests', () => {
    const { fixture, payment, paymentApi } = cartFixture();
    payment.error(new Error('Offline'));
    const retry = new Subject<{ items: { code: string; name: string; instructions: string | null }[] }>();
    paymentApi.publicOptions.mockReturnValue(retry);
    fixture.componentInstance.retryPayment();
    fixture.componentInstance.retryPayment();
    expect(paymentApi.publicOptions).toHaveBeenCalledTimes(2);
    retry.next({ items: [{ code: 'payLater', name: 'Later betalen', instructions: null }] });
    fixture.detectChanges();
    expect(fixture.componentInstance.paymentOptions()?.error).toBe('');
    expect(fixture.componentInstance.selectedPayment()).toBe('payLater');
  });

  it('retries delivery options after failure without duplicate requests', () => {
    const { fixture, delivery, deliveryApi } = cartFixture();
    delivery.error(new Error('Offline'));
    const retry = new Subject<PublicDeliveryMethod[]>();
    deliveryApi.publicMethods.mockReturnValue(retry);
    fixture.componentInstance.retryDelivery();
    fixture.componentInstance.retryDelivery();
    expect(deliveryApi.publicMethods).toHaveBeenCalledTimes(2);
    retry.next([{ id: 'delivery-1', name: 'PostNL', description: null, amount: '4.95', currency: 'EUR' }]);
    fixture.detectChanges();
    expect(fixture.componentInstance.deliveryMethods()?.error).toBe('');
    expect(fixture.componentInstance.selectedDelivery()).toBe('delivery-1');
  });

  it('does not restart the quote request while already loading', () => {
    const { fixture, quote, shopApi } = cartFixture();
    fixture.componentInstance.refresh();
    expect(shopApi.quote).toHaveBeenCalledTimes(1);
    quote.next({ at: '', lines: [], totals: [] });
    fixture.componentInstance.refresh();
    expect(shopApi.quote).toHaveBeenCalledTimes(2);
  });

  function checkoutFixture() {
    const response = new Subject<CustomerProfile>();
    const orders = { place: vi.fn(() => new Subject()), startOnlinePayment: vi.fn() };
    TestBed.configureTestingModule({ imports: [ShopCheckout], providers: [
      { provide: Auth, useValue: { session: signal({ customer: true }) } },
      { provide: CustomerAccountApi, useValue: { profile: vi.fn(() => response) } },
      { provide: OrderApi, useValue: orders },
    ] });
    const fixture = TestBed.createComponent(ShopCheckout);
    fixture.componentRef.setInput('lines', [{ productId: 'p', variantId: 'v', quantity: 1, expectedAmount: '10.00', expectedCurrency: 'EUR' }]);
    fixture.componentRef.setInput('paymentMethod', 'payLater');
    fixture.componentRef.setInput('deliveryMethodId', 'delivery-1');
    fixture.detectChanges();
    return { fixture, response };
  }

  const profile: CustomerProfile = { email: 'ada@example.test', name: 'Ada', addressLine: 'Straat 1', postalCode: '1234 AB', city: 'Utrecht', countryCode: 'NL', revision: null };

  it('offers manual entry when the checkout profile cannot be loaded', () => {
    const { fixture, response } = checkoutFixture();
    response.error(new Error('Offline'));
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Vul je gegevens zelf in.');
    expect(fixture.componentInstance.busy()).toBe(false);
  });

  it.each(['email', 'countryCode'] as const)('preserves an entered %s when a profile arrives late', (field) => {
    const { fixture, response } = checkoutFixture();
    fixture.componentInstance[field] = field === 'email' ? 'other@example.test' : 'BE';
    const entered = fixture.componentInstance[field];
    response.next(profile);
    expect(fixture.componentInstance[field]).toBe(entered);
    expect(fixture.componentInstance.customerName).toBe('');
  });

  it.each(['success', 'error'])('ignores a late profile %s while an order is being placed', (result) => {
    const { fixture, response } = checkoutFixture();
    fixture.componentInstance.customerName = 'Entered'; fixture.componentInstance.email = 'entered@example.test';
    fixture.componentInstance.addressLine = 'Street 1'; fixture.componentInstance.postalCode = '1234 AB'; fixture.componentInstance.city = 'Utrecht';
    fixture.componentInstance.submit();
    if (result === 'success') response.next(profile);
    else response.error(new Error('Offline'));
    expect(fixture.componentInstance.customerName).toBe('Entered');
    expect(fixture.componentInstance.email).toBe('entered@example.test');
    expect(fixture.componentInstance.busy()).toBe(true);
    expect(fixture.componentInstance.notice()).toBe('');
  });
});
