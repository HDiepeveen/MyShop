import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { SeoSettings } from './seo-settings';
import { ProductSeoEdit } from './product-seo-edit';
const revision = '10000000-0000-0000-0000-000000000001';
const info = { productId: 'p', name: 'Opel Corsa', revision, values: { seoTitle: null, seoDescription: null, webAddress: null }, resolvedTitle: 'Opel Corsa · MyShop', resolvedDescription: 'Een auto.', resolvedAddress: 'opel-corsa-p' };
describe('SEO editors', () => {
  let http: HttpTestingController;
  beforeEach(() => { TestBed.configureTestingModule({ imports: [SeoSettings, ProductSeoEdit], providers: [provideHttpClient(), provideHttpClientTesting()] }); http = TestBed.inject(HttpTestingController); });
  afterEach(() => http.verify());
  it('saves the assortment heading and title with revision and reloads the stored settings', () => {
    const fixture = TestBed.createComponent(SeoSettings);
    http.expectOne('/api/seo-settings').flush({ heading: 'Assortiment', seoTitle: 'MyShop', revision });
    const page = fixture.componentInstance;
    page.heading = 'Auto’s te koop'; page.seoTitle = 'Auto kopen | MyShop';
    page.shopName = 'Autohuis Hans'; page.welcomeText = 'Welkom bij Hans'; page.introduction = 'Onze occasions.'; page.save(); page.save();
    const request = http.expectOne('/api/seo-settings');
    expect(request.request.body).toEqual({ heading: 'Auto’s te koop', seoTitle: 'Auto kopen | MyShop', revision, shopName: 'Autohuis Hans', welcomeText: 'Welkom bij Hans', introduction: 'Onze occasions.', footerText: 'Ontdek wat bij je past.', company: { heading: 'Over ons en contact', name: null, description: null, address: null, email: null, phone: null, openingHours: null } });
    request.flush(null);
    http.expectOne('/api/seo-settings').flush({ heading: page.heading, seoTitle: page.seoTitle, revision: 'new' });
    expect(page.settings()?.revision).toBe('new');
    expect(page.busy()).toBe(false);
  });
  it('keeps an edited heading when saving fails', () => {
    const fixture = TestBed.createComponent(SeoSettings);
    http.expectOne('/api/seo-settings').flush({ heading: 'Assortiment', seoTitle: 'MyShop', revision });
    const page = fixture.componentInstance; page.heading = 'Mijn ontwerp'; page.shopName = 'Mijn bedrijf'; page.welcomeText = 'Mijn welkom'; page.introduction = 'Mijn introductie'; page.save();
    http.expectOne('/api/seo-settings').flush({}, { status: 409, statusText: 'Conflict' });
    expect(page.shopName).toBe('Mijn bedrijf'); expect(page.welcomeText).toBe('Mijn welkom'); expect(page.introduction).toBe('Mijn introductie');
    expect(page.heading).toBe('Mijn ontwerp'); expect(page.error()).toBeTruthy(); expect(page.busy()).toBe(false);
  });
  it('saves company information and an empty footer text without losing drafts on conflicts', () => {
    const fixture = TestBed.createComponent(SeoSettings);
    http.expectOne('/api/seo-settings').flush({ heading: 'Assortiment', seoTitle: 'MyShop', revision });
    const page = fixture.componentInstance; page.footerText = '';
    page.company = { heading: 'Ons bedrijf', name: 'Hans', description: 'Eerste regel\nTweede regel', address: 'Straat 1', email: 'contact@example.test', phone: '030 1234567', openingHours: 'Maandag' };
    page.save(); const request = http.expectOne('/api/seo-settings');
    expect(request.request.body.company).toEqual(page.company); expect(request.request.body.footerText).toBe('');
    request.flush({}, { status: 409, statusText: 'Conflict' });
    expect(page.company.description).toBe('Eerste regel\nTweede regel'); expect(page.footerText).toBe('');
  });
  function product() {
    const fixture = TestBed.createComponent(ProductSeoEdit); fixture.componentRef.setInput('productId', 'p'); fixture.detectChanges();
    http.expectNone('/api/products/p/seo');
    const details = fixture.nativeElement.querySelector('details') as HTMLDetailsElement;
    details.open = true; details.dispatchEvent(new Event('toggle'));
    http.expectOne('/api/products/p/seo').flush(info); fixture.detectChanges();
    return fixture;
  }
  it('loads product SEO on opening and saves blank optional fields as null', () => {
    const fixture = product(); const page = fixture.componentInstance;
    expect(fixture.nativeElement.textContent).toContain('/winkel/opel-corsa-p');
    page.save();
    const request = http.expectOne('/api/products/p/seo'); expect(request.request.body).toEqual({ seoTitle: null, seoDescription: null, webAddress: null, revision });
    request.flush(null); http.expectOne('/api/products/p/seo').flush(info); expect(page.notice()).toContain('opgeslagen');
  });
  it('shows address conflicts clearly without discarding the draft', () => {
    const fixture = product(); const page = fixture.componentInstance; page.webAddress = 'opel-corsa'; page.save();
    http.expectOne('/api/products/p/seo').flush({ code: 'webAddressInUse' }, { status: 409, statusText: 'Conflict' });
    expect(page.webAddress).toBe('opel-corsa'); expect(page.error()).toContain('ander product'); expect(page.busy()).toBe(false);
  });
  it('does not apply a late product response after switching products', () => {
    const fixture = TestBed.createComponent(ProductSeoEdit); fixture.componentRef.setInput('productId', 'p'); fixture.detectChanges();
    fixture.componentInstance.load(); const old = http.expectOne('/api/products/p/seo');
    fixture.componentRef.setInput('productId', 'other'); fixture.detectChanges(); old.flush(info);
    expect(fixture.componentInstance.info()).toBeNull(); expect(fixture.componentInstance.loading()).toBe(false);
  });
});
