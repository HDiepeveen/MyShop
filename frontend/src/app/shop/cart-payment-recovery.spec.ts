import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideRouter } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { of, Subject } from 'rxjs';
import { ShopCart } from './shop-cart';
import { Cart } from './cart';
import { ShopApi } from './shop.api';
import { Auth } from '../auth/auth';
import { OrderApi } from '../checkout/order.api';
import { PendingPayment } from '../checkout/pending-payment';
import { PaymentOptionsApi } from '../checkout/payment-options.api';
import { DeliveryMethodsApi } from '../checkout/delivery-methods.api';
const token = '95186104-e167-4ce3-94d9-26f982e5e10a';
const line = { productId: '10000000-0000-0000-0000-000000000001', variantId: '20000000-0000-0000-0000-000000000001', quantity: 2 };
const receipt = { id: line.productId, number: 'MS-PAID', placedAt: '', totals: [], paymentInstructions: null, deliveryMethod: null };
describe('Cart reopening after payment', () => {
  beforeEach(() => localStorage.clear()); afterEach(() => localStorage.clear());
  function setup() {
    const response = new Subject<any>(); const complete = vi.fn(() => response);
    TestBed.configureTestingModule({ providers: [
      provideRouter([]), { provide: Auth, useValue: { session: signal(null) } },
      { provide: ShopApi, useValue: { quote: () => of({ at: '', lines: [], totals: [] }) } },
      { provide: OrderApi, useValue: { completeOnlinePayment: complete } },
      { provide: PaymentOptionsApi, useValue: { publicOptions: () => of({ checkoutEnabled: true, items: [] }) } },
      { provide: DeliveryMethodsApi, useValue: { publicMethods: () => of([]) } },
    ] });
    const cart = TestBed.inject(Cart); cart.addLines([line]);
    const pending = TestBed.inject(PendingPayment); pending.save(token, cart.lines());
    const fixture = TestBed.createComponent(ShopCart); fixture.detectChanges();
    return { fixture, cart, pending, response, complete, page: fixture.componentInstance };
  }
  it('clears the paid basket when returning without the Mollie return page', () => {
    const { cart, pending, response, complete, page } = setup();
    expect(complete).toHaveBeenCalledWith(token); expect(page.checkoutBusy()).toBe(true);
    response.next(receipt); expect(cart.lines()).toEqual([]); expect(pending.token()).toBeNull();
    expect(page.orderReceipt()?.number).toBe('MS-PAID'); expect(page.checkoutBusy()).toBe(false);
  });
  it('preserves changes made while the status request is in flight', () => {
    const { cart, pending, response, page } = setup(); cart.setQuantity(line, 3); response.next(receipt);
    expect(cart.lines()[0].quantity).toBe(3); expect(pending.token()).toBeNull();
    expect(page.orderReceipt()).toBeNull(); expect(page.notice()).toContain('behouden');
  });
  it.each(['paymentFailed', 'paymentCanceled', 'paymentExpired'])('retains the basket and allows a retry for %s', code => {
    const { cart, pending, response, page } = setup();
    response.error(new HttpErrorResponse({ status: 409, error: { code } }));
    expect(cart.lines()).toEqual([line]); expect(pending.token()).toBeNull(); expect(page.unresolvedPayment()).toBeNull();
  });
  it.each(['paymentPending', 'network'])('retains an uncertain payment and prevents another checkout for %s', code => {
    const { fixture, cart, response, pending, page } = setup();
    response.error(new HttpErrorResponse({ status: code === 'network' ? 503 : 409, error: { code } }));
    fixture.detectChanges();
    expect(cart.lines()).toEqual([line]); expect(pending.token()).toBe(token);
    expect(page.unresolvedPayment()).toBe(token); expect(fixture.nativeElement.querySelector('app-shop-checkout')).toBeNull();
    expect(fixture.nativeElement.querySelector('a[href*="checkoutToken"]')).not.toBeNull();
  });
  it('does not clear a newer payment saved from another tab', () => {
    const { cart, pending, response, page } = setup(); const newer = '95186104-e167-4ce3-94d9-26f982e5e10b';
    pending.save(newer, cart.lines()); response.next(receipt);
    expect(cart.lines()).toEqual([line]); expect(pending.token()).toBe(newer); expect(page.unresolvedPayment()).toBe(newer);
  });
  it('does not clear the basket after navigating away during the request', () => {
    const { fixture, cart, pending, response } = setup(); fixture.destroy(); response.next(receipt);
    expect(cart.lines()).toEqual([line]); expect(pending.token()).toBe(token);
  });
});
