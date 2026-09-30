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

    fixture.componentInstance.changePage(20);
    http
      .expectOne(
        (request) =>
          request.params.get('offset') === '20' &&
          request.params.get('status') === 'awaitingPayment',
      )
      .flush({}, { status: 503, statusText: 'Unavailable' });
    fixture.componentInstance.retry();
    http
      .expectOne(
        (request) =>
          request.params.get('offset') === '20' &&
          request.params.get('status') === 'awaitingPayment',
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

    fixture.componentInstance.markPaid(fixture.componentInstance.state()!.data!);
    fixture.componentInstance.markPaid(fixture.componentInstance.state()!.data!);
    const update = http.expectOne('/api/orders/' + summary.id + '/status');
    expect(update.request.method).toBe('PUT');
    expect(update.request.body).toEqual({ status: 'paid', revision });
    update.flush({
      status: 'paid',
      paidAt: '2026-09-30T09:00:00Z',
      shippedAt: null,
      revision: 'new-revision',
    });
    http.expectOne('/api/orders/' + summary.id).flush({
      ...summary,
      status: 'paid',
      paidAt: '2026-09-30T09:00:00Z',
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
    expect(fixture.nativeElement.textContent).toContain('De bestelling is als betaald gemarkeerd.');

    fixture.componentInstance.markShipped(fixture.componentInstance.state()!.data!);
    fixture.componentInstance.markShipped(fixture.componentInstance.state()!.data!);
    const shipment = http.expectOne('/api/orders/' + summary.id + '/status');
    expect(shipment.request.method).toBe('PUT');
    expect(shipment.request.body).toEqual({ status: 'shipped', revision: 'new-revision' });
    shipment.flush({
      status: 'shipped',
      paidAt: '2026-09-30T09:00:00Z',
      shippedAt: '2026-09-30T10:00:00Z',
      revision: 'shipped-revision',
    });
    http.expectOne('/api/orders/' + summary.id).flush({
      ...summary,
      status: 'shipped',
      paidAt: '2026-09-30T09:00:00Z',
      shippedAt: '2026-09-30T10:00:00Z',
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
      shippedAt: null,
      cancelledAt: '2026-09-30T10:00:00Z',
      cancellationReason: 'Klant ziet af.',
      revision: 'cancelled-revision',
    });
    http.expectOne('/api/orders/' + summary.id).flush({
      ...summary,
      status: 'cancelled',
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
});
