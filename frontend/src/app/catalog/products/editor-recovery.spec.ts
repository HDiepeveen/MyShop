import { TestBed } from '@angular/core/testing';
import { Subject } from 'rxjs';
import { CatalogApi } from '../catalog.api';
import { ProductEdit } from './product-edit';
import { VariantEdit } from './variant-edit';
import { ProductDelete } from './product-delete';
import { ProductEditState } from './product-edit-state';

const variant = { id: 'variant-1', name: 'Blauw', sku: null, price: null, priceRules: [], attributeValues: [], stockQuantity: null };
const product = { id: 'product-1', name: 'Shirt', productTypeId: 'type-1', revision: 'revision-1', categoryIds: [], attributeValues: [], variants: [variant] };

describe('Product editor recovery', () => {
  function setup(kind: 'product' | 'variant' | 'delete') {
    const response = new Subject<void>();
    const api = {
      renameProduct: vi.fn(() => response), addVariant: vi.fn(() => response),
      renameVariant: vi.fn(() => response), deleteProduct: vi.fn(() => response),
    };
    TestBed.configureTestingModule({
      imports: [ProductEdit, VariantEdit, ProductDelete],
      providers: [ProductEditState, { provide: CatalogApi, useValue: api }],
    });
    const fixture = kind === 'product' ? TestBed.createComponent(ProductEdit)
      : kind === 'variant' ? TestBed.createComponent(VariantEdit) : TestBed.createComponent(ProductDelete);
    if (kind === 'product') fixture.componentRef.setInput('product', product);
    else {
      fixture.componentRef.setInput('productId', product.id);
      if (kind === 'variant') fixture.componentRef.setInput('variant', variant);
      else fixture.componentRef.setInput('productName', product.name);
    }
    fixture.detectChanges();
    const page = fixture.componentInstance;
    function write() {
      if (page instanceof ProductEdit) { page.name = 'Nieuw shirt'; page.rename(); }
      else if (page instanceof VariantEdit) { page.name = 'Rood'; page.rename(); }
      else { page.confirming.set(true); page.remove(); }
    }
    function changeInput() {
      if (kind === 'product') fixture.componentRef.setInput('product', { ...product, id: 'product-2', name: 'Broek' });
      else {
        fixture.componentRef.setInput('productId', 'product-2');
        if (kind === 'variant') fixture.componentRef.setInput('variant', { ...variant, id: 'variant-2', name: 'Groen' });
        else fixture.componentRef.setInput('productName', 'Broek');
      }
      fixture.detectChanges();
    }
    return { fixture, page, response, write, changeInput };
  }

  for (const kind of ['product', 'variant', 'delete'] as const) {
    it(`clears old ${kind} errors and confirmations when inputs change`, () => {
      const { page, changeInput } = setup(kind);
      page.error.set('Old error');
      if (page instanceof ProductDelete) page.confirming.set(true);
      if (page instanceof VariantEdit) page.confirmingRemove.set(true);
      changeInput();
      expect(page.error()).toBe('');
      if (page instanceof ProductEdit) expect(page.name).toBe('Broek');
      if (page instanceof VariantEdit) {
        expect(page.name).toBe('Groen');
        expect(page.confirmingRemove()).toBe(false);
      }
      if (page instanceof ProductDelete) expect(page.confirming()).toBe(false);
    });

    it.each(['success', 'error'])(`ignores late ${kind} write %s after changing inputs`, (result) => {
      const { page, response, write, changeInput } = setup(kind);
      const notify = vi.fn();
      if (page instanceof ProductDelete) page.removed.subscribe(notify);
      else page.saved.subscribe(notify);
      write();
      changeInput();
      if (result === 'success') response.next();
      else response.error(new Error('Offline'));
      expect(notify).not.toHaveBeenCalled();
      expect(page.error()).toBe('');
      expect(page.busy()).toBe(false);
    });

    it(`releases the active ${kind} write lock when leaving`, () => {
      const { fixture, response, write } = setup(kind);
      write();
      expect(TestBed.inject(ProductEditState).busy()).toBe(true);
      fixture.destroy();
      expect(response.observed).toBe(false);
      expect(TestBed.inject(ProductEditState).busy()).toBe(false);
    });

    it(`keeps another editor's lock when the idle ${kind} editor is destroyed`, () => {
      const { fixture } = setup(kind);
      TestBed.inject(ProductEditState).busy.set(true);
      fixture.destroy();
      expect(TestBed.inject(ProductEditState).busy()).toBe(true);
    });
  }

  it('clears a draft variant name when switching products', () => {
    const { page, changeInput } = setup('product');
    const editor = page as ProductEdit;
    editor.variantName = 'Onvoltooid';
    changeInput();
    expect(editor.variantName).toBe('');
  });

  it.each(['success', 'error'])('ignores late variant creation %s for the previous product', (result) => {
    const { page, response, changeInput } = setup('product');
    const editor = page as ProductEdit;
    const saved = vi.fn();
    editor.saved.subscribe(saved);
    editor.variantName = 'Rood';
    editor.addVariant();
    changeInput();
    editor.variantName = 'Nieuwe invoer';
    if (result === 'success') response.next();
    else response.error(new Error('Offline'));
    expect(editor.variantName).toBe('Nieuwe invoer');
    expect(editor.error()).toBe('');
    expect(editor.busy()).toBe(false);
    expect(saved).not.toHaveBeenCalled();
  });
});
