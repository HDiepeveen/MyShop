import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from '../app.routes';
import { Title, Meta } from '@angular/platform-browser';
describe('Category pages', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter(routes), provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  it('loads category products, applies metadata and leaves the category when changing its filter', async () => {
    const harness = await RouterTestingHarness.create('/assortiment/occasions?categoryId=other');
    http
      .expectOne('/api/shop/category-pages/occasions')
      .flush({
        id: 'c',
        name: 'Occasions',
        webAddress: 'occasions',
        seoTitle: 'Occasions Zwolle',
        seoDescription: 'Bekijk onze occasions.',
      });
    harness.detectChanges();
    http.expectOne('/api/shop/categories').flush([{ id: 'c', name: 'Occasions' }]);
    const products = http.expectOne((r) => r.url === '/api/shop/products');
    expect(products.request.params.get('categoryId')).toBe('c');
    products.flush({ items: [], totalCount: 0, offset: 0, limit: 20 });
    harness.detectChanges();
    expect(TestBed.inject(Title).getTitle()).toBe('Occasions Zwolle');
    expect(TestBed.inject(Meta).getTag('name="description"')?.content).toBe(
      'Bekijk onze occasions.',
    );
    expect(document.querySelector<HTMLLinkElement>('link[rel="canonical"]')?.href).toContain(
      '/assortiment/occasions',
    );
    expect(harness.routeNativeElement?.textContent).toContain('Occasions');
  });
  it('shows a nonindexable error instead of the full assortment for an unknown category', async () => {
    const harness = await RouterTestingHarness.create('/assortiment/missing');
    http
      .expectOne('/api/shop/category-pages/missing')
      .flush({}, { status: 404, statusText: 'Not Found' });
    harness.detectChanges();
    expect(harness.routeNativeElement?.textContent).toContain('Categorie niet beschikbaar');
    expect(TestBed.inject(Meta).getTag('name="robots"')?.content).toBe('noindex');
    http.expectNone((r) => r.url === '/api/shop/products');
  });
});
