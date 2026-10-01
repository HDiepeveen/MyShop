import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { Auth } from './auth';
import { adminGuard, authErrors, customerGuard, safeReturnUrl } from './auth-routing';
import { Login } from './login';
import { Password } from './password';

@Component({ template: 'Protected' })
class Protected {}
@Component({ template: 'Forbidden' })
class Forbidden {}

describe('Admin authentication', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authErrors])),
        provideHttpClientTesting(),
        provideRouter([
          { path: 'inloggen', component: Login },
          { path: 'geen-toegang', component: Forbidden },
          { path: 'producten', component: Protected, canActivate: [adminGuard] },
          { path: 'winkel/account', component: Protected, canActivate: [customerGuard] },
          { path: 'account', component: Password },
        ]),
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => {
    http.verify();
    document.cookie = 'XSRF-TOKEN=; Max-Age=0; path=/';
  });

  it('redirects a visitor to login preserving the requested page and filters', async () => {
    const harness = await RouterTestingHarness.create();
    const navigation = harness.navigateByUrl('/producten?search=coat&offset=20');
    await new Promise((resolve) => setTimeout(resolve, 0));
    http
      .expectOne('/api/auth/session')
      .flush({ authenticated: false, administrator: false, name: null });
    await navigation;
    expect(
      TestBed.inject(Router).parseUrl(TestBed.inject(Router).url).queryParams['returnUrl'],
    ).toBe('/producten?search=coat&offset=20');
    expect(harness.routeNativeElement!.textContent).toContain('Inloggen');
  });

  it('rejects an authenticated account without the administrator role', async () => {
    const harness = await RouterTestingHarness.create();
    const navigation = harness.navigateByUrl('/producten');
    await new Promise((resolve) => setTimeout(resolve, 0));
    http
      .expectOne('/api/auth/session')
      .flush({ authenticated: true, administrator: false, name: 'reader' });
    await navigation;
    expect(TestBed.inject(Router).url).toBe('/geen-toegang');
  });

  it('prepares CSRF protection before entering an administrator route', async () => {
    const harness = await RouterTestingHarness.create();
    const navigation = harness.navigateByUrl('/producten');
    await new Promise((resolve) => setTimeout(resolve, 0));
    http
      .expectOne('/api/auth/session')
      .flush({ authenticated: true, administrator: true, name: 'admin' });
    http.expectOne('/api/auth/csrf').flush(null);
    await navigation;
    expect(harness.routeNativeElement!.textContent).toBe('Protected');
  });

  it('logs in once, rotates the CSRF token and loads the authenticated session', () => {
    const fixture = TestBed.createComponent(Login);
    const login = fixture.componentInstance;
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    login.userName = ' admin ';
    login.password = 'secret';
    login.submit();
    login.submit();
    document.cookie = 'XSRF-TOKEN=anonymous; path=/';
    http.expectOne('/api/auth/csrf').flush(null);
    const request = http.expectOne('/api/auth/login');
    expect(request.request.body).toEqual({ userName: 'admin', password: 'secret' });
    expect(request.request.headers.get('X-XSRF-TOKEN')).toBe('anonymous');
    request.flush(null);
    http.expectOne('/api/auth/csrf').flush(null);
    http
      .expectOne('/api/auth/session')
      .flush({ authenticated: true, administrator: true, name: 'admin' });
    expect(login.password).toBe('');
    expect(navigate).toHaveBeenCalledWith('/');
    expect(TestBed.inject(Auth).session()?.name).toBe('admin');
  });

  it('logs a customer in and permits the protected customer account route', async () => {
    const auth = TestBed.inject(Auth);
    auth.customerLogin('customer@example.com', 'secret').subscribe();
    http.expectOne('/api/auth/csrf').flush(null);
    const customerLogin = http.expectOne('/api/customer/auth/login');
    expect(customerLogin.request.body).toEqual({
      email: 'customer@example.com',
      password: 'secret',
    });
    customerLogin.flush(null);
    http.expectOne('/api/auth/csrf').flush(null);
    http.expectOne('/api/auth/session').flush({
      authenticated: true,
      administrator: false,
      customer: true,
      name: 'customer@example.com',
    });
    const harness = await RouterTestingHarness.create();
    const navigation = harness.navigateByUrl('/winkel/account');
    await new Promise((resolve) => setTimeout(resolve, 0));
    http.expectOne('/api/auth/session').flush({
      authenticated: true,
      administrator: false,
      customer: true,
      name: 'customer@example.com',
    });
    http.expectOne('/api/auth/csrf').flush(null);
    await navigation;
    expect(harness.routeNativeElement!.textContent).toBe('Protected');
  });

  it('shows a generic login failure and clears the password without retrying', () => {
    const fixture = TestBed.createComponent(Login);
    const login = fixture.componentInstance;
    login.userName = 'admin';
    login.password = 'wrong';
    login.submit();
    http.expectOne('/api/auth/csrf').flush(null);
    http.expectOne('/api/auth/login').flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(login.password).toBe('');
    expect(login.busy()).toBe(false);
    expect(login.error()).toContain('Inloggen mislukt');
    expect(TestBed.inject(Router).url).toBe('/');
  });

  it('changes the password and requires a new login', () => {
    const fixture = TestBed.createComponent(Password);
    const page = fixture.componentInstance;
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    page.current = 'old';
    page.password = page.confirm = 'New-Password42!';
    page.submit();
    page.submit();
    http.expectOne('/api/auth/csrf').flush(null);
    const request = http.expectOne('/api/auth/password');
    expect(request.request.body).toEqual({
      currentPassword: 'old',
      newPassword: 'New-Password42!',
    });
    request.flush(null);
    expect(page.password).toBe('');
    expect(navigate).toHaveBeenCalledWith(['/inloggen'], { queryParams: { reason: 'changed' } });
  });

  it('does not send mismatching new passwords', () => {
    const page = TestBed.createComponent(Password).componentInstance;
    page.current = 'old';
    page.password = 'New-Password42!';
    page.confirm = 'different';
    page.submit();
    http.expectNone('/api/auth/csrf');
  });

  it.each([401, 403])('redirects rejected catalog writes without replaying them (%s)', (status) => {
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    TestBed.inject(HttpClient)
      .post('/api/categories', { name: 'New' })
      .subscribe({ error: () => {} });
    http.expectOne('/api/categories').flush({}, { status, statusText: 'Denied' });
    expect(navigate).toHaveBeenCalled();
    expect(navigate.mock.calls[0][0]).toEqual([status === 401 ? '/inloggen' : '/geen-toegang']);
    http.expectNone('/api/categories');
  });

  it('clears local session state when logout confirms the session has already expired', () => {
    const auth = TestBed.inject(Auth);
    auth.session.set({ authenticated: true, administrator: true, customer: false, name: 'admin' });
    let completed = false;
    auth.logout().subscribe(() => (completed = true));
    http.expectOne('/api/auth/csrf').flush(null);
    http.expectOne('/api/auth/logout').flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(completed).toBe(true);
    expect(auth.session()).toBeNull();
  });

  it('does not consider a failed logout a completed logout', () => {
    const auth = TestBed.inject(Auth);
    auth.session.set({ authenticated: true, administrator: true, customer: false, name: 'admin' });
    auth.logout().subscribe({ error: () => {} });
    http.expectOne('/api/auth/csrf').flush(null);
    http.expectOne('/api/auth/logout').flush({}, { status: 503, statusText: 'Unavailable' });
    expect(auth.session()?.authenticated).toBe(true);
  });
});

it.each(['https://evil.test', '//evil.test', '/\\evil.test', '/inloggen', '/producten\n', null])(
  'rejects unsafe return path %s',
  (value) => {
    expect(safeReturnUrl(value)).toBe('/');
  },
);
it('preserves a local product detail return path', () => {
  expect(safeReturnUrl('/producten/123?search=shirt&offset=20')).toBe(
    '/producten/123?search=shirt&offset=20',
  );
});
it('allows the protected payment settings return path', () => {
  expect(safeReturnUrl('/instellingen/betalen')).toBe('/instellingen/betalen');
});
it('allows a protected order detail return path', () => {
  expect(safeReturnUrl('/bestellingen/123?offset=20')).toBe('/bestellingen/123?offset=20');
  expect(safeReturnUrl('/klanten/123')).toBe('/klanten/123');
});
