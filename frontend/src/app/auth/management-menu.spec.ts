import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { Auth } from './auth';
import { managementMenuGroups } from './management-menu';

describe('Separate payment provider access', () => {
  it('requires the provider permission independently of checkout options', () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient()] });
    const auth = TestBed.inject(Auth);
    auth.session.set({ authenticated: true, administrator: false, businessAdministrator: true, customer: false, name: 'Staff', permissions: ['payments'] });
    expect(auth.canOpen('/instellingen/betalen')).toBe(true);
    expect(auth.canOpen('/instellingen/betaalinstellingen')).toBe(false);
    auth.session.update(session => ({ ...session!, permissions: ['paymentSettings'] }));
    expect(auth.canOpen('/instellingen/betaalinstellingen')).toBe(true);
    expect(auth.canOpen('/instellingen/betalen')).toBe(false);
    expect(auth.canOpen('/instellingen/bedrijfsbeheerders')).toBe(false);
  });
  it('keeps the agreed groups in one shared definition', () => {
    expect(managementMenuGroups.map(group => group.map(item => item.code))).toEqual([
      ['categories', 'productTypes', 'products'], ['overview', 'orders', 'customers'],
      ['seo', 'appearance'], ['payments', 'delivery', 'billing'], ['companyAccess'], ['paymentSettings', 'email'],
    ]);
  });
});
