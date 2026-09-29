import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { CategoryDetail } from './category-detail';
import { By } from '@angular/platform-browser';
import { CategoryEdit } from './category-edit';
import { CategoryManagement } from './category-management';

describe('CategoryDetail', () => {
  let http: HttpTestingController;
  const params = new BehaviorSubject(convertToParamMap({ id: 'c' }));
  beforeEach(() => {
    params.next(convertToParamMap({ id: 'c' }));
    TestBed.configureTestingModule({
      imports: [CategoryDetail],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { paramMap: params } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  it('coordinates rename and hierarchy writes through the detail provider', () => {
    const fixture = TestBed.createComponent(CategoryDetail);
    http
      .expectOne('/api/categories/c')
      .flush({ id: 'c', name: 'Shirts', isRoot: true, parentCategoryId: null });
    fixture.detectChanges();
    http
      .expectOne('/api/categories/c/usage')
      .flush({ categoryId: 'c', directChildCount: 0, productAssignmentCount: 0, isInUse: false });
    http.expectOne('/api/categories?offset=0&limit=20').flush([]);
    const rename = fixture.debugElement.query(By.directive(CategoryEdit))
      .componentInstance as CategoryEdit;
    const management = fixture.debugElement.query(By.directive(CategoryManagement))
      .componentInstance as CategoryManagement;
    rename.name = 'New';
    rename.rename();
    management.parentId = 'parent';
    management.move();
    management.confirming.set(true);
    management.remove();
    http.expectNone((r) => r.method === 'PUT' || r.method === 'DELETE');
    http.expectOne('/api/categories/c/name').flush({}, { status: 409, statusText: 'Conflict' });
    expect(management.busy()).toBe(false);
    management.move();
    rename.rename();
    http.expectNone('/api/categories/c/name');
    http.expectOne('/api/categories/c/parent').flush({}, { status: 409, statusText: 'Conflict' });
    expect(rename.busy()).toBe(false);
  });
  it('renders a subcategory and a navigable parent without exposing IDs as labels', () => {
    const fixture = TestBed.createComponent(CategoryDetail);
    http
      .expectOne('/api/categories/c')
      .flush({ id: 'c', name: 'Shirts', isRoot: false, parentCategoryId: 'parent' });
    fixture.detectChanges();
    http
      .expectOne('/api/categories/c/usage')
      .flush({ categoryId: 'c', directChildCount: 0, productAssignmentCount: 0, isInUse: false });
    http.expectOne('/api/categories/parent').flush({
      id: 'parent',
      name: 'Clothing',
      isRoot: true,
      parentCategoryId: null,
    });
    http.match('/api/categories?offset=0&limit=20').forEach((request) => request.flush([]));
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('h1').textContent).toBe('Shirts');
    expect(fixture.nativeElement.querySelector('a[href="/producten?categoryId=c"]')).not.toBeNull();
    expect(
      fixture.nativeElement.querySelector('a[href="/categorieen/parent"]').textContent,
    ).toContain('Bovenliggende');
    expect(
      fixture.nativeElement.querySelector('a[href="/categorieen/parent"]').textContent,
    ).toContain('Clothing');
  });

  it('keeps the parent link on failure and clears parent state on navigation', () => {
    const fixture = TestBed.createComponent(CategoryDetail);
    http.expectOne('/api/categories/c').flush({
      id: 'c',
      name: 'Shirts',
      isRoot: false,
      parentCategoryId: 'parent',
    });
    fixture.detectChanges();
    http.expectOne('/api/categories/c/usage').flush({
      categoryId: 'c',
      directChildCount: 0,
      productAssignmentCount: 0,
      isInUse: false,
    });
    http.expectOne('/api/categories?offset=0&limit=20').flush([]);
    http.expectOne('/api/categories/parent').flush({}, { status: 503, statusText: 'Unavailable' });
    fixture.detectChanges();
    expect(
      fixture.nativeElement.querySelector('a[href="/categorieen/parent"]').textContent,
    ).toContain('Bekijk bovenliggende categorie');
    fixture.componentInstance.retryParent();
    http.expectOne('/api/categories/parent').flush({
      id: 'parent',
      name: 'Clothing',
      isRoot: true,
      parentCategoryId: null,
    });
    fixture.detectChanges();
    expect(
      fixture.nativeElement.querySelector('a[href="/categorieen/parent"]').textContent,
    ).toContain('Clothing');
    params.next(convertToParamMap({ id: 'root' }));
    http.expectOne('/api/categories/root').flush({
      id: 'root',
      name: 'Root',
      isRoot: true,
      parentCategoryId: null,
    });
    fixture.detectChanges();
    http.expectOne('/api/categories/root/usage').flush({
      categoryId: 'root',
      directChildCount: 0,
      productAssignmentCount: 0,
      isInUse: false,
    });
    http.match('/api/categories?offset=0&limit=20').forEach((request) => request.flush([]));
    expect(fixture.componentInstance.parentState()).toBeNull();
    expect(fixture.nativeElement.querySelector('a[href="/categorieen/parent"]')).toBeNull();
  });

  it('clears the previous category success message on route changes', () => {
    const fixture = TestBed.createComponent(CategoryDetail);
    http.expectOne('/api/categories/c').flush({ id: 'c', name: 'Category', isRoot: true });
    fixture.detectChanges();
    http
      .expectOne('/api/categories/c/usage')
      .flush({ categoryId: 'c', directChildCount: 0, productAssignmentCount: 0, isInUse: false });
    http.match('/api/categories?offset=0&limit=20').forEach((request) => request.flush([]));
    fixture.componentInstance.notice.set('Previous success');
    params.next(convertToParamMap({ id: 'other' }));
    http.expectOne('/api/categories/other').flush({ id: 'other', name: 'Other', isRoot: true });
    fixture.detectChanges();
    http.expectOne('/api/categories/other/usage').flush({
      categoryId: 'other',
      directChildCount: 0,
      productAssignmentCount: 0,
      isInUse: false,
    });
    http.match('/api/categories?offset=0&limit=20').forEach((request) => request.flush([]));
    expect(fixture.componentInstance.notice()).toBe('');
  });
  it('cancels stale category reads and can retry a failure', () => {
    const fixture = TestBed.createComponent(CategoryDetail);
    const old = http.expectOne('/api/categories/c');
    params.next(convertToParamMap({ id: 'new' }));
    expect(old.cancelled).toBe(true);
    http.expectOne('/api/categories/new').flush({}, { status: 404, statusText: 'Missing' });
    http
      .match('/api/categories/new/usage')
      .forEach((request) => request.flush({}, { status: 404, statusText: 'Missing' }));
    http.match('/api/categories?offset=0&limit=20').forEach((request) => request.flush([]));
    expect(fixture.componentInstance.state()?.data).toBeNull();
    expect(fixture.componentInstance.state()?.error).toBeTruthy();
    fixture.componentInstance.reload();
    http
      .expectOne('/api/categories/new')
      .flush({ id: 'new', name: 'New', isRoot: true, parentCategoryId: null });
    fixture.detectChanges();
    http
      .expectOne('/api/categories/new/usage')
      .flush({ categoryId: 'new', directChildCount: 0, productAssignmentCount: 0, isInUse: false });
    http.match('/api/categories?offset=0&limit=20').forEach((request) => request.flush([]));
    expect(fixture.componentInstance.state()?.error).toBe('');
  });
  it('clears a success notice on writes and preserves the lock for equivalent routes', () => {
    const fixture = TestBed.createComponent(CategoryDetail);
    http.expectOne('/api/categories/c').flush({}, { status: 404, statusText: 'Missing' });
    fixture.componentInstance.notice.set('Saved');
    fixture.componentInstance.editState.busy.set(true);
    fixture.detectChanges();
    expect(fixture.componentInstance.notice()).toBe('');
    params.next(convertToParamMap({ id: 'c' }));
    expect(fixture.componentInstance.editState.busy()).toBe(true);
    http.expectNone('/api/categories/c');
    params.next(convertToParamMap({ id: 'new' }));
    expect(fixture.componentInstance.editState.busy()).toBe(false);
    http.expectOne('/api/categories/new').flush({}, { status: 404, statusText: 'Missing' });
  });
});
