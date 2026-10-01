import { ProductEditState } from './product-edit-state';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { VariantEdit } from './variant-edit';

describe('VariantEdit', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VariantEdit],
      providers: [ProductEditState, provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  function setup() {
    const fixture = TestBed.createComponent(VariantEdit);
    fixture.componentRef.setInput('productId', 'p');
    fixture.componentRef.setInput('variant', {
      id: 'v',
      name: 'Original',
      sku: null,
      price: null,
      attributeValues: [],
    });
    fixture.detectChanges();
    return fixture;
  }
  it('blocks empty and unchanged names and duplicate writes, reports success after persistence', () => {
    const fixture = setup();
    const editor = fixture.componentInstance;
    const saved = vi.fn();
    editor.saved.subscribe(saved);
    editor.rename();
    editor.name = ' ';
    editor.rename();
    http.expectNone('/api/products/p/variants/v/name');
    editor.name = ' New ';
    editor.rename();
    editor.rename();
    const request = http.expectOne('/api/products/p/variants/v/name');
    expect(request.request.body).toEqual({ name: 'New' });
    expect(saved).not.toHaveBeenCalled();
    request.flush(null);
    expect(saved).toHaveBeenCalledExactlyOnceWith('De variantnaam is bijgewerkt.');
  });

  it('rejects invalid SKUs and blocks other actions during a SKU write', () => {
    const editor = setup().componentInstance;
    for (const sku of ['', 'a b', 'x'.repeat(65)]) {
      editor.sku = sku;
      editor.saveSku();
    }
    http.expectNone('/api/products/p/variants/v/sku');
    editor.sku = ' shirt-1 ';
    editor.saveSku();
    editor.name = 'Changed';
    editor.rename();
    http.expectNone('/api/products/p/variants/v/name');
    const request = http.expectOne('/api/products/p/variants/v/sku');
    expect(request.request.body).toEqual({ sku: 'shirt-1' });
    request.flush(null);
  });
  it('explains SKU conflicts without discarding the proposed value', () => {
    const fixture = setup();
    const editor = fixture.componentInstance;
    editor.sku = 'DUPLICATE';
    editor.saveSku();
    http
      .expectOne('/api/products/p/variants/v/sku')
      .flush({}, { status: 409, statusText: 'Conflict' });
    fixture.detectChanges();
    expect(editor.sku).toBe('DUPLICATE');
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain(
      'al in gebruik',
    );
  });

  it('only clears an existing SKU and waits for the server before reporting success', () => {
    const fixture = setup();
    const editor = fixture.componentInstance;
    const saved = vi.fn();
    editor.saved.subscribe(saved);
    editor.clearSku();
    http.expectNone('/api/products/p/variants/v/sku');
    fixture.componentRef.setInput('variant', {
      id: 'v',
      name: 'Original',
      sku: 'SKU',
      price: null,
      attributeValues: [],
    });
    fixture.detectChanges();
    editor.clearSku();
    editor.clearSku();
    const request = http.expectOne('/api/products/p/variants/v/sku');
    expect(request.request.method).toBe('DELETE');
    expect(saved).not.toHaveBeenCalled();
    request.flush({}, { status: 500, statusText: 'Error' });
    expect(editor.variant().sku).toBe('SKU');
    expect(saved).not.toHaveBeenCalled();
    editor.clearSku();
    http.expectOne('/api/products/p/variants/v/sku').flush(null);
    expect(saved).toHaveBeenCalledExactlyOnceWith('Het artikelnummer is gewist.');
  });

  it('accepts comma decimal prices and zero without changing cents', () => {
    const editor = setup().componentInstance;
    for (const [text, amount] of [
      ['29,95', 29.95],
      ['0', 0],
      ['0.29', 0.29],
    ] as const) {
      editor.amount = text;
      editor.currency = 'eur';
      editor.savePrice();
      const request = http.expectOne('/api/products/p/variants/v/price');
      expect(request.request.body).toEqual({ amount, currency: 'EUR' });
      request.flush(null);
    }
  });
  it('rejects malformed, negative, overprecise and unsafe prices before writing', () => {
    const editor = setup().componentInstance;
    for (const amount of [
      '',
      '-1',
      '1.234',
      '1e3',
      'NaN',
      'Infinity',
      '1,2.3',
      '9999999999999999.99',
    ]) {
      editor.amount = amount;
      editor.savePrice();
    }
    editor.amount = '10';
    editor.currency = 'EU';
    editor.savePrice();
    http.expectNone('/api/products/p/variants/v/price');
  });
  it('keeps price input after a failed request and prevents duplicate submissions', () => {
    const editor = setup().componentInstance;
    editor.amount = '12,50';
    editor.savePrice();
    editor.savePrice();
    http
      .expectOne('/api/products/p/variants/v/price')
      .flush({}, { status: 400, statusText: 'Bad request' });
    expect(editor.amount).toBe('12,50');
    expect(editor.error()).toBeTruthy();
    expect(editor.busy()).toBe(false);
  });

  it('can clear a zero price and does not pretend a failed deletion succeeded', () => {
    const fixture = setup();
    const editor = fixture.componentInstance;
    const saved = vi.fn();
    editor.saved.subscribe(saved);
    editor.clearPrice();
    http.expectNone('/api/products/p/variants/v/price');
    fixture.componentRef.setInput('variant', {
      id: 'v',
      name: 'Original',
      sku: null,
      price: { amount: 0, currency: 'EUR' },
      attributeValues: [],
    });
    fixture.detectChanges();
    editor.clearPrice();
    editor.clearPrice();
    const request = http.expectOne('/api/products/p/variants/v/price');
    expect(request.request.method).toBe('DELETE');
    request.flush({}, { status: 409, statusText: 'Conflict' });
    expect(editor.variant().price?.amount).toBe(0);
    expect(saved).not.toHaveBeenCalled();
    editor.clearPrice();
    http.expectOne('/api/products/p/variants/v/price').flush(null);
    expect(saved).toHaveBeenCalledExactlyOnceWith('De basisprijs is gewist.');
  });
  it('sets zero stock, rejects invalid quantities and can stop tracking stock', () => {
    const fixture = setup();
    const editor = fixture.componentInstance;
    for (const quantity of ['', '-1', '1.5', '2147483648']) {
      editor.stock = quantity;
      editor.saveStock();
    }
    http.expectNone('/api/products/p/variants/v/stock');
    editor.stock = '0';
    editor.saveStock();
    const set = http.expectOne('/api/products/p/variants/v/stock');
    expect(set.request.body).toEqual({ quantity: 0 });
    set.flush(null);
    fixture.componentRef.setInput('variant', {
      ...editor.variant(),
      stockQuantity: 0,
    });
    fixture.detectChanges();
    editor.clearStock();
    const clear = http.expectOne('/api/products/p/variants/v/stock');
    expect(clear.request.method).toBe('DELETE');
    clear.flush(null);
  });
  it('preserves user input on conflict and allows a deliberate retry', () => {
    const editor = setup().componentInstance;
    const saved = vi.fn();
    editor.saved.subscribe(saved);
    editor.name = 'New';
    editor.rename();
    http
      .expectOne('/api/products/p/variants/v/name')
      .flush({}, { status: 409, statusText: 'Conflict' });
    expect(editor.name).toBe('New');
    expect(editor.busy()).toBe(false);
    expect(editor.error()).toBeTruthy();
    expect(saved).not.toHaveBeenCalled();
    editor.rename();
    http.expectOne('/api/products/p/variants/v/name').flush(null);
    expect(editor.error()).toBe('');
  });

  it('requires confirmation before removing a variant and keeps the last variant protected', () => {
    const fixture = setup();
    const editor = fixture.componentInstance;
    editor.removeVariant();
    http.expectNone((r) => r.method === 'DELETE');
    editor.confirmingRemove.set(true);
    editor.removeVariant();
    http.expectNone((r) => r.method === 'DELETE');
    fixture.componentRef.setInput('variantCount', 2);
    fixture.detectChanges();
    editor.removeVariant();
    editor.removeVariant();
    const request = http.expectOne('/api/products/p/variants/v');
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
    expect(editor.confirmingRemove()).toBe(false);
  });
});
