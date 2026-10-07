import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { InvoicePage } from './invoice-page';
const seller = {
  name: 'Seller BV',
  addressLine: 'Street 1',
  postalCode: '1234 AB',
  city: 'Utrecht',
  vatId: 'NL123456789B01',
  kvkNumber: '',
  invoicePrefix: 'INV-',
  revision: 'company',
};
const buyer = {
  name: 'Buyer',
  addressLine: 'Street 2',
  postalCode: '2345 BC',
  city: 'Amsterdam',
  countryCode: 'NL',
  vatId: '',
};
const document = {
  id: 'invoice',
  orderId: 'order',
  number: 'INV-2026-000001',
  orderNumber: 'ORDER-1',
  seller,
  buyer,
  issuedAt: '2026-10-07T10:00:00Z',
  supplyDate: '2026-10-07',
  lines: [],
  totals: [],
  net: '100.00',
  vat: '21.00',
  gross: '121.00',
  currency: 'EUR',
  taxStatement: '',
};
describe('Invoice page', () => {
  let http: HttpTestingController;
  afterEach(() => {
    http.verify();
    vi.restoreAllMocks();
  });
  function setup(customer = false) {
    const params = new BehaviorSubject(convertToParamMap({ id: 'order' }));
    TestBed.configureTestingModule({
      imports: [InvoicePage],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: ActivatedRoute,
          useValue: { paramMap: params, snapshot: { data: { customerInvoice: customer } } },
        },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(InvoicePage);
    return { fixture, page: fixture.componentInstance, params };
  }
  it('displays saved invoice values and prints only a loaded document', () => {
    const { page, fixture } = setup();
    const print = vi.spyOn(window, 'print').mockImplementation(() => {});
    page.print();
    expect(print).not.toHaveBeenCalled();
    http.expectOne('/api/orders/order/invoice').flush(document);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('INV-2026-000001');
    expect(fixture.nativeElement.textContent).toContain('Seller BV');
    page.print();
    expect(print).toHaveBeenCalledOnce();
  });
  it('requires review and blocks repeated issuance while pending', () => {
    const { page } = setup();
    http.expectOne('/api/orders/order/invoice').flush({}, { status: 404, statusText: 'Missing' });
    http
      .expectOne('/api/orders/order')
      .flush({
        id: 'order',
        revision: 'revision',
        customer: { name: buyer.name },
        deliveryAddress: buyer,
        placedAt: '2026-10-07T10:00:00Z',
        paidAt: null,
        shippedAt: null,
      });
    page.issue();
    http.expectNone('/api/auth/csrf');
    page.reviewed = true;
    page.issue();
    page.issue();
    http.expectOne('/api/auth/csrf').flush(null);
    const request = http.expectOne('/api/orders/order/invoice');
    expect(request.request.body.taxReviewed).toBe(true);
    expect(request.request.body.orderRevision).toBe('revision');
    request.flush(document);
    expect(page.invoice()!.number).toBe(document.number);
  });
  it('cancels obsolete reads when another order is opened', () => {
    const { page, params } = setup();
    const old = http.expectOne('/api/orders/order/invoice');
    params.next(convertToParamMap({ id: 'other' }));
    expect(old.cancelled).toBe(true);
    http.expectOne('/api/orders/other/invoice').flush({ ...document, orderId: 'other' });
    expect(page.id).toBe('other');
  });
  it('does not expose the admin issuance form to customers', () => {
    const { fixture, page } = setup(true);
    http
      .expectOne('/api/customer/orders/order/invoice')
      .flush({}, { status: 404, statusText: 'Missing' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('nog geen factuur');
    page.issue();
    http.expectNone('/api/auth/csrf');
    http.expectNone('/api/orders/order');
  });
});
