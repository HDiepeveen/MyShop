import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { convertToParamMap, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { ProductList } from './product-list';
import { ProductDetail } from './product-detail';
import { ProductCreate } from './product-create';
import { readProductListQuery } from './product-list-query';
const item = { id: 'p', name: 'Shirt', productTypeId: 't', variantCount: 1, isPublished: false };
describe('Product page size choices', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: 'producten', component: ProductList },
          { path: 'producten/nieuw', component: ProductCreate },
          { path: 'producten/:id', component: ProductDetail },
        ]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  async function setup(limit = 50) {
    const harness = await RouterTestingHarness.create(
      '/producten?limit=' +
        limit +
        '&offset=' +
        limit +
        '&stock=untracked&published=false&search=shirt&categoryId=c&productTypeId=t',
    );
    const request = http.expectOne((r) => r.url === '/api/products');
    request.flush({ items: [item], totalCount: 201, offset: limit, limit });
    harness.detectChanges();
    return {
      harness,
      list: harness.routeDebugElement!.componentInstance as ProductList,
      initial: request.request,
    };
  }
  it.each([20, 50, 100])('restores page size %s and shows a whole page number', async (limit) => {
    const { harness, list, initial } = await setup(limit);
    expect(initial.params.get('limit')).toBe(String(limit));
    expect(initial.params.get('offset')).toBe(String(limit));
    expect(list.pageSize()).toBe(limit);
    expect(harness.routeNativeElement!.textContent).toContain('Pagina 2');
    const select = harness.routeNativeElement!.querySelector(
      'select[name="pageSize"]',
    ) as HTMLSelectElement;
    expect(select.selectedOptions[0].textContent?.trim()).toBe(String(limit));
  });
  it.each([20, 50, 100])('pages in steps of %s and returns to page one', async (limit) => {
    const { harness, list } = await setup(limit);
    list.changePage(list.pageSize());
    await harness.fixture.whenStable();
    let request = http.expectOne((r) => r.url === '/api/products');
    expect(request.request.params.get('offset')).toBe(String(limit * 2));
    expect(request.request.params.get('limit')).toBe(String(limit));
    expect(request.request.params.get('stock')).toBe('untracked');
    request.flush({ items: [], totalCount: 201, offset: limit * 2, limit });
    list.changePage(-list.offset());
    await harness.fixture.whenStable();
    request = http.expectOne((r) => r.url === '/api/products');
    expect(request.request.params.get('offset')).toBe('0');
    expect(request.request.params.get('limit')).toBe(String(limit));
    request.flush({ items: [item], totalCount: 201, offset: 0, limit });
  });
  it('changes page size at page one without dropping other filters', async () => {
    const { harness, list } = await setup();
    list.changePageSize(100);
    await harness.fixture.whenStable();
    let request = http.expectOne((r) => r.url === '/api/products');
    expect(request.request.params.get('limit')).toBe('100');
    expect(request.request.params.get('offset')).toBe('0');
    expect(request.request.params.get('published')).toBe('false');
    expect(request.request.params.get('categoryId')).toBe('c');
    expect(request.request.params.get('search')).toBe('shirt');
    request.flush({ items: [item], totalCount: 201, offset: 0, limit: 100 });
    list.changePageSize(20);
    await harness.fixture.whenStable();
    request = http.expectOne((r) => r.url === '/api/products');
    expect(request.request.params.get('limit')).toBe('20');
    request.flush({ items: [item], totalCount: 201, offset: 0, limit: 20 });
    expect(list.listQuery().limit).toBeUndefined();
  });
  it('retains the chosen size while searching or changing stock filters', async () => {
    const { harness, list } = await setup();
    list.searchText = 'new';
    list.search();
    await harness.fixture.whenStable();
    let request = http.expectOne((r) => r.url === '/api/products');
    expect(request.request.params.get('limit')).toBe('50');
    expect(request.request.params.get('search')).toBe('new');
    request.flush({ items: [item], totalCount: 201, offset: 0, limit: 50 });
    list.filterStock('low');
    await harness.fixture.whenStable();
    request = http.expectOne((r) => r.url === '/api/products');
    expect(request.request.params.get('limit')).toBe('50');
    expect(request.request.params.get('stock')).toBe('low');
    request.flush({ items: [item], totalCount: 201, offset: 0, limit: 50 });
  });
  it('keeps the size in product detail and creation back links', async () => {
    const { harness } = await setup();
    const links = Array.from(
      harness.routeNativeElement!.querySelectorAll('a'),
    ) as HTMLAnchorElement[];
    expect(
      links.find((link) => link.textContent?.includes('Nieuw product'))!.getAttribute('href'),
    ).toContain('limit=50');
    await harness.navigateByUrl(
      links.find((link) => link.textContent?.trim() === 'Shirt')!.getAttribute('href')!,
      ProductDetail,
    );
    http.expectOne('/api/products/p').flush({}, { status: 404, statusText: 'Missing' });
    harness.detectChanges();
    expect(harness.routeNativeElement!.querySelector('a.back')!.getAttribute('href')).toContain(
      'limit=50',
    );
    await harness.navigateByUrl('/producten/nieuw?limit=50&offset=50', ProductCreate);
    http.expectOne((r) => r.url === '/api/product-types').flush([]);
    harness.detectChanges();
    expect(harness.routeNativeElement!.querySelector('a.back')!.getAttribute('href')).toContain(
      'limit=50',
    );
  });
  it.each(['0', '25', '101', 'invalid', '50.5', '5e1'])(
    'normalizes unsupported page size %s to 20',
    (limit) => {
      expect(readProductListQuery(convertToParamMap({ limit })).limit).toBeUndefined();
    },
  );
  it('normalizes offsets that do not align with the selected page size', () => {
    expect(readProductListQuery(convertToParamMap({ limit: '50', offset: '20' })).offset).toBe(0);
    expect(readProductListQuery(convertToParamMap({ limit: '50', offset: '50' })).offset).toBe(50);
    expect(readProductListQuery(convertToParamMap({ limit: '100', offset: '200' })).offset).toBe(
      200,
    );
  });
});
