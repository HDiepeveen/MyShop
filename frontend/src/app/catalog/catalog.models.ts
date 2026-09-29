export interface ProductSummary {
  id: string;
  productTypeId: string;
  name: string;
  variantCount: number;
  isPublished?: boolean;
}
export interface ProductPage {
  items: ProductSummary[];
  offset: number;
  limit: number;
  totalCount: number;
}
export interface TypeSummary {
  id: string;
  name: string;
  attributeDefinitionCount: number;
}
export interface Category {
  id: string;
  name: string;
  parentCategoryId: string | null;
  isRoot: boolean;
}
export interface CategorySummary {
  id: string;
  name: string;
  parentCategoryId: string | null;
  isRoot: boolean;
  directChildCount: number;
}
export interface CategoryUsage {
  categoryId: string;
  directChildCount: number;
  productAssignmentCount: number;
  isInUse: boolean;
}
export interface AttributeDefinition {
  id: string;
  code: string;
  displayName: string;
  dataType: string;
  scope: string;
  isRequired: boolean;
  isFilterable: boolean;
}
export interface NewAttributeDefinition {
  code: string;
  displayName: string;
  dataType: number;
  scope: number;
  isRequired: boolean;
  isFilterable: boolean;
}
export interface ProductType {
  id: string;
  name: string;
  attributeDefinitions: AttributeDefinition[];
}
export interface AttributeValue {
  attributeDefinitionId: string;
  dataType: string;
  value: string | number | boolean | string[];
}
export interface PriceRule {
  id: string;
  name: string;
  adjustmentType: number;
  value: number;
  priority: number;
  startsAt: string | null;
  endsAt: string | null;
}

export interface Variant {
  id: string;
  name: string;
  sku: string | null;
  price: { amount: number; currency: string } | null;
  priceRules: PriceRule[];
  attributeValues: AttributeValue[];
}
export interface ProductPresentation {
  description: string;
  imageUrl: string | null;
  imageAlt: string;
  isPublished: boolean;
}
export interface Product {
  presentation?: ProductPresentation;
  id: string;
  productTypeId: string;
  name: string;
  categoryIds: string[];
  attributeValues: AttributeValue[];
  variants: Variant[];
  revision: string;
}
export interface AttributeIssue {
  attributeDefinitionId: string;
  variantId: string | null;
  code: string;
}
export interface AttributeValidation {
  productId: string;
  isValid: boolean;
  issues: AttributeIssue[];
}
export interface CreatedProduct {
  id: string;
  productTypeId: string;
  name: string;
  initialVariantId: string;
  initialVariantName: string;
  revision: string;
}
