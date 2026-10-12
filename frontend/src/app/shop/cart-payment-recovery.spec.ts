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
  it('allows a new basket containing different products while an old payment is open', () => {
    const { cart, response, page, fixture } = setup();
    cart.clear(); cart.add('10000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000002');
    response.error(new HttpErrorResponse({ status: 409, error: { code: 'paymentPending' } }));
    fixture.detectChanges();
    expect(page.unresolvedPayment()).toBe(token); expect(page.blocksCheckout()).toBe(false);
    expect(cart.count()).toBe(1); expect(fixture.nativeElement.textContent).toContain('nieuwe winkelmand afrekenen');
  });
  it('still blocks a repeated payment for the same variant after changing its quantity', () => {
    const { cart, response, page } = setup(); cart.setQuantity(line, 3);
    response.error(new HttpErrorResponse({ status: 409, error: { code: 'paymentPending' } }));
    expect(page.blocksCheckout()).toBe(true); expect(cart.count()).toBe(3);
  });
  it('does not tell an empty basket that it can pay a new order', () => {
    const { cart, response, fixture, page } = setup(); cart.clear();
    response.error(new HttpErrorResponse({ status: 409, error: { code: 'paymentPending' } }));
    fixture.detectChanges();
    expect(page.unresolvedPayment()).toBe(token);
    expect(fixture.nativeElement.textContent).toContain('Je winkelmand is leeg.');
    expect(fixture.nativeElement.textContent).not.toContain('nieuwe winkelmand afrekenen');
    expect(fixture.nativeElement.querySelector('a[href*="checkoutToken"]')).toBeNull();
  });
  it('restores the existing Mollie information and checkout URL after navigation', () => {
    const { response, fixture, page, complete } = setup();
    response.error(new HttpErrorResponse({ status: 409, error: { code: 'paymentPending', checkoutUrl: 'https://www.mollie.com/checkout/existing', payment: {
      providerName: 'Mollie', paymentReference: 'OP-EXISTING', totals: [{ currency: 'EUR', amount: '36.30' }],
      deliveryMethod: { id: line.productId, name: 'Standaardbezorging', description: null, amount: '0.00', currency: 'EUR' },
    } } }));
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Betaalprovider: Mollie');
    expect(fixture.nativeElement.textContent).toContain('OP-EXISTING');
    expect(fixture.nativeElement.textContent).toContain('36,30');
    expect(fixture.nativeElement.querySelector('a[href="https://www.mollie.com/checkout/existing"]')?.textContent).toContain('iDEAL-betaalpagina');
    expect(page.blocksCheckout()).toBe(true); expect(complete).toHaveBeenCalledTimes(1);
  });
  it('does not show old payment details as the details of a changed basket', () => {
    const { response, fixture, cart } = setup(); cart.setQuantity(line, 3);
    response.error(new HttpErrorResponse({ status: 409, error: { code: 'paymentPending', payment: { providerName: 'Mollie', paymentReference: 'OLD' } } }));
    fixture.detectChanges(); expect(fixture.nativeElement.querySelector('[aria-label="Voorbereide online betaling"]')).toBeNull();
  });
  it('rechecks the restored payment without starting a new payment', () => {
    const { response, complete, page, cart } = setup();
    response.error(new HttpErrorResponse({ status: 409, error: { code: 'paymentPending' } }));
    complete.mockReturnValueOnce(of(receipt) as any);
    page.checkPreparedPayment();
    expect(complete).toHaveBeenLastCalledWith(token); expect(complete).toHaveBeenCalledTimes(2);
    expect(cart.lines()).toEqual([]); expect(page.orderReceipt()?.number).toBe('MS-PAID');
  });
});
