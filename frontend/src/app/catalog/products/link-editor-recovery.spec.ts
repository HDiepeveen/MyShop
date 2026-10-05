import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Subject } from 'rxjs';
import { CatalogApi } from '../catalog.api';
import { ProductCategories } from './product-categories';
import { OrphanValues } from './orphan-values';
import { ProductEditState } from './product-edit-state';

const product = {
  id: 'p',
  name: 'Shirt',
  productTypeId: 't',
  revision: 'r',
  categoryIds: ['a'],
  attributeValues: [],
  variants: [],
};
const values = [{ attributeDefinitionId: 'gone', dataType: 'Text', value: 'Old' }];

describe('Product link editor recovery', () => {
  function setup(kind: 'assign' | 'remove' | 'orphan') {
    const response = new Subject<void>();
    const options = new Subject<any[]>();
    const assigned = new Subject<any>();
    const api = {
      category: vi.fn(() => assigned),
      categories: vi.fn(() => options),
      assignCategory: vi.fn(() => response),
      removeCategory: vi.fn(() => response),
      clearAttribute: vi.fn(() => response),
    };
    TestBed.configureTestingModule({
      imports: [ProductCategories, OrphanValues],
      providers: [ProductEditState, provideRouter([]), { provide: CatalogApi, useValue: api }],
    });
    const fixture =
      kind === 'orphan'
        ? TestBed.createComponent(OrphanValues)
        : TestBed.createComponent(ProductCategories);
    if (kind === 'orphan') {
      fixture.componentRef.setInput('productId', 'p');
      fixture.componentRef.setInput('values', values);
      fixture.componentRef.setInput('definitions', []);
    } else fixture.componentRef.setInput('product', product);
    fixture.detectChanges();
    TestBed.tick();
    const page = fixture.componentInstance;
    function write() {
      if (page instanceof OrphanValues) {
        page.confirming.set('gone');
        page.clear('gone');
      } else if (kind === 'assign') page.assign('b');
      else page.remove('a');
    }
    function change() {
      if (page instanceof OrphanValues) fixture.componentRef.setInput('variantId', 'next');
      else fixture.componentRef.setInput('product', { ...product, id: 'next', categoryIds: [] });
      fixture.detectChanges();
      TestBed.tick();
    }
    return { fixture, page, api, response, options, assigned, write, change };
  }
  for (const kind of ['assign', 'remove', 'orphan'] as const) {
    it.each(['success', 'error'])(
      'ignores late ' + kind + ' %s after switching context',
      (result) => {
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
      },
    );
    it('releases its own ' + kind + ' lock on leaving', () => {
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
  it('closes and clears the category picker when the product changes', () => {
    const { page, options, change } = setup('assign');
    const editor = page as ProductCategories;
    editor.searchText = 'Old';
    editor.togglePicker();
    editor.error.set('Old failure');
    change();
    expect(editor.choosing()).toBe(false);
    expect(editor.searchText).toBe('');
    expect(editor.offset()).toBe(0);
    expect(editor.options()).toBeNull();
    expect(options.observed).toBe(false);
    expect(editor.error()).toBe('');
  });
  it('does not restart assigned category loading while loading or saving', () => {
    const { page, api, assigned, write } = setup('assign');
    const editor = page as ProductCategories;
    editor.retryAssigned();
    expect(api.category).toHaveBeenCalledTimes(1);
    assigned.next({ id: 'a', name: 'A' });
    assigned.complete();
    write();
    editor.retryAssigned();
    expect(api.category).toHaveBeenCalledTimes(1);
  });
  it('retries the failed picker query once and keeps its page during loading', () => {
    const { page, api, options } = setup('assign');
    const editor = page as ProductCategories;
    editor.togglePicker();
    editor.retryOptions();
    editor.changePage(20);
    expect(api.categories).toHaveBeenCalledTimes(1);
    expect(editor.offset()).toBe(0);
    options.next(Array.from({ length: 20 }, (_, i) => ({ id: String(i), name: 'A' })));
    options.complete();
    const retry = new Subject<any[]>();
    api.categories.mockReturnValue(retry);
    editor.changePage(20);
    editor.changePage(20);
    editor.retryOptions();
    expect(editor.offset()).toBe(20);
    expect(api.categories).toHaveBeenCalledTimes(2);
    retry.error(new Error('Offline'));
    const recovery = new Subject<any[]>();
    api.categories.mockReturnValue(recovery);
    editor.retryOptions();
    editor.retryOptions();
    expect(api.categories).toHaveBeenCalledTimes(3);
    expect(api.categories).toHaveBeenLastCalledWith(20, '');
    recovery.next([]);
    recovery.complete();
    editor.changePage(20);
    expect(editor.offset()).toBe(20);
    api.categories.mockReturnValue(new Subject<any[]>());
    editor.changePage(-20);
    expect(editor.offset()).toBe(0);
  });
  it('clears orphan confirmation and errors when definitions or values change', () => {
    const { fixture, page } = setup('orphan');
    const editor = page as OrphanValues;
    editor.confirming.set('gone');
    editor.error.set('Old');
    fixture.componentRef.setInput('definitions', [{ id: 'gone' }]);
    fixture.detectChanges();
    expect(editor.confirming()).toBeNull();
    expect(editor.error()).toBe('');
    expect(editor.orphans()).toHaveLength(0);
  });
  it.each(['values', 'definitions'])(
    'ignores an orphan delete result after %s changes',
    (input) => {
      const { fixture, page, response, write } = setup('orphan');
      const saved = vi.fn();
      page.saved.subscribe(saved);
      write();
      fixture.componentRef.setInput(input, []);
      fixture.detectChanges();
      response.next();
      expect(saved).not.toHaveBeenCalled();
      expect(page.busy()).toBe(false);
    },
  );
});
