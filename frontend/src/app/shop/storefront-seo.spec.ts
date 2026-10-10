import { TestBed } from '@angular/core/testing';
import { Meta, Title } from '@angular/platform-browser';
import { StorefrontSeo } from './storefront-seo';
describe('Storefront SEO metadata', () => {
  beforeEach(() => TestBed.configureTestingModule({}));
  afterEach(() => { document.querySelector('link[rel="canonical"]')?.remove(); document.querySelector('meta[name="description"]')?.remove(); document.querySelector('meta[name="robots"]')?.remove(); });
  it('applies titles and descriptions as text and provides a canonical product URL', () => {
    const service = TestBed.inject(StorefrontSeo); service.apply('Opel Corsa kopen', '<script>text</script>', '/winkel/opel-corsa');
    expect(TestBed.inject(Title).getTitle()).toBe('Opel Corsa kopen');
    expect(TestBed.inject(Meta).getTag('name="description"')?.content).toBe('<script>text</script>');
    expect(document.querySelector<HTMLLinkElement>('link[rel="canonical"]')?.href).toContain('/winkel/opel-corsa');
  });
  it('removes stale product metadata and excludes account, cart and administration pages', () => {
    const service = TestBed.inject(StorefrontSeo);
    for (const path of ['/inloggen', '/producten', '/winkel/account', '/winkel/winkelmand', '/winkel/betaling']) {
      service.apply('Product', 'Description', '/winkel/product'); service.reset(path);
      expect(document.querySelector('link[rel="canonical"]')).toBeNull();
      expect(TestBed.inject(Meta).getTag('name="description"')).toBeNull();
      expect(TestBed.inject(Meta).getTag('name="robots"')?.content).toBe('noindex');
    }
    service.reset('/winkel?search=auto'); expect(TestBed.inject(Meta).getTag('name="robots"')).toBeNull();
  });
});
