import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { ProductList } from './product-list';

describe('ProductList', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ProductList],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  it('keeps the applied search while paging and resets offset for a new search', () => {
    const fixture = TestBed.createComponent(ProductList);
    http.expectOne((r) => r.url === '/api/products').flush({ items: [], totalCount: 50 });
    fixture.componentInstance.searchText = 'coat';
    fixture.componentInstance.search();
    http
      .expectOne((r) => r.params.get('search') === 'coat' && r.params.get('offset') === '0')
      .flush({ items: [], totalCount: 50 });
    fixture.componentInstance.searchText = 'unsubmitted';
    fixture.componentInstance.changePage(20);
    http
      .expectOne((r) => r.params.get('search') === 'coat' && r.params.get('offset') === '20')
      .flush({ items: [], totalCount: 50 });
    fixture.componentInstance.search();
    http
      .expectOne((r) => r.params.get('search') === 'unsubmitted' && r.params.get('offset') === '0')
      .flush({ items: [], totalCount: 0 });
    expect(fixture.componentInstance.offset()).toBe(0);
  });
  it('cancels a stale request when a new search starts', () => {
    const fixture = TestBed.createComponent(ProductList);
    const old = http.expectOne((r) => r.url === '/api/products');
    fixture.componentInstance.searchText = 'new';
    fixture.componentInstance.search();
    expect(old.cancelled).toBe(true);
    http.expectOne((r) => r.params.get('search') === 'new').flush({ items: [], totalCount: 0 });
    expect(fixture.componentInstance.state()?.data?.totalCount).toBe(0);
  });
  it('shows an error and can retry without stale rows', async () => {
    const fixture = TestBed.createComponent(ProductList);
    http
      .expectOne((r) => r.url === '/api/products')
      .flush({}, { status: 503, statusText: 'Unavailable' });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
    fixture.componentInstance.retry();
    expect(fixture.componentInstance.state()?.loading).toBe(true);
    http.expectOne((r) => r.url === '/api/products').flush({ items: [], totalCount: 0 });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Geen producten gevonden');
  });
  it('opens the product when an SKU is found', () => {
    const fixture = TestBed.createComponent(ProductList);
    http.expectOne((r) => r.url === '/api/products');
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture.componentInstance.skuText = ' shirt-1 ';
    fixture.componentInstance.lookupSku();
    http.expectOne('/api/products/by-sku/shirt-1').flush({ productId: 'p', productVariantId: 'v' });
    expect(navigate).toHaveBeenCalledWith(['/producten', 'p']);
    expect(fixture.componentInstance.skuBusy()).toBe(false);
  });
  it('shows an SKU lookup error and prevents duplicate submits', () => {
    const fixture = TestBed.createComponent(ProductList);
    http.expectOne((r) => r.url === '/api/products');
    fixture.componentInstance.skuText = 'DUPLICATE';
    fixture.componentInstance.lookupSku();
    fixture.componentInstance.lookupSku();
    const request = http.expectOne('/api/products/by-sku/DUPLICATE');
    request.flush({ title: 'SKU not found' }, { status: 404, statusText: 'Not Found' });
    expect(fixture.componentInstance.skuBusy()).toBe(false);
    expect(fixture.componentInstance.skuError()).toContain('niet meer beschikbaar');
  });
});
