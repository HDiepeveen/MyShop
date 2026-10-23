import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { ShopLegal } from './shop-legal';
import { ShopBranding } from './shop-branding';
import { Meta } from '@angular/platform-browser';

describe('Legal documents', () => {
  function setup(slug: string) {
    TestBed.configureTestingModule({
      imports: [ShopLegal],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting(),
        { provide: ActivatedRoute, useValue: { snapshot: { data: { legalPage: slug } } } },
        { provide: ShopBranding, useValue: { name: signal('Autoservice Ganpat'), contactEnabled: signal(true) } }],
    });
    return TestBed.createComponent(ShopLegal);
  }

  it.each(['algemene-voorwaarden', 'privacyverklaring', 'cookiebeleid'])('loads %s without login and offers its matching download', slug => {
    const fixture = setup(slug);
    TestBed.inject(HttpTestingController).expectOne('/legal/' + slug + '.html').flush('<h2>Contactgegevens</h2><p>Popovstraat 7-B</p><script>alert(1)</script>');
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    expect(element.textContent).toContain('Popovstraat 7-B');
    expect(element.querySelector('script')).toBeNull();
    expect(element.querySelector('a[download]')?.getAttribute('href')).toBe('/legal/' + slug + '.md');
    expect(TestBed.inject(Meta).getTag('name="robots"')?.content).toBe('noindex');
    TestBed.inject(HttpTestingController).verify();
  });

  it('keeps the download accessible when loading the text fails', () => {
    const fixture = setup('cookiebeleid');
    TestBed.inject(HttpTestingController).expectOne('/legal/cookiebeleid.html').flush('', { status: 503, statusText: 'Unavailable' });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('a[download]')).not.toBeNull();
  });
});
