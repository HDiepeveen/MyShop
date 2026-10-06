import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { CustomerWishlist } from './customer-wishlist';
import { ShopDetail } from '../shop/shop-detail';
import { readWishlistQuery, readWishlistReturn } from './wishlist-query';

const product = {
  productId: 'p',
  name: 'Shirt',
  imageUrl: null,
  imageAlt: '',
  isAvailable: true,
  addedAt: '',
};
describe('Wishlist browsing', () => {
  let http: HttpTestingController;
  function setup(parameters: Record<string, string> = {}) {
    const query = convertToParamMap(parameters);
    TestBed.configureTestingModule({
      imports: [CustomerWishlist, ShopDetail],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { queryParamMap: query },
            queryParamMap: new BehaviorSubject(query),
            paramMap: new BehaviorSubject(convertToParamMap({ id: 'p' })),
          },
        },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(CustomerWishlist);
    const request = http.expectOne((r) => r.url === '/api/customer/wishlist');
    return { fixture, page: fixture.componentInstance, request };
  }
  afterEach(() => {
    http.verify();
    vi.restoreAllMocks();
  });
  it('restores search/sort/page and carries that context in product links', () => {
    const { fixture, page, request } = setup({ search: ' shirt ', sort: 'name', offset: '20' });
    expect(request.request.params.get('search')).toBe('shirt');
    expect(request.request.params.get('sort')).toBe('name');
    expect(request.request.params.get('offset')).toBe('20');
    request.flush({ items: [product], totalCount: 41, offset: 20, limit: 20 });
    fixture.detectChanges();
    const link = fixture.nativeElement.querySelector('article a') as HTMLAnchorElement;
    expect(link.getAttribute('href')).toContain('from=wishlist');
    expect(link.getAttribute('href')).toContain('wishlistSearch=shirt');
    expect(link.getAttribute('href')).toContain('wishlistSort=name');
    expect(link.getAttribute('href')).toContain('wishlistOffset=20');
    page.searchText = 'draft';
    page.load(40);
    const next = http.expectOne((r) => r.url === '/api/customer/wishlist');
    expect(next.request.params.get('search')).toBe('shirt');
    expect(next.request.params.get('sort')).toBe('name');
    next.flush({ items: [], totalCount: 41, offset: 40, limit: 20 });
  });
  it('changes search and sort at page one and clears search without losing sort', () => {
    const { page, request } = setup({ offset: '20' });
    request.flush({ items: [], totalCount: 0, offset: 20, limit: 20 });
    page.searchText = ' shirt ';
    page.applySearch();
    let next = http.expectOne((r) => r.url === '/api/customer/wishlist');
    expect(next.request.params.get('search')).toBe('shirt');
    expect(next.request.params.get('offset')).toBe('0');
    next.flush({ items: [], totalCount: 0, offset: 0, limit: 20 });
    page.changeSort('name');
    next = http.expectOne((r) => r.url === '/api/customer/wishlist');
    expect(next.request.params.get('sort')).toBe('name');
    expect(next.request.params.get('search')).toBe('shirt');
    next.flush({ items: [], totalCount: 0, offset: 0, limit: 20 });
    page.clearSearch();
    next = http.expectOne((r) => r.url === '/api/customer/wishlist');
    expect(next.request.params.has('search')).toBe(false);
    expect(next.request.params.get('sort')).toBe('name');
    next.flush({ items: [], totalCount: 0, offset: 0, limit: 20 });
  });
  it('retains filters during retry and distinguishes a filtered empty list', () => {
    const { fixture, page, request } = setup({ search: 'missing', sort: 'name', offset: '20' });
    request.flush({}, { status: 503, statusText: 'Offline' });
    page.load(20);
    const retry = http.expectOne((r) => r.url === '/api/customer/wishlist');
    expect(retry.request.params.get('search')).toBe('missing');
    expect(retry.request.params.get('sort')).toBe('name');
    retry.flush({ items: [], offset: 0, limit: 20, totalCount: 0 });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(
      'Geen producten gevonden met deze zoekterm',
    );
    expect(fixture.nativeElement.textContent).not.toContain('Je verlanglijst is nog leeg');
  });
  it('protects browsing choices during loading and removal', () => {
    const { page, request } = setup({ search: 'shirt' });
    page.changeSort('name');
    page.clearSearch();
    page.applySearch();
    expect(page.sort()).toBe('newest');
    expect(page.search).toBe('shirt');
    request.flush({ items: [product], offset: 0, limit: 20, totalCount: 1 });
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    page.remove(product);
    const csrf = http.expectOne('/api/auth/csrf');
    page.changeSort('name');
    page.clearSearch();
    expect(page.search).toBe('shirt');
    expect(page.sort()).toBe('newest');
    csrf.flush(null);
    http.expectOne('/api/customer/wishlist/p').flush({}, { status: 409, statusText: 'Conflict' });
  });
  it('shows a safe wishlist back link only for an explicit wishlist context', () => {
    const { fixture, request } = setup({
      from: 'wishlist',
      wishlistSearch: 'shirt',
      wishlistSort: 'name',
      wishlistOffset: '20',
    });
    request.flush({ items: [], offset: 0, limit: 20, totalCount: 0 });
    fixture.destroy();
    const detail = TestBed.createComponent(ShopDetail);
    http.expectOne('/api/shop/products/p').flush({}, { status: 404, statusText: 'Missing' });
    http.expectOne('/api/shop/products/p/prices').flush({}, { status: 404, statusText: 'Missing' });
    detail.detectChanges();
    expect(detail.componentInstance.wishlistReturn()).toEqual({
      search: 'shirt',
      sort: 'name',
      offset: 20,
    });
    expect(
      detail.nativeElement
        .querySelector('a[href*="/winkel/account/verlanglijst"]')
        .getAttribute('href'),
    ).toContain('sort=name');
    expect(readWishlistReturn(convertToParamMap({ from: 'https://example.com' }))).toBeNull();
    expect(readWishlistQuery(convertToParamMap({ sort: 'unknown', offset: '-20' }))).toEqual({
      search: '',
      sort: 'newest',
      offset: 0,
    });
  });
});
