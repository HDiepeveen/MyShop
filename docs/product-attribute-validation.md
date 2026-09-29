# Product attribute validation

Use `GET /api/products/{productId}/attribute-validation` to inspect product and variant attributes against the product's current type definition.

The query reports missing required values and stored values that no longer match their definitions. It does not modify the product, change its revision, or prevent draft products from being edited. It is an attribute diagnostic, not a publication or checkout eligibility decision: prices, SKUs, stock, and categories are outside its scope.

## Response

A successful inspection returns HTTP 200, including when issues are found:

```json
{
  "productId": "11111111-1111-1111-1111-111111111111",
  "isValid": false,
  "issues": [
    {
      "attributeDefinitionId": "22222222-2222-2222-2222-222222222222",
      "variantId": null,
      "code": "MissingRequired"
    },
    {
      "attributeDefinitionId": "33333333-3333-3333-3333-333333333333",
      "variantId": "44444444-4444-4444-4444-444444444444",
      "code": "UnknownDefinition"
    }
  ]
}
```

`isValid` is true exactly when `issues` is empty. `variantId: null` identifies a product-level issue; otherwise it identifies the affected variant. IDs can be used with the existing product, variant, and product type endpoints to obtain labels and edit values.

| Code | Meaning |
|---|---|
| `MissingRequired` | A required definition has no value at its configured product/variant level. Each variant is checked separately. |
| `UnknownDefinition` | A stored value refers to a definition absent from the current product type, for example after removing that definition. |
| `WrongScope` | The definition exists, but the stored value is on the wrong product/variant level. |
| `WrongDataType` | The definition and scope match, but the stored value has a different datatype. |

A present value is checked for unknown definition first, then wrong scope, then wrong datatype; it receives at most one of those issues. A missing required value is checked independently at the correct level. For example, placing a required variant attribute on the product can produce a product-level `WrongScope` and a `MissingRequired` for each variant without its own value.

A value with the wrong datatype is reported as `WrongDataType`, not additionally as missing. Boolean false and numeric zero count as supplied values. Normal domain validation still rejects empty text and empty multichoice lists when constructing values.

Results have a stable order: product-level issues first, then variants by their GUID ordering, and within each level by attribute definition GUID and issue code. Do not use array position as an issue identity; use attribute definition ID, variant ID, and code.

## Errors

| Status | Meaning |
|---|---|
| 400 | The bound product ID is empty. A malformed GUID does not match the route. |
| 404 | The product does not exist, or its product type cannot be found; the problem title distinguishes them. |
| 200 | The inspection completed; inspect `isValid` and `issues`. |

Request cancellation and unexpected storage failures propagate normally. The endpoint does not turn storage failures into validation issues.

## Editing workflow

1. Read the validation endpoint for the product.
2. Set a missing product value with `PUT /api/products/{productId}/attributes/{attributeDefinitionId}`, or a variant value with `PUT /api/products/{productId}/variants/{variantId}/attributes/{attributeDefinitionId}`.
3. For an obsolete stored value, review and remove it through the existing attribute DELETE endpoint when appropriate.
4. Read validation again to see the current result.

Making a definition required can reveal missing values on existing products. Removing a definition can reveal obsolete values; this query deliberately preserves those stored values.

Product and product type are read separately through the existing repositories. Concurrent edits can change either while the query runs, so this is a diagnostic view of the values read, not a transactional guarantee or a write precondition. Re-query after editing.

## Verification

The tests cover domain diagnostics, query failure/cancellation behavior, API contracts, serialized HTTP handler flows for all seven attribute types, and SQL Server reads after persisted edits. Enable the real database tests as described in [Database development](database-development.md).
