import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { CategoryList } from './categories/category-list';
import { TypeList } from './types/type-list';
import { ProductList } from './products/product-list';

for (const config of [
  {
    route: 'categorieen',
    endpoint: '/api/categories',
    component: CategoryList,
    noun: 'categorieën',
    product: false,
  },
  {
    route: 'producttypen',
    endpoint: '/api/product-types',
    component: TypeList,
    noun: 'producttypen',
    product: false,
  },
  {
    route: 'producten',
    endpoint: '/api/products',
    component: ProductList,
    noun: 'producten',
    product: true,
  },
]) {
  describe(config.route + ' list recovery', () => {
    let http: HttpTestingController;
    const empty = () => (config.product ? { items: [], totalCount: 0 } : []);
    const filters = () => (config.product ? '&categoryId=c&productTypeId=t' : '');
    function request(offset: number) {
      return http.expectOne(
        (r) =>
          r.url === config.endpoint &&
          r.params.get('offset') === String(offset) &&
          r.params.get('search') === 'shirt' &&
          (!config.product ||
            (r.params.get('categoryId') === 'c' && r.params.get('productTypeId') === 't')),
      );
    }
    function button(harness: RouterTestingHarness, label: string) {
      return Array.from(harness.routeNativeElement!.querySelectorAll('button')).find(
        (b) => b.textContent?.trim() === label,
      )!;
    }
    beforeEach(() => {
      TestBed.configureTestingModule({
        providers: [
          provideRouter([{ path: config.route, component: config.component }]),
          provideHttpClient(),
          provideHttpClientTesting(),
        ],
      });
      http = TestBed.inject(HttpTestingController);
    });
    afterEach(() => http.verify());

    it('refreshes the applied query without submitting a draft or changing the URL', async () => {
      const url = '/' + config.route + '?search=shirt&offset=40' + filters();
      const harness = await RouterTestingHarness.create(url);
      request(40).flush(empty());
      const list = harness.routeDebugElement!.componentInstance as
        CategoryList | TypeList | ProductList;
      list.searchText = 'unsent draft';
      harness.detectChanges();
      button(harness, 'Overzicht verversen').click();
      const pending = request(40);
      harness.detectChanges();
      expect(button(harness, 'Overzicht verversen').disabled).toBe(true);
      expect(TestBed.inject(Router).url).toBe(url);
      expect(list.searchText).toBe('unsent draft');
      pending.flush(empty());
      harness.detectChanges();
      expect(button(harness, 'Overzicht verversen').disabled).toBe(false);
    });

    it('returns directly to the first page keeping the applied search and filters', async () => {
      const harness = await RouterTestingHarness.create(
        '/' + config.route + '?search=shirt&offset=60' + filters(),
      );
      request(60).flush(empty());
      harness.detectChanges();
      button(harness, 'Eerste pagina').click();
      await harness.fixture.whenStable();
      request(0).flush(empty());
      harness.detectChanges();
      const params = TestBed.inject(Router).parseUrl(TestBed.inject(Router).url).queryParams;
      expect(params['search']).toBe('shirt');
      expect(params['offset']).toBeUndefined();
      if (config.product) {
        expect(params['categoryId']).toBe('c');
        expect(params['productTypeId']).toBe('t');
      }
      expect(button(harness, 'Eerste pagina')).toBeUndefined();
    });

    it('distinguishes an empty later page from no matching results on page one', async () => {
      const harness = await RouterTestingHarness.create(
        '/' + config.route + '?search=shirt&offset=20' + filters(),
      );
      request(20).flush(empty());
      harness.detectChanges();
      expect(harness.routeNativeElement!.textContent).toContain(
        'Geen ' + config.noun + ' op deze pagina',
      );
      expect(harness.routeNativeElement!.textContent).not.toContain(
        'Geen ' + config.noun + ' gevonden',
      );
      await harness.navigateByUrl('/' + config.route + '?search=shirt' + filters());
      request(0).flush(empty());
      harness.detectChanges();
      expect(harness.routeNativeElement!.textContent).toContain(
        'Geen ' + config.noun + ' gevonden',
      );
      expect(harness.routeNativeElement!.textContent).not.toContain('op deze pagina');
    });
  });
}

describe('Product search retry', () => {
  it('repeats an unchanged search after a failure while retaining both filters', async () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'producten', component: ProductList }]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    const http = TestBed.inject(HttpTestingController);
    const url = '/producten?search=shirt&categoryId=c&productTypeId=t';
    const harness = await RouterTestingHarness.create(url);
    http
      .expectOne((r) => r.url === '/api/products')
      .flush({}, { status: 503, statusText: 'Unavailable' });
    const list = harness.routeDebugElement!.componentInstance as ProductList;
    list.searchText = ' shirt ';
    list.search();
    http
      .expectOne(
        (r) =>
          r.params.get('search') === 'shirt' &&
          r.params.get('offset') === '0' &&
          r.params.get('categoryId') === 'c' &&
          r.params.get('productTypeId') === 't',
      )
      .flush({ items: [], totalCount: 0 });
    harness.detectChanges();
    expect(TestBed.inject(Router).url).toBe(url);
    expect(list.state()?.error).toBeFalsy();
    http.verify();
  });
});
