import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { Subject } from 'rxjs';
import { AdminSession, Auth } from '../auth/auth';
import { Password } from '../auth/password';
import { CustomerLogin } from './customer-login';
import { CustomerRegister } from './customer-register';

const session: AdminSession = {
  authenticated: true,
  customer: true,
  administrator: false,
  name: 'Ada',
};

describe('Customer authentication recovery', () => {
  afterEach(() => vi.restoreAllMocks());

  function setup(register = false, reason = '') {
    const response = new Subject<AdminSession>();
    const auth = { customerLogin: vi.fn(() => response), registerCustomer: vi.fn(() => response) };
    TestBed.configureTestingModule({
      imports: [CustomerLogin, CustomerRegister],
      providers: [
        provideRouter([]),
        { provide: Auth, useValue: auth },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: convertToParamMap({ reason }) } },
        },
      ],
    });
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    const fixture = register
      ? TestBed.createComponent(CustomerRegister)
      : TestBed.createComponent(CustomerLogin);
    const page = fixture.componentInstance;
    page.email = 'ada@example.test';
    page.password = 'Valid-Password-123!';
    if (page instanceof CustomerRegister) page.confirmation = page.password;
    return { fixture, page, response, auth, navigate };
  }

  for (const register of [false, true]) {
    const name = register ? 'registration' : 'login';
    it(`stops ${name} requests and navigation after leaving the page`, () => {
      const { fixture, page, response, navigate } = setup(register);
      page.submit();
      fixture.destroy();
      expect(response.observed).toBe(false);
      response.next(session);
      expect(navigate).not.toHaveBeenCalled();
    });

    it(`disables ${name} inputs during the request and restores them after failure`, async () => {
      const { fixture, page, response } = setup(register);
      fixture.detectChanges();
      await fixture.whenStable();
      page.submit();
      fixture.detectChanges();
      await fixture.whenStable();
      const inputs = Array.from(
        fixture.nativeElement.querySelectorAll('input'),
      ) as HTMLInputElement[];
      expect(inputs.length).toBe(register ? 3 : 2);
      expect(inputs.every((input) => input.disabled)).toBe(true);
      response.error(new Error('Offline'));
      fixture.detectChanges();
      await fixture.whenStable();
      expect(inputs.every((input) => !input.disabled)).toBe(true);
      expect(page.busy()).toBe(false);
    });

    it(`clears ${name} passwords after success`, () => {
      const { page, response, navigate } = setup(register);
      page.submit();
      response.next(session);
      expect(page.password).toBe('');
      if (page instanceof CustomerRegister) expect(page.confirmation).toBe('');
      expect(navigate).toHaveBeenCalledWith(['/winkel/account']);
    });
  }

  for (const register of [false, true]) {
    it.each(['blank email', 'long email', 'long password'])(
      'rejects invalid ' + (register ? 'registration ' : 'login ') + '%s',
      (invalid) => {
        const { fixture, page, auth } = setup(register);
        if (invalid === 'blank email') page.email = '   ';
        else if (invalid === 'long email') page.email = 'x'.repeat(321);
        else {
          page.password = 'x'.repeat(129);
          if (page instanceof CustomerRegister) page.confirmation = page.password;
        }
        fixture.detectChanges();
        expect((fixture.nativeElement.querySelector('button') as HTMLButtonElement).disabled).toBe(
          true,
        );
        page.submit();
        expect(auth.customerLogin).not.toHaveBeenCalled();
        expect(auth.registerCustomer).not.toHaveBeenCalled();
        expect(page.busy()).toBe(false);
      },
    );
    it('trims email but preserves password for ' + (register ? 'registration' : 'login'), () => {
      const { page, auth } = setup(register);
      page.email = '  ada@example.test  ';
      page.password = ' Valid-Password-123! ';
      if (page instanceof CustomerRegister) page.confirmation = page.password;
      page.submit();
      expect(register ? auth.registerCustomer : auth.customerLogin).toHaveBeenCalledWith(
        'ada@example.test',
        ' Valid-Password-123! ',
      );
    });
  }
  it.each([0, 11])('rejects a registration password of %s characters', (length) => {
    const { page, auth } = setup(true);
    const register = page as CustomerRegister;
    register.password = register.confirmation = 'x'.repeat(length);
    register.submit();
    expect(register.failure()).toContain('12 tot');
    expect(auth.registerCustomer).not.toHaveBeenCalled();
    expect(register.busy()).toBe(false);
  });
  it('accepts registration input at the length limits', () => {
    const { page, auth } = setup(true);
    const register = page as CustomerRegister;
    register.email = 'x'.repeat(308) + '@example.com';
    register.password = register.confirmation = 'x'.repeat(128);
    register.submit();
    expect(auth.registerCustomer).toHaveBeenCalledOnce();
  });
  it('does not revalidate or show a mismatch during pending registration', () => {
    const { page, auth } = setup(true);
    const register = page as CustomerRegister;
    register.submit();
    register.confirmation = 'changed';
    register.submit();
    expect(register.failure()).toBe('');
    expect(auth.registerCustomer).toHaveBeenCalledTimes(1);
  });

  it('does not send login requests with blank credentials', () => {
    const { page, auth } = setup();
    page.email = '   ';
    page.submit();
    page.email = 'ada@example.test';
    page.password = '';
    page.submit();
    expect(auth.customerLogin).not.toHaveBeenCalled();
    expect(page.busy()).toBe(false);
  });

  it.each([
    ['changed', 'Je wachtwoord is gewijzigd. Log opnieuw in.'],
    ['expired', 'Je sessie is verlopen. Log opnieuw in.'],
  ])('explains the %s login reason', (reason, message) => {
    const { fixture } = setup(false, reason);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(message);
    expect(fixture.nativeElement.querySelector('[role="status"]')).not.toBeNull();
  });

  it('blocks overlong current and new passwords before sending a change request', () => {
    const auth = { changePassword: vi.fn(() => new Subject<void>()) };
    TestBed.configureTestingModule({
      imports: [Password],
      providers: [provideRouter([]), { provide: Auth, useValue: auth }],
    });
    const fixture = TestBed.createComponent(Password);
    const page = fixture.componentInstance;
    page.current = 'x'.repeat(129);
    page.password = page.confirm = 'Valid-Password-123!';
    page.submit();
    fixture.detectChanges();
    expect((fixture.nativeElement.querySelector('button') as HTMLButtonElement).disabled).toBe(
      true,
    );
    page.current = 'Current-123!';
    page.password = page.confirm = 'x'.repeat(129);
    page.submit();
    fixture.detectChanges();
    expect((fixture.nativeElement.querySelector('button') as HTMLButtonElement).disabled).toBe(
      true,
    );
    expect(auth.changePassword).not.toHaveBeenCalled();
  });
});
