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
  it('uploads an icon with CSRF and preserves unsaved color choices', () => {
    const page = TestBed.createComponent(AppearanceSettingsPage).componentInstance;
    http.expectOne('/api/shop-appearance').flush({ colors: defaultColors, revision: 'revision-1' });
    page.choose('Donker');
    page.iconFile = new File(['png'], 'icon.png', { type: 'image/png' });
    const input = document.createElement('input');
    input.type = 'file';
    page.uploadIcon(input);
    page.uploadIcon(input);
    http.expectOne('/api/auth/csrf').flush(null);
    const upload = http.expectOne('/api/shop-icon');
    const form = upload.request.body as FormData;
    expect(form.get('revision')).toBe('revision-1');
    expect((form.get('file') as File).name).toBe('icon.png');
    upload.flush(null);
    http
      .expectOne('/api/shop/settings')
      .flush({
        shopName: 'MyShop',
        iconUrl: '/api/shop/icon?v=11111111-1111-1111-1111-111111111111',
      });
    http
      .expectOne('/api/shop-appearance')
      .flush({
        colors: defaultColors,
        revision: 'revision-2',
        iconUrl: '/api/shop/icon?v=11111111-1111-1111-1111-111111111111',
      });
    expect(page.colors).toEqual(shopThemes[2].colors);
    expect(page.revision).toBe('revision-2');
    expect(page.iconFile).toBeNull();
    expect(page.iconUrl).toContain('/api/shop/icon');
    page.removeIcon();
    http.expectNone('/api/auth/csrf');
    page.removingIcon.set(true);
    page.removeIcon();
    http.expectOne('/api/auth/csrf').flush(null);
    const removal = http.expectOne('/api/shop-icon');
    expect(removal.request.method).toBe('DELETE');
    expect(removal.request.body).toEqual({ revision: 'revision-2' });
    removal.flush(null);
    http.expectOne('/api/shop/settings').flush({ shopName: 'MyShop', iconUrl: null });
    http
      .expectOne('/api/shop-appearance')
      .flush({ colors: defaultColors, revision: 'revision-3', iconUrl: null });
    expect(page.iconUrl).toBeNull();
    expect(page.colors).toEqual(shopThemes[2].colors);
  });
  it('rejects oversized icon files without starting an upload', () => {
    const page = TestBed.createComponent(AppearanceSettingsPage).componentInstance;
    http.expectOne('/api/shop-appearance').flush({ colors: defaultColors, revision: 'revision-1' });
    const input = document.createElement('input');
    input.type = 'file';
    Object.defineProperty(input, 'files', {
      value: [new File([new Uint8Array(1024 * 1024 + 1)], 'icon.png')],
    });
    page.chooseIcon(input);
    page.uploadIcon(input);
    expect(page.iconFile).toBeNull();
    expect(page.error()).toContain('1 MB');
    http.expectNone('/api/auth/csrf');
  });
  it('resets all colors in the preview and applies them only after saving', () => {
    const fixture = TestBed.createComponent(AppearanceSettingsPage);
    const page = fixture.componentInstance;
    const branding = TestBed.inject(ShopBranding);
    branding.colors.set({ ...shopThemes[2].colors });
    http
      .expectOne('/api/shop-appearance')
      .flush({ colors: shopThemes[2].colors, revision: 'revision-1' });
    fixture.detectChanges();
    const reset = Array.from(
      fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
    ).find((button) => button.textContent?.includes('Originele kleuren herstellen'))!;
    reset.click();
    fixture.detectChanges();
    expect(page.colors).toEqual(defaultColors);
    expect(page.selectedTheme()).toBe('Standaard');
    expect(page.warnings()).toEqual([]);
    expect(branding.colors()).toEqual(shopThemes[2].colors);
    http.expectNone('/api/auth/csrf');
    expect(page.notice()).toContain('Klik op Kleuren opslaan');
    page.save();
    page.resetColors();
    expect(page.busy()).toBe(true);
    http.expectOne('/api/auth/csrf').flush(null);
    const save = http.expectOne('/api/shop-appearance');
    expect(save.request.body).toEqual({ colors: defaultColors, revision: 'revision-1' });
    save.flush(null);
    http.expectOne('/api/shop/settings').flush({ shopName: 'MyShop', appearance: defaultColors });
    http.expectOne('/api/shop-appearance').flush({ colors: defaultColors, revision: 'revision-2' });
    expect(branding.colors()).toEqual(defaultColors);
  });
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
