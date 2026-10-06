import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { CustomerOrderList } from './customer-order-list';
import { CustomerOrderDetail } from './customer-order-detail';
import { readCustomerOrderQuery } from './customer-order-query';

const item = {
  id: 'o',
  number: 'MS-123',
  placedAt: '2026-10-06T10:00:00Z',
  paymentMethod: 'payLater',
  status: 'shipped',
  shippedAt: null,
  shippingCarrier: null,
  trackingCode: null,
  totals: [],
};
describe('Customer order filters', () => {
  let http: HttpTestingController;
  function setup(parameters: Record<string, string> = {}) {
    const query = convertToParamMap(parameters);
    TestBed.configureTestingModule({
      imports: [CustomerOrderList, CustomerOrderDetail],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { queryParamMap: query },
            queryParamMap: new BehaviorSubject(query),
            paramMap: new BehaviorSubject(convertToParamMap({ id: 'o' })),
          },
        },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(CustomerOrderList);
    const request = http.expectOne((r) => r.url === '/api/customer/orders');
    return { fixture, page: fixture.componentInstance, request };
  }
  afterEach(() => http.verify());
  it('restores status, submitted search and page from a detail back link', () => {
    const { fixture, page, request } = setup({
      status: 'shipped',
      search: ' MS-123 ',
      offset: '20',
    });
    expect(request.request.params.get('status')).toBe('shipped');
    expect(request.request.params.get('search')).toBe('MS-123');
    expect(request.request.params.get('offset')).toBe('20');
    request.flush({ items: [item], totalCount: 41, offset: 20, limit: 20 });
    fixture.detectChanges();
    expect(page.searchText).toBe('MS-123');
    expect(page.contextQuery()).toEqual({ search: 'MS-123', status: 'shipped', offset: 20 });
    const link = fixture.nativeElement.querySelector('article a') as HTMLAnchorElement;
    expect(link.getAttribute('href')).toContain('search=MS-123');
    expect(link.getAttribute('href')).toContain('status=shipped');
    expect(link.getAttribute('href')).toContain('offset=20');
    page.searchText = 'draft';
    page.next();
    const next = http.expectOne((r) => r.url === '/api/customer/orders');
    expect(next.request.params.get('offset')).toBe('40');
    expect(next.request.params.get('search')).toBe('MS-123');
    next.flush({ items: [], totalCount: 41, offset: 40, limit: 20 });
  });
  it('combines new search with status and resets the page', () => {
    const { page, request } = setup({ status: 'paid', offset: '20' });
    request.flush({ items: [], totalCount: 0, offset: 20, limit: 20 });
    page.searchText = ' MS-45 ';
    page.applySearch();
    const search = http.expectOne((r) => r.url === '/api/customer/orders');
    expect(search.request.params.get('search')).toBe('MS-45');
    expect(search.request.params.get('status')).toBe('paid');
    expect(search.request.params.get('offset')).toBe('0');
    search.flush({ items: [], totalCount: 0, offset: 0, limit: 20 });
    page.filterStatus('cancelled');
    const filtered = http.expectOne((r) => r.url === '/api/customer/orders');
    expect(filtered.request.params.get('status')).toBe('cancelled');
    expect(filtered.request.params.get('search')).toBe('MS-45');
    filtered.flush({ items: [], totalCount: 0, offset: 0, limit: 20 });
    page.clearFilters();
    const cleared = http.expectOne((r) => r.url === '/api/customer/orders');
    expect(cleared.request.params.has('status')).toBe(false);
    expect(cleared.request.params.has('search')).toBe(false);
    cleared.flush({ items: [], totalCount: 0, offset: 0, limit: 20 });
  });
  it('keeps filters during retry and distinguishes filtered empty results', () => {
    const { fixture, page, request } = setup({ status: 'refunded', search: 'MS-0' });
    request.flush({}, { status: 503, statusText: 'Offline' });
    page.load(page.offset());
    const retry = http.expectOne((r) => r.url === '/api/customer/orders');
    expect(retry.request.params.get('status')).toBe('refunded');
    expect(retry.request.params.get('search')).toBe('MS-0');
    retry.flush({ items: [], totalCount: 0, offset: 0, limit: 20 });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(
      'Geen bestellingen gevonden met deze filters',
    );
    expect(fixture.nativeElement.textContent).not.toContain('nog geen bestellingen geplaatst');
  });
  it('preserves filter state while loading and rejects oversized search', () => {
    const { page, request } = setup({ search: 'MS-1' });
    page.filterStatus('paid');
    page.clearFilters();
    page.searchText = 'draft';
    page.applySearch();
    http.expectNone((r) => r.url === '/api/customer/orders');
    expect(page.search).toBe('MS-1');
    expect(page.statusFilter()).toBeNull();
    request.flush({ items: [], totalCount: 0, offset: 0, limit: 20 });
    page.searchText = 'x'.repeat(201);
    page.applySearch();
    http.expectNone((r) => r.url === '/api/customer/orders');
    expect(page.search).toBe('MS-1');
    expect(page.failure()).toContain('200');
  });
  it('retains query context in the customer detail back link', () => {
    const { request, fixture } = setup({ status: 'shipped', search: 'MS-1', offset: '20' });
    request.flush({ items: [], totalCount: 0, offset: 20, limit: 20 });
    fixture.destroy();
    const detail = TestBed.createComponent(CustomerOrderDetail);
    http.expectOne('/api/customer/orders/o').flush({}, { status: 404, statusText: 'Missing' });
    detail.detectChanges();
    expect(detail.componentInstance.listQuery()).toEqual({
      status: 'shipped',
      search: 'MS-1',
      offset: 20,
    });
    expect(detail.nativeElement.querySelector('a').getAttribute('href')).toContain(
      'status=shipped',
    );
  });
  it('normalizes unknown status and paging parameters', () => {
    const { request } = setup();
    request.flush({ items: [], totalCount: 0, offset: 0, limit: 20 });
    expect(readCustomerOrderQuery(convertToParamMap({ status: 'unknown', offset: '-20' }))).toEqual(
      { status: null, search: '', offset: 0 },
    );
  });
});
