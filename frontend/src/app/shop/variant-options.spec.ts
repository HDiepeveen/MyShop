import { ShopProduct, ShopVariant } from './shop.api';
import { chooseOption, optionAvailable, variantOptions } from './variant-options';

export function optionVariant(
  id: string,
  size: string,
  colour: string,
  isAvailable = true,
): ShopVariant {
  return {
    id,
    name: size + ' / ' + colour,
    isAvailable,
    attributes: [
      { attributeDefinitionId: 'size', value: size },
      { attributeDefinitionId: 'colour', value: colour },
    ],
  };
}
export const optionProduct: ShopProduct = {
  id: '10000000-0000-0000-0000-000000000001',
  name: 'Katoenen T-shirt Hans',
  description: '',
  imageUrl: null,
  imageAlt: '',
  categories: [],
  variantDefinitions: [
    { id: 'size', name: 'Maat' },
    { id: 'colour', name: 'Kleur' },
  ],
  variants: [
    optionVariant('20000000-0000-0000-0000-000000000001', 'M', 'blauw'),
    optionVariant('20000000-0000-0000-0000-000000000002', 'M', 'zwart'),
    optionVariant('20000000-0000-0000-0000-000000000003', 'L', 'zwart'),
    optionVariant('20000000-0000-0000-0000-000000000004', 'XL', 'blauw', false),
  ],
};

describe('storefront variant options', () => {
  it('builds separate lists using values instead of variant names', () => {
    const model = variantOptions(optionProduct)!;
    expect(model.dimensions.map((d) => [d.name, d.values])).toEqual([
      ['Maat', ['M', 'L', 'XL']],
      ['Kleur', ['blauw', 'zwart']],
    ]);
    expect(model.otherVariants).toEqual([]);
  });
  it('disables unavailable prefixes and missing combinations but allows switching size', () => {
    const model = variantOptions(optionProduct)!;
    const mediumBlue = model.variants[0],
      largeBlack = model.variants[2];
    expect(optionAvailable(model, mediumBlue, 'size', 'L')).toBe(true);
    expect(optionAvailable(model, mediumBlue, 'size', 'XL')).toBe(false);
    expect(optionAvailable(model, largeBlack, 'colour', 'blauw')).toBe(false);
    expect(chooseOption(model, mediumBlue, 'size', 'L')).toBe(largeBlack);
    expect(chooseOption(model, largeBlack, 'size', 'M')).toBe(model.variants[1]);
    expect(chooseOption(model, largeBlack, 'colour', 'blauw')).toBeUndefined();
    expect(chooseOption(model, largeBlack, 'size', 'missing')).toBeUndefined();
  });
  it('falls back when combinations are ambiguous or no scalar values exist', () => {
    expect(
      variantOptions({
        ...optionProduct,
        variants: [optionProduct.variants[0], { ...optionProduct.variants[0], id: 'duplicate' }],
      }),
    ).toBeNull();
    expect(variantOptions({ ...optionProduct, variantDefinitions: [] })).toBeNull();
  });
  it('keeps incomplete variants accessible separately and ignores unused definitions', () => {
    const other = { id: 'other', name: 'Standaard' };
    const model = variantOptions({
      ...optionProduct,
      variantDefinitions: [...optionProduct.variantDefinitions!, { id: 'unused', name: 'Unused' }],
      variants: [other, ...optionProduct.variants],
    })!;
    expect(model.dimensions).toHaveLength(2);
    expect(model.otherVariants).toEqual([other]);
    expect(chooseOption(model, other, 'size', 'L')?.id).toBe(optionProduct.variants[2].id);
  });
});
