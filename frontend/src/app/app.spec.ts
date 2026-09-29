import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';

it('offers accessible navigation and a skip link', async () => {
  TestBed.configureTestingModule({
    imports: [App],
    providers: [provideRouter([]), provideHttpClient()],
  });
  const fixture = TestBed.createComponent(App);
  await fixture.whenStable();
  const element = fixture.nativeElement as HTMLElement;
  expect(element.querySelector('nav')?.getAttribute('aria-label')).toBe('Hoofdnavigatie');
  expect(element.querySelectorAll('nav a').length).toBe(4);
  expect(element.querySelector('.skip')?.getAttribute('href')).toBe('#content');
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
