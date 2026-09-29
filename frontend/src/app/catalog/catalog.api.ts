import { map } from 'rxjs';
import { parseProduct } from './product-json';
import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import {
  AttributeValidation,
  CategorySummary,
  Category,
  CreatedProduct,
  ProductPage,
  ProductType,
  NewAttributeDefinition,
  TypeSummary,
} from './catalog.models';

@Injectable({ providedIn: 'root' })
export class CatalogApi {
  private readonly http = inject(HttpClient);
  products(
    offset = 0,
    search = '',
    filters: { categoryId?: string | null; productTypeId?: string | null } = {},
  ) {
    let params = this.paging(offset, search);
    if (filters.categoryId) params = params.set('categoryId', filters.categoryId);
    if (filters.productTypeId) params = params.set('productTypeId', filters.productTypeId);
    return this.http.get<ProductPage>('/api/products', { params });
  }
  setPresentation(
    id: string,
    value: import('./catalog.models').ProductPresentation & { revision: string },
  ) {
    return this.http.put<void>('/api/products/' + encodeURIComponent(id) + '/presentation', value);
  }
  product(id: string) {
    return this.http
      .get('/api/products/' + encodeURIComponent(id), { responseType: 'text' })
      .pipe(map(parseProduct));
  }
  productBySku(sku: string) {
    return this.http.get<{ productId: string; productVariantId: string }>(
      '/api/products/by-sku/' + encodeURIComponent(sku.trim()),
    );
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
  removeCategory(productId: string, categoryId: string) {
    return this.http.delete<void>(
      '/api/products/' +
        encodeURIComponent(productId) +
        '/categories/' +
        encodeURIComponent(categoryId),
    );
  }
  assignCategory(productId: string, categoryId: string) {
    return this.http.put<void>(
      '/api/products/' +
        encodeURIComponent(productId) +
        '/categories/' +
        encodeURIComponent(categoryId),
      null,
    );
  }
  renameCategory(id: string, name: string) {
    return this.http.patch<void>('/api/categories/' + encodeURIComponent(id) + '/name', {
      name: name.trim(),
    });
  }
  category(id: string) {
    return this.http.get<Category>('/api/categories/' + encodeURIComponent(id));
  }
  categoryUsage(id: string) {
    return this.http.get<import('./catalog.models').CategoryUsage>(
      '/api/categories/' + encodeURIComponent(id) + '/usage',
    );
  }
  moveCategory(id: string, parentCategoryId: string | null) {
    return this.http.put<void>('/api/categories/' + encodeURIComponent(id) + '/parent', {
      parentCategoryId,
    });
  }
  deleteCategory(id: string) {
    return this.http.delete<void>('/api/categories/' + encodeURIComponent(id));
  }
  removeVariant(productId: string, variantId: string) {
    return this.http.delete<void>(
      '/api/products/' +
        encodeURIComponent(productId) +
        '/variants/' +
        encodeURIComponent(variantId),
    );
  }
  deleteProduct(id: string) {
    return this.http.delete<void>('/api/products/' + encodeURIComponent(id));
  }
  setAttribute(
    productId: string,
    definitionId: string,
    body: string,
    variantId: string | null = null,
  ) {
    // Numeric tokens are prepared without conversion to JavaScript numbers.
    return this.http.put<void>(this.attributeUrl(productId, definitionId, variantId), body, {
      headers: { 'Content-Type': 'application/json' },
    });
  }
  clearAttribute(productId: string, definitionId: string, variantId: string | null = null) {
    return this.http.delete<void>(this.attributeUrl(productId, definitionId, variantId));
  }
  private attributeUrl(productId: string, definitionId: string, variantId: string | null) {
    const owner =
      variantId === null
        ? '/api/products/' + encodeURIComponent(productId)
        : this.variantUrl(productId, variantId);
    return owner + '/attributes/' + encodeURIComponent(definitionId);
  }
  removeDefinition(typeId: string, id: string) {
    return this.http.delete<void>(
      '/api/product-types/' + encodeURIComponent(typeId) + '/attributes/' + encodeURIComponent(id),
    );
  }
  configureDefinition(typeId: string, id: string, isRequired: boolean, isFilterable: boolean) {
    return this.http.put<void>(
      '/api/product-types/' +
        encodeURIComponent(typeId) +
        '/attributes/' +
        encodeURIComponent(id) +
        '/configuration',
      { isRequired, isFilterable },
    );
  }
  renameDefinition(typeId: string, id: string, displayName: string) {
    return this.http.patch<void>(
      '/api/product-types/' +
        encodeURIComponent(typeId) +
        '/attributes/' +
        encodeURIComponent(id) +
        '/name',
      { displayName: displayName.trim() },
    );
  }
  addDefinition(typeId: string, definition: NewAttributeDefinition) {
    return this.http.post<unknown>(
      '/api/product-types/' + encodeURIComponent(typeId) + '/attributes',
      definition,
    );
  }
  listPriceRules(productId: string, variantId: string) {
    return this.http.get<{ rules: import('./catalog.models').PriceRule[]; revision: string }>(
      '/api/products/' +
        encodeURIComponent(productId) +
        '/variants/' +
        encodeURIComponent(variantId) +
        '/price-rules',
    );
  }
  addPriceRule(
    productId: string,
    variantId: string,
    rule: Omit<import('./catalog.models').PriceRule, 'id'>,
  ) {
    return this.http.post<{ id: string }>(
      '/api/products/' +
        encodeURIComponent(productId) +
        '/variants/' +
        encodeURIComponent(variantId) +
        '/price-rules',
      {
        name: rule.name.trim(),
        adjustmentType: rule.adjustmentType,
        value: rule.value,
        priority: rule.priority,
        startsAt: rule.startsAt,
        endsAt: rule.endsAt,
      },
    );
  }
  updatePriceRule(
    productId: string,
    variantId: string,
    rule: import('./catalog.models').PriceRule,
  ) {
    return this.http.put<void>(
      '/api/products/' +
        encodeURIComponent(productId) +
        '/variants/' +
        encodeURIComponent(variantId) +
        '/price-rules/' +
        encodeURIComponent(rule.id),
      {
        name: rule.name.trim(),
        adjustmentType: rule.adjustmentType,
        value: rule.value,
        priority: rule.priority,
        startsAt: rule.startsAt,
        endsAt: rule.endsAt,
      },
    );
  }
  removePriceRule(productId: string, variantId: string, id: string) {
    return this.http.delete<void>(
      '/api/products/' +
        encodeURIComponent(productId) +
        '/variants/' +
        encodeURIComponent(variantId) +
        '/price-rules/' +
        encodeURIComponent(id),
    );
  }
  deleteType(id: string) {
    return this.http.delete<void>('/api/product-types/' + encodeURIComponent(id));
  }
  typeUsage(id: string) {
    return this.http.get<{ productTypeId: string; productCount: number; isInUse: boolean }>(
      '/api/product-types/' + encodeURIComponent(id) + '/usage',
    );
  }
  renameType(id: string, name: string) {
    return this.http.patch<void>('/api/product-types/' + encodeURIComponent(id) + '/name', {
      name: name.trim(),
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
  renameVariant(productId: string, variantId: string, name: string) {
    return this.http.patch<void>(this.variantUrl(productId, variantId) + '/name', {
      name: name.trim(),
    });
  }
  setVariantSku(productId: string, variantId: string, sku: string) {
    return this.http.put<void>(this.variantUrl(productId, variantId) + '/sku', { sku: sku.trim() });
  }
  clearVariantSku(productId: string, variantId: string) {
    return this.http.delete<void>(this.variantUrl(productId, variantId) + '/sku');
  }
  setVariantPrice(productId: string, variantId: string, amount: number, currency: string) {
    return this.http.put<void>(this.variantUrl(productId, variantId) + '/price', {
      amount,
      currency: currency.trim().toUpperCase(),
    });
  }
  clearVariantPrice(productId: string, variantId: string) {
    return this.http.delete<void>(this.variantUrl(productId, variantId) + '/price');
  }
  private variantUrl(productId: string, variantId: string) {
    return (
      '/api/products/' +
      encodeURIComponent(productId) +
      '/variants/' +
      encodeURIComponent(variantId)
    );
  }
  private paging(offset: number, search: string) {
    let params = new HttpParams().set('offset', offset).set('limit', 20);
    if (search.trim()) params = params.set('search', search.trim());
    return params;
  }
}
