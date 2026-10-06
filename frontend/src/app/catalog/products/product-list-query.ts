import { ParamMap } from '@angular/router';
import { readListQuery } from '../list-query';

export type ProductStockFilter = 'low' | 'out' | 'untracked';
export function readProductListQuery(params: ParamMap) {
  return {
    categoryId: params.get('categoryId') || null,
    productTypeId: params.get('productTypeId') || null,
    ...readListQuery(params),
    ...(['low', 'out', 'untracked'].includes(params.get('stock') ?? '')
      ? { stock: params.get('stock') as ProductStockFilter }
      : {}),
    ...(params.get('published') === 'true' || params.get('published') === 'false'
      ? { published: params.get('published') === 'true' }
      : {}),
  };
}
