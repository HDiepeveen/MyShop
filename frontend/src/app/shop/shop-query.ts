import { ParamMap } from '@angular/router';
import { readListQuery } from '../catalog/list-query';
export type ShopSort = 'nameAsc' | 'nameDesc';
export function readShopQuery(parameters: ParamMap) {
  return {
    ...readListQuery(parameters),
    categoryId: parameters.get('categoryId') ?? '',
    sort: (parameters.get('sort') === 'nameDesc' ? 'nameDesc' : 'nameAsc') as ShopSort,
    availableOnly: parameters.get('availableOnly') === 'true',
  };
}
export function shopContextQuery(query: ReturnType<typeof readShopQuery>) {
  return {
    search: query.search || null,
    categoryId: query.categoryId || null,
    offset: query.offset || null,
    ...(query.sort === 'nameDesc' ? { sort: query.sort } : {}),
    ...(query.availableOnly ? { availableOnly: true } : {}),
  };
}
