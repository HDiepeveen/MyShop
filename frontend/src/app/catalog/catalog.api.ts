import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import {
  AttributeValidation,
  CategorySummary,
  CreatedProduct,
  Product,
  ProductPage,
  ProductType,
  TypeSummary,
} from './catalog.models';

@Injectable({ providedIn: 'root' })
export class CatalogApi {
  private readonly http = inject(HttpClient);
  products(offset = 0, search = '') {
    return this.http.get<ProductPage>('/api/products', { params: this.paging(offset, search) });
  }
  product(id: string) {
    return this.http.get<Product>('/api/products/' + encodeURIComponent(id));
  }
  types(offset = 0, search = '') {
    return this.http.get<TypeSummary[]>('/api/product-types', {
      params: this.paging(offset, search),
    });
  }
  type(id: string) {
    return this.http.get<ProductType>('/api/product-types/' + encodeURIComponent(id));
  }
  categories(offset = 0, search = '') {
    return this.http.get<CategorySummary[]>('/api/categories', {
      params: this.paging(offset, search),
    });
  }
  createType(name: string) {
    return this.http.post<{ id: string; name: string }>('/api/product-types', {
      name: name.trim(),
    });
  }
  createCategory(name: string) {
    return this.http.post<{ id: string; name: string }>('/api/categories', {
      name: name.trim(),
      parentCategoryId: null,
    });
  }
  createProduct(productTypeId: string, name: string, initialVariantName: string) {
    return this.http.post<CreatedProduct>('/api/products', {
      productTypeId,
      name: name.trim(),
      initialVariantName: initialVariantName.trim(),
    });
  }
  renameProduct(id: string, name: string) {
    return this.http.patch<void>('/api/products/' + encodeURIComponent(id) + '/name', {
      name: name.trim(),
    });
  }
  addVariant(id: string, name: string) {
    return this.http.post<{ id: string; name: string }>(
      '/api/products/' + encodeURIComponent(id) + '/variants',
      { name: name.trim() },
    );
  }
  validation(id: string) {
    return this.http.get<AttributeValidation>(
      '/api/products/' + encodeURIComponent(id) + '/attribute-validation',
    );
  }
  private paging(offset: number, search: string) {
    let params = new HttpParams().set('offset', offset).set('limit', 20);
    if (search.trim()) params = params.set('search', search.trim());
    return params;
  }
}
