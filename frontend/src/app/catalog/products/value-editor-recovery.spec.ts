import { TestBed } from '@angular/core/testing';
import { Subject } from 'rxjs';
import { CatalogApi } from '../catalog.api';
import { AttributeEdit } from './attribute-edit';
import { PriceRuleEdit } from './price-rule-edit';
import { ProductPresentationEdit } from './product-presentation';
import { ProductEditState } from './product-edit-state';

const variant = {
  id: 'v',
  name: 'Blue',
  sku: null,
  price: null,
  priceRules: [],
  attributeValues: [],
};
const product = {
  id: 'p',
  name: 'Shirt',
  revision: 'r',
  productTypeId: 't',
  categoryIds: [],
  attributeValues: [],
  variants: [variant],
};
const definition = {
  id: 'a',
  displayName: 'Material',
  dataType: 'Text',
  scope: 'Product',
  isRequired: false,
};

describe('Value editor recovery', () => {
  function setup(kind: 'attribute' | 'clear' | 'price' | 'presentation') {
    const response = new Subject<void>();
    const api = {
      setAttribute: vi.fn(() => response),
      clearAttribute: vi.fn(() => response),
      addPriceRule: vi.fn(() => response),
      setPresentation: vi.fn(() => response),
      removePriceRule: vi.fn(() => response),
    };
    TestBed.configureTestingModule({
      imports: [AttributeEdit, PriceRuleEdit, ProductPresentationEdit],
      providers: [ProductEditState, { provide: CatalogApi, useValue: api }],
    });
    const fixture =
      kind === 'presentation'
        ? TestBed.createComponent(ProductPresentationEdit)
        : kind === 'price'
          ? TestBed.createComponent(PriceRuleEdit)
          : TestBed.createComponent(AttributeEdit);
    if (kind === 'presentation') fixture.componentRef.setInput('product', product);
    else {
      fixture.componentRef.setInput('productId', 'p');
      if (kind === 'price') fixture.componentRef.setInput('variant', variant);
      else {
        fixture.componentRef.setInput('definition', definition);
        fixture.componentRef.setInput('current', {
          attributeDefinitionId: 'a',
          dataType: 'Text',
          value: 'Cotton',
        });
      }
    }
    fixture.detectChanges();
    const page = fixture.componentInstance;
    function write() {
      if (page instanceof ProductPresentationEdit) page.save(false);
      else if (page instanceof PriceRuleEdit) {
        page.name = 'Sale';
        page.value = '10';
        page.save();
      } else if (kind === 'clear') page.clear();
      else page.save();
    }
    function change() {
      if (kind === 'presentation')
        fixture.componentRef.setInput('product', { ...product, id: 'next' });
      else fixture.componentRef.setInput('productId', 'next');
      fixture.detectChanges();
    }
    return { fixture, page, response, api, write, change };
  }
  for (const kind of ['attribute', 'clear', 'price', 'presentation'] as const) {
    it.each(['success', 'error'])('ignores late ' + kind + ' %s on another product', (result) => {
      const { page, response, write, change } = setup(kind);
      const saved = vi.fn();
      page.saved.subscribe(saved);
      write();
      change();
      if (result === 'success') response.next();
      else response.error(new Error('Offline'));
      expect(saved).not.toHaveBeenCalled();
      expect(page.error()).toBe('');
      expect(page.busy()).toBe(false);
    });
    it('releases its own ' + kind + ' lock when leaving', () => {
      const { fixture, response, write } = setup(kind);
      write();
      expect(TestBed.inject(ProductEditState).busy()).toBe(true);
      fixture.destroy();
      expect(response.observed).toBe(false);
      expect(TestBed.inject(ProductEditState).busy()).toBe(false);
    });
    it('preserves another editor lock when idle ' + kind + ' leaves', () => {
      const { fixture } = setup(kind);
      TestBed.inject(ProductEditState).busy.set(true);
      fixture.destroy();
      expect(TestBed.inject(ProductEditState).busy()).toBe(true);
    });
  }
  it('resets an edited discount and confirmation on product change', () => {
    const { page, change } = setup('price');
    const editor = page as PriceRuleEdit;
    editor.editingId = 'old';
    editor.name = 'Old';
    editor.confirming.set('old');
    editor.error.set('Old failure');
    change();
    expect(editor.editingId).toBeNull();
    expect(editor.name).toBe('');
    expect(editor.confirming()).toBeNull();
    expect(editor.error()).toBe('');
  });
  it('keeps a pending discount draft when reset or edit is invoked', () => {
    const { page, write } = setup('price');
    const editor = page as PriceRuleEdit;
    write();
    editor.reset();
    editor.edit({
      id: 'other',
      name: 'Other',
      adjustmentType: 1,
      value: 5,
      priority: 0,
      startsAt: null,
      endsAt: null,
    });
    expect(editor.name).toBe('Sale');
    expect(editor.value).toBe('10');
  });
  it('refuses discount actions for rules outside the current variant', () => {
    const { page, api } = setup('price');
    const editor = page as PriceRuleEdit;
    const rule = {
      id: 'old',
      name: 'Old',
      adjustmentType: 1,
      value: 5,
      priority: 0,
      startsAt: null,
      endsAt: null,
    };
    editor.edit(rule);
    expect(editor.editingId).toBeNull();
    editor.confirming.set('old');
    editor.remove(rule);
    editor.editingId = 'old';
    editor.name = 'Sale';
    editor.value = '10';
    editor.save();
    expect(api.removePriceRule).not.toHaveBeenCalled();
    expect(api.addPriceRule).not.toHaveBeenCalled();
    expect(editor.busy()).toBe(false);
  });
  it('clears old attribute validation when changing context', () => {
    const { page, change } = setup('attribute');
    const editor = page as AttributeEdit;
    editor.error.set('Old');
    editor.validationError.set('Invalid');
    editor.text = 'Draft';
    change();
    expect(editor.error()).toBe('');
    expect(editor.validationError()).toBe('');
    expect(editor.text).toBe('Cotton');
  });
});
