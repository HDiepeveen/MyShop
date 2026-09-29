import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { ProductList } from './product-list';

describe('ProductList', () => {
  let http: HttpTestingController;
  const filters = new BehaviorSubject(convertToParamMap({}));
  beforeEach(() => {
    filters.next(convertToParamMap({}));
    TestBed.configureTestingModule({
      imports: [ProductList],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ActivatedRoute, useValue: { queryParamMap: filters } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  it('combines route filters with applied name search, paging and retry', () => {
    filters.next(convertToParamMap({ categoryId: 'c', productTypeId: 't' }));
    const fixture = TestBed.createComponent(ProductList);
    const first = http.expectOne((r) => r.url === '/api/products');
    expect(first.request.params.get('categoryId')).toBe('c');
    expect(first.request.params.get('productTypeId')).toBe('t');
    first.flush({ items: [], totalCount: 50 });
    fixture.componentInstance.searchText = 'coat';
    fixture.componentInstance.search();
    http
      .expectOne(
        (r) =>
          r.params.get('search') === 'coat' &&
          r.params.get('categoryId') === 'c' &&
          r.params.get('productTypeId') === 't',
      )
      .flush({ items: [], totalCount: 50 });
    fixture.componentInstance.searchText = 'not submitted';
    fixture.componentInstance.changePage(20);
    const page = http.expectOne((r) => r.params.get('offset') === '20');
    expect(page.request.params.get('search')).toBe('coat');
    expect(page.request.params.get('categoryId')).toBe('c');
    page.flush({}, { status: 503, statusText: 'Unavailable' });
    fixture.componentInstance.retry();
    http.expectOne(page.request.urlWithParams).flush({ items: [], totalCount: 50 });
  });
  it('cancels old results and resets the page when filters change', () => {
    const fixture = TestBed.createComponent(ProductList);
    http.expectOne((r) => r.url === '/api/products').flush({ items: [], totalCount: 50 });
    fixture.componentInstance.changePage(20);
    const previous = http.expectOne((r) => r.params.get('offset') === '20');
    filters.next(convertToParamMap({ categoryId: 'new' }));
    expect(previous.cancelled).toBe(true);
    expect(fixture.componentInstance.offset()).toBe(0);
    http
      .expectOne((r) => r.params.get('categoryId') === 'new' && r.params.get('offset') === '0')
      .flush({ items: [], totalCount: 0 });
    filters.next(convertToParamMap({ categoryId: 'new', unrelated: 'value' }));
    http.expectNone((r) => r.url === '/api/products');
  });
  it('renders filter context and removes only the requested filter', () => {
    filters.next(convertToParamMap({ categoryId: 'c', productTypeId: 't' }));
    const fixture = TestBed.createComponent(ProductList);
    http.expectOne((r) => r.url === '/api/products').flush({ items: [], totalCount: 0 });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('a[href="/categorieen/c"]')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('a[href="/producttypen/t"]')).not.toBeNull();
    expect(fixture.nativeElement.textContent).toContain('hele assortiment');
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture.componentInstance.clearFilter('categoryId');
    expect(navigate).toHaveBeenCalledWith([], {
      relativeTo: TestBed.inject(ActivatedRoute),
      queryParams: { categoryId: null },
      queryParamsHandling: 'merge',
    });
    filters.next(convertToParamMap({ productTypeId: 't' }));
    const remaining = http.expectOne((r) => r.url === '/api/products');
    expect(remaining.request.params.has('categoryId')).toBe(false);
    expect(remaining.request.params.get('productTypeId')).toBe('t');
    remaining.flush({ items: [], totalCount: 0 });
    fixture.componentInstance.clearFilter('productTypeId');
    expect(navigate).toHaveBeenLastCalledWith([], {
      relativeTo: TestBed.inject(ActivatedRoute),
      queryParams: { productTypeId: null },
      queryParamsHandling: 'merge',
    });
  });
  it('keeps the applied search while paging and resets offset for a new search', () => {
    const fixture = TestBed.createComponent(ProductList);
    http.expectOne((r) => r.url === '/api/products').flush({ items: [], totalCount: 50 });
    fixture.componentInstance.searchText = 'coat';
    fixture.componentInstance.search();
    http
      .expectOne((r) => r.params.get('search') === 'coat' && r.params.get('offset') === '0')
      .flush({ items: [], totalCount: 50 });
    fixture.componentInstance.searchText = 'unsubmitted';
    fixture.componentInstance.changePage(20);
    http
      .expectOne((r) => r.params.get('search') === 'coat' && r.params.get('offset') === '20')
      .flush({ items: [], totalCount: 50 });
    fixture.componentInstance.search();
    http
      .expectOne((r) => r.params.get('search') === 'unsubmitted' && r.params.get('offset') === '0')
      .flush({ items: [], totalCount: 0 });
    expect(fixture.componentInstance.offset()).toBe(0);
  });
  it('cancels a stale request when a new search starts', () => {
    const fixture = TestBed.createComponent(ProductList);
    const old = http.expectOne((r) => r.url === '/api/products');
    fixture.componentInstance.searchText = 'new';
    fixture.componentInstance.search();
    expect(old.cancelled).toBe(true);
    http.expectOne((r) => r.params.get('search') === 'new').flush({ items: [], totalCount: 0 });
    expect(fixture.componentInstance.state()?.data?.totalCount).toBe(0);
  });
  it('shows an error and can retry without stale rows', async () => {
    const fixture = TestBed.createComponent(ProductList);
    http
      .expectOne((r) => r.url === '/api/products')
      .flush({}, { status: 503, statusText: 'Unavailable' });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
    fixture.componentInstance.retry();
    expect(fixture.componentInstance.state()?.loading).toBe(true);
    http.expectOne((r) => r.url === '/api/products').flush({ items: [], totalCount: 0 });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Geen producten gevonden');
  });
  it('opens the product when an SKU is found', () => {
    const fixture = TestBed.createComponent(ProductList);
    http.expectOne((r) => r.url === '/api/products');
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture.componentInstance.skuText = ' shirt-1 ';
    fixture.componentInstance.lookupSku();
    http.expectOne('/api/products/by-sku/shirt-1').flush({ productId: 'p', productVariantId: 'v' });
    expect(navigate).toHaveBeenCalledWith(['/producten', 'p']);
    expect(fixture.componentInstance.skuBusy()).toBe(false);
  });
  it('rejects invalid SKU input without a request and accepts a corrected value', () => {
    const fixture = TestBed.createComponent(ProductList);
    http.expectOne((r) => r.url === '/api/products').flush({ items: [], totalCount: 0 });
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    for (const sku of ['A B', 'A\tB', 'A\u0085B', 'A'.repeat(65)]) {
      fixture.componentInstance.skuText = sku;
      fixture.componentInstance.lookupSku();
      expect(fixture.componentInstance.skuError()).toContain('maximaal 64 tekens zonder spaties');
      expect(fixture.componentInstance.skuBusy()).toBe(false);
    }
    http.expectNone((r) => r.url.startsWith('/api/products/by-sku/'));
    fixture.componentInstance.skuText = 'A'.repeat(64);
    fixture.componentInstance.lookupSku();
    expect(fixture.componentInstance.skuError()).toBe('');
    http
      .expectOne('/api/products/by-sku/' + 'A'.repeat(64))
      .flush({ productId: 'p', productVariantId: 'v' });
    expect(navigate).toHaveBeenCalledWith(['/producten', 'p']);
  });
  it('shows an SKU lookup error and prevents duplicate submits', () => {
    const fixture = TestBed.createComponent(ProductList);
    http.expectOne((r) => r.url === '/api/products');
    fixture.componentInstance.skuText = 'DUPLICATE';
    fixture.componentInstance.lookupSku();
    fixture.componentInstance.lookupSku();
    const request = http.expectOne('/api/products/by-sku/DUPLICATE');
    request.flush({ title: 'SKU not found' }, { status: 404, statusText: 'Not Found' });
    expect(fixture.componentInstance.skuBusy()).toBe(false);
    expect(fixture.componentInstance.skuError()).toContain(
      'Geen product gevonden met dit artikelnummer',
    );
    expect(fixture.componentInstance.skuText).toBe('DUPLICATE');
  });
  it('distinguishes an unavailable service from a missing SKU and clears the error on retry', () => {
    const fixture = TestBed.createComponent(ProductList);
    http.expectOne((r) => r.url === '/api/products').flush({ items: [], totalCount: 0 });
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture.componentInstance.skuText = 'SHIRT';
    fixture.componentInstance.lookupSku();
    http
      .expectOne('/api/products/by-sku/SHIRT')
      .flush({}, { status: 502, statusText: 'Bad Gateway' });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain(
      'niet bereikbaar',
    );
    expect(navigate).not.toHaveBeenCalled();
    fixture.componentInstance.lookupSku();
    expect(fixture.componentInstance.skuError()).toBe('');
    http.expectOne('/api/products/by-sku/SHIRT').flush({ productId: 'p', productVariantId: 'v' });
    expect(navigate).toHaveBeenCalledWith(['/producten', 'p']);
  });
});
