import { ShopProduct, ShopVariant } from './shop.api';

export function optionValue(variant: ShopVariant | undefined, definitionId: string) {
  return variant?.attributes?.find((a) => a.attributeDefinitionId === definitionId)?.value;
}

export function variantOptions(product: ShopProduct | null | undefined) {
  if (!product) return null;
  const definitions = (product.variantDefinitions ?? []).filter((d) =>
    product.variants.some((v) => optionValue(v, d.id) !== undefined),
  );
  if (!definitions.length) return null;
  const variants = product.variants.filter((v) =>
    definitions.every((d) => optionValue(v, d.id) !== undefined),
  );
  if (!variants.length) return null;
  const keys = variants.map((v) => JSON.stringify(definitions.map((d) => optionValue(v, d.id))));
  // Two variants with the same options need their names to distinguish them.
  if (new Set(keys).size !== keys.length) return null;
  return {
    dimensions: definitions.map((d) => ({
      ...d,
      values: [...new Set(variants.map((v) => optionValue(v, d.id)!))],
    })),
    variants,
    otherVariants: product.variants.filter((v) => !variants.includes(v)),
  };
}

export function optionAvailable(
  model: NonNullable<ReturnType<typeof variantOptions>>,
  selected: ShopVariant | undefined,
  definitionId: string,
  value: string,
) {
  const index = model.dimensions.findIndex((d) => d.id === definitionId);
  if (index < 0) return false;
  return model.variants.some(
    (v) =>
      v.isAvailable !== false &&
      optionValue(v, definitionId) === value &&
      model.dimensions
        .slice(0, index)
        .every((d) => optionValue(v, d.id) === optionValue(selected, d.id)),
  );
}

export function chooseOption(
  model: NonNullable<ReturnType<typeof variantOptions>>,
  selected: ShopVariant | undefined,
  definitionId: string,
  value: string,
) {
  if (!optionAvailable(model, selected, definitionId, value)) return undefined;
  const index = model.dimensions.findIndex((d) => d.id === definitionId);
  const candidates = model.variants.filter(
    (v) =>
      v.isAvailable !== false &&
      optionValue(v, definitionId) === value &&
      model.dimensions
        .slice(0, index)
        .every((d) => optionValue(v, d.id) === optionValue(selected, d.id)),
  );
  return (
    candidates.find((v) =>
      model.dimensions
        .slice(index + 1)
        .every((d) => optionValue(v, d.id) === optionValue(selected, d.id)),
    ) ?? candidates[0]
  );
}
