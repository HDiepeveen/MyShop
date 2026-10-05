import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject, Subject, of } from 'rxjs';
import { CatalogApi } from '../catalog.api';
import { Category, CategorySummary, CategoryUsage } from '../catalog.models';
import { CategoryEdit } from './category-edit';
import { CategoryManagement } from './category-management';
import { CategoryDetail } from './category-detail';
import { CategoryEditState } from './category-edit-state';

const category: Category = { id: 'category-1', name: 'Shirts', parentCategoryId: null, isRoot: true };
const usage: CategoryUsage = { categoryId: category.id, directChildCount: 0, productAssignmentCount: 0, isInUse: false };

describe('Category editor recovery', () => {
  function setup(management = false) {
    const response = new Subject<void>();
    const categories = new Subject<CategorySummary[]>();
    const usageResponse = new Subject<CategoryUsage>();
    const api = {
      renameCategory: vi.fn(() => response), moveCategory: vi.fn(() => response), deleteCategory: vi.fn(() => response),
      categories: vi.fn(() => categories), categoryUsage: vi.fn(() => usageResponse),
    };
    TestBed.configureTestingModule({
      imports: [CategoryEdit, CategoryManagement],
      providers: [CategoryEditState, { provide: CatalogApi, useValue: api }],
    });
    const fixture = management ? TestBed.createComponent(CategoryManagement) : TestBed.createComponent(CategoryEdit);
    fixture.componentRef.setInput('category', category);
    fixture.detectChanges();
    categories.next([]); usageResponse.next(usage);
    const page = fixture.componentInstance;
    function write() {
      if (page instanceof CategoryEdit) { page.name = 'Nieuwe naam'; page.rename(); }
      else { page.parentId = 'parent-1'; page.move(); }
    }
    return { fixture, page, response, categories, usageResponse, api, write };
  }

  for (const management of [false, true]) {
    const name = management ? 'management' : 'rename';
    it.each(['success', 'error'])(`ignores a late ${name} %s after switching categories`, (result) => {
      const { fixture, page, response, write } = setup(management);
      const saved = vi.fn();
      page.saved.subscribe(saved);
      write();
      fixture.componentRef.setInput('category', { ...category, id: 'category-2', name: 'Broeken' });
      fixture.detectChanges();
      if (result === 'success') response.next();
      else response.error(new Error('Offline'));
      expect(saved).not.toHaveBeenCalled();
      expect(page.error()).toBe('');
      expect(page.busy()).toBe(false);
    });

    it(`releases only its own ${name} write lock on destruction`, () => {
      const { fixture, response, write } = setup(management);
      write();
      expect(TestBed.inject(CategoryEditState).busy()).toBe(true);
      fixture.destroy();
      expect(response.observed).toBe(false);
      expect(TestBed.inject(CategoryEditState).busy()).toBe(false);
    });

    it(`preserves another editor's lock when the idle ${name} editor is destroyed`, () => {
      const { fixture } = setup(management);
      TestBed.inject(CategoryEditState).busy.set(true);
      fixture.destroy();
      expect(TestBed.inject(CategoryEditState).busy()).toBe(true);
    });
  }

  it('keeps the chosen parent while the move is pending', () => {
    const { page, response, write } = setup(true);
    const editor = page as CategoryManagement;
    write();
    editor.resetParent();
    expect(editor.parentId).toBe('parent-1');
    response.error(new Error('Offline'));
    editor.resetParent();
    expect(editor.parentId).toBeNull();
  });

  it('does not restart usage reads while already loading', () => {
    const { page, api } = setup(true);
    const editor = page as CategoryManagement;
    api.categoryUsage.mockReturnValue(new Subject<CategoryUsage>());
    editor.reloadUsage();
    editor.reloadUsage();
    expect(api.categoryUsage).toHaveBeenCalledTimes(2);
  });

  it('does not restart the parent category list while already loading', () => {
    const { page, api } = setup(true);
    const editor = page as CategoryManagement;
    api.categories.mockReturnValue(new Subject<CategorySummary[]>());
    editor.retryCategories();
    editor.retryCategories();
    expect(api.categories).toHaveBeenCalledTimes(2);
  });

  it('does not move a category while its parent options are loading', () => {
    const { page, api } = setup(true);
    const editor = page as CategoryManagement;
    api.categories.mockReturnValue(new Subject<CategorySummary[]>());
    editor.retryCategories();
    editor.parentId = 'parent-1';
    editor.move();
    expect(api.moveCategory).not.toHaveBeenCalled();
  });

  function detailSetup() {
    const response = new Subject<Category>();
    const parent = new Subject<Category>();
    const api = {
      category: vi.fn((id: string) => id === category.id ? response : parent),
      categories: vi.fn(() => of([])), categoryUsage: vi.fn(() => of(usage)),
    };
    TestBed.configureTestingModule({ imports: [CategoryDetail], providers: [provideRouter([]),
      { provide: CatalogApi, useValue: api },
      { provide: ActivatedRoute, useValue: { paramMap: new BehaviorSubject(convertToParamMap({ id: category.id })), queryParamMap: of(convertToParamMap({})) } },
    ] });
    return { fixture: TestBed.createComponent(CategoryDetail), response, parent, api };
  }

  it('blocks detail reloads while loading or editing', () => {
    const { fixture, response, api } = detailSetup();
    fixture.componentInstance.reload();
    expect(api.category).toHaveBeenCalledTimes(1);
    response.next(category);
    fixture.componentInstance.editState.busy.set(true);
    fixture.componentInstance.reload();
    expect(api.category).toHaveBeenCalledTimes(1);
    fixture.componentInstance.editState.busy.set(false);
    fixture.componentInstance.reload();
    expect(api.category).toHaveBeenCalledTimes(2);
  });

  it('does not restart the parent name request while loading', () => {
    const { fixture, response, parent, api } = detailSetup();
    response.next({ ...category, parentCategoryId: 'parent-1', isRoot: false });
    fixture.detectChanges();
    fixture.componentInstance.retryParent();
    expect(api.category.mock.calls.filter(([id]) => id === 'parent-1')).toHaveLength(1);
    parent.error(new Error('Offline'));
    api.category.mockReturnValue(new Subject<Category>());
    fixture.componentInstance.retryParent();
    expect(api.category.mock.calls.filter(([id]) => id === 'parent-1')).toHaveLength(2);
  });
});
