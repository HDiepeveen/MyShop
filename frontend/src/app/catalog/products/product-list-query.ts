import { ParamMap } from '@angular/router';
import { readListQuery } from '../list-query';

export function readProductListQuery(params: ParamMap) {
  return {
    categoryId: params.get('categoryId') || null,
    productTypeId: params.get('productTypeId') || null,
    ...readListQuery(params),
  };
}
