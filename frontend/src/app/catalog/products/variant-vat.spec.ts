import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { VariantEdit } from './variant-edit';
import { ProductEditState } from './product-edit-state';
describe('Net price and VAT editing', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VariantEdit],
      providers: [ProductEditState, provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  function setup(price: unknown = null) {
    const fixture = TestBed.createComponent(VariantEdit);
    fixture.componentRef.setInput('productId', 'product');
    fixture.componentRef.setInput('variant', {
      id: 'variant',
      name: 'Variant',
      sku: null,
      price,
      priceRules: [],
      attributeValues: [],
    });
    fixture.detectChanges();
    return { fixture, editor: fixture.componentInstance };
  }
  it.each([
    ['21', '21,00', '121,00'],
    ['9', '9,00', '109,00'],
    ['0', '0,00', '100,00'],
    ['exempt', '0,00', '100,00'],
  ])('previews treatment %s and submits net input', async (choice, vat, gross) => {
    const { editor, fixture } = setup();
    await fixture.whenStable();
    const input = fixture.nativeElement.querySelector('input[name="amount"]') as HTMLInputElement;
    input.value = '100,00';
    input.dispatchEvent(new Event('input'));
    const select = fixture.nativeElement.querySelector('select[name="vat"]') as HTMLSelectElement;
    select.value = choice;
    select.dispatchEvent(new Event('change'));
    await fixture.whenStable();
    fixture.detectChanges();
    expect(editor.taxPreview()).toEqual({ net: '100,00', vat, gross });
    expect(fixture.nativeElement.textContent.replace(/\s+/g, ' ')).toContain(
      'Totaal voor de klant: ' + gross,
    );
    editor.savePrice();
    const request = http.expectOne('/api/products/product/variants/variant/price');
    expect(request.request.body).toEqual({
      netAmount: 100,
      currency: 'EUR',
      vatRate: choice === 'exempt' ? 0 : Number(choice),
      vatExempt: choice === 'exempt',
    });
    request.flush(null);
  });
  it('keeps legacy customer prices intact until net input and treatment are explicit', () => {
    const { editor, fixture } = setup({
      amount: 121,
      currency: 'EUR',
      grossAmount: '121.00',
      vatRate: null,
    });
    expect(editor.amount).toBe('');
    expect(editor.vatChoice).toBe('');
    expect(fixture.nativeElement.textContent).toContain('De bestaande klantprijs is 121.00');
    editor.amount = '100';
    editor.savePrice();
    http.expectNone(() => true);
    editor.vatChoice = '21';
    editor.savePrice();
    http.expectOne('/api/products/product/variants/variant/price').flush(null);
  });
  it('loads stored net amounts and clears tax fields along with the price', () => {
    const { editor } = setup({
      amount: 121,
      currency: 'EUR',
      netAmount: '100.00',
      vatAmount: '21.00',
      grossAmount: '121.00',
      vatRate: 21,
      vatExempt: false,
      isNetPrice: true,
    });
    expect(editor.amount).toBe('100.00');
    expect(editor.vatChoice).toBe('21');
    editor.clearPrice();
    http.expectOne('/api/products/product/variants/variant/price').flush(null);
  });
  it('uses exact cents and rounds half cents upwards at half-cent boundaries', () => {
    const { editor } = setup();
    editor.amount = '0.50';
    editor.vatChoice = '9';
    expect(editor.taxPreview()).toEqual({ net: '0,50', vat: '0,05', gross: '0,55' });
    editor.amount = '1.50';
    expect(editor.taxPreview()).toEqual({ net: '1,50', vat: '0,14', gross: '1,64' });
    editor.amount = '1000000000.00';
    editor.vatChoice = '21';
    expect(editor.taxPreview()!.gross).toBe('1210000000,00');
  });
  it('loads a configured decimal percentage and previews it with exact cents', () => {
    const { editor } = setup();
    editor.loadVatRates();
    http
      .expectOne('/api/billing/vat-rates?enabledOnly=true')
      .flush([{ name: 'Custom', percentage: 12.5, exempt: false }]);
    editor.amount = '100';
    editor.vatChoice = '12.5';
    expect(editor.taxPreview()).toEqual({ net: '100,00', vat: '12,50', gross: '112,50' });
    editor.savePrice();
    const request = http.expectOne('/api/products/product/variants/variant/price');
    expect(request.request.body.vatRate).toBe(12.5);
    request.flush(null);
  });
});
