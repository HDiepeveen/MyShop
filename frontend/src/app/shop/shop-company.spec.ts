import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { ShopCompany } from './shop-company';
import { ShopBranding } from './shop-branding';
describe('Public company page', () => {
  let http: HttpTestingController;
  const company = { heading: 'Over ons en contact', name: 'Autohuis Hans', description: null, address: null, email: null, phone: null, openingHours: null };
  beforeEach(() => { TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])] }); http = TestBed.inject(HttpTestingController); });
  afterEach(() => { http.verify(); document.querySelector('link[rel="canonical"]')?.remove(); document.querySelector('meta[name="description"]')?.remove(); });
  it('renders company contact details as text with a safe email link and SEO metadata', () => {
    TestBed.inject(ShopBranding).name.set('Autohuis Hans');
    const fixture = TestBed.createComponent(ShopCompany);
    http.expectOne('/api/shop/company').flush({ ...company, description: 'Familiebedrijf\nSinds 2001 <script>text</script>', address: 'Straat 1\n1234 AB Utrecht', email: 'contact+shop@example.test', phone: '030 1234567', openingHours: 'Maandag\n09:00 – 18:00' });
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    expect(root.textContent).toContain('Familiebedrijf\nSinds 2001 <script>text</script>');
    expect(root.querySelector('script')).toBeNull();
    expect(root.querySelector('a[href^="mailto:"]')?.getAttribute('href')).toBe('mailto:contact%2Bshop%40example.test');
    expect(TestBed.inject(Title).getTitle()).toBe('Over ons en contact · Autohuis Hans');
    expect(document.querySelector<HTMLLinkElement>('link[rel="canonical"]')?.href).toContain('/winkel/informatie/bedrijf');
  });
  it('recovers from a failed read and hides empty contact fields', () => {
    const fixture = TestBed.createComponent(ShopCompany);
    http.expectOne('/api/shop/company').flush({}, { status: 503, statusText: 'Unavailable' });
    fixture.detectChanges(); expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
    fixture.componentInstance.retry(); http.expectOne('/api/shop/company').flush(company); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Bedrijfsinformatie wordt binnenkort toegevoegd.');
    expect(fixture.nativeElement.querySelector('dt')).toBeNull();
  });
});
