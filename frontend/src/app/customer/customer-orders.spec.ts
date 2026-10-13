import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { CustomerOrderDetail } from './customer-order-detail';
import { CustomerOrderList } from './customer-order-list';

const id = '11111111-1111-1111-1111-111111111111';
const summary = {
  id,
  number: 'MS-1',
  placedAt: '2026-10-01T08:00:00Z',
  paymentMethod: 'payLater',
  status: 'awaitingPayment',
  shippedAt: null,
  shippingCarrier: null,
  trackingCode: null,
  totals: [{ currency: 'EUR', amount: '25.00' }],
};

describe('Customer orders', () => {
  let http: HttpTestingController;
  afterEach(() => {
    http.verify();
    vi.restoreAllMocks();
  });

  it.each(['2.00', '0.00', null])('separates article totals from delivery costs %s', (deliveryAmount) => {
    TestBed.configureTestingModule({
      imports: [CustomerOrderDetail],
      providers: [
        provideHttpClient(), provideHttpClientTesting(), provideRouter([]),
        { provide: ActivatedRoute, useValue: {
          paramMap: new BehaviorSubject(convertToParamMap({ id })),
          queryParamMap: new BehaviorSubject(convertToParamMap({})),
        } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(CustomerOrderDetail);
    const total = deliveryAmount === '2.00' ? '47.98' : '45.98';
    http.expectOne('/api/customer/orders/' + id).flush({
      ...summary,
      customer: { name: 'Hans', email: 'hans@example.test' },
      deliveryAddress: { addressLine: 'Straat 1', postalCode: '1234 AB', city: 'Utrecht', countryCode: 'NL' },
      totals: [{ currency: 'EUR', amount: total }, { currency: 'USD', amount: '0.30' }],
      deliveryMethod: deliveryAmount === null ? null : {
        id, name: 'Standaardbezorging', amount: deliveryAmount, currency: 'EUR',
      },
      lines: [
        { productId: id, variantId: '1', productName: 'Espresso normal roast', variantName: 'Bonen / 100 gram', quantity: 1, unitAmount: '9.68', currency: 'EUR', totalAmount: '9.68' },
        { productId: id, variantId: '2', productName: 'Espresso Extra Dark Roast', variantName: 'Hele bonen / 250 gram', quantity: 3, unitAmount: '12.10', currency: 'EUR', totalAmount: '36.30' },
        { productId: id, variantId: '3', productName: 'Extra', variantName: 'A', quantity: 1, unitAmount: '0.10', currency: 'USD', totalAmount: '0.10' },
        { productId: id, variantId: '4', productName: 'Extra', variantName: 'B', quantity: 1, unitAmount: '0.20', currency: 'USD', totalAmount: '0.20' },
      ],
    });
    fixture.detectChanges();
    const panels = Array.from(fixture.nativeElement.querySelectorAll('section')) as HTMLElement[];
    const articles = panels.find((panel) => panel.querySelector('h2')?.textContent === 'Artikelen')!;
    const grandTotal = panels.find((panel) => panel.querySelector('h2')?.textContent === 'Totaal bestelling')!;
    expect(articles.textContent).toContain('Subtotaal artikelen EUR 45,98');
    expect(articles.textContent).toContain('Subtotaal artikelen USD 0,30');
    expect(articles.textContent).not.toContain('47,98');
    expect(grandTotal.textContent).toContain('EUR ' + total.replace('.', ','));
    expect(grandTotal.textContent).toContain('USD 0,30');
    expect(grandTotal.textContent).toContain('Inclusief bezorgkosten.');
    expect(panels.indexOf(grandTotal)).toBeGreaterThan(panels.indexOf(articles));
    if (deliveryAmount !== null) {
      const delivery = panels.find((panel) => panel.querySelector('h2')?.textContent === 'Bezorgoptie')!;
      expect(panels.indexOf(grandTotal)).toBe(panels.indexOf(delivery) + 1);
    }
  });

  it('shows only the account order page and requests the next page', () => {
    TestBed.configureTestingModule({
      imports: [CustomerOrderList],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(CustomerOrderList);
    http
      .expectOne(
        (request) =>
          request.url === '/api/customer/orders' &&
          request.params.get('offset') === '0' &&
          request.params.get('limit') === '20',
      )
      .flush({
        items: [{
          ...summary,
          status: 'shipped',
          shippedAt: '2026-10-01T10:00:00Z',
          shippingCarrier: 'PostNL',
          trackingCode: '3S123',
        }],
        offset: 0,
        limit: 20,
        totalCount: 21,
      });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('MS-1');
    expect(fixture.nativeElement.textContent).toContain('Verzonden');
    expect(fixture.nativeElement.textContent).toContain('Later betalen');
    expect(fixture.nativeElement.textContent).toContain('PostNL');
    expect(fixture.nativeElement.textContent).toContain('3S123');
    expect(fixture.nativeElement.textContent).toContain('EUR 25,00');
    const detailLinks = Array.from(
      fixture.nativeElement.querySelectorAll('a'),
    ) as HTMLAnchorElement[];
    expect(
      detailLinks.some(
        (link) => link.textContent?.includes('Details bekijken') && link.href.endsWith('/' + id),
      ),
    ).toBe(true);
    fixture.componentInstance.next();
    http
      .expectOne(
        (request) =>
          request.url === '/api/customer/orders' && request.params.get('offset') === '20',
      )
      .flush({ items: [], offset: 20, limit: 20, totalCount: 21 });
    expect(fixture.componentInstance.offset()).toBe(20);
  });

  it('does not mark an unpaid online order as paid', () => {
    TestBed.configureTestingModule({
      imports: [CustomerOrderList],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(CustomerOrderList);
    http.expectOne((request) => request.url === '/api/customer/orders').flush({
      items: [{ ...summary, paymentMethod: 'online' }],
      offset: 0,
      limit: 20,
      totalCount: 1,
    });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Wacht op betaling');
    expect(fixture.nativeElement.textContent).toContain('Online betalen');
    expect(fixture.nativeElement.textContent).not.toContain('Online betaald');
  });
  it('shows the immutable customer order snapshot and fulfilment data', () => {
    const params = new BehaviorSubject(convertToParamMap({ id }));
    TestBed.configureTestingModule({
      imports: [CustomerOrderDetail],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { paramMap: params, queryParamMap: new BehaviorSubject(convertToParamMap({})) } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(CustomerOrderDetail);
    http.expectOne('/api/customer/orders/' + id).flush({
      ...summary,
      paymentMethod: 'online',
      status: 'shipped',
      customer: { name: 'Ada', email: 'ada@example.test' },
      deliveryAddress: {
        addressLine: 'Straat 1',
        postalCode: '1234 AB',
        city: 'Utrecht',
        countryCode: 'NL',
      },
      paymentInstructions: 'Betaal binnen 14 dagen.',
      paidAt: '2026-10-01T09:00:00Z',
      shippedAt: '2026-10-01T10:00:00Z',
      shippingCarrier: 'PostNL',
      trackingCode: '3S123',
      cancelledAt: null,
      refundedAt: null,
      revision: '22222222-2222-2222-2222-222222222222',
      lines: [
        {
          productId: id,
          variantId: id,
          productName: 'Shirt',
          variantName: 'Blauw',
          quantity: 2,
          unitAmount: '12.50',
          currency: 'EUR',
          totalAmount: '25.00',
        },
      ],
    });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Verzonden');
    expect(fixture.nativeElement.textContent).toContain('Bestelling geplaatst');
    expect(fixture.nativeElement.textContent).toContain('Betaling ontvangen');
    expect(fixture.nativeElement.textContent).toContain('Bestelling verzonden');
    expect(fixture.nativeElement.textContent).toContain('direct online betalen');
    expect(fixture.nativeElement.textContent).toContain('Shirt · Blauw');
    expect(fixture.nativeElement.textContent).toContain('PostNL');
    expect(fixture.nativeElement.textContent).toContain('3S123');
  });

  it('recovers from a load failure when navigating to another order', () => {
    const params = new BehaviorSubject(convertToParamMap({ id }));
    TestBed.configureTestingModule({
      imports: [CustomerOrderDetail],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { paramMap: params, queryParamMap: new BehaviorSubject(convertToParamMap({})) } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(CustomerOrderDetail);
    http.expectOne('/api/customer/orders/' + id).flush(null, {
      status: 503,
      statusText: 'Service Unavailable',
    });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
    const nextId = '22222222-2222-2222-2222-222222222222';
    params.next(convertToParamMap({ id: nextId }));
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Bestelling ophalen');
    http.expectOne('/api/customer/orders/' + nextId).flush({
      ...summary,
      id: nextId,
      number: 'MS-2',
      customer: { name: 'Ada', email: 'ada@example.test' },
      deliveryAddress: { addressLine: 'Straat 1', postalCode: '1234 AB', city: 'Utrecht', countryCode: 'NL' },
      lines: [],
    });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Bestelling MS-2');
    params.next(convertToParamMap({ id }));
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain('Bestelling MS-2');
    expect(fixture.nativeElement.textContent).toContain('Bestelling ophalen');
    http.expectOne('/api/customer/orders/' + id).flush(null, {
      status: 404,
      statusText: 'Not Found',
    });
  });
  it('confirms and cancels an awaiting order with CSRF protection', () => {
    const params = new BehaviorSubject(convertToParamMap({ id }));
    TestBed.configureTestingModule({
      imports: [CustomerOrderDetail],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { paramMap: params, queryParamMap: new BehaviorSubject(convertToParamMap({})) } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(CustomerOrderDetail);
    http.expectOne('/api/customer/orders/' + id).flush({
      ...summary,
      customer: { name: 'Ada', email: 'ada@example.test' },
      deliveryAddress: {
        addressLine: 'Straat 1',
        postalCode: '1234 AB',
        city: 'Utrecht',
        countryCode: 'NL',
      },
      paymentInstructions: null,
      paidAt: null,
      shippedAt: null,
      shippingCarrier: null,
      trackingCode: null,
      cancelledAt: null,
      refundedAt: null,
      revision: '22222222-2222-2222-2222-222222222222',
      lines: [],
    });
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    fixture.componentInstance.cancel(fixture.componentInstance.order()!);
    http.expectOne('/api/auth/csrf').flush(null);
    const cancellation = http.expectOne('/api/customer/orders/' + id + '/cancel');
    expect(cancellation.request.method).toBe('POST');
    expect(cancellation.request.body).toEqual({
      revision: '22222222-2222-2222-2222-222222222222',
    });
    cancellation.flush({
      status: 'cancelled',
      cancelledAt: '2026-10-01T09:00:00Z',
      revision: '33333333-3333-3333-3333-333333333333',
    });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('De bestelling is geannuleerd.');
    expect(fixture.componentInstance.order()?.status).toBe('cancelled');
  });
});
