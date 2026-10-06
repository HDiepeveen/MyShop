import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject, Subject, of } from 'rxjs';
import { CatalogApi } from '../catalog.api';
import { Product, Variant } from '../catalog.models';
import { ProductDetail } from './product-detail';
import { VariantEdit } from './variant-edit';
const makeVariant = (
  id: string,
  name: string,
  sku: string | null,
  stockQuantity: number | null,
): Variant => ({ id, name, sku, stockQuantity, price: null, attributeValues: [], priceRules: [] });
const variants = [
  makeVariant('v0', 'Blue small', 'BLUE-S', 0),
  makeVariant('v5', 'Blue large', 'BLUE-L', 5),
  makeVariant('v6', 'Green', 'GREEN', 6),
  makeVariant('vn', 'Unlimited', null, null),
];
const product: Product = {
  id: 'p',
  productTypeId: 't',
  name: 'Shirt',
  revision: 'r',
  variants,
  attributeValues: [],
  categoryIds: [],
};
describe('Variant filters in product details', () => {
  function setup(stock = 'low') {
    const params = new BehaviorSubject(convertToParamMap({ id: 'p' }));
    const response = new Subject<Product>();
    const write = new Subject<void>();
    const api = {
      product: vi.fn(() => response),
      type: vi.fn(() => of({ id: 't', name: 'Type', attributeDefinitions: [] })),
      validation: vi.fn(() => of({ productId: 'p', isValid: true, issues: [] })),
      renameVariant: vi.fn(() => write),
    };
    TestBed.configureTestingModule({
      imports: [ProductDetail],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { paramMap: params, queryParamMap: of(convertToParamMap({ stock })) },
        },
        { provide: CatalogApi, useValue: api },
      ],
    });
    const fixture = TestBed.createComponent(ProductDetail);
    response.next(product);
    fixture.detectChanges();
    TestBed.tick();
    return { fixture, page: fixture.componentInstance, response, write, api, params };
  }
  it('inherits the stock context and shows matching and total counts', () => {
    const { fixture, page } = setup();
    expect(page.visibleVariants(variants).map((variant) => variant.id)).toEqual(['v0', 'v5']);
    expect(fixture.nativeElement.textContent).toContain('2 van 4 varianten');
    expect(fixture.debugElement.queryAll(By.directive(VariantEdit))).toHaveLength(2);
  });
  it('combines case-insensitive name and SKU search with stock selection', () => {
    const { page } = setup();
    page.variantSearchText = ' blue-l ';
    page.searchVariants();
    expect(page.visibleVariants(variants).map((variant) => variant.id)).toEqual(['v5']);
    page.variantSearchText = 'SMALL';
    page.searchVariants();
    expect(page.visibleVariants(variants).map((variant) => variant.id)).toEqual(['v0']);
    page.filterVariants('untracked');
    page.variantSearchText = 'unlimited';
    page.searchVariants();
    expect(page.visibleVariants(variants).map((variant) => variant.id)).toEqual(['vn']);
  });
  it.each([
    ['out', ['v0']],
    ['untracked', ['vn']],
    ['all', ['v0', 'v5', 'v6', 'vn']],
  ] as const)('selects %s without changing persisted product data', (stock, expected) => {
    const { page, api } = setup();
    page.filterVariants(stock);
    expect(page.visibleVariants(variants).map((variant) => variant.id)).toEqual(expected);
    expect(product.variants).toBe(variants);
    expect(api.product).toHaveBeenCalledTimes(1);
  });
  it('shows an empty match and clears filters without losing product context', () => {
    const { fixture, page } = setup();
    page.variantSearchText = 'Missing';
    page.searchVariants();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Geen varianten gevonden');
    page.clearVariantFilters();
    fixture.detectChanges();
    expect(page.variantSearchText).toBe('');
    expect(fixture.debugElement.queryAll(By.directive(VariantEdit))).toHaveLength(4);
    expect(page.listQuery()?.stock).toBe('low');
  });
  it('keeps the true total variant count when only one editor is shown', () => {
    const { fixture, page } = setup();
    page.filterVariants('out');
    fixture.detectChanges();
    const editor = fixture.debugElement.query(By.directive(VariantEdit))
      .componentInstance as VariantEdit;
    expect(editor.variantCount()).toBe(4);
  });
  it('preserves a pending variant write while rejecting filter changes', async () => {
    const { fixture, page, write } = setup();
    const editor = fixture.debugElement.query(By.directive(VariantEdit))
      .componentInstance as VariantEdit;
    editor.name = 'New';
    editor.rename();
    page.variantSearchText = 'missing';
    page.searchVariants();
    page.filterVariants('all');
    page.clearVariantFilters();
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.variantSearch()).toBe('');
    expect(page.variantStock()).toBe('low');
    expect(write.observed).toBe(true);
    expect(page.editState.busy()).toBe(true);
    expect(
      (fixture.nativeElement.querySelector('input[name="variantSearch"]') as HTMLInputElement)
        .disabled,
    ).toBe(true);
    write.error(new Error('Offline'));
    expect(page.editState.busy()).toBe(false);
  });
  it('preserves filters after refresh and resets them when the product changes', () => {
    const { fixture, page, response, params } = setup();
    page.variantSearchText = 'large';
    page.searchVariants();
    page.reload();
    response.next({ ...product, revision: 'new' });
    fixture.detectChanges();
    TestBed.tick();
    expect(page.variantSearch()).toBe('large');
    params.next(convertToParamMap({ id: 'other' }));
    response.next({ ...product, id: 'other' });
    fixture.detectChanges();
    TestBed.tick();
    expect(page.variantSearch()).toBe('');
    expect(page.variantStock()).toBe('low');
  });
  it('rejects oversized search without replacing the current selection', () => {
    const { page } = setup();
    page.variantSearchText = 'small';
    page.searchVariants();
    page.variantSearchText = 'x'.repeat(201);
    page.searchVariants();
    expect(page.variantSearch()).toBe('small');
    expect(page.variantFilterError()).toContain('200');
  });
});
