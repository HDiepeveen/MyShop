import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AppearanceSettingsPage } from './appearance-settings';
import { ShopBranding } from '../shop/shop-branding';
import { defaultColors, shopThemes } from './shop-theme';
describe('Appearance settings', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [AppearanceSettingsPage],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  it('previews without applying and saves once with CSRF and the current revision', () => {
    const fixture = TestBed.createComponent(AppearanceSettingsPage);
    const page = fixture.componentInstance;
    http.expectOne('/api/shop-appearance').flush({ colors: defaultColors, revision: 'revision-1' });
    page.choose('Donker');
    fixture.detectChanges();
    expect(
      fixture.nativeElement
        .querySelector('[aria-label="Voorbeeld kleurenschema"]')
        .style.getPropertyValue('--shop-background'),
    ).toBe('#111827');
    expect(TestBed.inject(ShopBranding).colors()).toEqual(defaultColors);
    page.save();
    page.save();
    http.expectOne('/api/auth/csrf').flush(null);
    const save = http.expectOne('/api/shop-appearance');
    expect(save.request.method).toBe('PUT');
    expect(save.request.body).toEqual({ colors: shopThemes[2].colors, revision: 'revision-1' });
    save.flush(null);
    http
      .expectOne('/api/shop/settings')
      .flush({ shopName: 'MyShop', appearance: shopThemes[2].colors });
    http
      .expectOne('/api/shop-appearance')
      .flush({ colors: shopThemes[2].colors, revision: 'revision-2' });
    expect(page.revision).toBe('revision-2');
    expect(TestBed.inject(ShopBranding).colors()).toEqual(shopThemes[2].colors);
  });
  it('retains custom colors after a save conflict', () => {
    const page = TestBed.createComponent(AppearanceSettingsPage).componentInstance;
    http.expectOne('/api/shop-appearance').flush({ colors: defaultColors, revision: 'revision-1' });
    page.choose('Blauw');
    page.save();
    http.expectOne('/api/auth/csrf').flush(null);
    http
      .expectOne('/api/shop-appearance')
      .flush({ code: 'concurrency' }, { status: 409, statusText: 'Conflict' });
    expect(page.colors).toEqual(shopThemes[3].colors);
    expect(page.revision).toBe('revision-1');
    expect(page.busy()).toBe(false);
    expect(page.error()).toBeTruthy();
    expect(TestBed.inject(ShopBranding).colors()).toEqual(defaultColors);
  });
});
