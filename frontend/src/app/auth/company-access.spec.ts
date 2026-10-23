import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { CompanyAccessPage, CompanyAccessData } from './company-access';

const data: CompanyAccessData = {
  permissions: [{ code: 'orders', name: 'Bestellingen' }, { code: 'products', name: 'Producten' }],
  roles: [{ id: 'r1', name: 'Orderbeheer', revision: 'revision-1', permissions: ['orders'] }],
  accounts: [{ id: 'u1', userName: 'employee', email: 'staff@example.test', revision: 'user-revision', enabled: true, roles: ['r1'] }],
};
describe('Company account and role configuration', () => {
  let http: HttpTestingController;
  function setup() {
    TestBed.configureTestingModule({ imports: [CompanyAccessPage], providers: [provideHttpClient(), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(CompanyAccessPage);
    http.expectOne('/api/company-access').flush(data);
    fixture.detectChanges();
    return { fixture, page: fixture.componentInstance };
  }
  afterEach(() => http.verify());
  it('uses menu labels and groups rather than backend ordering or outdated labels', () => {
    const { page, fixture } = setup();
    page.data.set({ ...data, permissions: [
      { code: 'email', name: 'Email' }, { code: 'paymentSettings', name: 'Mollie' },
      { code: 'seo', name: 'Webshop en SEO' }, { code: 'products', name: 'Products' },
      { code: 'orders', name: 'Orders' },
    ] });
    fixture.detectChanges();
    expect(page.permissionGroups().map(group => group.map(item => item.name))).toEqual([
      ['Producten'], ['Bestellingen'], ['Website en SEO'], ['Bedrijfsbeheerders en rollen'], ['Betaalinstellingen', 'E-mailinstellingen'],
    ]);
    expect(fixture.nativeElement.querySelectorAll('.permission-group').length).toBe(5);
    expect(fixture.nativeElement.textContent).toContain('alleen de volledige beheerder');
    expect(page.permissionNames({ ...data.roles[0], permissions: ['email', 'products', 'seo'] })).toBe('Producten, Website en SEO, E-mailinstellingen');
  });
  it('edits only checked permissions with the current revision and CSRF protection', async () => {
    const { fixture, page } = setup();
    (fixture.nativeElement.querySelector('section button') as HTMLButtonElement).click();
    await fixture.whenStable();
    const checks = fixture.nativeElement.querySelectorAll('fieldset')[0].querySelectorAll('input') as NodeListOf<HTMLInputElement>;
    checks[0].click(); checks[1].click();
    page.saveRole(); page.saveRole();
    http.expectOne('/api/auth/csrf').flush(null);
    const save = http.expectOne('/api/company-access/roles/r1');
    expect(save.request.method).toBe('PUT');
    expect(save.request.body).toEqual({ name: 'Orderbeheer', permissions: ['products'], revision: 'revision-1' });
    save.flush({}, { status: 409, statusText: 'Conflict' });
    expect(page.busy()).toBe(false);
    expect(page.rolePermissions).toEqual(['products']);
    expect(page.failure()).toBeTruthy();
  });
  it('creates an account with selected roles and clears the password after saving', () => {
    const { page } = setup();
    page.userName = 'staff'; page.email = 'staff@example.test'; page.password = 'StrongPassword123!';
    page.saveAccount();
    http.expectNone('/api/auth/csrf');
    page.accountRoles = ['r1'];
    page.saveAccount(); page.saveAccount();
    http.expectOne('/api/auth/csrf').flush(null);
    const save = http.expectOne('/api/company-access/accounts');
    expect(save.request.body).toEqual({ userName: 'staff', email: 'staff@example.test', password: 'StrongPassword123!', roles: ['r1'] });
    save.flush(data.accounts[0]);
    expect(page.password).toBe('');
    http.expectOne('/api/company-access').flush(data);
    expect(page.notice()).toContain('opgeslagen');
  });
  it('blocks accounts using the current revision without submitting a password', () => {
    const { page } = setup();
    page.editAccount(page.data()!.accounts[0]); page.enabled = false;
    page.saveAccount();
    http.expectOne('/api/auth/csrf').flush(null);
    const save = http.expectOne('/api/company-access/accounts/u1');
    expect(save.request.body).toEqual({ revision: 'user-revision', enabled: false, roles: ['r1'] });
    save.flush({}, { status: 503, statusText: 'Unavailable' });
    expect(page.busy()).toBe(false);
    expect(page.failure()).toBeTruthy();
  });
});
