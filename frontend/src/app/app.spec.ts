import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { Auth } from './auth/auth';
import { App } from './app';
import webshopVersion from '../../../version.json';
import { ShopBranding } from './shop/shop-branding';

it.each([false, true])('shows optional management navigation without granting access for customer=%s', async customer => {
  TestBed.configureTestingModule({ imports: [App], providers: [provideRouter([]), provideHttpClient()] });
  const fixture = TestBed.createComponent(App);
  const auth = TestBed.inject(Auth);
  const branding = TestBed.inject(ShopBranding);
  auth.session.set({ authenticated: customer, administrator: false, customer, name: null });
  await fixture.whenStable();
  expect(fixture.nativeElement.querySelector('aside')).toBeNull();
  branding.showManagementMenuWithoutLogin.set(true);
  await fixture.whenStable();
  expect(fixture.nativeElement.querySelectorAll('aside nav a').length).toBe(14);
  expect(auth.canOpen('/bestellingen')).toBe(false);
  auth.session.set({ authenticated: true, administrator: false, businessAdministrator: true, customer: false, name: 'Staff', permissions: ['orders'] });
  await fixture.whenStable();
  expect(fixture.nativeElement.querySelectorAll('aside nav a').length).toBe(1);
});

it('shows only assigned management menu items to company administrators', async () => {
  TestBed.configureTestingModule({ imports: [App], providers: [provideRouter([]), provideHttpClient()] });
  const fixture = TestBed.createComponent(App);
  const auth = TestBed.inject(Auth);
  auth.session.set({ authenticated: true, administrator: false, businessAdministrator: true, permissions: ['orders', 'products'], customer: false, name: 'Staff' });
  await fixture.whenStable();
  const links = Array.from(fixture.nativeElement.querySelectorAll('aside nav a')) as HTMLAnchorElement[];
  expect(links.map(link => link.textContent?.trim())).toEqual(['Producten', 'Bestellingen']);
  expect(auth.canOpen('/instellingen/bedrijfsbeheerders')).toBe(false);
  expect(auth.canOpen('/producten/123')).toBe(true);
  expect(auth.canOpen('/klanten')).toBe(false);
  expect(auth.landingPage()).toBe('/producten');
});

it('offers accessible navigation and a skip link', async () => {
  TestBed.configureTestingModule({
    imports: [App],
    providers: [provideRouter([]), provideHttpClient()],
  });
  const fixture = TestBed.createComponent(App);
  TestBed.inject(Auth).session.set({ authenticated: true, administrator: true, customer: false, name: 'Admin' });
  await fixture.whenStable();
  const element = fixture.nativeElement as HTMLElement;
  expect(element.querySelector('nav')?.getAttribute('aria-label')).toBe('Hoofdnavigatie');
  expect(element.querySelectorAll('.sidebar nav a').length).toBe(14);
  const websiteLinks = element.querySelectorAll('nav[aria-label="Website bekijken"] a');
  expect(Array.from(websiteLinks, link => [link.textContent?.trim(), link.getAttribute('href')])).toEqual([['Home', '/'], ['Assortiment', '/assortiment']]);
  expect(element.querySelector('.skip')?.getAttribute('href')).toBe('#content');
  expect(element.querySelector('.sidebar-footer .webshop-version')?.textContent).toContain(webshopVersion.version);
  expect(element.querySelector('.sidebar-footer')?.textContent?.trim()).toBe('Catalogusbeheer ' + webshopVersion.version);
  expect(element.querySelector('footer .webshop-version')).toBeNull();
});

it('keeps the skip link on the current page and focuses main content', async () => {
  TestBed.configureTestingModule({
    imports: [App],
    providers: [provideRouter([]), provideHttpClient()],
  });
  const fixture = TestBed.createComponent(App);
  await fixture.whenStable();
  const event = new MouseEvent('click', { bubbles: true, cancelable: true });
  fixture.nativeElement.querySelector('.skip').dispatchEvent(event);
  expect(event.defaultPrevented).toBe(true);
  expect(document.activeElement).toBe(fixture.nativeElement.querySelector('main'));
});

it('shows the management return link and logout for an administrator', async () => {
  TestBed.configureTestingModule({
    imports: [App],
    providers: [provideRouter([{ path: 'assortiment', children: [] }, { path: '', children: [] }]), provideHttpClient()],
  });
  const fixture = TestBed.createComponent(App);
  TestBed.inject(Auth).session.set({ authenticated: true, administrator: true, customer: false, name: 'Admin' });
  await TestBed.inject(Router).navigateByUrl('/assortiment');
  await fixture.whenStable();
  const group = (fixture.nativeElement as HTMLElement).querySelector('.session-actions')!;
  expect(group.querySelector('a')?.getAttribute('href')).toBe('/beheer');
  expect(group.querySelector('button')?.textContent?.trim()).toBe('Uitloggen');
  const contact = fixture.nativeElement.querySelector('nav[aria-label="Winkelnavigatie"] a[href="/contact"]');
  expect(contact?.textContent.trim()).toBe('Contact');
  expect(fixture.nativeElement.querySelector('footer a[href="/contact"]')).toBeNull();
  expect(Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('header a')).some(a => a.textContent?.trim() === 'Beheer')).toBe(true);
});

it.each([true, false])('does not offer a direct admin return link to customer=%s', async customer => {
  TestBed.configureTestingModule({
    imports: [App],
    providers: [provideRouter([{ path: 'assortiment', children: [] }]), provideHttpClient()],
  });
  const fixture = TestBed.createComponent(App);
  TestBed.inject(Auth).session.set({ authenticated: customer, administrator: false, customer, name: customer ? 'Customer' : null });
  await TestBed.inject(Router).navigateByUrl('/assortiment');
  await fixture.whenStable();
  const nav = fixture.nativeElement.querySelector('nav[aria-label="Winkelnavigatie"]') as HTMLElement;
  expect(Array.from(nav.querySelectorAll('a')).some(anchor => anchor.textContent?.trim() === 'Beheer')).toBe(false);
});

 it('hides administration navigation when logged out or signed in as a customer and updates after logout', async () => {
  TestBed.configureTestingModule({
    imports: [App], providers: [provideRouter([]), provideHttpClient()],
  });
  const fixture = TestBed.createComponent(App);
  const auth = TestBed.inject(Auth);
  auth.session.set(null);
  await fixture.whenStable();
  expect(fixture.nativeElement.querySelector('.sidebar')).toBeNull();
  expect(fixture.nativeElement.querySelector('.webshop-version')).toBeNull();
  expect(fixture.nativeElement.querySelector('.workspace').classList.contains('no-sidebar')).toBe(true);
  auth.session.set({ authenticated: true, administrator: false, customer: true, name: 'Customer' });
  fixture.detectChanges();
  expect(fixture.nativeElement.querySelector('.sidebar')).toBeNull();
  expect(fixture.nativeElement.querySelector('.webshop-version')).toBeNull();
  auth.session.set({ authenticated: true, administrator: true, customer: false, name: 'Admin' });
  fixture.detectChanges();
  expect(fixture.nativeElement.querySelector('nav[aria-label="Hoofdnavigatie"]')).not.toBeNull();
  expect(fixture.nativeElement.querySelector('.workspace').classList.contains('no-sidebar')).toBe(false);
  auth.session.set(null);
  fixture.detectChanges();
  expect(fixture.nativeElement.querySelector('.sidebar')).toBeNull();
  expect(fixture.nativeElement.querySelector('.webshop-version')).toBeNull();
});
