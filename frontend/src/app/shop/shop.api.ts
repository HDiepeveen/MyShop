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
@Injectable({ providedIn: 'root' })
export class ShopApi {
  private readonly http = inject(HttpClient);
  products(offset = 0, search = '') {
    let params = new HttpParams().set('offset', offset).set('limit', 20);
    if (search.trim()) params = params.set('search', search.trim());
    return this.http.get<ShopPage>('/api/shop/products', { params });
  }
  product(id: string) {
    return this.http.get<ShopProduct>('/api/shop/products/' + encodeURIComponent(id));
  }
}
