import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { convertToParamMap, provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { ProductList } from './product-list';
import { ProductDetail } from './product-detail';
import { ProductCreate } from './product-create';
import { readProductListQuery } from './product-list-query';

const page = {
  items: [{ id: 'p', name: 'Shirt', productTypeId: 't', variantCount: 1, isPublished: false }],
  totalCount: 50,
  offset: 20,
  limit: 20,
};
describe('Publication status filter', () => {
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
  async function setup() {
    const harness = await RouterTestingHarness.create(
      '/producten?published=false&search=shirt&categoryId=c&productTypeId=t&offset=20',
    );
    const request = http.expectOne((r) => r.url === '/api/products');
    expect(request.request.params.get('published')).toBe('false');
    request.flush(page);
    harness.detectChanges();
    return { harness, list: harness.routeDebugElement!.componentInstance as ProductList };
  }
  it('combines draft status with search and category/type filters and retains it when paging', async () => {
    const { harness, list } = await setup();
    expect((harness.routeNativeElement!.querySelector('select') as HTMLSelectElement).value).toBe(
      'draft',
    );
    list.searchText = 'draft search';
    list.changePage(20);
    await harness.fixture.whenStable();
    const request = http.expectOne((r) => r.url === '/api/products');
    expect(request.request.params.get('offset')).toBe('40');
    expect(request.request.params.get('published')).toBe('false');
    expect(request.request.params.get('search')).toBe('shirt');
    expect(request.request.params.get('categoryId')).toBe('c');
    expect(request.request.params.get('productTypeId')).toBe('t');
    request.flush({ ...page, offset: 40 });
  });
  it('changes publication status at page one and removes only that filter for all products', async () => {
    const { harness, list } = await setup();
    list.filterPublication('published');
    await harness.fixture.whenStable();
    let request = http.expectOne((r) => r.url === '/api/products');
    expect(request.request.params.get('published')).toBe('true');
    expect(request.request.params.get('offset')).toBe('0');
    expect(request.request.params.get('search')).toBe('shirt');
    request.flush({ ...page, offset: 0 });
    list.filterPublication('all');
    await harness.fixture.whenStable();
    request = http.expectOne((r) => r.url === '/api/products');
    expect(request.request.params.has('published')).toBe(false);
    expect(request.request.params.get('categoryId')).toBe('c');
    request.flush({ ...page, offset: 0 });
    expect(TestBed.inject(Router).url).not.toContain('published=');
  });
  it('keeps publication status while searching or removing a different filter', async () => {
    const { harness, list } = await setup();
    list.searchText = 'new';
    list.search();
    await harness.fixture.whenStable();
    let request = http.expectOne((r) => r.url === '/api/products');
    expect(request.request.params.get('published')).toBe('false');
    expect(request.request.params.get('search')).toBe('new');
    request.flush({ ...page, offset: 0 });
    list.clearFilter('categoryId');
    await harness.fixture.whenStable();
    request = http.expectOne((r) => r.url === '/api/products');
    expect(request.request.params.has('categoryId')).toBe(false);
    expect(request.request.params.get('published')).toBe('false');
    request.flush({ ...page, offset: 0 });
  });
  it('preserves publication context through product detail and creation back links', async () => {
    const { harness } = await setup();
    const links = Array.from(harness.routeNativeElement!.querySelectorAll('a'));
    expect(
      links.find((link) => link.textContent?.includes('Nieuw product'))?.getAttribute('href'),
    ).toContain('published=false');
    const detailUrl = links
      .find((link) => link.textContent?.trim() === 'Shirt')!
      .getAttribute('href')!;
    const opening = harness.navigateByUrl(detailUrl, ProductDetail);
    await opening;
    http.expectOne('/api/products/p').flush({}, { status: 404, statusText: 'Missing' });
    harness.detectChanges();
    expect(harness.routeNativeElement!.querySelector('a.back')!.getAttribute('href')).toContain(
      'published=false',
    );
    await harness.navigateByUrl('/producten/nieuw?published=false&search=shirt', ProductCreate);
    http.expectOne((r) => r.url === '/api/product-types').flush([]);
    harness.detectChanges();
    expect(harness.routeNativeElement!.querySelector('a.back')!.getAttribute('href')).toContain(
      'published=false',
    );
  });
  it('normalizes unsupported publication values to all products', () => {
    expect(readProductListQuery(convertToParamMap({ published: 'false' })).published).toBe(false);
    expect(readProductListQuery(convertToParamMap({ published: 'true' })).published).toBe(true);
    expect(readProductListQuery(convertToParamMap({ published: '1' })).published).toBeUndefined();
  });
});
