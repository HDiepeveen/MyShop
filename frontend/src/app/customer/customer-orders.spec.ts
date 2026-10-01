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
  totals: [{ currency: 'EUR', amount: '25.00' }],
};

describe('Customer orders', () => {
  let http: HttpTestingController;
  afterEach(() => http.verify());

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
      .flush({ items: [summary], offset: 0, limit: 20, totalCount: 21 });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('MS-1');
    expect(fixture.nativeElement.textContent).toContain('Wacht op betaling');
    expect(fixture.nativeElement.textContent).toContain('EUR 25,00');
    fixture.componentInstance.next();
    http
      .expectOne(
        (request) =>
          request.url === '/api/customer/orders' && request.params.get('offset') === '20',
      )
      .flush({ items: [], offset: 20, limit: 20, totalCount: 21 });
    expect(fixture.componentInstance.offset()).toBe(20);
  });

  it('shows the immutable customer order snapshot and fulfilment data', () => {
    const params = new BehaviorSubject(convertToParamMap({ id }));
    TestBed.configureTestingModule({
      imports: [CustomerOrderDetail],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { paramMap: params } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(CustomerOrderDetail);
    http
      .expectOne('/api/customer/orders/' + id)
      .flush({
        ...summary,
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
    expect(fixture.nativeElement.textContent).toContain('Shirt · Blauw');
    expect(fixture.nativeElement.textContent).toContain('PostNL');
    expect(fixture.nativeElement.textContent).toContain('3S123');
  });
});
