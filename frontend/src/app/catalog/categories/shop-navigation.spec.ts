import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ShopNavigation } from './shop-navigation';
import { ShopBranding } from '../../shop/shop-branding';
describe('ShopNavigation', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ShopNavigation],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  it('saves visibility with its revision and updates the header immediately', () => {
    const editor = TestBed.createComponent(ShopNavigation).componentInstance;
    editor.load();
    http
      .expectOne('/api/categories/navigation-settings')
      .flush({ showAssortmentInMenu: true, revision: 'r1' });
    editor.showAssortmentInMenu = false;
    editor.save();
    editor.save();
    const request = http.expectOne('/api/categories/navigation-settings');
    expect(request.request.body).toEqual({ showAssortmentInMenu: false, revision: 'r1' });
    request.flush(null);
    http.expectOne('/api/shop/settings').flush({ showAssortmentInMenu: false });
    http
      .expectOne('/api/categories/navigation-settings')
      .flush({ showAssortmentInMenu: false, revision: 'r2' });
    expect(TestBed.inject(ShopBranding).showAssortmentInMenu()).toBe(false);
    expect(editor.saved()).toBe(true);
    expect(editor.settings()?.revision).toBe('r2');
  });
  it('shows save conflicts and allows reloading the current setting', () => {
    const editor = TestBed.createComponent(ShopNavigation).componentInstance;
    editor.load();
    http
      .expectOne('/api/categories/navigation-settings')
      .flush({ showAssortmentInMenu: true, revision: 'r1' });
    editor.save();
    http
      .expectOne('/api/categories/navigation-settings')
      .flush({}, { status: 409, statusText: 'Conflict' });
    expect(editor.error()).not.toBe('');
    expect(editor.busy()).toBe(false);
    editor.load();
    http
      .expectOne('/api/categories/navigation-settings')
      .flush({ showAssortmentInMenu: false, revision: 'r2' });
    expect(editor.showAssortmentInMenu).toBe(false);
    expect(editor.error()).toBe('');
  });
});
