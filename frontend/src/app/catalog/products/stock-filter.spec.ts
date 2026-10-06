import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { convertToParamMap, provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { ProductList } from './product-list';
import { ProductDetail } from './product-detail';
import { readProductListQuery } from './product-list-query';
const page = {
  items: [{ id: 'p', name: 'Shirt', productTypeId: 't', variantCount: 2, isPublished: true }],
  totalCount: 50,
  offset: 20,
  limit: 20,
};
describe('Managed product stock filters', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: 'producten', component: ProductList },
          { path: 'producten/:id', component: ProductDetail },
        ]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  async function setup() {
    const harness = await RouterTestingHarness.create(
      '/producten?stock=low&published=true&search=shirt&categoryId=c&productTypeId=t&offset=20',
    );
    const request = http.expectOne((r) => r.url === '/api/products');
    expect(request.request.params.get('stock')).toBe('low');
    expect(request.request.params.get('published')).toBe('true');
    request.flush(page);
    harness.detectChanges();
    return { harness, list: harness.routeDebugElement!.componentInstance as ProductList };
  }
  it('renders a stock filter and preserves combined criteria when paging', async () => {
    const { harness, list } = await setup();
    expect(
      (harness.routeNativeElement!.querySelector('select[name="stock"]') as HTMLSelectElement)
        .value,
    ).toBe('low');
    expect(harness.routeNativeElement!.textContent).toContain('minstens één variant');
    list.searchText = 'draft';
    list.changePage(20);
    await harness.fixture.whenStable();
    const request = http.expectOne((r) => r.url === '/api/products');
    expect(request.request.params.get('stock')).toBe('low');
    expect(request.request.params.get('offset')).toBe('40');
    expect(request.request.params.get('search')).toBe('shirt');
    expect(request.request.params.get('productTypeId')).toBe('t');
    request.flush({ ...page, offset: 40 });
  });
  it.each(['out', 'untracked'])('changes stock filter to %s at page one', async (stock) => {
    const { harness, list } = await setup();
    list.filterStock(stock);
    await harness.fixture.whenStable();
    const request = http.expectOne((r) => r.url === '/api/products');
    expect(request.request.params.get('stock')).toBe(stock);
    expect(request.request.params.get('offset')).toBe('0');
    expect(request.request.params.get('published')).toBe('true');
    expect(request.request.params.get('categoryId')).toBe('c');
    request.flush({ ...page, offset: 0 });
  });
  it('clears only stock and retains stock during changes to other filters', async () => {
    const { harness, list } = await setup();
    list.filterPublication('draft');
    await harness.fixture.whenStable();
    let request = http.expectOne((r) => r.url === '/api/products');
    expect(request.request.params.get('stock')).toBe('low');
    expect(request.request.params.get('published')).toBe('false');
    request.flush({ ...page, offset: 0 });
    list.searchText = 'new';
    list.search();
    await harness.fixture.whenStable();
    request = http.expectOne((r) => r.url === '/api/products');
    expect(request.request.params.get('stock')).toBe('low');
    expect(request.request.params.get('search')).toBe('new');
    request.flush({ ...page, offset: 0 });
    list.filterStock('all');
    await harness.fixture.whenStable();
    request = http.expectOne((r) => r.url === '/api/products');
    expect(request.request.params.has('stock')).toBe(false);
    expect(request.request.params.get('published')).toBe('false');
    expect(request.request.params.get('search')).toBe('new');
    request.flush({ ...page, offset: 0 });
    expect(TestBed.inject(Router).url).not.toContain('stock=');
  });
  it('retains the stock filter through detail back navigation and normalizes unknown codes', async () => {
    const { harness } = await setup();
    const link = (
      Array.from(harness.routeNativeElement!.querySelectorAll('a')) as HTMLAnchorElement[]
    ).find((link) => link.textContent?.trim() === 'Shirt')!;
    expect(link.getAttribute('href')).toContain('stock=low');
    await harness.navigateByUrl(link.getAttribute('href')!, ProductDetail);
    http.expectOne('/api/products/p').flush({}, { status: 404, statusText: 'Missing' });
    harness.detectChanges();
    expect(harness.routeNativeElement!.querySelector('a.back')!.getAttribute('href')).toContain(
      'stock=low',
    );
    expect(readProductListQuery(convertToParamMap({ stock: 'unknown' })).stock).toBeUndefined();
    expect(readProductListQuery(convertToParamMap({ stock: 'out' })).stock).toBe('out');
  });
});
