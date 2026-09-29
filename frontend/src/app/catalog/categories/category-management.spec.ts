import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { CategoryManagement } from './category-management';

describe('CategoryManagement', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [CategoryManagement],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  function setup() {
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
    return fixture.componentInstance;
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
  it('searches and pages parent categories', () => {
    const editor = setup();
    editor.categorySearch = 'kleding';
    editor.searchCategories();
    http.expectOne('/api/categories?offset=0&limit=20&search=kleding').flush([]);
    editor.changeCategoryPage(20);
    http.expectOne('/api/categories?offset=20&limit=20&search=kleding').flush([]);
    expect(editor.categoryOffset()).toBe(20);
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
