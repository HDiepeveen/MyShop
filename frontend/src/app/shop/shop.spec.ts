import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from '../app.routes';
import { authErrors } from '../auth/auth-routing';
import { ShopList } from './shop-list';
import { ShopDetail } from './shop-detail';
import { ShopImage } from './shop-image';
import { App } from '../app';

const product = {
  id: 'p',
  name: 'Linen shirt',
  description: '<b>Linen</b>\nComfortable.',
  imageUrl: 'https://example.com/a.jpg',
  imageAlt: 'Linen shirt',
  categories: [{ id: 'c1', name: 'Kleding' }],
  variants: [
    { id: 'v1', name: 'Small' },
    { id: 'v2', name: 'Large' },
  ],
};

describe('Public storefront', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter(routes),
        provideHttpClient(withInterceptors([authErrors])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  it('opens without authentication and preserves search/page when following product links', async () => {
    const harness = await RouterTestingHarness.create(
      '/winkel?search=shirt&offset=20&categoryId=c1',
    );
    http.expectNone((r) => r.url.startsWith('/api/auth'));
    http.expectOne('/api/shop/categories').flush([{ id: 'c1', name: 'Kleding' }]);
    http.expectOne('/api/shop/products?offset=20&limit=20&search=shirt&categoryId=c1').flush({
      at: '2026-09-30T12:00:00Z',
      items: [
        {
          ...product,
          prices: [{ currency: 'EUR', minimumAmount: '12.50', maximumAmount: '25.00' }],
        },
      ],
      totalCount: 45,
      offset: 20,
      limit: 20,
    });
    harness.detectChanges();
    expect(harness.routeNativeElement!.textContent).toContain('EUR 12,50');
    expect(harness.routeNativeElement!.textContent).toContain('25,00');
    const link = harness.routeNativeElement!.querySelector('a[href*="/winkel/p?"]')!;
    expect(link.getAttribute('href')).toContain('search=shirt');
    expect(link.getAttribute('href')).toContain('offset=20');
    expect(link.getAttribute('href')).toContain('categoryId=c1');
    const list = harness.routeDebugElement!.componentInstance as ShopList;
    const input = harness.routeNativeElement!.querySelector('input')!;
    input.value = 'coat';
    input.dispatchEvent(new Event('input'));
    await harness.fixture.whenStable();
    harness.routeNativeElement!.querySelector<HTMLButtonElement>('button[type=submit]')!.click();
    await harness.fixture.whenStable();
    http
      .expectOne('/api/shop/products?offset=0&limit=20&search=coat&categoryId=c1')
      .flush({ items: [], totalCount: 0, offset: 0, limit: 20, at: '2026-09-30T12:00:00Z' });
    expect(TestBed.inject(Router).url).toContain('search=coat');
    expect(TestBed.inject(Router).url).toContain('categoryId=c1');
    list.clearSearch();
    await harness.fixture.whenStable();
    http
      .expectOne('/api/shop/products?offset=0&limit=20&categoryId=c1')
      .flush({ items: [], totalCount: 0, offset: 0, limit: 20, at: '2026-09-30T12:00:00Z' });
    list.filterCategory('');
    await harness.fixture.whenStable();
    http
      .expectOne('/api/shop/products?offset=0&limit=20')
      .flush({ items: [], totalCount: 0, offset: 0, limit: 20, at: '2026-09-30T12:00:00Z' });
  });
  it('uses the applied search while paging and cancels obsolete reads', async () => {
    const harness = await RouterTestingHarness.create('/winkel?search=shirt');
    http.expectOne('/api/shop/categories').flush([]);
    const obsolete = http.expectOne('/api/shop/products?offset=0&limit=20&search=shirt');
    const list = harness.routeDebugElement!.componentInstance as ShopList;
    list.searchText = 'unsent';
    list.goToPage(20);
    await harness.fixture.whenStable();
    expect(obsolete.cancelled).toBe(true);
    http
      .expectOne('/api/shop/products?offset=20&limit=20&search=shirt')
      .flush({ items: [], totalCount: 0, offset: 20, limit: 20, at: '2026-09-30T12:00:00Z' });
    harness.detectChanges();
    expect(harness.routeNativeElement!.textContent).toContain('Geen producten op deze pagina');
    expect(list.searchText).toBe('unsent');
    list.goToPage(0);
    await harness.fixture.whenStable();
    http
      .expectOne('/api/shop/products?offset=0&limit=20&search=shirt')
      .flush({ items: [], totalCount: 0, offset: 0, limit: 20, at: '2026-09-30T12:00:00Z' });
  });
  it('selects variants, escapes product text and retains the return context', async () => {
    const harness = await RouterTestingHarness.create(
      '/winkel/p?search=shirt&offset=20&categoryId=c1',
    );
    http.expectOne('/api/shop/products/p').flush(product);
    http
      .expectOne('/api/shop/products/p/prices')
      .flush({ at: '2026-09-30T12:00:00Z', variants: [] });
    await harness.fixture.whenStable();
    harness.detectChanges();
    const detail = harness.routeDebugElement!.componentInstance as ShopDetail;
    expect(detail.selected()?.name).toBe('Small');
    const select = harness.routeNativeElement!.querySelector('select')!;
    select.value = 'v2';
    select.dispatchEvent(new Event('change'));
    await harness.fixture.whenStable();
    expect(detail.selected()?.name).toBe('Large');
    expect(harness.routeNativeElement!.querySelector('.description b')).toBeNull();
    expect(harness.routeNativeElement!.querySelector('.description')?.textContent).toContain(
      '<b>Linen</b>',
    );
    expect(harness.routeNativeElement!.querySelector('.back')?.getAttribute('href')).toContain(
      'offset=20',
    );
    expect(harness.routeNativeElement!.querySelector('.back')?.getAttribute('href')).toContain(
      'categoryId=c1',
    );
    const categoryLink = harness.routeNativeElement!.querySelector(
      'nav[aria-label="Productcategorieën"] a',
    );
    expect(categoryLink?.textContent).toContain('Kleding');
    expect(categoryLink?.getAttribute('href')).toContain('categoryId=c1');
    http.expectNone((r) => r.url.startsWith('/api/auth'));
  });
  it('removes stale details when navigating to a missing or withdrawn product', async () => {
    const harness = await RouterTestingHarness.create('/winkel/p');
    http.expectOne('/api/shop/products/p').flush(product);
    http
      .expectOne('/api/shop/products/p/prices')
      .flush({ at: '2026-09-30T12:00:00Z', variants: [] });
    await harness.fixture.whenStable();
    await harness.navigateByUrl('/winkel/withdrawn');
    http
      .expectOne('/api/shop/products/withdrawn/prices')
      .flush({}, { status: 404, statusText: 'Not Found' });
    http
      .expectOne('/api/shop/products/withdrawn')
      .flush({}, { status: 404, statusText: 'Not Found' });
    harness.detectChanges();
    expect(harness.routeNativeElement!.textContent).toContain('Product niet beschikbaar');
    expect(harness.routeNativeElement!.textContent).not.toContain('Linen shirt');
    expect(TestBed.inject(Router).url).toBe('/winkel/withdrawn');
    const detail = harness.routeDebugElement!.componentInstance as ShopDetail;
    detail.retry();
    http.expectOne('/api/shop/products/withdrawn').flush(product);
    await harness.fixture.whenStable();
    expect(detail.selected()?.id).toBe('v1');
  });
  it('shows the selected price, zero and missing prices, and clears stale prices on refresh failure', async () => {
    const harness = await RouterTestingHarness.create('/winkel/p');
    http.expectOne('/api/shop/products/p').flush(product);
    http.expectOne('/api/shop/products/p/prices').flush({
      at: '2026-09-30T12:00:00Z',
      variants: [
        { variantId: 'v1', amount: 75, currency: 'EUR' },
        { variantId: 'v2', amount: 0, currency: 'USD' },
      ],
    });
    await harness.fixture.whenStable();
    harness.detectChanges();
    expect(harness.routeNativeElement!.querySelector('.price')?.textContent).toContain('75.00');
    const select = harness.routeNativeElement!.querySelector('select')!;
    select.value = 'v2';
    select.dispatchEvent(new Event('change'));
    await harness.fixture.whenStable();
    expect(harness.routeNativeElement!.querySelector('.price')?.textContent).toContain('USD0.00');
    const detail = harness.routeDebugElement!.componentInstance as ShopDetail;
    detail.refreshPrices();
    harness.detectChanges();
    expect(harness.routeNativeElement!.querySelector('.price')).toBeNull();
    http.expectOne('/api/shop/products/p/prices').flush({}, { status: 500, statusText: 'Error' });
    await harness.fixture.whenStable();
    expect(harness.routeNativeElement!.textContent).toContain('Prijs niet beschikbaar');
    expect(detail.selectedId()).toBe('v2');
    detail.refreshPrices();
    http.expectOne('/api/shop/products/p/prices').flush({
      at: '2026-09-30T12:01:00Z',
      variants: [{ variantId: 'v2', amount: null, currency: null }],
    });
    await harness.fixture.whenStable();
    expect(harness.routeNativeElement!.textContent).toContain('nog geen prijs beschikbaar');
    expect(harness.routeNativeElement!.querySelector('.price')).toBeNull();
  });
  it('preserves the domain money precision for every currency', async () => {
    const harness = await RouterTestingHarness.create('/winkel/p');
    http.expectOne('/api/shop/products/p').flush(product);
    http.expectOne('/api/shop/products/p/prices').flush({
      at: '2026-09-30T12:00:00Z',
      variants: [{ variantId: 'v1', amount: 10.25, currency: 'JPY' }],
    });
    await harness.fixture.whenStable();
    expect(harness.routeNativeElement!.querySelector('.price')?.textContent).toContain('JPY10.25');
  });
  it('cancels obsolete price requests when the product changes', async () => {
    const harness = await RouterTestingHarness.create('/winkel/p');
    http.expectOne('/api/shop/products/p').flush(product);
    const obsolete = http.expectOne('/api/shop/products/p/prices');
    await harness.navigateByUrl('/winkel/other');
    expect(obsolete.cancelled).toBe(true);
    http.expectOne('/api/shop/products/other').flush({ ...product, id: 'other' });
    http
      .expectOne('/api/shop/products/other/prices')
      .flush({ at: '2026-09-30T12:00:00Z', variants: [] });
    await harness.fixture.whenStable();
    expect(harness.routeNativeElement!.querySelector('.price')).toBeNull();
    expect(harness.routeNativeElement!.textContent).toContain('nog geen prijs beschikbaar');
  });
  it('keeps unknown shop paths in the public area', async () => {
    const harness = await RouterTestingHarness.create('/winkel/unknown/path');
    expect(harness.routeNativeElement!.textContent).toContain('Pagina niet gevonden');
    http.expectNone((r) => r.url.startsWith('/api/'));
  });
  it('shows a public shell without administration navigation', async () => {
    const fixture = TestBed.createComponent(App);
    await TestBed.inject(Router).navigateByUrl('/winkel');
    await fixture.whenStable();
    http.expectOne('/api/shop/categories').flush([]);
    http
      .expectOne('/api/shop/products?offset=0&limit=20')
      .flush({ items: [], totalCount: 0, offset: 0, limit: 20, at: '2026-09-30T12:00:00Z' });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.sidebar')).toBeNull();
    expect(fixture.nativeElement.querySelector('nav')?.getAttribute('aria-label')).toBe(
      'Winkelnavigatie',
    );
    expect(fixture.nativeElement.querySelector('[href="/producten"]')).toBeNull();
    http.expectNone((r) => r.url.startsWith('/api/auth'));
  });
  it('shows an image fallback and retries when the image URL changes', async () => {
    const fixture = TestBed.createComponent(ShopImage);
    fixture.componentRef.setInput('url', 'https://example.com/a.jpg');
    fixture.componentRef.setInput('alt', 'A shirt');
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('img').alt).toBe('A shirt');
    fixture.nativeElement.querySelector('img').dispatchEvent(new Event('error'));
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Afbeelding niet beschikbaar');
    fixture.componentRef.setInput('url', 'https://example.com/b.jpg');
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('img')).not.toBeNull();
  });
});
