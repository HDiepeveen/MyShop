import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { Subject } from 'rxjs';
import { Auth } from '../auth/auth';
import { OrderApi, OnlinePaymentStart } from './order.api';
import { CustomerAccountApi } from '../customer/customer-account.api';
import { ShopCheckout } from './shop-checkout';

const line = {
  productId: 'p',
  variantId: 'v',
  quantity: 1,
  expectedAmount: '12.50',
  expectedCurrency: 'EUR',
};
describe('Checkout validation', () => {
  function setup() {
    const api = {
      place: vi.fn(() => new Subject()),
      startOnlinePayment: vi.fn(() => new Subject()),
      completeOnlinePayment: vi.fn(() => new Subject()),
    };
    TestBed.configureTestingModule({
      imports: [ShopCheckout],
      providers: [
        { provide: Auth, useValue: { session: signal(null) } },
        { provide: CustomerAccountApi, useValue: {} },
        { provide: OrderApi, useValue: api },
      ],
    });
    const fixture = TestBed.createComponent(ShopCheckout);
    fixture.componentRef.setInput('lines', [line]);
    fixture.componentRef.setInput('paymentMethod', 'payLater');
    fixture.componentRef.setInput('deliveryMethodId', 'd');
    const page = fixture.componentInstance;
    page.customerName = 'Ada';
    page.email = 'ada@example.test';
    page.addressLine = 'Street 1';
    page.postalCode = '1234 AB';
    page.city = 'Utrecht';
    return { fixture, page, api };
  }
  for (const [field, maximum] of [
    ['customerName', 200],
    ['email', 320],
    ['addressLine', 200],
    ['postalCode', 32],
    ['city', 100],
  ] as const) {
    it.each(['blank', 'long'])('rejects ' + field + ' %s without submitting', (invalid) => {
      const { page, api } = setup();
      page[field] = invalid === 'blank' ? '  ' : 'x'.repeat(maximum + 1);
      page.submit();
      expect(page.failure()).toBeTruthy();
      expect(page.busy()).toBe(false);
      expect(api.place).not.toHaveBeenCalled();
      expect(api.startOnlinePayment).not.toHaveBeenCalled();
    });
  }
  it.each(['a', 'NL1', '12', ''])('rejects country code %s', (country) => {
    const { page, api } = setup();
    page.countryCode = country;
    page.submit();
    expect(api.place).not.toHaveBeenCalled();
    expect(page.failure()).toContain('landcode');
  });
  it.each([0, 100, 1.5, NaN])('rejects cart quantity %s', (quantity) => {
    const { fixture, page, api } = setup();
    fixture.componentRef.setInput('lines', [{ ...line, quantity }]);
    page.submit();
    expect(api.place).not.toHaveBeenCalled();
  });
  it.each([0, 21])('rejects cart length %s', (length) => {
    const { fixture, page, api } = setup();
    fixture.componentRef.setInput(
      'lines',
      Array.from({ length }, () => line),
    );
    page.submit();
    expect(api.place).not.toHaveBeenCalled();
  });
  it('rejects missing product or variant identifiers', () => {
    const { fixture, page, api } = setup();
    fixture.componentRef.setInput('lines', [{ ...line, variantId: '' }]);
    page.submit();
    expect(api.place).not.toHaveBeenCalled();
  });
  it.each(['paymentMethod', 'deliveryMethodId'])('rejects missing %s', (input) => {
    const { fixture, page, api } = setup();
    fixture.componentRef.setInput(input, '');
    page.submit();
    expect(api.place).not.toHaveBeenCalled();
    expect(page.busy()).toBe(false);
  });
  it('retains an existing online start after invalid input', () => {
    const { page } = setup();
    const payment = { checkoutToken: 't', providerPaymentId: 'provider' } as OnlinePaymentStart;
    page.onlinePayment.set(payment);
    page.city = '';
    page.submit();
    expect(page.onlinePayment()).toBe(payment);
  });
  it('only completes the currently displayed online payment', () => {
    const { page, api } = setup();
    const payment = { checkoutToken: 't', providerPaymentId: 'provider' } as OnlinePaymentStart;
    page.completeOnlinePayment(payment);
    expect(api.completeOnlinePayment).not.toHaveBeenCalled();
    page.onlinePayment.set(payment);
    page.completeOnlinePayment({ ...payment });
    expect(api.completeOnlinePayment).not.toHaveBeenCalled();
    page.completeOnlinePayment(payment);
    page.completeOnlinePayment(payment);
    expect(api.completeOnlinePayment).toHaveBeenCalledExactlyOnceWith('t', 'provider');
  });
  it('announces checkout activity synchronously and releases it after failure or success', () => {
    const { page, api } = setup();
    const events: boolean[] = [];
    page.busyChanged.subscribe((value) => events.push(value));
    const first = new Subject();
    api.place.mockReturnValue(first);
    page.submit();
    expect(events).toEqual([true]);
    first.error(new Error('Offline'));
    expect(events).toEqual([true, false]);
    const second = new Subject();
    api.place.mockReturnValue(second);
    page.submit();
    second.next({ id: 'o', number: 'MS-1', totals: [] });
    expect(events).toEqual([true, false, true, false]);
    page.city = '';
    page.submit();
    expect(events).toEqual([true, false, true, false]);
  });
  it('locks checkout fields while placing an order and restores them after failure', async () => {
    const { fixture, page, api } = setup();
    const operation = new Subject();
    api.place.mockReturnValue(operation);
    fixture.detectChanges();
    await fixture.whenStable();
    page.submit();
    fixture.detectChanges();
    await fixture.whenStable();
    const inputs = Array.from(
      fixture.nativeElement.querySelectorAll('input'),
    ) as HTMLInputElement[];
    expect(inputs).toHaveLength(6);
    expect(inputs.every((input) => input.disabled)).toBe(true);
    operation.error(new Error('Offline'));
    fixture.detectChanges();
    await fixture.whenStable();
    expect(inputs.every((input) => !input.disabled)).toBe(true);
    expect(page.customerName).toBe('Ada');
  });
  it('permits valid customer details and an online payment choice', () => {
    const { fixture, page, api } = setup();
    fixture.componentRef.setInput('paymentMethod', 'online');
    page.submit();
    expect(api.startOnlinePayment).toHaveBeenCalledOnce();
  });
});
