import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { CustomerWishlist } from './customer-wishlist';

describe('Customer wishlist', () => {
  let http: HttpTestingController;
  afterEach(() => http.verify());

  it('lists, pages and removes saved products with CSRF protection', () => {
    TestBed.configureTestingModule({
      imports: [CustomerWishlist],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(CustomerWishlist);
    const product = {
      productId: '11111111-1111-1111-1111-111111111111',
      name: 'Bewaarproduct', imageUrl: null, imageAlt: 'Product', isAvailable: true,
      addedAt: '2026-10-01T10:00:00Z',
    };
    http.expectOne((request) => request.url === '/api/customer/wishlist' && request.params.get('offset') === '0')
      .flush({ items: [product], offset: 0, limit: 20, totalCount: 21 });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Bewaarproduct');
    fixture.componentInstance.load(20);
    http.expectOne((request) => request.url === '/api/customer/wishlist' && request.params.get('offset') === '20')
      .flush({ items: [product], offset: 20, limit: 20, totalCount: 21 });
    fixture.componentInstance.remove(product);
    http.expectOne('/api/auth/csrf').flush(null);
    const removal = http.expectOne('/api/customer/wishlist/' + product.productId);
    expect(removal.request.method).toBe('DELETE');
    removal.flush(null);
    expect(fixture.componentInstance.items()).toEqual([]);
  });

  it('shows an unavailable saved product without a product link', () => {
    TestBed.configureTestingModule({
      imports: [CustomerWishlist],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(CustomerWishlist);
    http.expectOne('/api/customer/wishlist?offset=0&limit=20').flush({
      items: [{ productId: 'id', name: 'Ingetrokken', imageUrl: null, imageAlt: '', isAvailable: false, addedAt: '' }],
      offset: 0, limit: 20, totalCount: 1,
    });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('momenteel niet beschikbaar');
    expect(fixture.nativeElement.querySelector('a[href="/winkel/id"]')).toBeNull();
  });
});
