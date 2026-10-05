import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { ProductList } from './products/product-list';
import { TypeList } from './types/type-list';
import { CategoryList } from './categories/category-list';

for (const kind of ['product', 'type', 'category'] as const) {
  describe(kind + ' list loading recovery', () => {
    let http: HttpTestingController;
    function setup() {
      const params = new BehaviorSubject(convertToParamMap({ search: 'shirt', offset: '20' }));
      TestBed.configureTestingModule({
        imports: [ProductList, TypeList, CategoryList],
        providers: [
          provideHttpClient(),
          provideHttpClientTesting(),
          provideRouter([]),
          { provide: ActivatedRoute, useValue: { queryParamMap: params } },
        ],
      });
      http = TestBed.inject(HttpTestingController);
      const fixture =
        kind === 'product'
          ? TestBed.createComponent(ProductList)
          : kind === 'type'
            ? TestBed.createComponent(TypeList)
            : TestBed.createComponent(CategoryList);
      const endpoint =
        kind === 'product'
          ? '/api/products'
          : kind === 'type'
            ? '/api/product-types'
            : '/api/categories';
      const initial = http.expectOne((r) => r.url === endpoint);
      const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
      return { fixture, page: fixture.componentInstance, params, endpoint, initial, navigate };
    }
    afterEach(() => http.verify());
    if (kind !== 'product') {
      it('refreshes an initial pending overview after creating an item', () => {
        const { page, initial, params, endpoint } = setup();
        initial.flush([]);
        params.next(convertToParamMap({}));
        const old = http.expectOne((r) => r.url === endpoint);
        const editor = page as TypeList | CategoryList;
        editor.name = 'New';
        editor.create();
        http.expectOne((r) => r.url === endpoint && r.method === 'POST').flush({ id: 'new' });
        expect(old.cancelled).toBe(true);
        http.expectOne((r) => r.url === endpoint).flush([{ id: 'new', name: 'New' }]);
        expect(editor.saved()).toContain('New');
      });
    }

    it('keeps a pending read and retries a failed query only once', () => {
      const { page, initial, endpoint } = setup();
      page.retry();
      page.retry();
      expect(initial.cancelled).toBe(false);
      http.expectNone((r) => r.url === endpoint);
      initial.flush({}, { status: 503, statusText: 'Unavailable' });
      page.retry();
      const retry = http.expectOne((r) => r.url === endpoint);
      page.retry();
      expect(retry.cancelled).toBe(false);
      expect(retry.request.params.get('offset')).toBe('20');
      expect(retry.request.params.get('search')).toBe('shirt');
      retry.flush(kind === 'product' ? { items: [], totalCount: 0 } : []);
    });
    it('does not navigate pages during loading and retains the submitted search', () => {
      const { page, params, endpoint, initial, navigate } = setup();
      page.changePage(20);
      page.changePage(-20);
      expect(navigate).not.toHaveBeenCalled();
      initial.flush(
        kind === 'product'
          ? { items: [], totalCount: 60 }
          : Array.from({ length: 20 }, (_, i) => ({ id: String(i), name: 'Type' })),
      );
      page.searchText = 'draft';
      page.changePage(20);
      expect(navigate).toHaveBeenCalledOnce();
      expect(navigate).toHaveBeenCalledWith(
        [],
        expect.objectContaining({ queryParams: { offset: 40 }, queryParamsHandling: 'merge' }),
      );
      params.next(convertToParamMap({ search: 'shirt', offset: '40' }));
      const next = http.expectOne((r) => r.url === endpoint);
      page.changePage(20);
      expect(navigate).toHaveBeenCalledOnce();
      expect(next.request.params.get('search')).toBe('shirt');
      next.flush(kind === 'product' ? { items: [], totalCount: 60 } : []);
      page.changePage(-40);
      expect(navigate).toHaveBeenCalledTimes(2);
    });
  });
}
