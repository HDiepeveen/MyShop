import { ProductEditState } from './product-edit-state';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { ProductCategories } from './product-categories';

describe('ProductCategories', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ProductCategories],
      providers: [
        ProductEditState,
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  function setup(categoryIds: string[] = []) {
    const fixture = TestBed.createComponent(ProductCategories);
    fixture.componentRef.setInput('product', { id: 'p', categoryIds });
    fixture.detectChanges();
    TestBed.tick();
    return fixture;
  }
  it('resolves assigned names and reports a failed lookup without hiding other categories', () => {
    const fixture = setup(['a', 'b']);
    http.expectOne('/api/categories/a').flush({ id: 'a', name: 'Wonen' });
    http.expectOne('/api/categories/b').flush({}, { status: 404, statusText: 'Missing' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Wonen');
    expect(fixture.nativeElement.textContent).toContain('kon niet worden opgehaald');
    expect(fixture.nativeElement.querySelector('a[href="/categorieen/a"]')).not.toBeNull();
  });
  it('pages with the submitted search and cancels obsolete picker requests', () => {
    const editor = setup().componentInstance;
    editor.togglePicker();
    const initial = http.expectOne((r) => r.url === '/api/categories');
    editor.searchText = ' Wonen ';
    editor.search();
    expect(initial.cancelled).toBe(true);
    const search = http.expectOne((r) => r.params.get('search') === 'Wonen');
    search.flush(Array.from({ length: 20 }, (_, i) => ({ id: String(i), name: 'Category' })));
    editor.searchText = 'Unsubmitted';
    editor.changePage(20);
    const next = http.expectOne((r) => r.params.get('offset') === '20');
    expect(next.request.params.get('search')).toBe('Wonen');
    next.flush([]);
    editor.changePage(-20);
    http.expectOne((r) => r.params.get('offset') === '0').flush([]);
  });

  it('removes only existing links and keeps them visible until persistence succeeds', () => {
    const fixture = setup(['a']);
    http.expectOne('/api/categories/a').flush({ id: 'a', name: 'A' });
    const editor = fixture.componentInstance;
    const saved = vi.fn();
    editor.saved.subscribe(saved);
    editor.remove('missing');
    http.expectNone('/api/products/p/categories/missing');
    editor.remove('a');
    editor.remove('a');
    editor.assign('b');
    http.expectNone('/api/products/p/categories/b');
    const request = http.expectOne('/api/products/p/categories/a');
    expect(request.request.method).toBe('DELETE');
    request.flush({}, { status: 500, statusText: 'Failure' });
    expect(editor.isAssigned('a')).toBe(true);
    expect(saved).not.toHaveBeenCalled();
    editor.remove('a');
    http.expectOne('/api/products/p/categories/a').flush(null);
    expect(saved).toHaveBeenCalledExactlyOnceWith('De categorie is ontkoppeld.');
  });

  it('only renders removal controls for assigned categories, not picker results', () => {
    const fixture = setup();
    fixture.componentInstance.togglePicker();
    http
      .expectOne((r) => r.url === '/api/categories')
      .flush([{ id: 'c', name: 'Available', isRoot: true }]);
    fixture.detectChanges();
    const buttons = Array.from(
      fixture.nativeElement.querySelectorAll('button'),
    ) as HTMLButtonElement[];
    expect(buttons.some((button) => button.textContent?.includes('Ontkoppelen'))).toBe(false);
  });
  it('prevents duplicate and already assigned writes and retains search after failure', () => {
    const fixture = setup(['a']);
    http.expectOne('/api/categories/a').flush({ id: 'a', name: 'A' });
    const editor = fixture.componentInstance;
    const saved = vi.fn();
    editor.saved.subscribe(saved);
    editor.assign('a');
    http.expectNone('/api/products/p/categories/a');
    editor.searchText = 'Keep';
    editor.assign('b');
    editor.assign('b');
    const request = http.expectOne('/api/products/p/categories/b');
    expect(request.request.method).toBe('PUT');
    request.flush({}, { status: 409, statusText: 'Conflict' });
    expect(saved).not.toHaveBeenCalled();
    expect(editor.searchText).toBe('Keep');
    expect(editor.busy()).toBe(false);
    editor.assign('b');
    http.expectOne('/api/products/p/categories/b').flush(null);
    expect(saved).toHaveBeenCalledExactlyOnceWith('De categorie is gekoppeld.');
  });
});
