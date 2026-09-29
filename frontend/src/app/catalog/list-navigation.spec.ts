import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { CategoryList } from './categories/category-list';
import { TypeList } from './types/type-list';

for (const config of [
  { route: 'categorieen', endpoint: '/api/categories', component: CategoryList },
  { route: 'producttypen', endpoint: '/api/product-types', component: TypeList },
]) {
  describe(config.route + ' URL navigation', () => {
    let http: HttpTestingController;
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
    it('restores search and page, preserves draft input while paging, and clears search', async () => {
      const harness = await RouterTestingHarness.create();
      await harness.navigateByUrl('/' + config.route + '?search=shirt&offset=20');
      const list = harness.routeDebugElement!.componentInstance as CategoryList | TypeList;
      http
        .expectOne(config.endpoint + '?offset=20&limit=20&search=shirt')
        .flush([
          {
            id: 'id',
            name: 'Shirts',
            isRoot: true,
            directChildCount: 0,
            attributeDefinitionCount: 0,
          },
        ]);
      harness.detectChanges();
      expect(list.searchText).toBe('shirt');
      const link = harness.routeNativeElement!.querySelector(
        'a[href*="/id?"]',
      ) as HTMLAnchorElement;
      expect(link.getAttribute('href')).toContain('search=shirt');
      expect(link.getAttribute('href')).toContain('offset=20');
      list.searchText = 'draft';
      list.changePage(20);
      await harness.fixture.whenStable();
      http.expectOne(config.endpoint + '?offset=40&limit=20&search=shirt').flush([]);
      expect(TestBed.inject(Router).url).toContain('offset=40');
      list.search();
      await harness.fixture.whenStable();
      http.expectOne(config.endpoint + '?offset=0&limit=20&search=draft').flush([]);
      list.clearSearch();
      await harness.fixture.whenStable();
      http.expectOne(config.endpoint + '?offset=0&limit=20').flush([]);
      expect(list.offset()).toBe(0);
      expect(list.searchText).toBe('');
    });
    it('cancels obsolete reads on navigation and retries the current URL', async () => {
      const harness = await RouterTestingHarness.create();
      await harness.navigateByUrl('/' + config.route + '?search=old&offset=20');
      const old = http.expectOne(config.endpoint + '?offset=20&limit=20&search=old');
      await harness.navigateByUrl('/' + config.route + '?search=new&offset=-20');
      expect(old.cancelled).toBe(true);
      const list = harness.routeDebugElement!.componentInstance as CategoryList | TypeList;
      http
        .expectOne(config.endpoint + '?offset=0&limit=20&search=new')
        .flush({}, { status: 503, statusText: 'Unavailable' });
      list.retry();
      http.expectOne(config.endpoint + '?offset=0&limit=20&search=new').flush([]);
      expect(list.offset()).toBe(0);
      expect(list.searchText).toBe('new');
    });
    it('resets a filtered list after creation and refreshes the unfiltered first page', async () => {
      const harness = await RouterTestingHarness.create();
      await harness.navigateByUrl('/' + config.route + '?search=old&offset=20');
      http.expectOne(config.endpoint + '?offset=20&limit=20&search=old').flush([]);
      const list = harness.routeDebugElement!.componentInstance as CategoryList | TypeList;
      list.name = 'New';
      list.create();
      http.expectOne(config.endpoint).flush({ id: 'new', name: 'New' });
      await harness.fixture.whenStable();
      http.expectOne(config.endpoint + '?offset=0&limit=20').flush([]);
      expect(TestBed.inject(Router).url).toBe('/' + config.route);
    });
  });
}
