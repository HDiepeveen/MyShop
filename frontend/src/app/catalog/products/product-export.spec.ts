import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { CatalogApi } from '../catalog.api';
import { ProductList } from './product-list';
describe('Product selection CSV export', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'producten', component: ProductList }]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  it('exports the applied selection without paging or an unsubmitted search', async () => {
    const harness = await RouterTestingHarness.create(
      '/producten?search=shirt&categoryId=c&productTypeId=t&published=false&stock=low&limit=50&offset=50',
    );
    http
      .expectOne((r) => r.url === '/api/products')
      .flush({ items: [], totalCount: 1200, limit: 50, offset: 50 });
    const page = harness.routeDebugElement!.componentInstance as ProductList;
    page.searchText = 'draft';
    harness.detectChanges();
    const link = harness.routeNativeElement!.querySelector(
      'a[download="myshop-products.csv"]',
    ) as HTMLAnchorElement;
    const url = new URL(link.getAttribute('href')!, 'http://localhost');
    expect(url.pathname).toBe('/api/products/export');
    expect(url.searchParams.get('search')).toBe('shirt');
    expect(url.searchParams.get('categoryId')).toBe('c');
    expect(url.searchParams.get('productTypeId')).toBe('t');
    expect(url.searchParams.get('published')).toBe('false');
    expect(url.searchParams.get('stock')).toBe('low');
    expect(url.searchParams.has('offset')).toBe(false);
    expect(url.searchParams.has('limit')).toBe(false);
    expect(harness.routeNativeElement!.textContent).toContain('1.000');
  });
  it('encodes special search characters and omits empty criteria', () => {
    const api = TestBed.inject(CatalogApi);
    const url = new URL(
      api.exportProductsUrl({ search: ' A&B +shirt ', published: true, stock: 'untracked' }),
      'http://localhost',
    );
    expect(url.searchParams.get('search')).toBe('A&B +shirt');
    expect(url.searchParams.get('published')).toBe('true');
    expect(api.exportProductsUrl({ search: '  ' })).toBe('/api/products/export');
  });
});
