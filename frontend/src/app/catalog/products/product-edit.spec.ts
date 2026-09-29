import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ProductEdit } from './product-edit';

describe('ProductEdit', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ProductEdit],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  function setup() {
    const fixture = TestBed.createComponent(ProductEdit);
    fixture.componentRef.setInput('product', { id: 'product', name: 'Original', variants: [] });
    fixture.detectChanges();
    return fixture;
  }
  it('does not write unchanged names and emits success only after persistence', () => {
    const fixture = setup();
    const saved = vi.fn();
    fixture.componentInstance.saved.subscribe(saved);
    fixture.componentInstance.rename();
    http.expectNone('/api/products/product/name');
    fixture.componentInstance.name = 'New';
    fixture.componentInstance.rename();
    fixture.componentInstance.rename();
    const request = http.expectOne('/api/products/product/name');
    expect(saved).not.toHaveBeenCalled();
    expect(request.request.body).toEqual({ name: 'New' });
    request.flush(null);
    expect(saved).toHaveBeenCalledOnce();
  });
  it('keeps variant input and reports conflicts without a success event', () => {
    const fixture = setup();
    const saved = vi.fn();
    fixture.componentInstance.saved.subscribe(saved);
    fixture.componentInstance.variantName = 'Variant';
    fixture.componentInstance.addVariant();
    http
      .expectOne('/api/products/product/variants')
      .flush({}, { status: 409, statusText: 'Conflict' });
    expect(fixture.componentInstance.variantName).toBe('Variant');
    expect(saved).not.toHaveBeenCalled();
    expect(fixture.componentInstance.busy()).toBe(false);
  });
});
