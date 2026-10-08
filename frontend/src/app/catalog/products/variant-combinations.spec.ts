import { AttributeDefinition, Product } from '../catalog.models';
import { combinationPreview, combinationRequest } from './variant-combinations';

describe('variant combinations', () => {
  const definitions = [
    { id: 'size', displayName: 'Maat', dataType: 'Choice', scope: 'Variant' },
    { id: 'colour', displayName: 'Kleur', dataType: 'Text', scope: 'Variant' },
  ] as AttributeDefinition[];
  const product = {
    variants: [
      {
        attributeValues: [
          { attributeDefinitionId: 'size', dataType: 'Choice', value: 'M' },
          { attributeDefinitionId: 'colour', dataType: 'Text', value: 'blauw' },
        ],
      },
    ],
  } as Product;
  it('previews every combination and skips existing ones based on values', () => {
    const preview = combinationPreview(product, definitions, {
      size: 'M\nL',
      colour: 'blauw\nzwart',
    });
    expect(preview.map((c) => c.name)).toEqual([
      'M / blauw',
      'M / zwart',
      'L / blauw',
      'L / zwart',
    ]);
    expect(preview[0].existing).toBe(true);
    preview[1].selected = false;
    const request = JSON.parse(combinationRequest('revision', preview, { stockQuantity: 3 }));
    expect(request.combinations.map((c: { name: string }) => c.name)).toEqual([
      'L / blauw',
      'L / zwart',
    ]);
    expect(request.stockQuantity).toBe(3);
  });
  it('rejects duplicate, empty and excessive option lists', () => {
    expect(() =>
      combinationPreview(product, definitions, { size: 'M\n M', colour: 'blue' }),
    ).toThrow('één keer');
    expect(() => combinationPreview(product, definitions, { size: '', colour: 'blue' })).toThrow(
      'Maat',
    );
    expect(() =>
      combinationPreview(product, definitions, {
        size: Array.from({ length: 51 }, (_, i) => String(i)).join('\n'),
        colour: 'blue\nred',
      }),
    ).toThrow('100');
  });
  it('preserves exact numeric values and identifies equivalent numeric options', () => {
    const numeric = [
      { id: 'weight', displayName: 'Gewicht', dataType: 'Integer', scope: 'Variant' },
    ] as AttributeDefinition[];
    const preview = combinationPreview({ variants: [] } as unknown as Product, numeric, {
      weight: '9223372036854775807',
    });
    expect(combinationRequest('revision', preview, { netAmount: null })).toContain(
      '"value":9223372036854775807',
    );
    expect(() => combinationPreview(product, numeric, { weight: '01\n1' })).toThrow('één keer');
  });
});
