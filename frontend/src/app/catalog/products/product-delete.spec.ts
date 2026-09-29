import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ProductDelete } from './product-delete';
import { ProductEditState } from './product-edit-state';

describe('ProductDelete', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ProductDelete],
      providers: [ProductEditState, provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  it('requires confirmation, deletes once and emits after success', () => {
    const fixture = TestBed.createComponent(ProductDelete);
    fixture.componentRef.setInput('productId', 'p/a');
    fixture.componentRef.setInput('productName', 'Product');
    fixture.detectChanges();
    const editor = fixture.componentInstance;
    editor.remove();
    http.expectNone((r) => r.method === 'DELETE');
    editor.confirming.set(true);
    editor.remove();
    editor.remove();
    const request = http.expectOne('/api/products/p%2Fa');
    expect(request.request.method).toBe('DELETE');
    const removed = vi.fn();
    editor.removed.subscribe(removed);
    request.flush(null);
    expect(removed).toHaveBeenCalledOnce();
  });
  it('keeps the confirmation and reports a failed deletion', () => {
    const fixture = TestBed.createComponent(ProductDelete);
    fixture.componentRef.setInput('productId', 'p');
    fixture.componentRef.setInput('productName', 'Product');
    fixture.detectChanges();
    const editor = fixture.componentInstance;
    editor.confirming.set(true);
    editor.remove();
    http.expectOne('/api/products/p').flush({}, { status: 404, statusText: 'Missing' });
    expect(editor.confirming()).toBe(true);
    expect(editor.error()).toBeTruthy();
    expect(editor.busy()).toBe(false);
  });
});
