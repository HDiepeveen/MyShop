import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject, Subject } from 'rxjs';
import { OrderDetailComponent } from './order-detail';
import { OrderDetail, OrderManagementApi } from './order-management.api';

const order: OrderDetail = {
  id: 'order-1',
  number: 'MS-1',
  placedAt: '2026-10-01T08:00:00Z',
  customer: { name: 'Ada', email: 'ada@example.test' },
  deliveryAddress: {
    addressLine: 'Straat 1',
    postalCode: '1234 AB',
    city: 'Utrecht',
    countryCode: 'NL',
  },
  paymentMethod: 'payLater',
  paymentInstructions: null,
  status: 'awaitingPayment',
  paidAt: null,
  paymentReference: null,
  shippedAt: null,
  shippingCarrier: null,
  trackingCode: null,
  cancelledAt: null,
  cancellationReason: null,
  refundedAt: null,
  refundReference: null,
  refundReason: null,
  revision: 'revision-1',
  lines: [],
  totals: [],
};

const actions = [
  {
    name: 'payment',
    method: 'markPaid',
    status: 'awaitingPayment',
    run: (component: OrderDetailComponent, value: OrderDetail) =>
      component.markPaid(value, 'bank-1'),
  },
  {
    name: 'shipping',
    method: 'markShipped',
    status: 'paid',
    run: (component: OrderDetailComponent, value: OrderDetail) =>
      component.markShipped(value, 'PostNL', '3S123'),
  },
  {
    name: 'cancellation',
    method: 'cancel',
    status: 'awaitingPayment',
    run: (component: OrderDetailComponent, value: OrderDetail) =>
      component.cancelOrder(value, 'Klant ziet af'),
  },
  {
    name: 'refund',
    method: 'refund',
    status: 'paid',
    run: (component: OrderDetailComponent, value: OrderDetail) =>
      component.refundOrder(value, 'refund-1', 'Dubbele betaling'),
  },
] as const;

