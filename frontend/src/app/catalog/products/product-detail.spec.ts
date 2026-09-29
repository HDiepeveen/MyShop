import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { ProductDetail } from './product-detail';

describe('ProductDetail', () => {
  let http: HttpTestingController;
  const params = new BehaviorSubject(convertToParamMap({ id: 'first' }));
  beforeEach(() => {
    params.next(convertToParamMap({ id: 'first' }));
    TestBed.configureTestingModule({
      imports: [ProductDetail],
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
  it('loads current product type and renders safe text', () => {
    const fixture = TestBed.createComponent(ProductDetail);
    http
      .expectOne('/api/products/first')
      .flush({
        id: 'first',
        name: '<img src=x>',
        productTypeId: 'type',
        variants: [],
        categoryIds: [],
        attributeValues: [],
      });
    http
      .expectOne('/api/product-types/type')
      .flush({ id: 'type', name: 'Type', attributeDefinitions: [] });
    fixture.detectChanges();
    TestBed.tick();
    http.expectOne('/api/products/first/attribute-validation').flush({ isValid: true, issues: [] });
    expect(fixture.nativeElement.querySelector('h1').textContent).toBe('<img src=x>');
    expect(fixture.nativeElement.querySelector('img')).toBeNull();
  });
  it('cancels the old product read on route changes', () => {
    const fixture = TestBed.createComponent(ProductDetail);
    const first = http.expectOne('/api/products/first');
    params.next(convertToParamMap({ id: 'second' }));
    expect(first.cancelled).toBe(true);
    http.expectOne('/api/products/second').flush({}, { status: 404, statusText: 'Not found' });
    expect(fixture.componentInstance.state()?.data).toBeNull();
    expect(fixture.componentInstance.state()?.error).toContain('niet meer beschikbaar');
  });
});
