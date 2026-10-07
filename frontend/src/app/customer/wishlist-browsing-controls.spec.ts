import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { CustomerWishlist } from './customer-wishlist';

describe('Wishlist browsing controls', () => {
  let http: HttpTestingController;
  const item = {
    productId: '11111111-1111-1111-1111-111111111111',
    name: 'Shirt',
    imageUrl: null,
    imageAlt: '',
    isAvailable: true,
    addedAt: '2026-10-07T10:00:00Z',
  };
  const page = { items: [item], offset: 20, limit: 20, totalCount: 61 };
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [CustomerWishlist],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: convertToParamMap({ search: 'shirt', sort: 'name', offset: '20' }),
            },
          },
        },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => {
    http.verify();
    vi.restoreAllMocks();
  });
  function read() {
    return http.expectOne((r) => r.url === '/api/customer/wishlist');
  }
  function setup() {
    const fixture = TestBed.createComponent(CustomerWishlist);
    read().flush(page);
    fixture.detectChanges();
    return { fixture, list: fixture.componentInstance };
  }
  it('shows total, page and the recorded addition date', () => {
    const { fixture, list } = setup();
    const text = fixture.nativeElement.textContent.replace(/\s+/g, ' ');
    expect(text).toContain('61 producten gevonden');
    expect(text).toContain('Pagina 2 van 4');
    expect(text).toContain('Toegevoegd op 07-10-2026');
    expect(list.pageCount()).toBe(4);
    const choices = fixture.nativeElement.querySelector(
      '[aria-label="Actieve verlanglijstkeuzes"]',
    );
    expect(choices.textContent).toContain('Zoekterm: shirt');
    expect(choices.textContent).toContain('Naam: A–Z');
  });
  it('opens first and last pages with the applied search and sort', () => {
    const { list } = setup();
    list.searchText = 'unsent';
    list.last();
    let request = read();
    expect(request.request.params.get('offset')).toBe('60');
    expect(request.request.params.get('search')).toBe('shirt');
    expect(request.request.params.get('sort')).toBe('name');
    request.flush({ ...page, offset: 60 });
    list.last();
    http.expectNone((r) => r.url === '/api/customer/wishlist');
    list.first();
    request = read();
    expect(request.request.params.get('offset')).toBe('0');
    request.flush({ ...page, offset: 0 });
    list.first();
    http.expectNone((r) => r.url === '/api/customer/wishlist');
  });
  it('jumps to a page and keeps the product return context', () => {
    const { fixture, list } = setup();
    list.pageNumber = 3;
    list.jumpToPage();
    const request = read();
    expect(request.request.params.get('offset')).toBe('40');
    request.flush({ ...page, offset: 40 });
    fixture.detectChanges();
    expect(list.productContext()).toMatchObject({
      wishlistSearch: 'shirt',
      wishlistSort: 'name',
      wishlistOffset: 40,
    });
    expect(
      fixture.nativeElement.querySelector('a[href*="/winkel/111"]').getAttribute('href'),
    ).toContain('wishlistOffset=40');
  });
  it.each([null, 0, -1, 1.5, 5, NaN, Infinity])('rejects invalid page %s', (pageNumber) => {
    const { list } = setup();
    list.pageNumber = pageNumber;
    list.jumpToPage();
    expect(list.pageError()).toContain('heel paginanummer');
    http.expectNone((r) => r.url === '/api/customer/wishlist');
    list.pageNumber = 2;
    list.jumpToPage();
    expect(list.pageError()).toBe('');
    http.expectNone((r) => r.url === '/api/customer/wishlist');
  });
  it('restores all choices while preserving the removal recovery option', () => {
    const { list } = setup();
    list.removedItem.set(item);
    list.searchText = 'unsent';
    list.resetFilters();
    const request = read();
    expect(request.request.params.keys()).toEqual(['offset', 'limit']);
    request.flush({ ...page, offset: 0 });
    expect(list.sort()).toBe('newest');
    expect(list.searchText).toBe('');
    expect(list.removedItem()).toEqual(item);
  });
  it('removes individual choices through the visible controls', () => {
    const { fixture, list } = setup();
    fixture.nativeElement.querySelector('[aria-label="Zoekfilter verwijderen"]').click();
    let request = read();
    expect(request.request.params.has('search')).toBe(false);
    expect(request.request.params.get('sort')).toBe('name');
    request.flush({ ...page, offset: 0 });
    fixture.detectChanges();
    fixture.nativeElement.querySelector('[aria-label="Standaardsortering herstellen"]').click();
    request = read();
    expect(request.request.params.has('sort')).toBe(false);
    request.flush({ ...page, offset: 0 });
    expect(list.sort()).toBe('newest');
  });
  it('refreshes the current applied query and blocks reads until it finishes', () => {
    const { fixture, list } = setup();
    list.searchText = 'unsent';
    const button = Array.from(
      fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
    ).find((b) => b.textContent?.includes('Verlanglijst vernieuwen'))!;
    button.click();
    const request = read();
    expect(request.request.params.get('offset')).toBe('20');
    expect(request.request.params.get('search')).toBe('shirt');
    fixture.detectChanges();
    expect(button.disabled).toBe(true);
    list.first();
    list.last();
    list.resetFilters();
    list.jumpToPage();
    list.load(20);
    http.expectNone((r) => r.url === '/api/customer/wishlist');
    request.flush(page);
  });
  it('blocks all navigation choices during removal and restoration', () => {
    const { list } = setup();
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    list.remove(item);
    http.expectOne('/api/auth/csrf').flush(null);
    const removal = http.expectOne('/api/customer/wishlist/' + item.productId);
    list.first();
    list.last();
    list.resetFilters();
    list.pageNumber = 3;
    list.jumpToPage();
    list.load(20);
    http.expectNone((r) => r.url === '/api/customer/wishlist');
    removal.flush(null);
    list.undoRemoval();
    http.expectOne('/api/auth/csrf').flush(null);
    const restore = http.expectOne('/api/customer/wishlist/' + item.productId);
    expect(restore.request.method).toBe('POST');
    list.first();
    list.last();
    list.resetFilters();
    list.jumpToPage();
    http.expectNone((r) => r.url === '/api/customer/wishlist');
    restore.flush(null);
    const request = read();
    expect(request.request.params.get('offset')).toBe('20');
    request.flush(page);
    expect(list.removedItem()).toBeNull();
  });
  it('returns from an empty page to page one without losing choices', () => {
    const { fixture, list } = setup();
    list.load(20);
    read().flush({ ...page, items: [], totalCount: 0 });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent.replace(/\s+/g, ' ')).not.toContain('Pagina 2 van 1');
    const button = Array.from(
      fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
    ).find((b) => b.textContent?.includes('Terug naar de eerste'))!;
    button.click();
    const request = read();
    expect(request.request.params.get('offset')).toBe('0');
    expect(request.request.params.get('search')).toBe('shirt');
    request.flush({ ...page, offset: 0, items: [], totalCount: 0 });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(
      'Geen producten gevonden met deze zoekterm.',
    );
  });
  it('retries after failure, rejects oversized offsets and cancels reads on destruction', () => {
    const { fixture, list } = setup();
    list.load(20);
    read().flush({}, { status: 500, statusText: 'Failed' });
    expect(list.loading()).toBe(false);
    expect(list.error()).not.toBe('');
    for (const offset of [-20, 1, NaN, 2147483660]) list.load(offset);
    http.expectNone((r) => r.url === '/api/customer/wishlist');
    list.load(20);
    const request = read();
    fixture.destroy();
    expect(request.cancelled).toBe(true);
  });
});
