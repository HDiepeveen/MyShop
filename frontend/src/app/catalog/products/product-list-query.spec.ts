import { convertToParamMap } from '@angular/router';
import { readProductListQuery } from './product-list-query';

describe('product list URL query', () => {
  it.each(['-20', '1', '20.5', 'Infinity', 'NaN', '1e3', '2147483648', '9007199254741000', 'abc'])(
    'falls back to page one for invalid offset %s',
    (offset) => {
      expect(readProductListQuery(convertToParamMap({ offset })).offset).toBe(0);
    },
  );
  it('keeps supported fields and trims the name search', () => {
    expect(
      readProductListQuery(
        convertToParamMap({
          offset: '40',
          search: ' coat ',
          categoryId: 'c',
          productTypeId: 't',
          returnUrl: 'https://example.com',
        }),
      ),
    ).toEqual({ offset: 40, search: 'coat', categoryId: 'c', productTypeId: 't' });
  });
});
