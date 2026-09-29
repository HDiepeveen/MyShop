import { TestBed } from '@angular/core/testing';
import { provideHttpClient, HttpErrorResponse } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { CatalogApi } from './catalog.api';
import { errorMessage } from './error-message';

describe('CatalogApi', () => {
  let api: CatalogApi;
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(CatalogApi);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  it('encodes search separately and preserves page offset', () => {
    api.products(20, '  A&B  ').subscribe();
    const request = http.expectOne((r) => r.url === '/api/products');
    expect(request.request.params.get('search')).toBe('A&B');
    expect(request.request.params.get('offset')).toBe('20');
    expect(request.request.params.get('limit')).toBe('20');
    request.flush({ items: [], offset: 20, limit: 20, totalCount: 0 });
  });
  it('omits empty searches for paged arrays', () => {
    api.types(40, '  ').subscribe();
    const request = http.expectOne((r) => r.url === '/api/product-types');
    expect(request.request.params.has('search')).toBe(false);
    expect(request.request.params.get('offset')).toBe('40');
    request.flush([]);
  });
  it('creates a product with its selected type and initial variant', () => {
    api.createProduct('type-id', ' Product ', ' Standard ').subscribe();
    const request = http.expectOne('/api/products');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      productTypeId: 'type-id',
      name: 'Product',
      initialVariantName: 'Standard',
    });
    request.flush({ id: 'product-id' });
  });
  it('does not retry a failed mutation', () => {
    api.renameProduct('id', ' New ').subscribe({ error: () => {} });
    const request = http.expectOne('/api/products/id/name');
    expect(request.request.method).toBe('PATCH');
    request.flush({}, { status: 409, statusText: 'Conflict' });
    http.expectNone('/api/products/id/name');
  });
  it('translates failures without leaking server internals', () => {
    const message = errorMessage(
      new HttpErrorResponse({ status: 500, error: { detail: 'SQL password' } }),
    );
    expect(message).not.toContain('SQL');
    expect(errorMessage(new HttpErrorResponse({ status: 0 }))).toContain('niet bereikbaar');
  });
});
