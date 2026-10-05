import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, Subject } from 'rxjs';
import { CatalogApi } from './catalog.api';
import { ProductCreate } from './products/product-create';
import { CategoryManagement } from './categories/category-management';
import { CategoryEditState } from './categories/category-edit-state';
import { TypeSummary, CategorySummary } from './catalog.models';

for (const kind of ['type', 'category'] as const) {
  describe(kind + ' picker guards', () => {
    function setup() {
      const types = new Subject<TypeSummary[]>();
      const categories = new Subject<CategorySummary[]>();
      const api = {
        types: vi.fn(() => types),
        categories: vi.fn(() => categories),
        categoryUsage: vi.fn(() =>
          of({ categoryId: 'c', isInUse: false, directChildCount: 0, productAssignmentCount: 0 }),
        ),
      };
      TestBed.configureTestingModule({
        imports: [ProductCreate, CategoryManagement],
        providers: [provideRouter([]), CategoryEditState, { provide: CatalogApi, useValue: api }],
      });
      const fixture =
        kind === 'type'
          ? TestBed.createComponent(ProductCreate)
          : TestBed.createComponent(CategoryManagement);
      if (kind === 'category')
        fixture.componentRef.setInput('category', {
          id: 'c',
          name: 'Category',
          parentCategoryId: null,
          isRoot: true,
        });
      fixture.detectChanges();
      TestBed.tick();
      const page = fixture.componentInstance;
      function change(delta: number) {
        if (page instanceof ProductCreate) page.changePage(delta);
        else page.changeCategoryPage(delta);
      }
      function offset() {
        return page instanceof ProductCreate ? page.offset() : page.categoryOffset();
      }
      function finish() {
        if (kind === 'type') {
          types.next([]);
          types.complete();
        } else {
          categories.next([]);
          categories.complete();
        }
      }
      const read = kind === 'type' ? api.types : api.categories;
      return { fixture, page, types, categories, api, read, change, offset, finish };
    }
    it('keeps a pending picker request and permits paging after loading', () => {
      const { read, change, offset, finish } = setup();
      change(20);
      expect(read).toHaveBeenCalledTimes(1);
      expect(offset()).toBe(0);
      finish();
      change(20);
      expect(read).toHaveBeenCalledTimes(2);
      expect(offset()).toBe(20);
    });
    it.each([NaN, Infinity, 1, 20.5, Number.MAX_SAFE_INTEGER + 1])(
      'rejects invalid delta %s',
      (delta) => {
        const { read, change, offset, finish } = setup();
        finish();
        change(delta);
        expect(read).toHaveBeenCalledTimes(1);
        expect(offset()).toBe(0);
      },
    );
    it('does not reload for zero movement or previous at the first page', () => {
      const { read, change, offset, finish } = setup();
      finish();
      change(0);
      change(-20);
      expect(read).toHaveBeenCalledTimes(1);
      expect(offset()).toBe(0);
    });
    if (kind === 'type') {
      it('selects only current results and protects selection during a save', () => {
        const { page, types } = setup();
        const editor = page as ProductCreate;
        const current = { id: 't', name: 'Type', attributeDefinitionCount: 0 };
        editor.selectType(current);
        expect(editor.selected()).toBeNull();
        types.next([current]);
        editor.selectType({ ...current });
        expect(editor.selected()).toBeNull();
        editor.selectType(current);
        expect(editor.selected()).toBe(current);
        const other = { ...current, id: 'other' };
        types.next([other]);
        editor.saving.set(true);
        editor.selectType(other);
        expect(editor.selected()).toBe(current);
        editor.saving.set(false);
        editor.selectType(other);
        expect(editor.selected()).toBe(other);
      });
    }
  });
}
