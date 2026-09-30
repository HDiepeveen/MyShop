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
  totals: [{ currency: 'EUR', amount: '12.50' }],
};

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

    vi.spyOn(TestBed.inject(Router), 'navigate').mockImplementation(async () => {
      query.next(convertToParamMap({ offset: '20' }));
      return true;
    });
    fixture.componentInstance.changePage(20);
    http
      .expectOne((request) => request.params.get('offset') === '20')
      .flush({}, { status: 503, statusText: 'Unavailable' });
    fixture.componentInstance.retry();
    http
      .expectOne((request) => request.params.get('offset') === '20')
      .flush({ items: [], offset: 20, limit: 20, totalCount: 21 });
    expect(fixture.componentInstance.offset()).toBe(20);
  });

  it('shows the complete order snapshot and retries loading', () => {
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

    params.next(convertToParamMap({ id: '22222222-2222-2222-2222-222222222222' }));
    http
      .expectOne('/api/orders/22222222-2222-2222-2222-222222222222')
      .flush({}, { status: 500, statusText: 'Error' });
    fixture.componentInstance.retry();
    http.expectOne('/api/orders/22222222-2222-2222-2222-222222222222');
  });
});
