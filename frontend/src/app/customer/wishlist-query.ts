import { ParamMap, convertToParamMap } from '@angular/router';
import { readListQuery } from '../catalog/list-query';
export type WishlistSort = 'newest' | 'name';
export function readWishlistQuery(params: ParamMap) {
  return {
    ...readListQuery(params),
    sort: (params.get('sort') === 'name' ? 'name' : 'newest') as WishlistSort,
  };
}
export function readWishlistReturn(params: ParamMap) {
  if (params.get('from') !== 'wishlist') return null;
  return readWishlistQuery(
    convertToParamMap({
      search: params.get('wishlistSearch') ?? '',
      sort: params.get('wishlistSort') ?? '',
      offset: params.get('wishlistOffset') ?? '0',
    }),
  );
}
