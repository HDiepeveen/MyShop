import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { CategoryEdit } from './category-edit';
import { CategoryEditState } from './category-edit-state';

describe('CategoryEdit', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [CategoryEdit],
      providers: [CategoryEditState, provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  function setup() {
    const fixture = TestBed.createComponent(CategoryEdit);
    fixture.componentRef.setInput('category', {
      id: 'c',
      name: 'Old',
      parentCategoryId: null,
      isRoot: true,
    });
    fixture.detectChanges();
    return fixture;
  }
  it('only saves a changed nonempty name and emits after the server succeeds', () => {
    const editor = setup().componentInstance;
    const saved = vi.fn();
    editor.saved.subscribe(saved);
    editor.rename();
    editor.name = ' ';
    editor.rename();
    http.expectNone('/api/categories/c/name');
    editor.name = ' New ';
    editor.rename();
    editor.rename();
    const request = http.expectOne('/api/categories/c/name');
    expect(request.request.method).toBe('PATCH');
    expect(request.request.body).toEqual({ name: 'New' });
    expect(saved).not.toHaveBeenCalled();
    request.flush(null);
    expect(saved).toHaveBeenCalledOnce();
  });
  it('preserves the name after a missing-category response', () => {
    const fixture = setup();
    fixture.componentInstance.name = 'New';
    fixture.componentInstance.rename();
    http.expectOne('/api/categories/c/name').flush({}, { status: 404, statusText: 'Missing' });
    fixture.detectChanges();
    expect(fixture.componentInstance.name).toBe('New');
    expect(fixture.componentInstance.busy()).toBe(false);
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain(
      'niet meer beschikbaar',
    );
  });
  it('blocks rename while another category operation is pending', () => {
    const editor = setup().componentInstance;
    editor.name = 'New';
    TestBed.inject(CategoryEditState).busy.set(true);
    editor.rename();
    http.expectNone('/api/categories/c/name');
    TestBed.inject(CategoryEditState).busy.set(false);
    editor.rename();
    http.expectOne('/api/categories/c/name').flush(null);
  });
});
