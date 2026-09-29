import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { CategoryManagement } from './category-management';
import { CategoryEditState } from './category-edit-state';

describe('CategoryManagement', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [CategoryManagement],
      providers: [CategoryEditState, provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  function setupFixture() {
    const fixture = TestBed.createComponent(CategoryManagement);
    fixture.componentRef.setInput('category', {
      id: 'c/a',
      name: 'Shirts',
      parentCategoryId: null,
      isRoot: true,
    });
    fixture.detectChanges();
    http
      .expectOne('/api/categories/c%2Fa/usage')
      .flush({ categoryId: 'c/a', directChildCount: 0, productAssignmentCount: 0, isInUse: false });
    http
      .expectOne('/api/categories?offset=0&limit=20')
      .flush([
        { id: 'p/b', name: 'Kleding', parentCategoryId: null, isRoot: true, directChildCount: 1 },
      ]);
    fixture.detectChanges();
    return fixture;
  }
  function setup() {
    return setupFixture().componentInstance;
  }
  it('moves a category with a nullable parent and emits after success', () => {
    const editor = setup();
    editor.parentId = 'p/b';
    const saved = vi.fn();
    editor.saved.subscribe(saved);
    editor.move();
    const request = http.expectOne('/api/categories/c%2Fa/parent');
    expect(request.request.body).toEqual({ parentCategoryId: 'p/b' });
    request.flush(null);
    expect(saved).toHaveBeenCalledOnce();
  });
  it('requires confirmation and usage safety before deletion', () => {
    const editor = setup();
    editor.remove();
    http.expectNone((r) => r.method === 'DELETE');
    editor.confirming.set(true);
    editor.remove();
    const request = http.expectOne('/api/categories/c%2Fa');
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
  });
  it('retains a chosen parent while paging and resets it without a write', async () => {
    const fixture = setupFixture();
    const editor = fixture.componentInstance;
    editor.parentId = 'p/b';
    editor.rememberParent();
    editor.changeCategoryPage(20);
    expect(editor.selectedParentName).toBe('Kleding');
    http.expectOne('/api/categories?offset=20&limit=20').flush([]);
    expect(editor.parentOnPage()).toBe(false);
    expect(editor.parentId).toBe('p/b');
    fixture.detectChanges();
    await fixture.whenStable();
    const select = fixture.nativeElement.querySelector('select') as HTMLSelectElement;
    expect(select.selectedOptions[0].textContent).toContain('Kleding');
    expect(fixture.nativeElement.textContent).toContain('Geen andere categorieën gevonden');
    editor.resetParent();
    expect(editor.parentId).toBeNull();
    http.expectNone((request) => request.method !== 'GET');
  });
  it('blocks self parenting and shares the category write lock', () => {
    const editor = setup();
    editor.parentId = 'c/a';
    editor.move();
    expect(editor.error()).toContain('zichzelf');
    editor.parentId = 'p/b';
    TestBed.inject(CategoryEditState).busy.set(true);
    editor.move();
    editor.confirming.set(true);
    editor.remove();
    http.expectNone((request) => request.method !== 'GET');
  });
  it('does not offer deletion when a category is in use', () => {
    const fixture = TestBed.createComponent(CategoryManagement);
    fixture.componentRef.setInput('category', {
      id: 'c',
      name: 'In gebruik',
      parentCategoryId: null,
      isRoot: true,
    });
    fixture.detectChanges();
    http
      .expectOne('/api/categories/c/usage')
      .flush({ categoryId: 'c', directChildCount: 1, productAssignmentCount: 2, isInUse: true });
    http.expectOne('/api/categories?offset=0&limit=20').flush([]);
    fixture.detectChanges();
    expect(fixture.componentInstance.canDelete()).toBe(false);
    fixture.componentInstance.confirming.set(true);
    fixture.componentInstance.remove();
    http.expectNone((r) => r.method === 'DELETE');
  });
  it('refreshes usage after refused deletion and requires new confirmation', () => {
    const editor = setup();
    editor.confirming.set(true);
    editor.remove();
    http.expectOne('/api/categories/c%2Fa').flush({}, { status: 409, statusText: 'Conflict' });
    expect(editor.confirming()).toBe(false);
    expect(editor.canDelete()).toBe(false);
    expect(editor.error()).toBeTruthy();
    http.expectOne('/api/categories/c%2Fa/usage').flush({
      categoryId: 'c/a',
      directChildCount: 0,
      productAssignmentCount: 1,
      isInUse: true,
    });
    expect(editor.canDelete()).toBe(false);
    editor.remove();
    http.expectNone((request) => request.method === 'DELETE');
  });
  it('keeps deletion unavailable on a usage read failure and can retry', () => {
    const editor = setup();
    editor.confirming.set(true);
    editor.reloadUsage();
    expect(editor.confirming()).toBe(false);
    expect(editor.canDelete()).toBe(false);
    http
      .expectOne('/api/categories/c%2Fa/usage')
      .flush({}, { status: 503, statusText: 'Unavailable' });
    expect(editor.canDelete()).toBe(false);
    editor.reloadUsage();
    http.expectOne('/api/categories/c%2Fa/usage').flush({
      categoryId: 'c/a',
      directChildCount: 0,
      productAssignmentCount: 0,
      isInUse: false,
    });
    expect(editor.canDelete()).toBe(true);
  });
  it('keeps the applied search while paging and resets the page for a new search', () => {
    const editor = setup();
    editor.categorySearch = 'kleding';
    editor.searchCategories();
    http.expectOne('/api/categories?offset=0&limit=20&search=kleding').flush([]);
    editor.categorySearch = 'schoenen';
    editor.changeCategoryPage(20);
    http.expectOne('/api/categories?offset=20&limit=20&search=kleding').flush([]);
    expect(editor.categoryOffset()).toBe(20);
    editor.searchCategories();
    http.expectOne('/api/categories?offset=0&limit=20&search=schoenen').flush([]);
    expect(editor.categoryOffset()).toBe(0);
  });
  it('does not change the picker query during a category write', () => {
    const editor = setup();
    editor.parentId = 'p/b';
    editor.move();
    const write = http.expectOne('/api/categories/c%2Fa/parent');
    editor.categorySearch = 'schoenen';
    editor.searchCategories();
    editor.changeCategoryPage(20);
    editor.retryCategories();
    http.expectNone((request) => request.url === '/api/categories');
    expect(editor.categoryOffset()).toBe(0);
    write.flush(null);
  });
  it('can retry a failed parent category read', () => {
    const fixture = TestBed.createComponent(CategoryManagement);
    fixture.componentRef.setInput('category', {
      id: 'c',
      name: 'Shirts',
      parentCategoryId: null,
      isRoot: true,
    });
    fixture.detectChanges();
    http.expectOne('/api/categories/c/usage').flush({
      categoryId: 'c',
      directChildCount: 0,
      productAssignmentCount: 0,
      isInUse: false,
    });
    http
      .expectOne('/api/categories?offset=0&limit=20')
      .flush({}, { status: 503, statusText: 'Unavailable' });
    expect(fixture.componentInstance.categories()?.error).toBeTruthy();
    fixture.componentInstance.retryCategories();
    http.expectOne('/api/categories?offset=0&limit=20').flush([]);
    expect(fixture.componentInstance.categories()?.data).toEqual([]);
  });
});
