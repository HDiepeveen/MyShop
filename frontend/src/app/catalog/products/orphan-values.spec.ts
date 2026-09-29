import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { OrphanValues } from './orphan-values';
import { ProductEditState } from './product-edit-state';
describe('OrphanValues', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [OrphanValues],
      providers: [ProductEditState, provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  function setup() {
    const f = TestBed.createComponent(OrphanValues);
    f.componentRef.setInput('productId', 'p');
    f.componentRef.setInput('definitions', [{ id: 'known' }]);
    f.componentRef.setInput('values', [
      { attributeDefinitionId: 'known', dataType: 'Text', value: 'Keep' },
      { attributeDefinitionId: 'gone', dataType: 'Boolean', value: false },
    ]);
    f.detectChanges();
    return f;
  }
  it('offers only orphaned values, preserves false and requires confirmation', () => {
    const f = setup(),
      c = f.componentInstance;
    expect(f.nativeElement.textContent).toContain('Nee');
    expect(f.nativeElement.textContent).not.toContain('Keep');
    c.clear('gone');
    c.confirming.set('known');
    c.clear('known');
    http.expectNone(() => true);
    c.confirming.set('gone');
    const saved = vi.fn();
    c.saved.subscribe(saved);
    c.clear('gone');
    c.clear('gone');
    const req = http.expectOne('/api/products/p/attributes/gone');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
    expect(saved).toHaveBeenCalledOnce();
  });

  it('clears only the selected variant and leaves the product endpoint untouched', () => {
    const f = setup();
    f.componentRef.setInput('variantId', 'v/a');
    f.detectChanges();
    const c = f.componentInstance;
    c.confirming.set('gone');
    c.clear('gone');
    const req = http.expectOne('/api/products/p/variants/v%2Fa/attributes/gone');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
    http.expectNone('/api/products/p/attributes/gone');
  });
  it('does not remove a value whose definition has reappeared', () => {
    const f = setup();
    const c = f.componentInstance;
    c.confirming.set('gone');
    f.componentRef.setInput('definitions', [{ id: 'known' }, { id: 'gone' }]);
    f.detectChanges();
    c.clear('gone');
    http.expectNone(() => true);
    expect(f.nativeElement.textContent.trim()).toBe('');
  });
  it('retains values after failure and observes the product-wide lock', () => {
    const c = setup().componentInstance;
    c.confirming.set('gone');
    TestBed.inject(ProductEditState).busy.set(true);
    c.clear('gone');
    http.expectNone(() => true);
    TestBed.inject(ProductEditState).busy.set(false);
    c.clear('gone');
    http
      .expectOne('/api/products/p/attributes/gone')
      .flush({}, { status: 409, statusText: 'Conflict' });
    expect(c.orphans()).toHaveLength(1);
    expect(c.error()).toBeTruthy();
    expect(c.busy()).toBe(false);
    http.expectNone(() => true);
  });
});
