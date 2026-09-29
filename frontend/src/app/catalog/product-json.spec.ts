import { parseProduct } from './product-json';
describe('parseProduct', () => {
  it('preserves integer and decimal tokens at product and variant scope', () => {
    const product = parseProduct(
      '{"attributeValues":[{"value":9223372036854775807}],"variants":[{"attributeValues":[{"value":0.1234567890123456789012345678}],"price":{"amount":12.5}}]}',
    );
    expect(product.attributeValues[0].value).toBe('9223372036854775807');
    expect(product.variants[0].attributeValues[0].value).toBe('0.1234567890123456789012345678');
    expect(product.variants[0].price?.amount).toBe(12.5);
  });
  it('leaves booleans, text and choice arrays unchanged', () => {
    const product = parseProduct(
      '{"attributeValues":[{"value":false},{"value":"Text"},{"value":["a","b"]}]}',
    );
    expect(product.attributeValues.map((value) => value.value)).toEqual([
      false,
      'Text',
      ['a', 'b'],
    ]);
  });
});
