import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { VariantGenerator } from './variant-generator';
import { ProductEditState } from './product-edit-state';

describe('VariantGenerator', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VariantGenerator],
      providers: [ProductEditState, provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  function setup() {
    const fixture = TestBed.createComponent(VariantGenerator);
    fixture.componentRef.setInput('product', { id: 'product', revision: 'revision', variants: [] });
    fixture.componentRef.setInput('definitions', [
      { id: 'size', displayName: 'Maat', dataType: 'Choice', scope: 'Variant' },
    ]);
    fixture.detectChanges();
    fixture.componentInstance.lists = { size: 'M\nL' };
    fixture.componentInstance.preview();
    http
      .expectOne('/api/billing/vat-rates?enabledOnly=true')
      .flush([{ name: '21%', percentage: 21, exempt: false }]);
    return fixture;
  }
  it('sends only selected combinations with defaults and waits for persistence', async () => {
    const fixture = setup(),
      component = fixture.componentInstance,
      saved = vi.fn();
    component.saved.subscribe(saved);
    fixture.detectChanges();
    await fixture.whenStable();
    (fixture.nativeElement.querySelector('input[type="checkbox"]') as HTMLInputElement).click();
    expect(component.selectedCount()).toBe(1);
    component.netAmount = '10,00';
    component.vatChoice = '21';
    component.stock = '3';
    expect(component.pricePreview()).toEqual({ net: '10,00', vat: '2,10', gross: '12,10' });
    component.create();
    component.create();
    const request = http.expectOne('/api/products/product/variant-combinations');
    expect(JSON.parse(request.request.body)).toMatchObject({
      revision: 'revision',
      netAmount: 10,
      vatRate: 21,
      stockQuantity: 3,
      combinations: [{ name: 'L' }],
    });
    expect(saved).not.toHaveBeenCalled();
    request.flush({ added: 1, skipped: 0 });
    expect(saved).toHaveBeenCalledOnce();
    expect(component.busy()).toBe(false);
  });
  it('invalidates preview when options change and retains choices after conflict', () => {
    const component = setup().componentInstance;
    component.create();
    http
      .expectOne('/api/products/product/variant-combinations')
      .flush({}, { status: 409, statusText: 'Conflict' });
    expect(component.combinations().length).toBe(2);
    expect(component.error()).toBeTruthy();
    component.changeList('size', 'XL');
    expect(component.combinations()).toEqual([]);
    component.create();
    http.expectNone('/api/products/product/variant-combinations');
  });
  it('rejects invalid prices, unavailable VAT and negative stock without a write', () => {
    const component = setup().componentInstance;
    component.netAmount = '1.001';
    component.create();
    expect(component.error()).toContain('prijs');
    component.netAmount = '1';
    component.vatChoice = '99';
    component.create();
    expect(component.error()).toContain('btw');
    component.netAmount = '';
    component.stock = '-1';
    component.create();
    expect(component.error()).toContain('beginvoorraad');
    http.expectNone('/api/products/product/variant-combinations');
  });
});
