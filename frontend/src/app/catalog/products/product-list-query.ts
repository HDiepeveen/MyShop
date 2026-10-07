import { ParamMap } from '@angular/router';
import { readListQuery } from '../list-query';

export type ProductStockFilter = 'low' | 'out' | 'untracked';
export function readProductListQuery(params: ParamMap) {
  const rawLimit = params.get('limit') ?? '20';
  const parsedLimit = Number(rawLimit);
  const limit = /^\d+$/.test(rawLimit) && [20, 50, 100].includes(parsedLimit) ? parsedLimit : 20;
  const rawOffset = params.get('offset') ?? '0',
    parsedOffset = Number(rawOffset);
  const offset =
    /^\d+$/.test(rawOffset) &&
    Number.isSafeInteger(parsedOffset) &&
    parsedOffset <= 2147483647 &&
    parsedOffset % limit === 0
      ? parsedOffset
      : 0;
  return {
    categoryId: params.get('categoryId') || null,
    productTypeId: params.get('productTypeId') || null,
    ...readListQuery(params),
    offset,
    ...(limit !== 20 ? { limit } : {}),
    ...(['low', 'out', 'untracked'].includes(params.get('stock') ?? '')
      ? { stock: params.get('stock') as ProductStockFilter }
      : {}),
    ...(params.get('published') === 'true' || params.get('published') === 'false'
      ? { published: params.get('published') === 'true' }
      : {}),
  };
}
