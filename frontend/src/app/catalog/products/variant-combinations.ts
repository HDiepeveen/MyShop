import { AttributeDefinition, Product } from '../catalog.models';
import { attributeInput } from '../attribute-input';

export interface CombinationPreview {
  name: string;
  values: { attributeDefinitionId: string; body: string }[];
  existing: boolean;
  selected: boolean;
}

export function variantDimensions(definitions: AttributeDefinition[]) {
  return definitions.filter((d) => d.scope === 'Variant' && d.dataType !== 'MultiChoice');
}

export function combinationPreview(
  product: Product,
  definitions: AttributeDefinition[],
  lists: Record<string, string>,
): CombinationPreview[] {
  const dimensions = variantDimensions(definitions);
  if (!dimensions.length || dimensions.length > 10)
    throw new Error('Gebruik 1 tot 10 variantkenmerken bij het producttype.');
  let combinations: { labels: string[]; values: CombinationPreview['values'] }[] = [
    { labels: [], values: [] },
  ];
  for (const definition of dimensions) {
    const lines = (lists[definition.id] ?? '')
      .split(/\r?\n/)
      .map((s) => s.trim())
      .filter(Boolean);
    if (!lines.length) throw new Error('Vul opties in voor ' + definition.displayName + '.');
    const options = lines.map((text) => {
      if (text.length > 100 || /[\u0000-\u001f\u007f]/.test(text))
        throw new Error('Gebruik opties van maximaal 100 tekens.');
      const parsed = attributeInput(definition.dataType, text);
      if (!parsed.body) throw new Error(definition.displayName + ': ' + parsed.error);
      return { text, body: parsed.body };
    });
    if (new Set(options.map((o) => o.body)).size !== options.length)
      throw new Error('Elke optie bij ' + definition.displayName + ' mag maar één keer voorkomen.');
    if (combinations.length * options.length > 100)
      throw new Error('Maak maximaal 100 combinaties per keer. Gebruik kortere lijsten.');
    combinations = combinations.flatMap((c) =>
      options.map((o) => ({
        labels: [...c.labels, o.text],
        values: [...c.values, { attributeDefinitionId: definition.id, body: o.body }],
      })),
    );
  }
  return combinations.map((c) => {
    const name = c.labels.join(' / ');
    if (name.length > 200)
      throw new Error('De variantnaam mag maximaal 200 tekens bevatten. Gebruik kortere opties.');
    const existing = product.variants.some((variant) =>
      c.values.every((value) => {
        const current = variant.attributeValues.find(
          (a) => a.attributeDefinitionId === value.attributeDefinitionId,
        );
        return (
          current && attributeInput(current.dataType, String(current.value)).body === value.body
        );
      }),
    );
    return { name, values: c.values, existing, selected: !existing };
  });
}

export function combinationRequest(
  revision: string,
  combinations: CombinationPreview[],
  defaults: object,
) {
  // Preserve exact integer/decimal tokens from attributeInput instead of converting through Number.
  return (
    '{"revision":' +
    JSON.stringify(revision) +
    ',"combinations":[' +
    combinations
      .filter((c) => c.selected && !c.existing)
      .map(
        (c) =>
          '{"name":' +
          JSON.stringify(c.name) +
          ',"values":[' +
          c.values
            .map(
              (v) =>
                '{"attributeDefinitionId":' +
                JSON.stringify(v.attributeDefinitionId) +
                ',"value":' +
                v.body +
                '}',
            )
            .join(',') +
          ']}',
      )
      .join(',') +
    '],' +
    JSON.stringify(defaults).slice(1)
  );
}
