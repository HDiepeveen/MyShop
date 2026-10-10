import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ShopTitleStrategy } from './shop-title-strategy';
import { Title } from '@angular/platform-browser';
import { TitleStrategy, provideRouter, Router } from '@angular/router';
import { ShopBranding } from './shop-branding';
import { App } from '../app';
describe('Webshop name', () => {
  let http: HttpTestingController;
  beforeEach(() => { TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(), { provide: TitleStrategy, useClass: ShopTitleStrategy }, provideRouter([{ path: 'winkel', children: [] }, { path: 'inloggen', children: [], title: 'Inloggen · MyShop' }])] }); http = TestBed.inject(HttpTestingController); });
  afterEach(() => { http.match('/api/auth/session').forEach(r => r.flush({ authenticated: false })); http.verify(); });
  it('retains the latest stored name on stale responses or temporary failures', () => {
    const branding = TestBed.inject(ShopBranding);
    expect(branding.name()).toBe('MyShop');
    branding.refresh(); const old = http.expectOne('/api/shop/settings');
    branding.refresh(); http.expectOne('/api/shop/settings').flush({ shopName: 'Autohuis Hans' });
    old.flush({ shopName: 'Old company' }); expect(branding.name()).toBe('Autohuis Hans');
    branding.refresh(); http.expectOne('/api/shop/settings').flush({}, { status: 503, statusText: 'Unavailable' });
    expect(branding.name()).toBe('Autohuis Hans');
  });
  it('updates a route title after loading the company name without replacing explicit SEO metadata', async () => {
    const branding = TestBed.inject(ShopBranding);
    await TestBed.inject(Router).navigateByUrl('/inloggen');
    const title = TestBed.inject(Title);
    expect(title.getTitle()).toBe('Inloggen · MyShop');
    branding.refresh(); http.expectOne('/api/shop/settings').flush({ shopName: 'Company $&' });
    TestBed.tick();
    expect(title.getTitle()).toBe('Inloggen · Company $&');
    title.setTitle('My own SEO title');
    branding.refresh(); http.expectOne('/api/shop/settings').flush({ shopName: 'Other company' });
    TestBed.tick();
    expect(title.getTitle()).toBe('My own SEO title');
  });
  it('shows the configured name as plain text in the shop header and footer', async () => {
    const fixture = TestBed.createComponent(App);
    http.expectOne('/api/shop/settings').flush({ shopName: 'Autohuis <Hans>' });
    await TestBed.inject(Router).navigateByUrl('/winkel');
    http.expectOne('/api/shop/settings').flush({ shopName: 'Autohuis <Hans>' });
    http.expectOne('/api/shop/payment-options').flush({ checkoutEnabled: true });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.shop-brand').textContent).toBe('Autohuis <Hans>');
    expect(fixture.nativeElement.querySelector('footer').textContent).toContain('Autohuis <Hans>');
    expect(fixture.nativeElement.querySelector('.shop-brand hans')).toBeNull();
  });
});
