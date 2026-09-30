import { CartLine } from './cart';
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
export interface ShopItem {
  id: string;
  name: string;
  imageUrl: string | null;
  imageAlt: string;
}
export interface ShopPage {
  items: ShopItem[];
  totalCount: number;
  offset: number;
  limit: number;
}
export interface ShopProduct extends ShopItem {
  description: string;
  variants: { id: string; name: string }[];
}
export interface ShopPrices {
  at: string;
  variants: { variantId: string; amount: number | null; currency: string | null }[];
}
export interface CartQuote {
  at: string;
  lines: {
    productId: string;
    variantId: string;
    quantity: number;
    name: string | null;
    variant: string | null;
    amount: string | null;
    currency: string | null;
    total: string | null;
    failure: 'unavailable' | 'priceMissing' | null;
  }[];
  totals: { currency: string; amount: string }[];
}
@Injectable({ providedIn: 'root' })
export class ShopApi {
  private readonly http = inject(HttpClient);
  quote(lines: readonly CartLine[]) {
    let params = new HttpParams();
    for (const line of lines)
      params = params.append('lines', `${line.productId}:${line.variantId}:${line.quantity}`);
    return this.http.get<CartQuote>('/api/shop/cart/quote', { params });
  }
  products(offset = 0, search = '') {
    let params = new HttpParams().set('offset', offset).set('limit', 20);
    if (search.trim()) params = params.set('search', search.trim());
    return this.http.get<ShopPage>('/api/shop/products', { params });
  }
  prices(id: string) {
    return this.http.get<ShopPrices>('/api/shop/products/' + encodeURIComponent(id) + '/prices');
  }
  product(id: string) {
    return this.http.get<ShopProduct>('/api/shop/products/' + encodeURIComponent(id));
  }
}
