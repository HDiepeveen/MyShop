import { ParamMap } from '@angular/router';
export type ShopSort = 'nameAsc' | 'nameDesc';
export function readShopQuery(parameters: ParamMap) {
  const limit =
    parameters.get('limit') === '50' ? 50 : parameters.get('limit') === '100' ? 100 : 20;
  const raw = parameters.get('offset') ?? '0';
  const number = Number(raw);
  const offset =
    /^\d+$/.test(raw) &&
    Number.isSafeInteger(number) &&
    number <= 2147483647 &&
    number % limit === 0
      ? number
      : 0;
  return {
    search: (parameters.get('search') ?? '').trim(),
    offset,
    limit,
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
    ...(query.limit !== 20 ? { limit: query.limit } : {}),
    ...(query.sort === 'nameDesc' ? { sort: query.sort } : {}),
    ...(query.availableOnly ? { availableOnly: true } : {}),
  };
}
