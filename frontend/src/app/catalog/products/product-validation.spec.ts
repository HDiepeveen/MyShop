import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ProductValidation } from './product-validation';

describe('ProductValidation', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ProductValidation],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  function setup() {
    const fixture = TestBed.createComponent(ProductValidation);
    fixture.componentRef.setInput('product', {
      id: 'product',
      variants: [{ id: 'variant', name: 'Maat L' }],
    });
    fixture.componentRef.setInput('type', {
      attributeDefinitions: [{ id: 'attribute', displayName: 'Materiaal' }],
    });
    fixture.detectChanges();
    TestBed.tick();
    return fixture;
  }
  it('translates issues with labels instead of raw identifiers', () => {
    const fixture = setup();
    http
      .expectOne('/api/products/product/attribute-validation')
      .flush({
        isValid: false,
        issues: [
          { attributeDefinitionId: 'attribute', variantId: 'variant', code: 'MissingRequired' },
        ],
      });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Materiaal');
    expect(fixture.nativeElement.textContent).toContain('Maat L');
    expect(fixture.nativeElement.textContent).toContain('verplichte waarde');
  });
  it('does not show a valid result after a failed recheck', () => {
    const fixture = setup();
    http
      .expectOne('/api/products/product/attribute-validation')
      .flush({ isValid: true, issues: [] });
    fixture.componentInstance.reload();
    http
      .expectOne('/api/products/product/attribute-validation')
      .flush({}, { status: 500, statusText: 'Error' });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.success')).toBeNull();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
  });
});
