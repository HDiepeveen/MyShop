import { OrphanValues } from './orphan-values';
import { PriceRuleEdit } from './price-rule-edit';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { By } from '@angular/platform-browser';
import { VariantEdit } from './variant-edit';
import { ProductEdit } from './product-edit';
import { ProductCategories } from './product-categories';
import { AttributeEdit } from './attribute-edit';
import { ProductDetail } from './product-detail';

describe('ProductDetail', () => {
  let http: HttpTestingController;
  const params = new BehaviorSubject(convertToParamMap({ id: 'first' }));
  beforeEach(() => {
    params.next(convertToParamMap({ id: 'first' }));
    TestBed.configureTestingModule({
      imports: [ProductDetail],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: params,
            queryParamMap: new BehaviorSubject(
              convertToParamMap({ search: 'coat', offset: '20', categoryId: 'c' }),
            ),
          },
        },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  it('does not reload while loading or saving and permits recovery after failure', () => {
    const fixture = TestBed.createComponent(ProductDetail);
    const page = fixture.componentInstance;
    const initial = http.expectOne('/api/products/first');
    page.reload();
    expect(initial.cancelled).toBe(false);
    http.expectNone('/api/products/first');
    initial.flush({}, { status: 500, statusText: 'Failure' });
    page.editState.busy.set(true);
    page.reload();
    http.expectNone('/api/products/first');
    page.editState.busy.set(false);
    page.reload();
    const retry = http.expectOne('/api/products/first');
    page.reload();
    expect(retry.cancelled).toBe(false);
    retry.flush({}, { status: 404, statusText: 'Missing' });
  });
  it('preserves list context in its back link and returns to page one after deletion', () => {
    const fixture = TestBed.createComponent(ProductDetail);
    http.expectOne('/api/products/first').flush({}, { status: 404, statusText: 'Missing' });
    fixture.detectChanges();
    const link = fixture.nativeElement.querySelector('a.back') as HTMLAnchorElement;
    expect(link.getAttribute('href')).toContain('search=coat');
    expect(link.getAttribute('href')).toContain('offset=20');
    expect(link.getAttribute('href')).toContain('categoryId=c');
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture.componentInstance.onRemoved();
    expect(navigate).toHaveBeenCalledWith(['/producten'], {
      queryParams: { search: 'coat', offset: null, categoryId: 'c', productTypeId: null },
    });
  });
  it('loads current product type and renders safe text', () => {
    const fixture = TestBed.createComponent(ProductDetail);
    http.expectOne('/api/products/first').flush({
      id: 'first',
      name: '<img src=x>',
      productTypeId: 'type',
      variants: [],
      categoryIds: [],
      attributeValues: [],
    });
    http
      .expectOne('/api/product-types/type')
      .flush({ id: 'type', name: 'Type', attributeDefinitions: [] });
    fixture.detectChanges();
    TestBed.tick();
    http.expectOne('/api/products/first/attribute-validation').flush({ isValid: true, issues: [] });
    expect(fixture.nativeElement.querySelector('h1').textContent).toBe('<img src=x>');
    expect(fixture.nativeElement.querySelector('img')).toBeNull();
  });

  it('serializes writes across editors, then reloads the persisted product after saving', () => {
    const fixture = TestBed.createComponent(ProductDetail);
    const product = {
      id: 'first',
      name: 'Product',
      productTypeId: 'type',
      categoryIds: [],
      attributeValues: [],
      variants: [{ id: 'v', name: 'Variant', sku: null, price: null, attributeValues: [] }],
    };
    http.expectOne('/api/products/first').flush(product);
    http
      .expectOne('/api/product-types/type')
      .flush({ id: 'type', name: 'Type', attributeDefinitions: [] });
    fixture.detectChanges();
    TestBed.tick();
    http.expectOne('/api/products/first/attribute-validation').flush({ isValid: true, issues: [] });
    const variant = fixture.debugElement.query(By.directive(VariantEdit))
      .componentInstance as VariantEdit;
    const editor = fixture.debugElement.query(By.directive(ProductEdit))
      .componentInstance as ProductEdit;
    const categories = fixture.debugElement.query(By.directive(ProductCategories))
      .componentInstance as ProductCategories;
    fixture.componentInstance.notice.set('Previous save');
    variant.sku = 'SKU';
    variant.saveSku();
    fixture.detectChanges();
    expect(fixture.componentInstance.notice()).toBe('');
    editor.name = 'Other';
    editor.rename();
    categories.assign('c');
    http.expectNone('/api/products/first/name');
    http.expectNone('/api/products/first/categories/c');
    http.expectOne('/api/products/first/variants/v/sku').flush(null);
    expect(fixture.componentInstance.notice()).toContain('artikelnummer');
    expect(fixture.componentInstance.state()?.loading).toBe(true);
    http
      .expectOne('/api/products/first')
      .flush({ ...product, variants: [{ ...product.variants[0], sku: 'SKU' }] });
    http
      .expectOne('/api/product-types/type')
      .flush({ id: 'type', name: 'Type', attributeDefinitions: [] });
    expect(fixture.componentInstance.state()?.data?.product.variants[0].sku).toBe('SKU');
    expect(fixture.componentInstance.editState.busy()).toBe(false);
  });
  it('clears stale notices and releases the page lock when changing products', () => {
    const fixture = TestBed.createComponent(ProductDetail);
    http.expectOne('/api/products/first').flush({}, { status: 404, statusText: 'Missing' });
    fixture.componentInstance.notice.set('Previous success');
    fixture.componentInstance.editState.busy.set(true);
    params.next(convertToParamMap({ id: 'second' }));
    http.expectOne('/api/products/second').flush({}, { status: 404, statusText: 'Missing' });
    expect(fixture.componentInstance.notice()).toBe('');
    expect(fixture.componentInstance.editState.busy()).toBe(false);
  });

  it('renders each definition at its scope and revalidates after clearing a required value', () => {
    const fixture = TestBed.createComponent(ProductDetail);
    const product = {
      id: 'first',
      name: 'Product',
      productTypeId: 'type',
      categoryIds: [],
      attributeValues: [{ attributeDefinitionId: 'a', dataType: 'Boolean', value: false }],
      variants: [{ id: 'v', name: 'Variant', sku: null, price: null, attributeValues: [] }],
    };
    const type = {
      id: 'type',
      name: 'Type',
      attributeDefinitions: [
        {
          id: 'a',
          displayName: 'Biologisch',
          dataType: 'Boolean',
          scope: 'Product',
          isRequired: true,
        },
        { id: 'b', displayName: 'Kleur', dataType: 'Text', scope: 'Variant', isRequired: false },
      ],
    };
    http.expectOne('/api/products/first').flush(product);
    http.expectOne('/api/product-types/type').flush(type);
    fixture.detectChanges();
    TestBed.tick();
    http.expectOne('/api/products/first/attribute-validation').flush({ isValid: true, issues: [] });
    const editors = fixture.debugElement
      .queryAll(By.directive(AttributeEdit))
      .map((item) => item.componentInstance as AttributeEdit);
    expect(editors.length).toBe(2);
    expect(editors[0].variantId()).toBeNull();
    expect(editors[1].variantId()).toBe('v');
    editors[0].clear();
    editors[1].text = 'Blue';
    editors[1].save();
    http.expectNone('/api/products/first/variants/v/attributes/b');
    http.expectOne('/api/products/first/attributes/a').flush(null);
    http.expectOne('/api/products/first').flush({ ...product, attributeValues: [] });
    http.expectOne('/api/product-types/type').flush(type);
    fixture.detectChanges();
    TestBed.tick();
    http.expectOne('/api/products/first/attribute-validation').flush({
      isValid: false,
      issues: [{ attributeDefinitionId: 'a', variantId: null, code: 'MissingRequired' }],
    });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(
      'Een verplichte waarde is nog niet ingevuld.',
    );
  });

  it('links to type management and revalidates after removing a variant orphan', () => {
    const fixture = TestBed.createComponent(ProductDetail);
    const product = {
      id: 'first',
      name: 'Product',
      productTypeId: 'type',
      categoryIds: [],
      attributeValues: [],
      variants: [
        {
          id: 'v',
          name: 'Variant',
          sku: null,
          price: null,
          attributeValues: [{ attributeDefinitionId: 'gone', dataType: 'Text', value: 'Old' }],
        },
      ],
    };
    const type = { id: 'type', name: 'Type', attributeDefinitions: [] };
    http.expectOne('/api/products/first').flush(product);
    http.expectOne('/api/product-types/type').flush(type);
    fixture.detectChanges();
    TestBed.tick();
    http.expectOne('/api/products/first/attribute-validation').flush({
      isValid: false,
      issues: [{ attributeDefinitionId: 'gone', variantId: 'v', code: 'UnknownDefinition' }],
    });
    expect(fixture.nativeElement.querySelector('a[href="/producttypen/type"]')).not.toBeNull();
    const editors = fixture.debugElement
      .queryAll(By.directive(OrphanValues))
      .map((e) => e.componentInstance as OrphanValues);
    expect(editors[0].orphans()).toHaveLength(0);
    expect(editors[1].variantId()).toBe('v');
    editors[1].confirming.set('gone');
    editors[1].clear('gone');
    http.expectOne('/api/products/first/variants/v/attributes/gone').flush(null);
    http
      .expectOne('/api/products/first')
      .flush({ ...product, variants: [{ ...product.variants[0], attributeValues: [] }] });
    http.expectOne('/api/product-types/type').flush(type);
    fixture.detectChanges();
    TestBed.tick();
    http.expectOne('/api/products/first/attribute-validation').flush({ isValid: true, issues: [] });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain('Waarden van verwijderde kenmerken');
    expect(fixture.nativeElement.textContent).toContain('Alle kenmerken passen');
  });
  it('renders variant price rules with the shared editor state', () => {
    const fixture = TestBed.createComponent(ProductDetail);
    const product = {
      id: 'first',
      name: 'Product',
      productTypeId: 'type',
      categoryIds: [],
      attributeValues: [],
      variants: [
        {
          id: 'v',
          name: 'Variant',
          sku: null,
          price: null,
          priceRules: [
            {
              id: 'r',
              name: 'Sale',
              adjustmentType: 1,
              value: 10,
              priority: 1,
              startsAt: null,
              endsAt: null,
            },
          ],
          attributeValues: [],
        },
      ],
    };
    http.expectOne('/api/products/first').flush(product);
    http
      .expectOne('/api/product-types/type')
      .flush({ id: 'type', name: 'Type', attributeDefinitions: [] });
    fixture.detectChanges();
    TestBed.tick();
    http.expectOne('/api/products/first/attribute-validation').flush({ isValid: true, issues: [] });
    const editor = fixture.debugElement.query(By.directive(PriceRuleEdit))
      .componentInstance as PriceRuleEdit;
    expect(editor.variant().priceRules[0].name).toBe('Sale');
    expect(editor.busy).toBe(fixture.componentInstance.editState.busy);
  });

  it('cancels the old product read on route changes', () => {
    const fixture = TestBed.createComponent(ProductDetail);
    const first = http.expectOne('/api/products/first');
    params.next(convertToParamMap({ id: 'second' }));
    expect(first.cancelled).toBe(true);
    http.expectOne('/api/products/second').flush({}, { status: 404, statusText: 'Not found' });
    expect(fixture.componentInstance.state()?.data).toBeNull();
    expect(fixture.componentInstance.state()?.error).toContain('niet meer beschikbaar');
  });
});
