# Cataloguslijsten opvragen

Alle drie cataloguslijsten ondersteunen offsetpaginering. Zonder parameters worden maximaal 50 resultaten vanaf positie 0 teruggegeven.

| Parameter | Standaard | Geldige waarde |
|---|---:|---|
| offset | 0 | Geheel getal van 0 tot en met 2147483647 |
| limit | 50 | Geheel getal van 1 tot en met 100 |
| search | Geen filter | Niet-lege zoektekst; spaties aan begin en einde worden verwijderd |

De zoektekst wordt als tekst in de naam gezocht, niet als een door de client opgegeven SQL-patroon. De database bepaalt de vergelijking en sortering van tekst. Alle lijsten worden op naam en vervolgens ID gesorteerd. Filters worden vÃ³Ã³r paginering toegepast.

Een ongeldige parameter geeft HTTP 400. Een geldige aanvraag zonder resultaten geeft HTTP 200 met een lege lijst. Een geannuleerde aanvraag wordt doorgegeven aan de onderliggende query.

## CategorieÃ«n

```http
GET /api/categories?offset=0&limit=2&rootsOnly=true
```

Het antwoord blijft een JSON-array:

```json
[
  {
    "id": "11111111-1111-1111-1111-111111111111",
    "name": "Clothing",
    "parentCategoryId": null,
    "isRoot": true,
    "directChildCount": 3
  }
]
```

Aanvullende filters:

- rootsOnly=true selecteert alleen categorieÃ«n zonder bovenliggende categorie.
- `parentCategoryId=<guid>` selecteert directe kinderen van die categorie.
- rootsOnly=true en parentCategoryId mogen niet worden gecombineerd.
- Een lege of ongeldige GUID wordt afgewezen.
- directChildCount is het aantal directe kinderen van de betreffende categorie; dit aantal wordt niet beperkt door de pagina of zoektekst.

Voorbeelden:

```http
GET /api/categories?offset=50&limit=50
GET /api/categories?search=shirt&parentCategoryId=11111111-1111-1111-1111-111111111111&offset=0&limit=20
```

## Producttypen

```http
GET /api/product-types?search=clothing&offset=0&limit=20
```

Het antwoord blijft een JSON-array:

```json
[
  {
    "id": "22222222-2222-2222-2222-222222222222",
    "name": "Clothing",
    "attributeDefinitionCount": 4
  }
]
```

attributeDefinitionCount telt alle definities van het producttype; dit aantal wordt niet beperkt door de pagina.

## Producten

De bestaande productlijst behoudt haar antwoord met paginametadata:

```http
GET /api/products?offset=0&limit=20&search=shirt&productTypeId=22222222-2222-2222-2222-222222222222&categoryId=11111111-1111-1111-1111-111111111111
```

```json
{
  "items": [
    {
      "id": "33333333-3333-3333-3333-333333333333",
      "productTypeId": "22222222-2222-2222-2222-222222222222",
      "name": "Shirt",
      "variantCount": 3
    }
  ],
  "offset": 0,
  "limit": 20,
  "totalCount": 1
}
```

productTypeId en categoryId zijn optionele filters en kunnen samen met search worden gebruikt. Het categoriefilter selecteert directe producttoewijzingen aan de opgegeven categorie. totalCount telt alle producten die aan de filters voldoen, vÃ³Ã³r paginering; het kan dus groter zijn dan het aantal items op de pagina.

## Volgende pagina en bestaand clientgedrag

CategorieÃ«n en producttypen waren eerder onbegrensd. De antwoordvorm blijft gelijk, maar clients die alle resultaten nodig hebben moeten nu door de pagina's lopen.

Gebruik bij iedere volgende aanvraag dezelfde filters en limit en verhoog offset met limit. Bij categorieÃ«n en producttypen is de laatste pagina bereikt wanneer de ontvangen array minder dan limit items bevat. Een precies volle laatste pagina kan nog Ã©Ã©n lege vervolgaanvraag opleveren. Deze twee lijsten bevatten geen totalCount.

Bij producten kan de client ook totalCount gebruiken. De sortering is stabiel bij ongewijzigde gegevens; meerdere aanvragen vormen geen database-momentopname. Toevoegingen, verwijderingen of naamswijzigingen tussen aanvragen kunnen de pagina-indeling veranderen.

## Voorbeelden van ongeldige aanvragen

```http
GET /api/categories?offset=-1
GET /api/categories?limit=101
GET /api/categories?rootsOnly=true&parentCategoryId=11111111-1111-1111-1111-111111111111
GET /api/product-types?limit=0
GET /api/product-types?offset=2147483648
GET /api/products?categoryId=invalid
```

Alle bovenstaande aanvragen leveren HTTP 400 op.
