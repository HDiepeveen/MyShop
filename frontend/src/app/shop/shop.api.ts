import { CompanyPage } from '../seo/seo.models';
import { ShopSort } from './shop-query';
import { CartLine } from './cart';
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
export interface ShopItem {
  id: string;
  webAddress?: string;
  name: string;
  imageUrl: string | null;
  imageAlt: string;
}
export interface ShopPage {
  welcomeText?: string;
  introduction?: string;
  heading?: string;
  seoTitle?: string;
  at: string;
  items: ShopListItem[];
  totalCount: number;
  offset: number;
  limit: number;
}
export interface ShopListItem extends ShopItem {
  prices: { currency: string; minimumAmount: string; maximumAmount: string }[];
  isAvailable?: boolean;
}
export interface ShopCategory {
  id: string;
  webAddress?: string;
  name: string;
}
export interface ShopProduct extends ShopItem {
  aboutHeading?: string;
  attributesHeading?: string;
  seoTitle?: string;
  seoDescription?: string;
  checkoutEnabled?: boolean;
  images?: { id: string; url: string; alternativeText: string }[];
  description: string;
  categories: ShopCategory[];
  attributes?: { attributeDefinitionId: string; name: string; value: string }[];
  variantDefinitions?: { id: string; name: string }[];
  variants: ShopVariant[];
}
export interface ShopVariant {
  id: string;
  webAddress?: string;
  name: string;
  isAvailable?: boolean;
  attributes?: { attributeDefinitionId: string; value: string }[];
}
export interface ShopPrices {
  at: string;
  variants: { variantId: string; amount: string | null; currency: string | null }[];
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
    failure: 'unavailable' | 'priceMissing' | 'outOfStock' | null;
  }[];
  totals: { currency: string; amount: string }[];
}
@Injectable({ providedIn: 'root' })
export class ShopApi {
  private readonly http = inject(HttpClient);
  company() { return this.http.get<CompanyPage>('/api/shop/company'); }
  quote(lines: readonly CartLine[]) {
    let params = new HttpParams();
    for (const line of lines)
      params = params.append('lines', `${line.productId}:${line.variantId}:${line.quantity}`);
    return this.http.get<CartQuote>('/api/shop/cart/quote', { params });
  }
  categories() {
    return this.http.get<ShopCategory[]>('/api/shop/categories');
  }
  products(
    offset = 0,
    search = '',
    categoryId = '',
    sort: ShopSort = 'nameAsc',
    availableOnly = false,
    limit = 20,
  ) {
    let params = new HttpParams().set('offset', offset).set('limit', limit);
    if (search.trim()) params = params.set('search', search.trim());
    if (categoryId) params = params.set('categoryId', categoryId);
    if (sort !== 'nameAsc') params = params.set('sort', sort);
    if (availableOnly) params = params.set('availableOnly', true);
    return this.http.get<ShopPage>('/api/shop/products', { params });
  }
  prices(id: string) {
    return this.http.get<ShopPrices>('/api/shop/products/' + encodeURIComponent(id) + '/prices');
  }
  product(id: string) {
    return this.http.get<ShopProduct>('/api/shop/products/' + encodeURIComponent(id));
  }
}
