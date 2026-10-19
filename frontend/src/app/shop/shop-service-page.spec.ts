import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from '../app.routes';

describe('Public home and service routes', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter(routes)] });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => { http.verify(); document.querySelector('link[rel="canonical"]')?.remove(); document.querySelector('meta[name="description"]')?.remove(); document.querySelector('meta[name="robots"]')?.remove(); });
  it('opens the enabled home at the base URL without requesting an admin session', async () => {
    const harness = await RouterTestingHarness.create();
    const navigation = harness.navigateByUrl('/');
    await vi.waitFor(() => http.expectOne('/api/shop/settings').flush({ homeEnabled: true }));
    await navigation;
    http.expectOne('/api/shop/home').flush({ enabled: true, heading: 'Garage abc', items: [{ title: 'APK', place: 'Zwolle', webAddress: 'apk-zwolle', description: 'Een veilige auto.' }] });
    harness.detectChanges();
    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe('Garage abc');
    expect(harness.routeNativeElement?.querySelector('.home-item a')).toBeNull();
    expect(harness.routeNativeElement?.textContent).not.toContain('Lees meer');
    http.expectNone('/api/auth/session');
  });
  it('opens root-level service pages and safely renders their content', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/apk-zwolle');
    http.expectOne('/api/shop/pages/apk-zwolle').flush({ title: 'APK', place: 'Zwolle', webAddress: 'apk-zwolle', description: '<script>geen HTML</script>\nKeuring op afspraak.' });
    harness.detectChanges();
    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe('APK');
    expect(harness.routeNativeElement?.querySelector('script')).toBeNull();
    expect(harness.routeNativeElement?.textContent).toContain('<script>geen HTML</script>');
    await harness.navigateByUrl('/autoreparatie-zwolle');
    http.expectOne('/api/shop/pages/autoreparatie-zwolle').flush({}, { status: 404, statusText: 'Not found' });
    harness.detectChanges();
    expect(harness.routeNativeElement?.textContent).toContain('Pagina niet beschikbaar');
    expect(document.querySelector('meta[name="robots"]')?.getAttribute('content')).toBe('noindex');
  });
});
