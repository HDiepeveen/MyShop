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
  it('encodes variant identifiers and sends normalized writes', () => {
    api.renameVariant('p/a', 'v/b', ' New ').subscribe();
    const rename = http.expectOne('/api/products/p%2Fa/variants/v%2Fb/name');
    expect(rename.request.method).toBe('PATCH');
    expect(rename.request.body).toEqual({ name: 'New' });
    rename.flush(null);
    api.setVariantSku('p', 'v', ' SKU-1 ').subscribe();
    const sku = http.expectOne('/api/products/p/variants/v/sku');
    expect(sku.request.method).toBe('PUT');
    expect(sku.request.body).toEqual({ sku: 'SKU-1' });
    sku.flush(null);
    api.setVariantPrice('p', 'v', 12.5, ' eur ').subscribe();
    const price = http.expectOne('/api/products/p/variants/v/price');
    expect(price.request.method).toBe('PUT');
    expect(price.request.body).toEqual({ amount: 12.5, currency: 'EUR' });
    price.flush(null);
  });
  it('clears optional variant values with DELETE and does not retry failures', () => {
    api.clearVariantSku('p', 'v').subscribe();
    const sku = http.expectOne('/api/products/p/variants/v/sku');
    expect(sku.request.method).toBe('DELETE');
    sku.flush(null);
    api.clearVariantPrice('p', 'v').subscribe({ error: () => {} });
    const price = http.expectOne('/api/products/p/variants/v/price');
    expect(price.request.method).toBe('DELETE');
    price.flush({}, { status: 409, statusText: 'Conflict' });
    http.expectNone('/api/products/p/variants/v/price');
  });

  it('sends attribute bodies unchanged with JSON content type at the correct scope', () => {
    const body = '{"dataType":1,"value":9223372036854775807}';
    api.setAttribute('p/a', 'd/b', body).subscribe();
    const product = http.expectOne('/api/products/p%2Fa/attributes/d%2Fb');
    expect(product.request.body).toBe(body);
    expect(product.request.headers.get('Content-Type')).toBe('application/json');
    expect(product.request.method).toBe('PUT');
    product.flush(null);
    api.setAttribute('p', 'd', body, 'v/a').subscribe();
    const variant = http.expectOne('/api/products/p/variants/v%2Fa/attributes/d');
    expect(variant.request.body).toBe(body);
    variant.flush(null);
    api.clearAttribute('p', 'd', 'v').subscribe();
    const clear = http.expectOne('/api/products/p/variants/v/attributes/d');
    expect(clear.request.method).toBe('DELETE');
    clear.flush(null);
  });

  it('uses encoded variant price-rule URLs and preserves rule values', () => {
    api.listPriceRules('p/a', 'v/b').subscribe();
    const list = http.expectOne('/api/products/p%2Fa/variants/v%2Fb/price-rules');
    expect(list.request.method).toBe('GET');
    list.flush({ rules: [], revision: 'r' });
    api
      .addPriceRule('p', 'v', {
        name: ' Sale ',
        adjustmentType: 1,
        value: 12.5,
        priority: 2,
        startsAt: null,
        endsAt: null,
      })
      .subscribe();
    const add = http.expectOne('/api/products/p/variants/v/price-rules');
    expect(add.request.method).toBe('POST');
    expect(add.request.body.name).toBe('Sale');
    expect(add.request.body.value).toBe(12.5);
    add.flush({ id: 'new' });
    api
      .updatePriceRule('p', 'v', {
        id: 'r/1',
        name: 'New',
        adjustmentType: 2,
        value: 5,
        priority: 1,
        startsAt: null,
        endsAt: null,
      })
      .subscribe();
    const update = http.expectOne('/api/products/p/variants/v/price-rules/r%2F1');
    expect(update.request.method).toBe('PUT');
    update.flush(null);
    api.removePriceRule('p', 'v', 'r/1').subscribe();
    const remove = http.expectOne('/api/products/p/variants/v/price-rules/r%2F1');
    expect(remove.request.method).toBe('DELETE');
    remove.flush(null);
  });
  it('translates failures without leaking server internals', () => {
    const message = errorMessage(
      new HttpErrorResponse({ status: 500, error: { detail: 'SQL password' } }),
    );
    expect(message).not.toContain('SQL');
    expect(errorMessage(new HttpErrorResponse({ status: 0 }))).toContain('niet bereikbaar');
  });
});
