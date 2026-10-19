import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { ShopHome } from './shop-home';

describe('Optional home page', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])] });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => { http.verify(); document.querySelector('link[rel="canonical"]')?.remove(); document.querySelector('meta[name="description"]')?.remove(); });
  it('shows only the independent home text and preserves safe multiline content', () => {
    const fixture = TestBed.createComponent(ShopHome);
    http.expectOne('/api/shop/home').flush({ enabled: true, heading: 'Garage abc', welcomeText: 'Welkom bij onze zaak', description: 'Welkom!\n<img src=x onerror=alert(1)>', items: [
      { title: 'Over Garage abc', description: 'Al 20 jaar.\nReparaties en APK.' },
      { title: null, description: '<img src=x onerror=alert(1)>' },
    ] });
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('h1')?.textContent).toBe('Welkom bij onze zaak');
    expect(root.querySelector('.home-item')).toBeNull();
    expect(root.textContent).not.toContain('Al 20 jaar.');
    expect(root.querySelector('.home-text p')?.textContent).toBe('Welkom!\n<img src=x onerror=alert(1)>');
    expect(root.querySelector('img')).toBeNull();
    expect(root.textContent).toContain('<img src=x onerror=alert(1)>');
    expect(root.querySelector('.button')?.getAttribute('href')).toBe('/winkel');
  });
  it('shows the configured hero background and recovers gracefully when the image cannot load', () => {
    const fixture = TestBed.createComponent(ShopHome);
    http.expectOne('/api/shop/home').flush({ enabled: true, heading: 'Welkom', description: 'Ons bedrijf', imageUrl: '/api/shop/home-image?v=123', items: [] });
    fixture.detectChanges();
    const image = fixture.nativeElement.querySelector('.home-hero img') as HTMLImageElement;
    expect(image.getAttribute('src')).toBe('/api/shop/home-image?v=123');
    expect(image.alt).toBe('');
    expect(fixture.nativeElement.querySelector('.home-hero.with-image')).toBeTruthy();
    image.dispatchEvent(new Event('error')); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.home-image')).toBeNull();
    expect(fixture.nativeElement.querySelector('h1').textContent).toBe('Welkom');
    expect(fixture.nativeElement.querySelector('.home-text').textContent).toBe('Ons bedrijf');
  });
  it('uses custom home metadata while preserving the visible welcome', () => {
    const fixture = TestBed.createComponent(ShopHome);
    http.expectOne('/api/shop/home').flush({ enabled: true, heading: 'Home', welcomeText: 'Welkom bezoeker', description: 'Zichtbare inhoud', seoTitle: 'Garage Zwolle | Ganpat', seoDescription: 'Een eigen SEO-omschrijving.', items: [] });
    fixture.detectChanges();
    expect(document.title).toBe('Garage Zwolle | Ganpat');
    expect(document.querySelector('meta[name="description"]')?.getAttribute('content')).toBe('Een eigen SEO-omschrijving.');
    expect(fixture.nativeElement.querySelector('h1').textContent).toBe('Welkom bezoeker');
    expect(fixture.nativeElement.textContent).toContain('Zichtbare inhoud');
  });
  it('does not show draft content when disabled and can retry', () => {
    const fixture = TestBed.createComponent(ShopHome);
    http.expectOne('/api/shop/home').flush({}, { status: 404, statusText: 'Not found' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Homepagina niet beschikbaar');
    fixture.componentInstance.retry();
    http.expectOne('/api/shop/home').flush({ enabled: true, heading: 'Welkom terug', items: [] });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Welkom terug');
  });
});
