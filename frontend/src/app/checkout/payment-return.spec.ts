import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { PaymentReturn } from './payment-return';
import { PendingPayment } from './pending-payment';
import { Cart } from '../shop/cart';

const token = '95186104-e167-4ce3-94d9-26f982e5e10a';
const product = '10000000-0000-0000-0000-000000000001';
const variant = '20000000-0000-0000-0000-000000000001';
const receipt = {
  id: product,
  number: 'MS-PAID',
  placedAt: '',
  paymentInstructions: null,
  totals: [{ currency: 'EUR', amount: '29.95' }],
  deliveryMethod: null,
};
describe('Mollie payment return', () => {
  let http: HttpTestingController;
  let params: BehaviorSubject<ReturnType<typeof convertToParamMap>>;
  beforeEach(() => {
    localStorage.clear();
    params = new BehaviorSubject(convertToParamMap({ checkoutToken: token }));
    TestBed.configureTestingModule({
      imports: [PaymentReturn],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { queryParamMap: params } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => {
    http.verify();
    localStorage.clear();
  });
  function start() {
    const fixture = TestBed.createComponent(PaymentReturn);
    fixture.detectChanges();
    http.expectOne('/api/auth/csrf').flush(null);
    return {
      fixture,
      page: fixture.componentInstance,
      request: http.expectOne('/api/shop/online-payments/' + token + '/complete'),
    };
  }
  it('only clears the matching cart after server-confirmed payment', () => {
    const cart = TestBed.inject(Cart);
    cart.add(product, variant, 2);
    TestBed.inject(PendingPayment).save(token, cart.lines());
    const { page, request, fixture } = start();
    page.check();
    http.expectNone('/api/auth/csrf');
    expect(cart.count()).toBe(2);
    request.flush(receipt);
    fixture.detectChanges();
    expect(cart.count()).toBe(0);
    expect(page.receipt()?.number).toBe('MS-PAID');
    expect(fixture.nativeElement.textContent).toContain('Je betaling is bevestigd.');
    page.check();
    http.expectNone('/api/auth/csrf');
  });
  it('preserves a cart changed after the payment start', () => {
    const cart = TestBed.inject(Cart);
    cart.add(product, variant);
    TestBed.inject(PendingPayment).save(token, cart.lines());
    cart.add(product, variant);
    const { page, request } = start();
    request.flush(receipt);
    expect(cart.count()).toBe(2);
    expect(page.cartNotice()).toContain('behouden');
  });
  it('keeps pending payments retryable without claiming success', () => {
    const cart = TestBed.inject(Cart);
    cart.add(product, variant);
    const { page, request } = start();
    request.flush({ code: 'paymentPending' }, { status: 409, statusText: 'Conflict' });
    expect(page.receipt()).toBeNull();
    expect(page.terminal()).toBe(false);
    expect(cart.count()).toBe(1);
    page.check();
    page.check();
    http.expectOne('/api/auth/csrf').flush(null);
    http.expectOne('/api/shop/online-payments/' + token + '/complete').flush(receipt);
    expect(page.receipt()).not.toBeNull();
  });
  for (const code of ['paymentFailed', 'paymentCanceled', 'paymentExpired'])
    it('preserves cart for ' + code, () => {
      const cart = TestBed.inject(Cart);
      cart.add(product, variant);
      const { page, request } = start();
      request.flush({ code }, { status: 409, statusText: 'Conflict' });
      expect(page.terminal()).toBe(true);
      expect(page.receipt()).toBeNull();
      expect(cart.count()).toBe(1);
      page.check();
      http.expectNone('/api/auth/csrf');
    });
  it('does not query invalid links', () => {
    params.next(convertToParamMap({ checkoutToken: 'invalid' }));
    const fixture = TestBed.createComponent(PaymentReturn);
    fixture.detectChanges();
    expect(fixture.componentInstance.validToken).toBe(false);
    http.expectNone('/api/auth/csrf');
  });
  it('cancels status retrieval when the route changes', () => {
    const { page, request } = start();
    params.next(convertToParamMap({ checkoutToken: 'invalid' }));
    expect(request.cancelled).toBe(true);
    expect(page.busy()).toBe(false);
    expect(page.receipt()).toBeNull();
  });
});
