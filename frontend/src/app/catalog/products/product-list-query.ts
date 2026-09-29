import { ParamMap } from '@angular/router';

export function readProductListQuery(params: ParamMap) {
  const raw = params.get('offset') ?? '0';
  const number = Number(raw);
  const offset =
    /^\d+$/.test(raw) && Number.isSafeInteger(number) && number <= 2147483647 && number % 20 === 0
      ? number
      : 0;
  return {
    categoryId: params.get('categoryId') || null,
    productTypeId: params.get('productTypeId') || null,
    search: (params.get('search') ?? '').trim(),
    offset,
  };
}
