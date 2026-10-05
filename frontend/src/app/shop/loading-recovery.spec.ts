import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { ShopList } from './shop-list';
import { ShopDetail } from './shop-detail';
import { OrderList } from '../checkout/order-list';

for (const kind of ['shop', 'orders'] as const) {
  describe(kind + ' loading recovery', () => {
    let http: HttpTestingController;
    function setup() {
      const params = new BehaviorSubject(convertToParamMap({ search: 'shirt', offset: '20' }));
      TestBed.configureTestingModule({
        imports: [ShopList, OrderList],
        providers: [
          provideHttpClient(),
          provideHttpClientTesting(),
          provideRouter([]),
          { provide: ActivatedRoute, useValue: { queryParamMap: params } },
        ],
      });
      http = TestBed.inject(HttpTestingController);
      const fixture =
        kind === 'shop' ? TestBed.createComponent(ShopList) : TestBed.createComponent(OrderList);
      const endpoint = kind === 'shop' ? '/api/shop/products' : '/api/orders';
      if (kind === 'shop') http.expectOne('/api/shop/categories').flush([]);
      const initial = http.expectOne((r) => r.url === endpoint);
      const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
      return { page: fixture.componentInstance, initial, endpoint, navigate };
    }
    afterEach(() => http.verify());
    it('does not restart pending reads and permits retry after failure', () => {
      const { page, initial, endpoint } = setup();
      page.retry();
      expect(initial.cancelled).toBe(false);
      initial.flush({}, { status: 503, statusText: 'Unavailable' });
      page.retry();
      const retry = http.expectOne((r) => r.url === endpoint);
      page.retry();
      expect(retry.cancelled).toBe(false);
      expect(retry.request.params.get('offset')).toBe('20');
      expect(retry.request.params.get('search')).toBe('shirt');
      retry.flush({ items: [], totalCount: 0, offset: 20, limit: 20 });
    });
    if (kind === 'orders')
      it('blocks page navigation while loading and permits it afterwards', () => {
        const { page, initial, navigate } = setup();
        if (page instanceof ShopList) page.goToPage(40);
        else page.changePage(20);
        expect(navigate).not.toHaveBeenCalled();
        initial.flush({ items: [], totalCount: 60, offset: 20, limit: 20 });
        page.searchText = 'draft';
        if (page instanceof ShopList) page.goToPage(40);
        else page.changePage(20);
        expect(navigate).toHaveBeenCalledOnce();
        expect(navigate.mock.calls[0][1]?.queryParams?.['offset']).toBe(40);
      });
  });
}
describe('Storefront detail and category recovery', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ShopList, ShopDetail],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            queryParamMap: new BehaviorSubject(convertToParamMap({})),
            paramMap: new BehaviorSubject(convertToParamMap({ id: 'p' })),
          },
        },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  it('retries failed categories once without reloading products or losing search', () => {
    const fixture = TestBed.createComponent(ShopList);
    const page = fixture.componentInstance;
    const products = http.expectOne((r) => r.url === '/api/shop/products');
    const categories = http.expectOne('/api/shop/categories');
    page.retryCategories();
    expect(categories.cancelled).toBe(false);
    categories.flush({}, { status: 503, statusText: 'Unavailable' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Categorieën konden niet');
    page.searchText = 'draft';
    page.retryCategories();
    const retry = http.expectOne('/api/shop/categories');
    page.retryCategories();
    expect(retry.cancelled).toBe(false);
    expect(products.cancelled).toBe(false);
    expect(page.searchText).toBe('draft');
    retry.flush([{ id: 'c', name: 'Clothing' }]);
    products.flush({ items: [], totalCount: 0 });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Clothing');
    expect(page.categories()?.error).toBe('');
  });
  it('keeps the product request pending and permits recovery after failure', () => {
    const page = TestBed.createComponent(ShopDetail).componentInstance;
    const initial = http.expectOne('/api/shop/products/p');
    const prices = http.expectOne('/api/shop/products/p/prices');
    page.refreshPrices();
    expect(prices.cancelled).toBe(false);
    prices.flush({}, { status: 503, statusText: 'Unavailable' });
    page.refreshPrices();
    const recovered = http.expectOne('/api/shop/products/p/prices');
    page.refreshPrices();
    expect(recovered.cancelled).toBe(false);
    recovered.flush({ variants: [] });
    page.retry();
    expect(initial.cancelled).toBe(false);
    initial.flush({}, { status: 503, statusText: 'Unavailable' });
    page.retry();
    const retry = http.expectOne('/api/shop/products/p');
    page.retry();
    expect(retry.cancelled).toBe(false);
    retry.flush({}, { status: 404, statusText: 'Missing' });
  });
});