describe('Order management recovery', () => {
  function setup(status: OrderDetail['status'] = 'awaitingPayment') {
    const params = new BehaviorSubject(convertToParamMap({ id: order.id }));
    const response = new Subject<OrderDetail>();
    const action = new Subject<void>();
    const api = {
      get: vi.fn(() => response),
      markPaid: vi.fn(() => action),
      markShipped: vi.fn(() => action),
      cancel: vi.fn(() => action),
      refund: vi.fn(() => action),
    };
    TestBed.configureTestingModule({
      imports: [OrderDetailComponent],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { paramMap: params } },
        { provide: OrderManagementApi, useValue: api },
      ],
    });
    const fixture = TestBed.createComponent(OrderDetailComponent);
    const current = { ...order, status };
    response.next(current);
    return { fixture, params, response, action, api, current };
  }

  it('offers copy controls for the stored administrator references', () => {
    const { fixture, current, response } = setup();
    response.next({
      ...current,
      status: 'refunded',
      paidAt: current.placedAt,
      paymentReference: 'BANK-1',
      shippedAt: current.placedAt,
      trackingCode: '3S-1',
      refundedAt: current.placedAt,
      refundReference: 'REFUND-1',
    });
    fixture.detectChanges();
    const controls = Array.from(
      fixture.nativeElement.querySelectorAll('app-copy-text'),
    ) as HTMLElement[];
    expect(controls).toHaveLength(4);
    expect(controls.map((control) => control.textContent).join(' ')).toContain(
      'Terugbetalingskenmerk',
    );
    expect(controls.every((control) => control.classList.contains('print-hide'))).toBe(true);
  });
  it('prints the loaded admin order and blocks stale snapshots or pending writes', () => {
    const { fixture, current } = setup();
    const print = vi.spyOn(window, 'print').mockImplementation(() => {});
    const page = fixture.componentInstance;
    page.printOrder({ ...current });
    page.saving.set(true);
    page.printOrder(current);
    expect(print).not.toHaveBeenCalled();
    page.saving.set(false);
    fixture.detectChanges();
    const button = (
      Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[]
    ).find((button) => button.textContent?.includes('afdrukken'))!;
    button.click();
    expect(print).toHaveBeenCalledOnce();
    expect(fixture.nativeElement.textContent).toContain('MS-1');
    expect(fixture.nativeElement.textContent).toContain('Ada');
    expect(fixture.nativeElement.classList.contains('printable-order')).toBe(true);
    print.mockRestore();
  });
  const invalidInputs = [
    {
      name: 'payment reference',
      status: 'awaitingPayment',
      run: (page: OrderDetailComponent, order: OrderDetail) =>
        page.markPaid(order, 'x'.repeat(101)),
    },
    {
      name: 'carrier',
      status: 'paid',
      run: (page: OrderDetailComponent, order: OrderDetail) =>
        page.markShipped(order, 'x'.repeat(101), 'tracking'),
    },
    {
      name: 'tracking code',
      status: 'paid',
      run: (page: OrderDetailComponent, order: OrderDetail) =>
        page.markShipped(order, 'carrier', 'x'.repeat(101)),
    },
    {
      name: 'cancellation reason',
      status: 'awaitingPayment',
      run: (page: OrderDetailComponent, order: OrderDetail) =>
        page.cancelOrder(order, 'x'.repeat(501)),
    },
    {
      name: 'refund reference',
      status: 'paid',
      run: (page: OrderDetailComponent, order: OrderDetail) =>
        page.refundOrder(order, 'x'.repeat(101), 'reason'),
    },
    {
      name: 'refund reason',
      status: 'paid',
      run: (page: OrderDetailComponent, order: OrderDetail) =>
        page.refundOrder(order, 'reference', 'x'.repeat(501)),
    },
  ] as const;
  for (const input of invalidInputs) {
    it('rejects oversized ' + input.name + ' before sending', () => {
      const { fixture, current, api } = setup(input.status);
      input.run(fixture.componentInstance, current);
      expect(fixture.componentInstance.actionError()).toContain('maximaal');
      expect(fixture.componentInstance.saving()).toBe(false);
      expect(api.markPaid).not.toHaveBeenCalled();
      expect(api.markShipped).not.toHaveBeenCalled();
      expect(api.cancel).not.toHaveBeenCalled();
      expect(api.refund).not.toHaveBeenCalled();
    });
  }
  for (const operation of actions) {
    it(
      'protects input fields during ' + operation.name + ' and restores them after failure',
      () => {
        const { fixture, current, action } = setup(operation.status);
        fixture.detectChanges();
        operation.run(fixture.componentInstance, current);
        fixture.detectChanges();
        const inputs = Array.from(fixture.nativeElement.querySelectorAll('input, textarea')) as (
          HTMLInputElement | HTMLTextAreaElement
        )[];
        expect(inputs.length).toBeGreaterThan(0);
        expect(inputs.every((input) => input.disabled)).toBe(true);
        action.error(new Error('Offline'));
        fixture.detectChanges();
        expect(inputs.every((input) => !input.disabled)).toBe(true);
      },
    );

    it(`ignores late ${operation.name} success after navigating`, () => {
      const { fixture, params, action, api, current } = setup(operation.status);
      operation.run(fixture.componentInstance, current);
      expect(api[operation.method]).toHaveBeenCalledTimes(1);
      const next = new Subject<OrderDetail>();
      api.get.mockReturnValue(next);
      params.next(convertToParamMap({ id: 'order-2' }));
      const other = { ...order, id: 'order-2', number: 'MS-2' };
      next.next(other);
      action.next();
      expect(fixture.componentInstance.state()?.data).toEqual(other);
      expect(fixture.componentInstance.notice()).toBe('');
      expect(fixture.componentInstance.saving()).toBe(false);
      expect(api.get).toHaveBeenCalledTimes(2);
    });

    it(`ignores late ${operation.name} failure after navigating`, () => {
      const { fixture, params, action, api, current } = setup(operation.status);
      operation.run(fixture.componentInstance, current);
      api.get.mockReturnValue(new Subject<OrderDetail>());
      params.next(convertToParamMap({ id: 'order-2' }));
      action.error(new Error('Offline'));
      expect(fixture.componentInstance.actionError()).toBe('');
      expect(fixture.componentInstance.saving()).toBe(false);
    });

    it(`rejects ${operation.name} on stale data or an invalid status`, () => {
      const { fixture, response, api, current } = setup(operation.status);
      operation.run(fixture.componentInstance, { ...current, revision: 'old-revision' });
      const invalid = { ...current, status: 'cancelled' as const };
      response.next(invalid);
      operation.run(fixture.componentInstance, invalid);
      expect(api[operation.method]).not.toHaveBeenCalled();
      expect(fixture.componentInstance.saving()).toBe(false);
    });
  }

  it('clears previous action messages when opening another order', () => {
    const { fixture, params, api } = setup();
    fixture.componentInstance.notice.set('Old notice');
    fixture.componentInstance.actionError.set('Old error');
    api.get.mockReturnValue(new Subject<OrderDetail>());
    params.next(convertToParamMap({ id: 'order-2' }));
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain('Old notice');
    expect(fixture.nativeElement.textContent).not.toContain('Old error');
    expect(fixture.nativeElement.textContent).not.toContain('MS-1');
  });

  it('does not refresh while saving or already loading', () => {
    const { fixture, action, api, current } = setup();
    fixture.componentInstance.markPaid(current, 'bank-1');
    fixture.componentInstance.retry();
    expect(api.get).toHaveBeenCalledTimes(1);
    api.get.mockReturnValue(new Subject<OrderDetail>());
    action.next();
    expect(api.get).toHaveBeenCalledTimes(2);
    fixture.componentInstance.retry();
    expect(api.get).toHaveBeenCalledTimes(2);
  });
});
