import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { convertToParamMap, provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from '../app.routes';
import { ShopList } from './shop-list';
import { ShopDetail } from './shop-detail';
import { readShopQuery, shopContextQuery } from './shop-query';

const page = {
  items: [{ id: 'p', name: 'Shirt', imageUrl: null, imageAlt: '', prices: [], isAvailable: true }],
  totalCount: 60,
  offset: 20,
  limit: 20,
  at: '2026-10-06T10:00:00Z',
};
describe('Storefront browsing options', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter(routes), provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  async function setup() {
    const harness = await RouterTestingHarness.create(
      '/winkel?search=shirt&categoryId=c&offset=20&sort=nameDesc&availableOnly=true',
    );
    http.expectOne('/api/shop/categories').flush([]);
    const request = http.expectOne((r) => r.url === '/api/shop/products');
    expect(request.request.params.get('sort')).toBe('nameDesc');
    expect(request.request.params.get('availableOnly')).toBe('true');
    request.flush(page);
    harness.detectChanges();
    return { harness, list: harness.routeDebugElement!.componentInstance as ShopList };
  }
  it('renders sorting and stock controls and preserves them in product links', async () => {
    const { harness } = await setup();
    const root = harness.routeNativeElement!;
    expect((root.querySelector('select[name="sort"]') as HTMLSelectElement).value).toBe('nameDesc');
    expect((root.querySelector('input[name="availableOnly"]') as HTMLInputElement).checked).toBe(
      true,
    );
    const link = root.querySelector('a.product') as HTMLAnchorElement;
    expect(link.getAttribute('href')).toContain('sort=nameDesc');
    expect(link.getAttribute('href')).toContain('availableOnly=true');
    expect(link.getAttribute('href')).toContain('offset=20');
  });
  it('retains browsing options when paging, searching and changing category', async () => {
    const { harness, list } = await setup();
    list.searchText = 'draft';
    list.goToPage(40);
    await harness.fixture.whenStable();
    let request = http.expectOne((r) => r.url === '/api/shop/products');
    expect(request.request.params.get('offset')).toBe('40');
    expect(request.request.params.get('search')).toBe('shirt');
    expect(request.request.params.get('sort')).toBe('nameDesc');
    request.flush({ ...page, offset: 40 });
    list.search();
    await harness.fixture.whenStable();
    request = http.expectOne((r) => r.url === '/api/shop/products');
    expect(request.request.params.get('offset')).toBe('0');
    expect(request.request.params.get('search')).toBe('draft');
    expect(request.request.params.get('availableOnly')).toBe('true');
    request.flush({ ...page, offset: 0 });
    list.filterCategory('other');
    await harness.fixture.whenStable();
    request = http.expectOne((r) => r.url === '/api/shop/products');
    expect(request.request.params.get('categoryId')).toBe('other');
    expect(request.request.params.get('sort')).toBe('nameDesc');
    expect(request.request.params.get('availableOnly')).toBe('true');
    request.flush({ ...page, offset: 0 });
  });
  it('resets pagination when options change and removes defaults from the URL', async () => {
    const { harness, list } = await setup();
    list.changeSort('nameAsc');
    await harness.fixture.whenStable();
    let request = http.expectOne((r) => r.url === '/api/shop/products');
    expect(request.request.params.has('sort')).toBe(false);
    expect(request.request.params.get('offset')).toBe('0');
    expect(request.request.params.get('availableOnly')).toBe('true');
    request.flush({ ...page, offset: 0 });
    list.filterAvailability(false);
    await harness.fixture.whenStable();
    request = http.expectOne((r) => r.url === '/api/shop/products');
    expect(request.request.params.has('availableOnly')).toBe(false);
    request.flush({ ...page, offset: 0 });
    expect(TestBed.inject(Router).url).not.toContain('sort=');
    expect(TestBed.inject(Router).url).not.toContain('availableOnly=');
  });
  it('preserves options in the detail back link and category link', async () => {
    const harness = await RouterTestingHarness.create(
      '/winkel/p?search=shirt&offset=20&sort=nameDesc&availableOnly=true',
    );
    http
      .expectOne('/api/shop/products/p')
      .flush({
        id: 'p',
        name: 'Shirt',
        imageUrl: null,
        imageAlt: '',
        description: '',
        categories: [{ id: 'c', name: 'Clothing' }],
        variants: [],
      });
    http.expectOne('/api/shop/products/p/prices').flush({ variants: [], at: page.at });
    harness.detectChanges();
    const detail = harness.routeDebugElement!.componentInstance as ShopDetail;
    expect(detail.contextQuery()).toMatchObject({
      sort: 'nameDesc',
      availableOnly: true,
      offset: 20,
    });
    expect(detail.categoryQuery('c')).toMatchObject({
      categoryId: 'c',
      sort: 'nameDesc',
      availableOnly: true,
      offset: null,
    });
    expect(harness.routeNativeElement!.querySelector('a.back')?.getAttribute('href')).toContain(
      'sort=nameDesc',
    );
  });
  it('normalizes unknown options to the existing browsing defaults', () => {
    const query = readShopQuery(convertToParamMap({ sort: 'price', availableOnly: 'invalid' }));
    expect(query.sort).toBe('nameAsc');
    expect(query.availableOnly).toBe(false);
    expect(shopContextQuery(query)).toEqual({ search: null, categoryId: null, offset: null });
  });
});
