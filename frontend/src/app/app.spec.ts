import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { Auth } from './auth/auth';
import { App } from './app';
import webshopVersion from '../../../version.json';

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
  expect(element.querySelectorAll('nav a').length).toBe(10);
  expect(element.querySelector('.skip')?.getAttribute('href')).toBe('#content');
  expect(element.querySelector('.sidebar-footer .webshop-version')?.textContent).toContain(webshopVersion.version);
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

it('lets an administrator return from the assortment to administration', async () => {
  TestBed.configureTestingModule({
    imports: [App],
    providers: [provideRouter([{ path: 'winkel', children: [] }, { path: '', children: [] }]), provideHttpClient()],
  });
  const fixture = TestBed.createComponent(App);
  TestBed.inject(Auth).session.set({ authenticated: true, administrator: true, customer: false, name: 'Admin' });
  const router = TestBed.inject(Router);
  await router.navigateByUrl('/winkel');
  await fixture.whenStable();
  const link = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll<HTMLAnchorElement>('nav a'))
    .find(anchor => anchor.textContent?.trim() === 'Beheer');
  expect(link?.getAttribute('href')).toBe('/');
  expect(fixture.nativeElement.querySelector('.webshop-version')).toBeNull();
  link!.click();
  await fixture.whenStable();
  expect(router.url).toBe('/');
  expect(fixture.nativeElement.querySelector('nav')?.getAttribute('aria-label')).toBe('Hoofdnavigatie');
});

it.each([true, false])('does not offer a direct admin return link to customer=%s', async customer => {
  TestBed.configureTestingModule({
    imports: [App],
    providers: [provideRouter([{ path: 'winkel', children: [] }]), provideHttpClient()],
  });
  const fixture = TestBed.createComponent(App);
  TestBed.inject(Auth).session.set({ authenticated: customer, administrator: false, customer, name: customer ? 'Customer' : null });
  await TestBed.inject(Router).navigateByUrl('/winkel');
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
