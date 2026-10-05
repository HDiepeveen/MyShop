import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { PriceRuleEdit } from './price-rule-edit';
import { ProductEditState } from './product-edit-state';

describe('PriceRuleEdit', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [PriceRuleEdit],
      providers: [ProductEditState, provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  function setup() {
    const fixture = TestBed.createComponent(PriceRuleEdit);
    fixture.componentRef.setInput('productId', 'p/a');
    fixture.componentRef.setInput('variant', {
      id: 'v/b',
      name: 'Naturel',
      sku: null,
      price: { amount: 100, currency: 'EUR' },
      priceRules: [],
      attributeValues: [],
    });
    fixture.detectChanges();
    return fixture.componentInstance;
  }
  it('validates percentages, dates and priority before sending', () => {
    const editor = setup();
    editor.name = 'Sale';
    editor.adjustmentType = 1;
    editor.value = '101';
    editor.priority = '0';
    expect(editor.valid()).toBe(false);
    editor.value = '10';
    editor.priority = '-1';
    expect(editor.valid()).toBe(false);
    editor.priority = '1';
    editor.startsAt = '2026-10-10T10:00';
    editor.endsAt = '2026-10-01T10:00';
    expect(editor.valid()).toBe(false);
    editor.endsAt = '2026-11-01T10:00';
    expect(editor.valid()).toBe(true);
  });
  it('adds a normalized rule once and emits after success', () => {
    const editor = setup();
    editor.name = ' Sale ';
    editor.adjustmentType = 2;
    editor.value = '12,50';
    editor.priority = '2';
    const saved = vi.fn();
    editor.saved.subscribe(saved);
    editor.save();
    editor.save();
    const request = http.expectOne('/api/products/p%2Fa/variants/v%2Fb/price-rules');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toMatchObject({
      name: 'Sale',
      adjustmentType: 2,
      value: 12.5,
      priority: 2,
    });
    request.flush({ id: 'rule' });
    expect(saved).toHaveBeenCalledOnce();
  });
  it('updates and deletes only after explicit confirmation', () => {
    const editor = setup();
    const rule = {
      id: 'r/1',
      name: 'Sale',
      adjustmentType: 1,
      value: 10,
      priority: 1,
      startsAt: null,
      endsAt: null,
    };
    editor.variant().priceRules!.push(rule);
    editor.edit(rule);
    editor.name = 'Sale nieuw';
    editor.save();
    const update = http.expectOne('/api/products/p%2Fa/variants/v%2Fb/price-rules/r%2F1');
    expect(update.request.method).toBe('PUT');
    update.flush(null);
    editor.confirming.set(null);
    editor.remove(rule);
    http.expectNone(() => true);
    editor.confirming.set(rule.id);
    editor.remove(rule);
    editor.remove(rule);
    const remove = http.expectOne('/api/products/p%2Fa/variants/v%2Fb/price-rules/r%2F1');
    expect(remove.request.method).toBe('DELETE');
    remove.flush(null);
  });
  it('keeps the draft and releases the shared lock after a server failure', () => {
    const editor = setup();
    editor.name = 'Sale';
    editor.value = '5';
    editor.priority = '1';
    editor.save();
    http
      .expectOne('/api/products/p%2Fa/variants/v%2Fb/price-rules')
      .flush({}, { status: 409, statusText: 'Conflict' });
    expect(editor.name).toBe('Sale');
    expect(editor.busy()).toBe(false);
    expect(editor.error()).toBeTruthy();
  });
});
