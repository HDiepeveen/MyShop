import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { CustomerLogin } from './customer-login';
import { CustomerRegister } from './customer-register';

describe('Customer authentication screens', () => {
  let http: HttpTestingController;

  afterEach(() => http.verify());

  it('logs a customer in and opens the account page', () => {
    TestBed.configureTestingModule({
      imports: [CustomerLogin],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    const fixture = TestBed.createComponent(CustomerLogin);
    const page = fixture.componentInstance;

    page.email = 'customer@example.test';
    page.password = 'Secret-123!';
    page.submit();
    page.submit();

    http.expectOne('/api/auth/csrf').flush(null);
    const login = http.expectOne('/api/customer/auth/login');
    expect(login.request.body).toEqual({
      email: 'customer@example.test',
      password: 'Secret-123!',
    });
    login.flush(null);
    http.expectOne('/api/auth/csrf').flush(null);
    http.expectOne('/api/auth/session').flush({
      authenticated: true,
      administrator: false,
      customer: true,
      name: 'customer@example.test',
    });

    expect(page.busy()).toBe(false);
    expect(navigate).toHaveBeenCalledWith(['/winkel/account']);
  });

  it('does not register when the passwords differ', () => {
    TestBed.configureTestingModule({
      imports: [CustomerRegister],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
    const page = TestBed.createComponent(CustomerRegister).componentInstance;

    page.email = 'customer@example.test';
    page.password = 'Secret-123!';
    page.confirmation = 'Different-123!';
    page.submit();

    expect(page.failure()).toBe('De wachtwoorden zijn niet gelijk.');
    http.expectNone('/api/auth/csrf');
  });

  it('explains an existing customer account instead of showing a generic save conflict', () => {
    TestBed.configureTestingModule({
      imports: [CustomerRegister],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(CustomerRegister);
    const page = fixture.componentInstance;
    page.email = 'customer@example.test';
    page.password = 'Secret-12345!';
    page.confirmation = page.password;
    page.submit();
    http.expectOne('/api/auth/csrf').flush(null);
    http.expectOne('/api/customer/auth/register').flush({ code: 'accountExists' }, {
      status: 409, statusText: 'Conflict',
    });
    fixture.detectChanges();
    expect(page.failure()).toContain('bestaat al een klantaccount');
    expect(page.failure()).toContain('Log in of herstel je wachtwoord');
    expect(fixture.nativeElement.querySelector('a[href="/winkel/inloggen"]')).not.toBeNull();
    expect(page.busy()).toBe(false);
  });

  it('registers a customer and opens the account page', () => {
    TestBed.configureTestingModule({
      imports: [CustomerRegister],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    const page = TestBed.createComponent(CustomerRegister).componentInstance;

    page.email = 'customer@example.test';
    page.password = 'Secret-12345!';
    page.confirmation = 'Secret-12345!';
    page.submit();
    page.submit();

    http.expectOne('/api/auth/csrf').flush(null);
    const registration = http.expectOne('/api/customer/auth/register');
    expect(registration.request.body).toEqual({
      email: 'customer@example.test',
      password: 'Secret-12345!',
    });
    registration.flush(null);
    http.expectOne('/api/auth/csrf').flush(null);
    http.expectOne('/api/auth/session').flush({
      authenticated: true,
      administrator: false,
      customer: true,
      name: 'customer@example.test',
    });

    expect(page.busy()).toBe(false);
    expect(navigate).toHaveBeenCalledWith(['/winkel/account']);
  });
});
