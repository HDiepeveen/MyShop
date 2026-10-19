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
  it('shows ordered text blocks, optional titles and safe multiline descriptions', () => {
    const fixture = TestBed.createComponent(ShopHome);
    http.expectOne('/api/shop/home').flush({ enabled: true, heading: 'Garage abc', items: [
      { title: 'Over Garage abc', description: 'Al 20 jaar.\nReparaties en APK.' },
      { title: null, description: '<img src=x onerror=alert(1)>' },
    ] });
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('h1')?.textContent).toBe('Garage abc');
    expect(root.querySelectorAll('.home-item h2').length).toBe(1);
    expect(root.querySelectorAll('.home-item p')[0].textContent).toContain('Al 20 jaar.\nReparaties en APK.');
    expect(root.querySelector('img')).toBeNull();
    expect(root.textContent).toContain('<img src=x onerror=alert(1)>');
    expect(root.querySelector('.button')?.getAttribute('href')).toBe('/winkel');
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
