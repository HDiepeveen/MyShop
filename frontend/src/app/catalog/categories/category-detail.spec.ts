import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { CategoryDetail } from './category-detail';

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
  it('renders a subcategory and a navigable parent without exposing IDs as labels', () => {
    const fixture = TestBed.createComponent(CategoryDetail);
    http
      .expectOne('/api/categories/c')
      .flush({ id: 'c', name: 'Shirts', isRoot: false, parentCategoryId: 'parent' });
    fixture.detectChanges();
    http
      .expectOne('/api/categories/c/usage')
      .flush({ categoryId: 'c', directChildCount: 0, productAssignmentCount: 0, isInUse: false });
    http.match('/api/categories?offset=0&limit=20').forEach((request) => request.flush([]));
    expect(fixture.nativeElement.querySelector('h1').textContent).toBe('Shirts');
    expect(
      fixture.nativeElement.querySelector('a[href="/categorieen/parent"]').textContent,
    ).toContain('bovenliggende');
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
});
