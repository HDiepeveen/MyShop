import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { CustomerEmail } from './customer-email';
import { Auth } from '../auth/auth';

describe('Customer email flows', () => {
  let http: HttpTestingController;
  afterEach(() => http.verify());
  function setup(mode: string, parameters: Record<string, string> = {}) {
    const params = new BehaviorSubject(convertToParamMap(parameters));
    const snapshot = { data: { emailMode: mode }, queryParamMap: params.value };
    TestBed.configureTestingModule({
      imports: [CustomerEmail],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ActivatedRoute, useValue: { snapshot, queryParamMap: params } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(CustomerEmail);
    fixture.detectChanges();
    return { fixture, page: fixture.componentInstance, params, snapshot };
  }
  it.each([
    ['forgot', 'request-password-reset'],
    ['resend', 'request-confirmation'],
  ])('requests a %s link once and gives a neutral response', (mode, endpoint) => {
    const { page } = setup(mode);
    page.email = ' customer@example.test ';
    page.submit();
    page.submit();
    http.expectOne('/api/auth/csrf').flush(null);
    const request = http.expectOne('/api/customer/auth/' + endpoint);
    expect(request.request.body).toEqual({ email: 'customer@example.test' });
    request.flush(null);
    expect(page.done()).toBe(true);
    expect(page.message()).toContain('in aanmerking');
    page.submit();
    http.expectNone('/api/auth/csrf');
  });
  it('requires an explicit action for confirmation and rejects missing links', () => {
    const { page } = setup('confirm');
    http.expectNone('/api/auth/csrf');
    page.submit();
    expect(page.failure()).toContain('ongeldig');
    http.expectNone('/api/auth/csrf');
  });
  it('submits the URL-safe confirmation token without modifying it', () => {
    const { page } = setup('confirm', { userId: 'customer', token: 'a_B-c' });
    page.submit();
    http.expectOne('/api/auth/csrf').flush(null);
    const request = http.expectOne('/api/customer/auth/confirm-email');
    expect(request.request.body).toEqual({ userId: 'customer', token: 'a_B-c' });
    request.flush(null);
    expect(page.message()).toContain('bevestigd');
  });
  it('validates repeated passwords and clears secrets after successful reset', () => {
    const { page } = setup('reset', { userId: 'customer', token: 'token' });
    page.password = 'StrongPassword123!';
    page.confirmation = 'different';
    page.submit();
    expect(page.failure()).not.toBe('');
    http.expectNone('/api/auth/csrf');
    page.confirmation = page.password;
    page.submit();
    http.expectOne('/api/auth/csrf').flush(null);
    const request = http.expectOne('/api/customer/auth/reset-password');
    expect(request.request.body.password).toBe('StrongPassword123!');
    request.flush(null);
    expect(page.password).toBe('');
    expect(page.confirmation).toBe('');
    expect(page.message()).toContain('Log opnieuw');
    expect(TestBed.inject(Auth).session()).toBeNull();
  });
  it('keeps retry possible after invalid or expired reset tokens', () => {
    const { page } = setup('reset', { userId: 'customer', token: 'expired' });
    page.password = page.confirmation = 'StrongPassword123!';
    page.submit();
    http.expectOne('/api/auth/csrf').flush(null);
    http
      .expectOne('/api/customer/auth/reset-password')
      .flush(
        { code: 'invalidLink', message: 'De link is ongeldig.' },
        { status: 400, statusText: 'Invalid' },
      );
    expect(page.busy()).toBe(false);
    expect(page.done()).toBe(false);
    expect(page.failure()).toContain('ongeldig');
    expect(page.password).toBe('StrongPassword123!');
  });
  it('ignores an old pending action when another link is opened', () => {
    const { page, params, snapshot } = setup('confirm', { userId: 'old', token: 'old' });
    page.submit();
    http.expectOne('/api/auth/csrf').flush(null);
    const old = http.expectOne('/api/customer/auth/confirm-email');
    snapshot.queryParamMap = convertToParamMap({ userId: 'new', token: 'new' });
    params.next(snapshot.queryParamMap);
    expect(old.cancelled).toBe(true);
    expect(page.busy()).toBe(false);
    expect(page.done()).toBe(false);
    page.submit();
    http.expectOne('/api/auth/csrf').flush(null);
    const current = http.expectOne('/api/customer/auth/confirm-email');
    expect(current.request.body.token).toBe('new');
    current.flush(null);
  });
  it('cancels pending work on leaving the page', () => {
    const { page, fixture } = setup('forgot');
    page.email = 'customer@example.test';
    page.submit();
    const request = http.expectOne('/api/auth/csrf');
    fixture.destroy();
    expect(request.cancelled).toBe(true);
  });
});
