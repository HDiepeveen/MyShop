import { ParamMap } from '@angular/router';

export function readListQuery(params: ParamMap) {
  const raw = params.get('offset') ?? '0';
  const number = Number(raw);
  const offset =
    /^\d+$/.test(raw) && Number.isSafeInteger(number) && number <= 2147483647 && number % 20 === 0
      ? number
      : 0;
  return { search: (params.get('search') ?? '').trim(), offset };
}
