import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { ProductList } from './product-list';

describe('Product list navigation', () => {
  it('restores a deep link and writes search and paging to the real router', async () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'producten', component: ProductList }]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    const http = TestBed.inject(HttpTestingController);
    const harness = await RouterTestingHarness.create();
    const list = await harness.navigateByUrl(
      '/producten?search=coat&offset=20&categoryId=c',
      ProductList,
    );
    http
      .expectOne(
        (r) =>
          r.params.get('search') === 'coat' &&
          r.params.get('offset') === '20' &&
          r.params.get('categoryId') === 'c',
      )
      .flush({ items: [{ id: 'p', name: 'Coat', variantCount: 1 }], totalCount: 60 });
    harness.detectChanges();
    expect(list.searchText).toBe('coat');
    const link = harness.routeNativeElement!.querySelector(
      'a[href^="/producten/p?"]',
    ) as HTMLAnchorElement;
    expect(link.getAttribute('href')).toContain('search=coat');
    expect(link.getAttribute('href')).toContain('offset=20');
    list.searchText = 'shirt';
    list.search();
    await harness.fixture.whenStable();
    http
      .expectOne((r) => r.params.get('search') === 'shirt' && r.params.get('offset') === '0')
      .flush({ items: [], totalCount: 60 });
    expect(TestBed.inject(Router).url).toContain('search=shirt');
    list.changePage(20);
    await harness.fixture.whenStable();
    http
      .expectOne((r) => r.params.get('search') === 'shirt' && r.params.get('offset') === '20')
      .flush({ items: [], totalCount: 60 });
    expect(TestBed.inject(Router).url).toContain('offset=20');
    list.clearSearch();
    await harness.fixture.whenStable();
    http
      .expectOne(
        (r) =>
          !r.params.has('search') &&
          r.params.get('offset') === '0' &&
          r.params.get('categoryId') === 'c',
      )
      .flush({ items: [], totalCount: 60 });
    http.verify();
  });
});
