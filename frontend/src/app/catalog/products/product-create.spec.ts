import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { ProductCreate } from './product-create';

describe('ProductCreate', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ProductCreate],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  it('requires a selected type and names before creating', () => {
    const fixture = TestBed.createComponent(ProductCreate);
    http.expectOne((r) => r.url === '/api/product-types').flush([]);
    fixture.componentInstance.name = 'Product';
    fixture.componentInstance.variantName = 'First';
    fixture.componentInstance.create();
    http.expectNone('/api/products');
  });
  it('creates once and opens the returned product', () => {
    const fixture = TestBed.createComponent(ProductCreate);
    http.expectOne((r) => r.url === '/api/product-types').flush([]);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture.componentInstance.selected.set({
      id: 'type',
      name: 'Type',
      attributeDefinitionCount: 0,
    });
    fixture.componentInstance.name = 'Product';
    fixture.componentInstance.variantName = 'First';
    fixture.componentInstance.create();
    fixture.componentInstance.create();
    http.expectOne('/api/products').flush({ id: 'new-id' });
    expect(navigate).toHaveBeenCalledWith(['/producten', 'new-id']);
  });
  it('keeps the chosen type across picker pages and preserves input on a failed save', () => {
    const fixture = TestBed.createComponent(ProductCreate);
    http.expectOne((r) => r.url === '/api/product-types').flush([]);
    fixture.componentInstance.selected.set({
      id: 'type',
      name: 'Type',
      attributeDefinitionCount: 0,
    });
    fixture.componentInstance.changePage(20);
    http.expectOne((r) => r.params.get('offset') === '20').flush([]);
    expect(fixture.componentInstance.selected()?.id).toBe('type');
    fixture.componentInstance.name = 'Product';
    fixture.componentInstance.variantName = 'First';
    fixture.componentInstance.create();
    http.expectOne('/api/products').flush({}, { status: 400, statusText: 'Bad request' });
    expect(fixture.componentInstance.name).toBe('Product');
    expect(fixture.componentInstance.saving()).toBe(false);
  });
});
