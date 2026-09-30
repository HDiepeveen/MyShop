import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { OrderDetailComponent } from './order-detail';
import { OrderList } from './order-list';

const summary = {
  id: '11111111-1111-1111-1111-111111111111',
  number: 'MS-1',
  placedAt: '2026-09-30T08:00:00Z',
  customerName: 'Ada Lovelace',
  paymentMethod: 'payLater',
  status: 'awaitingPayment',
  paidAt: null,
  shippedAt: null,
  cancelledAt: null,
  cancellationReason: null,
  refundedAt: null,
  refundReason: null,
  totals: [{ currency: 'EUR', amount: '12.50' }],
};
const revision = '33333333-3333-3333-3333-333333333333';

describe('Order management', () => {
  let http: HttpTestingController;
  afterEach(() => http.verify());

  it('lists orders, pages and retries a failed request', () => {
    const query = new BehaviorSubject(convertToParamMap({}));
    TestBed.configureTestingModule({
      imports: [OrderList],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { queryParamMap: query } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(OrderList);
    http
      .expectOne(
        (request) =>
          request.url === '/api/orders' &&
          request.params.get('offset') === '0' &&
          request.params.get('limit') === '20',
      )
      .flush({ items: [summary], offset: 0, limit: 20, totalCount: 21 });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Ada Lovelace');
    expect(fixture.nativeElement.textContent).toContain('Wacht op betaling');

    vi.spyOn(TestBed.inject(Router), 'navigate').mockImplementation(async (_commands, options) => {
      const next: Record<string, string> = {};
      for (const key of query.value.keys) {
        const value = query.value.get(key);
        if (value !== null) next[key] = value;
      }
      for (const [key, value] of Object.entries(options?.queryParams ?? {})) {
        if (value === null || value === undefined) delete next[key];
        else next[key] = String(value);
      }
      query.next(convertToParamMap(next));
      return true;
    });

    fixture.componentInstance.filterStatus('awaitingPayment');
    http
      .expectOne(
        (request) =>
          request.params.get('offset') === '0' &&
          request.params.get('status') === 'awaitingPayment',
      )
      .flush({ items: [summary], offset: 0, limit: 20, totalCount: 21 });
    expect(fixture.componentInstance.status()).toBe('awaitingPayment');

    fixture.componentInstance.searchText = '  Ada  ';
    fixture.componentInstance.applySearch();
    http
      .expectOne(
        (request) =>
          request.params.get('offset') === '0' &&
          request.params.get('status') === 'awaitingPayment' &&
          request.params.get('search') === 'Ada',
      )
      .flush({ items: [summary], offset: 0, limit: 20, totalCount: 21 });
    expect(fixture.componentInstance.listQuery().search).toBe('Ada');

    fixture.componentInstance.changePage(20);
    http
      .expectOne(
        (request) =>
          request.params.get('offset') === '20' &&
          request.params.get('status') === 'awaitingPayment' &&
          request.params.get('search') === 'Ada',
      )
      .flush({}, { status: 503, statusText: 'Unavailable' });
    fixture.componentInstance.retry();
    http
      .expectOne(
        (request) =>
          request.params.get('offset') === '20' &&
          request.params.get('status') === 'awaitingPayment' &&
          request.params.get('search') === 'Ada',
      )
      .flush({ items: [], offset: 20, limit: 20, totalCount: 21 });
    expect(fixture.componentInstance.offset()).toBe(20);
  });

  it('shows the snapshot, marks an order paid and retries loading', () => {
    const params = new BehaviorSubject(convertToParamMap({ id: summary.id }));
    TestBed.configureTestingModule({
      imports: [OrderDetailComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { paramMap: params } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(OrderDetailComponent);
    const request = http.expectOne('/api/orders/' + summary.id);
    request.flush({
      ...summary,
      paymentReference: null,
      paymentInstructions: 'Betaal binnen 14 dagen.',
      revision,
      customer: { name: 'Ada Lovelace', email: 'ada@example.test' },
      deliveryAddress: {
        addressLine: 'Straat 1',
        postalCode: '1234 AB',
        city: 'Utrecht',
        countryCode: 'NL',
      },
      lines: [
        {
          productId: 'p',
          variantId: 'v',
          productName: 'Shirt',
          variantName: 'Blauw',
          quantity: 2,
          unitAmount: '6.25',
          currency: 'EUR',
          totalAmount: '12.50',
        },
      ],
    });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('ada@example.test');
    expect(fixture.nativeElement.textContent).toContain('Straat 1');
    expect(fixture.nativeElement.textContent).toContain('Shirt');
    expect(fixture.nativeElement.textContent).toContain('Later betalen');
    expect(fixture.nativeElement.textContent).toContain('Betaal binnen 14 dagen.');

    fixture.componentInstance.markPaid(
      fixture.componentInstance.state()!.data!,
      '  bankafschrift 12345  ',
    );
    fixture.componentInstance.markPaid(fixture.componentInstance.state()!.data!, 'Tweede poging');
    const update = http.expectOne('/api/orders/' + summary.id + '/status');
    expect(update.request.method).toBe('PUT');
    expect(update.request.body).toEqual({
      status: 'paid',
      revision,
      paymentReference: 'bankafschrift 12345',
    });
    update.flush({
      status: 'paid',
      paidAt: '2026-09-30T09:00:00Z',
      paymentReference: 'bankafschrift 12345',
      shippedAt: null,
      revision: 'new-revision',
    });
    http.expectOne('/api/orders/' + summary.id).flush({
      ...summary,
      status: 'paid',
      paidAt: '2026-09-30T09:00:00Z',
      paymentReference: 'bankafschrift 12345',
      paymentInstructions: 'Betaal binnen 14 dagen.',
      shippedAt: null,
      revision: 'new-revision',
      customer: { name: 'Ada Lovelace', email: 'ada@example.test' },
      deliveryAddress: {
        addressLine: 'Straat 1',
        postalCode: '1234 AB',
        city: 'Utrecht',
        countryCode: 'NL',
      },
      lines: [],
    });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Betaald');
    expect(fixture.nativeElement.textContent).toContain('bankafschrift 12345');
    expect(fixture.nativeElement.textContent).toContain('De bestelling is als betaald gemarkeerd.');

    fixture.componentInstance.markShipped(
      fixture.componentInstance.state()!.data!,
      '  PostNL  ',
      '  3SMYSHOP123  ',
    );
    fixture.componentInstance.markShipped(
      fixture.componentInstance.state()!.data!,
      'DHL',
      'Tweede poging',
    );
    const shipment = http.expectOne('/api/orders/' + summary.id + '/status');
    expect(shipment.request.method).toBe('PUT');
    expect(shipment.request.body).toEqual({
      status: 'shipped',
      revision: 'new-revision',
      carrier: 'PostNL',
      trackingCode: '3SMYSHOP123',
    });
    shipment.flush({
      status: 'shipped',
      paidAt: '2026-09-30T09:00:00Z',
      paymentReference: 'bankafschrift 12345',
      shippedAt: '2026-09-30T10:00:00Z',
      shippingCarrier: 'PostNL',
      trackingCode: '3SMYSHOP123',
      revision: 'shipped-revision',
    });
    http.expectOne('/api/orders/' + summary.id).flush({
      ...summary,
      status: 'shipped',
      paidAt: '2026-09-30T09:00:00Z',
      paymentReference: 'bankafschrift 12345',
      paymentInstructions: 'Betaal binnen 14 dagen.',
      shippedAt: '2026-09-30T10:00:00Z',
      shippingCarrier: 'PostNL',
      trackingCode: '3SMYSHOP123',
      revision: 'shipped-revision',
      customer: { name: 'Ada Lovelace', email: 'ada@example.test' },
      deliveryAddress: {
        addressLine: 'Straat 1',
        postalCode: '1234 AB',
        city: 'Utrecht',
        countryCode: 'NL',
      },
      lines: [],
    });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Verzonden');
    expect(fixture.nativeElement.textContent).toContain('PostNL');
    expect(fixture.nativeElement.textContent).toContain('3SMYSHOP123');
    expect(fixture.nativeElement.textContent).toContain(
      'De bestelling is als verzonden gemarkeerd.',
    );

    params.next(convertToParamMap({ id: '22222222-2222-2222-2222-222222222222' }));
    http
      .expectOne('/api/orders/22222222-2222-2222-2222-222222222222')
      .flush({}, { status: 500, statusText: 'Error' });
    fixture.componentInstance.retry();
    http.expectOne('/api/orders/22222222-2222-2222-2222-222222222222');
  });

  it('cancels an awaiting order with a trimmed reason', () => {
    const params = new BehaviorSubject(convertToParamMap({ id: summary.id }));
    TestBed.configureTestingModule({
      imports: [OrderDetailComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { paramMap: params } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(OrderDetailComponent);
    http.expectOne('/api/orders/' + summary.id).flush({
      ...summary,
      paymentReference: null,
      paymentInstructions: null,
      revision,
      customer: { name: 'Ada Lovelace', email: 'ada@example.test' },
      deliveryAddress: {
        addressLine: 'Straat 1',
        postalCode: '1234 AB',
        city: 'Utrecht',
        countryCode: 'NL',
      },
      lines: [],
    });

    fixture.componentInstance.cancelOrder(
      fixture.componentInstance.state()!.data!,
      '  Klant ziet af.  ',
    );
    fixture.componentInstance.cancelOrder(
      fixture.componentInstance.state()!.data!,
      'Tweede poging',
    );
    const update = http.expectOne('/api/orders/' + summary.id + '/status');
    expect(update.request.body).toEqual({
      status: 'cancelled',
      revision,
      reason: 'Klant ziet af.',
    });
    update.flush({
      status: 'cancelled',
      paidAt: null,
      paymentReference: null,
      shippedAt: null,
      cancelledAt: '2026-09-30T10:00:00Z',
      cancellationReason: 'Klant ziet af.',
      revision: 'cancelled-revision',
    });
    http.expectOne('/api/orders/' + summary.id).flush({
      ...summary,
      status: 'cancelled',
      paymentReference: null,
      paymentInstructions: null,
      cancelledAt: '2026-09-30T10:00:00Z',
      cancellationReason: 'Klant ziet af.',
      revision: 'cancelled-revision',
      customer: { name: 'Ada Lovelace', email: 'ada@example.test' },
      deliveryAddress: {
        addressLine: 'Straat 1',
        postalCode: '1234 AB',
        city: 'Utrecht',
        countryCode: 'NL',
      },
      lines: [],
    });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Geannuleerd');
    expect(fixture.nativeElement.textContent).toContain('Klant ziet af.');
    expect(fixture.nativeElement.textContent).toContain('De bestelling is geannuleerd.');
  });

  it('registers a refund with a trimmed reference and reason', () => {
    const params = new BehaviorSubject(convertToParamMap({ id: summary.id }));
    TestBed.configureTestingModule({
      imports: [OrderDetailComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { paramMap: params } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(OrderDetailComponent);
    http.expectOne('/api/orders/' + summary.id).flush({
      ...summary,
      status: 'paid',
      paidAt: '2026-09-30T09:00:00Z',
      paymentReference: 'bankafschrift 12345',
      paymentInstructions: null,
      refundReference: null,
      revision,
      customer: { name: 'Ada Lovelace', email: 'ada@example.test' },
      deliveryAddress: {
        addressLine: 'Straat 1',
        postalCode: '1234 AB',
        city: 'Utrecht',
        countryCode: 'NL',
      },
      lines: [],
    });

    fixture.componentInstance.refundOrder(
      fixture.componentInstance.state()!.data!,
      '  bankafschrift 67890  ',
      '  Dubbele betaling.  ',
    );
    fixture.componentInstance.refundOrder(
      fixture.componentInstance.state()!.data!,
      'Tweede poging',
      'Tweede reden',
    );
    const update = http.expectOne('/api/orders/' + summary.id + '/status');
    expect(update.request.body).toEqual({
      status: 'refunded',
      revision,
      refundReference: 'bankafschrift 67890',
      reason: 'Dubbele betaling.',
    });
    update.flush({
      status: 'refunded',
      paidAt: '2026-09-30T09:00:00Z',
      paymentReference: 'bankafschrift 12345',
      refundedAt: '2026-09-30T10:00:00Z',
      refundReference: 'bankafschrift 67890',
      refundReason: 'Dubbele betaling.',
      revision: 'refunded-revision',
    });
    http.expectOne('/api/orders/' + summary.id).flush({
      ...summary,
      status: 'refunded',
      paidAt: '2026-09-30T09:00:00Z',
      paymentReference: 'bankafschrift 12345',
      paymentInstructions: null,
      refundedAt: '2026-09-30T10:00:00Z',
      refundReference: 'bankafschrift 67890',
      refundReason: 'Dubbele betaling.',
      revision: 'refunded-revision',
      customer: { name: 'Ada Lovelace', email: 'ada@example.test' },
      deliveryAddress: {
        addressLine: 'Straat 1',
        postalCode: '1234 AB',
        city: 'Utrecht',
        countryCode: 'NL',
      },
      lines: [],
    });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Terugbetaald');
    expect(fixture.nativeElement.textContent).toContain('bankafschrift 67890');
    expect(fixture.nativeElement.textContent).toContain('Dubbele betaling.');
    expect(fixture.nativeElement.textContent).toContain('De terugbetaling is geregistreerd.');
  });
});
