import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { CategoryNavigation } from './category-navigation';
import { CategoryEditState } from './category-edit-state';
describe('CategoryNavigation', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [CategoryNavigation],
      providers: [CategoryEditState, provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  function setup() {
    const fixture = TestBed.createComponent(CategoryNavigation);
    fixture.componentRef.setInput('category', {
      id: 'c',
      name: 'Occasions',
      parentCategoryId: null,
      isRoot: true,
    });
    fixture.detectChanges();
    return fixture;
  }
  it('loads on request, saves the revision and refreshes public navigation', () => {
    const fixture = setup();
    const editor = fixture.componentInstance;
    http.expectNone('/api/categories/c/navigation');
    editor.load();
    http
      .expectOne('/api/categories/c/navigation')
      .flush({ categoryId: 'c', showInMenu: false, revision: 'r1' });
    editor.showInMenu = true;
    editor.save();
    const request = http.expectOne('/api/categories/c/navigation');
    expect(request.request.body).toEqual({ showInMenu: true, revision: 'r1' });
    expect(editor.busy()).toBe(true);
    request.flush(null);
    http.expectOne('/api/shop/settings').flush({ categoryLinks: [{ id: 'c', name: 'Occasions' }] });
    http
      .expectOne('/api/categories/c/navigation')
      .flush({ categoryId: 'c', showInMenu: true, revision: 'r2' });
    expect(editor.busy()).toBe(false);
    expect(editor.saved()).toBe(true);
    expect(editor.settings()?.revision).toBe('r2');
  });
  it('shows conflicts and does not silently overwrite a newer revision', () => {
    const editor = setup().componentInstance;
    editor.load();
    http
      .expectOne('/api/categories/c/navigation')
      .flush({ categoryId: 'c', showInMenu: false, revision: 'r1' });
    editor.showInMenu = true;
    editor.save();
    http
      .expectOne('/api/categories/c/navigation')
      .flush({}, { status: 409, statusText: 'Conflict' });
    expect(editor.error()).not.toBe('');
    expect(editor.busy()).toBe(false);
    expect(editor.saved()).toBe(false);
  });
  it('ignores a previous category response after navigating to another category', () => {
    const fixture = setup();
    const editor = fixture.componentInstance;
    editor.load();
    const request = http.expectOne('/api/categories/c/navigation');
    fixture.componentRef.setInput('category', {
      id: 'd',
      name: 'Onderdelen',
      parentCategoryId: null,
      isRoot: true,
    });
    fixture.detectChanges();
    request.flush({ categoryId: 'c', showInMenu: true, revision: 'r1' });
    expect(editor.settings()).toBeNull();
  });
});
