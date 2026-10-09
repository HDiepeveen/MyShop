import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from '../app.routes';
import { ShopDetail } from './shop-detail';
import { Cart } from './cart';
import { ShopProduct } from './shop.api';

const optionProduct: ShopProduct = {
  id: '10000000-0000-0000-0000-000000000001',
  name: 'Katoenen T-shirt Hans',
  description: '',
  imageUrl: null,
  imageAlt: '',
  categories: [],
  variantDefinitions: [
    { id: 'size', name: 'Maat' },
    { id: 'colour', name: 'Kleur' },
  ],
  variants: [
    ['20000000-0000-0000-0000-000000000001', 'M', 'blauw', true],
    ['20000000-0000-0000-0000-000000000002', 'M', 'zwart', true],
    ['20000000-0000-0000-0000-000000000003', 'L', 'zwart', true],
    ['20000000-0000-0000-0000-000000000004', 'XL', 'blauw', false],
  ].map(([id, size, colour, available]) => ({
    id: String(id),
    name: size + ' / ' + colour,
    isAvailable: Boolean(available),
    attributes: [
      { attributeDefinitionId: 'size', value: String(size) },
      { attributeDefinitionId: 'colour', value: String(colour) },
    ],
  })),
};

describe('ShopDetail option selection', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    localStorage.removeItem('myshop.cart.v1');
    TestBed.configureTestingModule({
      providers: [provideRouter(routes), provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => {
    http.verify();
    localStorage.removeItem('myshop.cart.v1');
  });
  async function setup(other = false, product = optionProduct) {
    const harness = await RouterTestingHarness.create('/winkel/' + optionProduct.id);
    http.expectOne('/api/shop/products/' + optionProduct.id).flush({
      ...product,
      variants: other
        ? [
            { id: '20000000-0000-0000-0000-000000000005', name: 'Standaard' },
            ...product.variants,
          ]
        : product.variants,
    });
    http.expectOne('/api/shop/products/' + optionProduct.id + '/prices').flush({
      at: '2026-10-08T12:00:00Z',
      variants: optionProduct.variants.map((v, i) => ({
        variantId: v.id,
        amount: (10 + i).toFixed(2),
        currency: 'EUR',
      })),
    });
    await harness.fixture.whenStable();
    harness.detectChanges();
    return { harness, detail: harness.routeDebugElement!.componentInstance as ShopDetail };
  }
  it('renders separate size and colour controls and adds the matching variant with its price', async () => {
    const { harness, detail } = await setup();
    const root = harness.routeNativeElement!;
    expect(root.querySelector('select[name="variant"]')).toBeNull();
    const size = root.querySelector('select[name="option-size"]') as HTMLSelectElement;
    const colour = root.querySelector('select[name="option-colour"]') as HTMLSelectElement;
    expect(root.textContent).toContain('Gekozen variant:');
    expect(size.value).toBe('M');
    expect(colour.value).toBe('blauw');
    expect(Array.from(size.options).find((o) => o.value === 'XL')?.disabled).toBe(true);
    size.value = 'L';
    size.dispatchEvent(new Event('change'));
    await harness.fixture.whenStable();
    harness.detectChanges();
    expect(colour.value).toBe('zwart');
    expect(Array.from(colour.options).find((o) => o.value === 'blauw')?.disabled).toBe(true);
    expect(root.querySelector('.price')?.textContent).toContain('EUR 12,00');
    detail.addToCart();
    expect(TestBed.inject(Cart).lines()).toEqual([
      { productId: optionProduct.id, variantId: optionProduct.variants[2].id, quantity: 1 },
    ]);
  });
  it('shows single values as text while retaining choices and the matching cart variant', async () => {
    const product = { ...optionProduct, variants: optionProduct.variants.slice(0, 2) };
    const { harness, detail } = await setup(false, product);
    const root = harness.routeNativeElement!;
    expect(root.querySelector('select[name="option-size"]')).toBeNull();
    expect(root.querySelector('dl')?.textContent).toContain('Maat');
    expect(root.querySelector('dd')?.textContent).toBe('M');
    const colour = root.querySelector('select[name="option-colour"]') as HTMLSelectElement;
    colour.value = 'zwart';
    colour.dispatchEvent(new Event('change'));
    await harness.fixture.whenStable();
    harness.detectChanges();
    expect(root.querySelector('dd')?.textContent).toBe('M');
    detail.addToCart();
    expect(TestBed.inject(Cart).lines()[0].variantId).toBe(product.variants[1].id);
  });
  it('shows all characteristics as text when there is only one combination', async () => {
    const { harness, detail } = await setup(false, {
      ...optionProduct, variants: optionProduct.variants.slice(0, 1),
    });
    expect(harness.routeNativeElement!.querySelectorAll('select')).toHaveLength(0);
    expect(harness.routeNativeElement!.textContent).not.toContain('Gekozen variant:');
    expect(Array.from(harness.routeNativeElement!.querySelectorAll('dd')).map(d => d.textContent))
      .toEqual(['M', 'blauw']);
    detail.addToCart();
    expect(TestBed.inject(Cart).lines()[0].variantId).toBe(optionProduct.variants[0].id);
  });
  it('can return from an incomplete variant to a single fixed combination', async () => {
    const { harness, detail } = await setup(true, {
      ...optionProduct, variants: optionProduct.variants.slice(0, 1),
    });
    detail.selectVariant('20000000-0000-0000-0000-000000000005');
    harness.detectChanges();
    expect(harness.routeNativeElement!.textContent).toContain('Gekozen variant: Standaard');
    const button = Array.from(harness.routeNativeElement!.querySelectorAll('button'))
      .find(b => b.textContent?.includes('Deze uitvoering kiezen'))!;
    expect(button).toBeTruthy();
    button.click();
    harness.detectChanges();
    expect(detail.selectedId()).toBe(optionProduct.variants[0].id);
    expect(harness.routeNativeElement!.textContent).not.toContain('Gekozen variant:');
  });
  it('renders product characteristics as safe text above variant choices', async () => {
    const { harness } = await setup(false, {
      ...optionProduct,
      attributes: [{ attributeDefinitionId: 'brand', name: 'Merk', value: '<img src=x>Opel' }],
    });
    const root = harness.routeNativeElement!;
    const section = root.querySelector('[aria-label="Productkenmerken"]')!;
    expect(section.textContent).toContain('Merk');
    expect(section.textContent).toContain('<img src=x>Opel');
    expect(section.querySelector('img')).toBeNull();
    expect(section.compareDocumentPosition(root.querySelector('select[name="option-size"]')!) &
      Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  });
  it('hides the variant picker for one plain variant and still uses its price and cart identity', async () => {
    const variant = { id: optionProduct.variants[0].id, name: 'Standaard', isAvailable: true };
    const { harness, detail } = await setup(false, {
      ...optionProduct, variantDefinitions: [], variants: [variant],
    });
    const root = harness.routeNativeElement!;
    expect(root.querySelector('select[name="variant"]')).toBeNull();
    expect(root.textContent).not.toContain('Kies je variant');
    expect(root.textContent).not.toContain('Gekozen variant:');
    expect(root.querySelector('.price')?.textContent).toContain('EUR 10,00');
    expect(root.textContent).not.toContain('Prijs opgehaald op');
    expect(root.textContent).not.toContain('Prijs vernieuwen');
    detail.addToCart();
    expect(TestBed.inject(Cart).lines()[0].variantId).toBe(variant.id);
  });
  it('retains the picker for multiple plain variants', async () => {
    const { harness } = await setup(false, {
      ...optionProduct, variantDefinitions: [],
      variants: optionProduct.variants.slice(0, 2).map(v => ({ ...v, attributes: [] })),
    });
    expect(harness.routeNativeElement!.querySelector('select[name="variant"]')).not.toBeNull();
    expect(harness.routeNativeElement!.textContent).toContain('Gekozen variant:');
  });
  it('retains the sold-out notice and prevents ordering a single sold-out variant', async () => {
    const { harness, detail } = await setup(false, {
      ...optionProduct, variantDefinitions: [],
      variants: [{ id: optionProduct.variants[0].id, name: 'Standaard', isAvailable: false }],
    });
    expect(harness.routeNativeElement!.textContent).toContain('Deze variant is uitverkocht.');
    expect(harness.routeNativeElement!.querySelector('select[name="variant"]')).toBeNull();
    detail.addToCart();
    expect(TestBed.inject(Cart).lines()).toEqual([]);
  });
  it('clears cart feedback and refuses forged missing or sold-out choices', async () => {
    const { detail } = await setup();
    detail.addToCart();
    expect(detail.cartMessage()).toBeTruthy();
    detail.selectOption('colour', 'zwart');
    expect(detail.cartMessage()).toBe('');
    const selected = detail.selectedId();
    detail.selectOption('size', 'XL');
    detail.selectOption('colour', 'missing');
    expect(detail.selectedId()).toBe(selected);
    detail.selectOption('size', 'L');
    detail.selectOption('colour', 'blauw');
    expect(detail.selectedId()).toBe(optionProduct.variants[2].id);
  });
  it('prefers complete combinations and still permits explicitly choosing other executions', async () => {
    const { harness, detail } = await setup(true);
    expect(detail.selectedId()).toBe(optionProduct.variants[0].id);
    expect(harness.routeNativeElement!.querySelector('select[name="otherVariant"]')).not.toBeNull();
    detail.selectVariant('20000000-0000-0000-0000-000000000005');
    expect(detail.selectedOption('size')).toBe('');
    detail.selectOption('size', 'L');
    expect(detail.selectedId()).toBe(optionProduct.variants[2].id);
  });
  it('shows sold-out combinations without allowing them into the cart', async () => {
    const { harness, detail } = await setup();
    detail.retry();
    http
      .expectOne('/api/shop/products/' + optionProduct.id)
      .flush({
        ...optionProduct,
        variants: optionProduct.variants.map((v) => ({ ...v, isAvailable: false })),
      });
    await harness.fixture.whenStable();
    detail.addToCart();
    expect(TestBed.inject(Cart).lines()).toEqual([]);
    expect(detail.selected()?.isAvailable).toBe(false);
  });
});
