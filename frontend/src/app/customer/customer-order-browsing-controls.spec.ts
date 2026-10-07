import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { CustomerOrderList } from './customer-order-list';

describe('Customer order browsing controls', () => {
  let http: HttpTestingController;
  const page = {
    items: [
      {
        id: 'id',
        number: 'ORD-1',
        placedAt: '2026-10-07T10:00:00Z',
        status: 'paid',
        paymentMethod: 'payLater',
        shippedAt: null,
        shippingCarrier: null,
        trackingCode: null,
        totals: [],
      },
    ],
    totalCount: 61,
    offset: 20,
    limit: 20,
  };
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [CustomerOrderList],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: convertToParamMap({ search: 'ORD', status: 'paid', offset: '20' }),
            },
          },
        },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  function setup() {
    const fixture = TestBed.createComponent(CustomerOrderList);
    http.expectOne((r) => r.url === '/api/customer/orders').flush(page);
    fixture.detectChanges();
    return { fixture, list: fixture.componentInstance };
  }
  function request() {
    return http.expectOne((r) => r.url === '/api/customer/orders');
  }
  it('shows total, current page and applied filter values', () => {
    const { fixture, list } = setup();
    expect(fixture.nativeElement.textContent).toContain('61 bestellingen gevonden');
    expect(fixture.nativeElement.textContent.replace(/\s+/g, ' ')).toContain('Pagina 2 van 4');
    const filters = fixture.nativeElement.querySelector('[aria-label="Actieve bestelfilters"]');
    expect(filters.textContent).toContain('Bestelnummer: ORD');
    expect(filters.textContent).toContain('Status: Betaald');
    expect(list.pageCount()).toBe(4);
    expect(list.lastOffset()).toBe(60);
  });
  it('opens first and last pages retaining the applied filters', () => {
    const { list } = setup();
    list.searchText = 'unsent';
    list.last();
    let read = request();
    expect(read.request.params.get('offset')).toBe('60');
    expect(read.request.params.get('search')).toBe('ORD');
    expect(read.request.params.get('status')).toBe('paid');
    read.flush({ ...page, offset: 60 });
    list.last();
    http.expectNone((r) => r.url === '/api/customer/orders');
    list.first();
    read = request();
    expect(read.request.params.get('offset')).toBe('0');
    read.flush({ ...page, offset: 0 });
    list.first();
    http.expectNone((r) => r.url === '/api/customer/orders');
  });
  it('jumps to a valid page with current filters and preserves detail return context', () => {
    const { fixture, list } = setup();
    list.pageNumber = 3;
    list.jumpToPage();
    const read = request();
    expect(read.request.params.get('offset')).toBe('40');
    read.flush({ ...page, offset: 40 });
    fixture.detectChanges();
    expect(list.contextQuery()).toEqual({ search: 'ORD', status: 'paid', offset: 40 });
    expect(fixture.nativeElement.querySelector('h2 a').getAttribute('href')).toContain('offset=40');
    expect(list.pageError()).toBe('');
  });
  it.each([null, 0, -1, 1.5, 5, NaN, Infinity])(
    'rejects invalid page %s without reading',
    (pageNumber) => {
      const { list } = setup();
      list.pageNumber = pageNumber;
      list.jumpToPage();
      expect(list.pageError()).toContain('heel paginanummer');
      expect(list.offset()).toBe(20);
      http.expectNone((r) => r.url === '/api/customer/orders');
      list.pageNumber = 2;
      list.jumpToPage();
      expect(list.pageError()).toBe('');
      http.expectNone((r) => r.url === '/api/customer/orders');
    },
  );
  it('clears only search via the visible active filter button', () => {
    const { fixture, list } = setup();
    list.searchText = 'unsent';
    fixture.nativeElement.querySelector('[aria-label="Zoekfilter verwijderen"]').click();
    const read = request();
    expect(read.request.params.has('search')).toBe(false);
    expect(read.request.params.get('status')).toBe('paid');
    expect(read.request.params.get('offset')).toBe('0');
    read.flush({ ...page, offset: 0 });
    expect(list.searchText).toBe('');
  });
  it('clears only status via the visible active filter button', () => {
    const { fixture } = setup();
    fixture.nativeElement.querySelector('[aria-label="Statusfilter verwijderen"]').click();
    const read = request();
    expect(read.request.params.has('status')).toBe(false);
    expect(read.request.params.get('search')).toBe('ORD');
    read.flush({ ...page, offset: 0 });
  });
  it('refreshes the current page once and blocks other controls during the read', () => {
    const { fixture, list } = setup();
    const button = Array.from(
      fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
    ).find((b) => b.textContent?.includes('Bestellingen vernieuwen'))!;
    button.click();
    const read = request();
    expect(read.request.params.get('offset')).toBe('20');
    fixture.detectChanges();
    expect(button.disabled).toBe(true);
    list.first();
    list.last();
    list.clearSearch();
    list.filterStatus(null);
    list.jumpToPage();
    list.load(20);
    http.expectNone((r) => r.url === '/api/customer/orders');
    read.flush(page);
  });
  it('returns to page one from an empty page and distinguishes empty filtered results', () => {
    const { fixture, list } = setup();
    list.load(20);
    request().flush({ ...page, items: [], totalCount: 0 });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent.replace(/\s+/g, ' ')).not.toContain('Pagina 2 van 1');
    const button = Array.from(
      fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
    ).find((b) => b.textContent?.includes('Terug naar de eerste'))!;
    button.click();
    const read = request();
    expect(read.request.params.get('offset')).toBe('0');
    read.flush({ ...page, offset: 0, items: [], totalCount: 0 });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(
      'Geen bestellingen gevonden met deze filters.',
    );
  });
  it('allows refreshing again after a read failure and ignores invalid offsets', () => {
    const { list } = setup();
    list.load(20);
    request().flush({}, { status: 500, statusText: 'Failed' });
    expect(list.loading()).toBe(false);
    expect(list.failure()).not.toBe('');
    for (const offset of [-20, 1, NaN, 2147483660]) list.load(offset);
    http.expectNone((r) => r.url === '/api/customer/orders');
    list.load(20);
    request().flush(page);
    expect(list.failure()).toBe('');
  });
  it('cancels the refresh request when leaving the page', () => {
    const { fixture, list } = setup();
    list.load(20);
    const read = request();
    fixture.destroy();
    expect(read.cancelled).toBe(true);
  });
});
