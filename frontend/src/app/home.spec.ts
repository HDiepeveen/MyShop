import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Home } from './home';

describe('Home', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('shows the administrator dashboard and refreshes it', () => {
    const fixture = TestBed.createComponent(Home);
    fixture.detectChanges();

    http.expectOne('/api/dashboard').flush({
      productCount: 8,
      publishedProductCount: 6,
      draftProductCount: 2,
      customerCount: 4,
      orders: [
        { status: 'awaitingPayment', count: 3 },
        { status: 'paid', count: 2 },
        { status: 'shipped', count: 5 },
      ],
      activeRevenue: [{ currency: 'EUR', amount: '42.50' }],
      lowStock: [{
        productId: 'product-1',
        variantId: 'variant-1',
        productName: 'Linnen shirt',
        variantName: 'Blauw',
        sku: 'SKU-1',
        quantity: 2,
      }],
      recentOrders: [{
        id: 'order-1',
        number: 'MS-ORDER',
        placedAt: '2026-10-01T10:30:00Z',
        customerName: 'Ada Lovelace',
        status: 'paid',
        totals: [{ currency: 'EUR', amount: '42.50' }],
      }],
    });
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('a[href="/producten?published=true"]')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('a[href="/producten?published=false"]')).not.toBeNull();
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('8 producten');
    expect(text).toContain('6 gepubliceerd · 2 concept');
    expect(text).toContain('3 wacht op betaling');
    expect(text).toContain('4 accounts');
    expect(text).toContain('EUR 42,50');
    expect(text).toContain('Linnen shirt · Blauw');
    expect(text).toContain('2 op voorraad');
    expect(text).toContain('MS-ORDER');
    expect(text).toContain('Ada Lovelace');
    expect(text).toContain('Betaald');

    fixture.componentInstance.refresh();
    http.expectOne('/api/dashboard').flush({
      productCount: 0,
      publishedProductCount: 0,
      draftProductCount: 0,
      customerCount: 0,
      orders: [],
      activeRevenue: [],
      lowStock: [],
      recentOrders: [],
    });
  });
});
