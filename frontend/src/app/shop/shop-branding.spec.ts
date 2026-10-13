import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ShopTitleStrategy } from './shop-title-strategy';
import { Title } from '@angular/platform-browser';
import { TitleStrategy, provideRouter, Router } from '@angular/router';
import { ShopBranding } from './shop-branding';
import { App } from '../app';
describe('Webshop name', () => {
  it('shows the uploaded icon before the shop name and hides an unavailable image', async () => {
    const fixture = TestBed.createComponent(App);
    const iconUrl = '/api/shop/icon?v=11111111-1111-1111-1111-111111111111';
    http.expectOne('/api/shop/settings').flush({ shopName: 'Autohuis Hans', iconUrl });
    await TestBed.inject(Router).navigateByUrl('/winkel');
    http.expectOne('/api/shop/settings').flush({ shopName: 'Autohuis Hans', iconUrl });
    http.expectOne('/api/shop/payment-options').flush({ checkoutEnabled: true });
    fixture.detectChanges();
    const brand = fixture.nativeElement.querySelector('.shop-brand');
    const image = brand.querySelector('img');
    expect(image.getAttribute('src')).toBe(iconUrl);
    expect(image.getAttribute('alt')).toBe('');
    expect(brand.textContent.trim()).toBe('Autohuis Hans');
    image.dispatchEvent(new Event('error'));
    fixture.detectChanges();
    expect(brand.querySelector('img')).toBeNull();
    expect(brand.textContent.trim()).toBe('Autohuis Hans');
  });
  it('applies saved colors only to the storefront and removes them when returning to management', async () => {
    const fixture = TestBed.createComponent(App);
    const appearance = {
      primaryColor: '#a7d9ff',
      backgroundColor: '#111827',
      panelColor: '#1f2937',
      textColor: '#f3f4f6',
    };
    http.expectOne('/api/shop/settings').flush({ shopName: 'MyShop', appearance });
    await TestBed.inject(Router).navigateByUrl('/winkel');
    http.expectOne('/api/shop/settings').flush({ shopName: 'MyShop', appearance });
    http.expectOne('/api/shop/payment-options').flush({ checkoutEnabled: true });
    fixture.detectChanges();
    const workspace = fixture.nativeElement.querySelector('.workspace') as HTMLElement;
    expect(workspace.style.getPropertyValue('--shop-background')).toBe('#111827');
    expect(workspace.style.getPropertyValue('--shop-button-text')).toBe('#000000');
    await TestBed.inject(Router).navigateByUrl('/inloggen');
    http.expectOne('/api/shop/settings').flush({ shopName: 'MyShop', appearance });
    fixture.detectChanges();
    expect(workspace.classList.contains('shop')).toBe(false);
    expect(workspace.style.getPropertyValue('--shop-background')).toBe('');
  });

  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: TitleStrategy, useClass: ShopTitleStrategy },
        provideRouter([
          { path: 'winkel', children: [] },
          { path: 'inloggen', children: [], title: 'Inloggen · MyShop' },
        ]),
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => {
    http.match('/api/auth/session').forEach((r) => r.flush({ authenticated: false }));
    http.verify();
  });
  it('retains the latest stored name on stale responses or temporary failures', () => {
    const branding = TestBed.inject(ShopBranding);
    expect(branding.name()).toBe('MyShop');
    branding.refresh();
    const old = http.expectOne('/api/shop/settings');
    branding.refresh();
    http.expectOne('/api/shop/settings').flush({ shopName: 'Autohuis Hans' });
    old.flush({ shopName: 'Old company' });
    expect(branding.name()).toBe('Autohuis Hans');
    branding.refresh();
    http.expectOne('/api/shop/settings').flush({}, { status: 503, statusText: 'Unavailable' });
    expect(branding.name()).toBe('Autohuis Hans');
  });
  it('updates a route title after loading the company name without replacing explicit SEO metadata', async () => {
    const branding = TestBed.inject(ShopBranding);
    await TestBed.inject(Router).navigateByUrl('/inloggen');
    const title = TestBed.inject(Title);
    expect(title.getTitle()).toBe('Inloggen · MyShop');
    branding.refresh();
    http.expectOne('/api/shop/settings').flush({ shopName: 'Company $&' });
    TestBed.tick();
    expect(title.getTitle()).toBe('Inloggen · Company $&');
    title.setTitle('My own SEO title');
    branding.refresh();
    http.expectOne('/api/shop/settings').flush({ shopName: 'Other company' });
    TestBed.tick();
    expect(title.getTitle()).toBe('My own SEO title');
  });
  it('shows the configured name as plain text in the shop header and footer', async () => {
    const fixture = TestBed.createComponent(App);
    http
      .expectOne('/api/shop/settings')
      .flush({ shopName: 'Autohuis <Hans>', footerText: 'Onze occasions wachten op je.' });
    await TestBed.inject(Router).navigateByUrl('/winkel');
    http
      .expectOne('/api/shop/settings')
      .flush({ shopName: 'Autohuis <Hans>', footerText: 'Onze occasions wachten op je.' });
    http.expectOne('/api/shop/payment-options').flush({ checkoutEnabled: true });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.shop-brand').textContent.trim()).toBe('Autohuis <Hans>');
    expect(fixture.nativeElement.querySelector('footer').textContent).toContain('Autohuis <Hans>');
    expect(fixture.nativeElement.querySelector('footer').textContent).toContain(
      'Onze occasions wachten op je.',
    );
    expect(fixture.nativeElement.querySelector('footer a').getAttribute('href')).toBe(
      '/winkel/informatie/bedrijf',
    );
    expect(fixture.nativeElement.querySelector('.shop-brand hans')).toBeNull();
  });
});
