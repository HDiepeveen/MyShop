import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { convertToParamMap, provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from '../app.routes';
import { ShopList } from './shop-list';
import { ShopDetail } from './shop-detail';
import { readShopQuery, shopContextQuery } from './shop-query';

describe('Storefront page navigation', () => {
  let http: HttpTestingController;
  const page = {
    items: [{ id: 'p', name: 'Shirt', imageUrl: null, imageAlt: '', prices: [] }],
    offset: 50,
    limit: 50,
    totalCount: 121,
    at: '',
  };
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter(routes), provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  async function setup() {
    const harness = await RouterTestingHarness.create(
      '/winkel?limit=50&offset=50&search=shirt&categoryId=c&sort=nameDesc&availableOnly=true',
    );
    http.expectOne('/api/shop/categories').flush([{ id: 'c', name: 'Kleding' }]);
    const request = http.expectOne((r) => r.url === '/api/shop/products');
    expect(request.request.params.get('limit')).toBe('50');
    request.flush(page);
    harness.detectChanges();
    return { harness, list: harness.routeDebugElement!.componentInstance as ShopList };
  }
  async function nextRequest(harness: RouterTestingHarness) {
    await harness.fixture.whenStable();
    return http.expectOne((r) => r.url === '/api/shop/products');
  }
  it('normalizes page sizes and aligns offsets to the selected size', () => {
    for (const [limit, offset, expectedLimit, expectedOffset] of [
      ['50', '100', 50, 100],
      ['100', '200', 100, 200],
      ['20', '40', 20, 40],
      ['51', '40', 20, 40],
      ['50', '20', 50, 0],
      ['100', '-100', 100, 0],
      ['50', '2147483650', 50, 0],
      ['50', '1e2', 50, 0],
    ]) {
      const result = readShopQuery(convertToParamMap({ limit, offset }));
      expect(result.limit).toBe(expectedLimit);
      expect(result.offset).toBe(expectedOffset);
    }
    expect(shopContextQuery(readShopQuery(convertToParamMap({})))).not.toHaveProperty('limit');
  });
  it('renders the selected size, result range and product return context', async () => {
    const { harness, list } = await setup();
    expect(list.pageCount()).toBe(3);
    expect(list.lastOffset()).toBe(100);
    expect(harness.routeNativeElement!.textContent).toContain('51–51');
    expect(harness.routeNativeElement!.querySelector('a.product')!.getAttribute('href')).toContain(
      'limit=50',
    );
    expect(
      harness.routeNativeElement!.querySelector('nav[aria-label="Actieve filters"]')!.textContent,
    ).toContain('Kleding');
  });
  it('changes size while preserving applied filters and resetting the page', async () => {
    const { harness, list } = await setup();
    list.searchText = 'unsent';
    list.changePageSize(100);
    const request = await nextRequest(harness);
    expect(request.request.params.get('limit')).toBe('100');
    expect(request.request.params.get('offset')).toBe('0');
    expect(request.request.params.get('search')).toBe('shirt');
    expect(request.request.params.get('sort')).toBe('nameDesc');
    expect(request.request.params.get('availableOnly')).toBe('true');
    request.flush({ ...page, offset: 0, limit: 100 });
    list.changePageSize(20);
    const defaultRequest = await nextRequest(harness);
    expect(defaultRequest.request.params.get('limit')).toBe('20');
    defaultRequest.flush({ ...page, offset: 0, limit: 20 });
    expect(TestBed.inject(Router).url).not.toContain('limit=');
  });
  it('moves to the last page and jumps directly while retaining filters', async () => {
    const { harness, list } = await setup();
    list.goToPage(list.lastOffset());
    let request = await nextRequest(harness);
    expect(request.request.params.get('offset')).toBe('100');
    request.flush({ ...page, offset: 100 });
    list.pageNumber = 1;
    list.jumpToPage();
    request = await nextRequest(harness);
    expect(request.request.params.get('offset')).toBe('0');
    expect(request.request.params.get('limit')).toBe('50');
    expect(request.request.params.get('categoryId')).toBe('c');
    request.flush({ ...page, offset: 0 });
  });
  it('rejects invalid page jumps and page sizes without navigating', async () => {
    const { harness, list } = await setup();
    for (const value of [null, 0, 1.5, 4, NaN, Infinity]) {
      list.pageNumber = value;
      list.jumpToPage();
      expect(list.pageError()).toContain('heel paginanummer');
    }
    for (const offset of [-50, 20, NaN, Infinity, 2147483650, 50]) list.goToPage(offset);
    list.changePageSize(30);
    list.changePageSize(50);
    await harness.fixture.whenStable();
    http.expectNone((r) => r.url === '/api/shop/products');
    expect(TestBed.inject(Router).url).toContain('offset=50');
  });
  it('resets all filters while keeping page size and clearing unsent input', async () => {
    const { harness, list } = await setup();
    list.searchText = 'unsent';
    list.resetFilters();
    const request = await nextRequest(harness);
    expect(request.request.params.keys()).toEqual(['offset', 'limit']);
    request.flush({ ...page, offset: 0 });
    harness.detectChanges();
    expect(list.hasFilters()).toBe(false);
    expect(list.searchText).toBe('');
    expect(TestBed.inject(Router).url).toBe('/winkel?limit=50');
  });
  it('removes an individual category filter without losing the others or page size', async () => {
    const { harness, list } = await setup();
    (
      harness.routeNativeElement!.querySelector(
        '[aria-label="Categoriefilter verwijderen"]',
      ) as HTMLButtonElement
    ).click();
    const request = await nextRequest(harness);
    expect(request.request.params.has('categoryId')).toBe(false);
    expect(request.request.params.get('search')).toBe('shirt');
    expect(request.request.params.get('availableOnly')).toBe('true');
    expect(request.request.params.get('limit')).toBe('50');
    request.flush({ ...page, offset: 0 });
  });
  it('offers recovery for an empty page and navigates to page one', async () => {
    const { harness, list } = await setup();
    list.retry();
    http
      .expectOne((r) => r.url === '/api/shop/products')
      .flush({ ...page, items: [], totalCount: 0 });
    harness.detectChanges();
    const button = Array.from(harness.routeNativeElement!.querySelectorAll('button')).find((b) =>
      b.textContent?.includes('Terug naar de eerste'),
    );
    expect(button).toBeDefined();
    button!.click();
    const request = await nextRequest(harness);
    expect(request.request.params.get('offset')).toBe('0');
    request.flush({ ...page, offset: 0, items: [], totalCount: 0 });
    expect(list.pageCount()).toBe(1);
  });
  it('keeps the applied query on invalid search and clears the error after a valid search', async () => {
    const { harness, list } = await setup();
    list.searchText = 'x'.repeat(201);
    list.search();
    await harness.fixture.whenStable();
    http.expectNone((r) => r.url === '/api/shop/products');
    expect(list.searchError()).toContain('200');
    expect(list.query().search).toBe('shirt');
    list.searchText = ' new ';
    list.search();
    const request = await nextRequest(harness);
    expect(request.request.params.get('search')).toBe('new');
    request.flush({ ...page, offset: 0 });
    expect(list.searchError()).toBe('');
  });
  it('preserves page size in product detail back and category links', async () => {
    const harness = await RouterTestingHarness.create(
      '/winkel/p?limit=100&offset=200&search=shirt',
    );
    http.expectOne('/api/shop/products/p').flush({
      id: 'p',
      name: 'Shirt',
      description: '',
      imageUrl: null,
      imageAlt: '',
      variants: [],
      categories: [{ id: 'c', name: 'Kleding' }],
    });
    http.expectOne('/api/shop/products/p/prices').flush({ variants: [], at: '' });
    harness.detectChanges();
    const detail = harness.routeDebugElement!.componentInstance as ShopDetail;
    expect(detail.contextQuery()).toMatchObject({ limit: 100, offset: 200 });
    expect(detail.categoryQuery('c')).toMatchObject({ limit: 100, offset: null, categoryId: 'c' });
    expect(harness.routeNativeElement!.querySelector('a.back')!.getAttribute('href')).toContain(
      'limit=100',
    );
  });
  it('removes search, stock and sort independently through the filter buttons', async () => {
    const { harness } = await setup();
    for (const [label, parameter] of [
      ['Zoekfilter verwijderen', 'search'],
      ['Voorraadfilter verwijderen', 'availableOnly'],
      ['Standaardsortering herstellen', 'sort'],
    ]) {
      (
        harness.routeNativeElement!.querySelector(
          '[aria-label="' + label + '"]',
        ) as HTMLButtonElement
      ).click();
      const request = await nextRequest(harness);
      expect(request.request.params.has(parameter)).toBe(false);
      expect(request.request.params.get('limit')).toBe('50');
      expect(request.request.params.get('categoryId')).toBe('c');
      request.flush({ ...page, offset: 0 });
      harness.detectChanges();
    }
  });
});
