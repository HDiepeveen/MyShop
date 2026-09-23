# Leesfuncties voor catalogusbeheer

## Eén attribuutdefinitie lezen

```http
GET /api/product-types/22222222-2222-2222-2222-222222222222/attributes/44444444-4444-4444-4444-444444444444
```

Dit is dezelfde URL-vorm als de Location-header van een geslaagde aanvraag om een attribuutdefinitie aan te maken. Het producttype en de attribuutdefinitie moeten bij elkaar horen.

```json
{
  "id": "44444444-4444-4444-4444-444444444444",
  "code": "color",
  "displayName": "Color",
  "dataType": "Choice",
  "scope": "Variant",
  "isRequired": false,
  "isFilterable": true
}
```

De antwoordvorm is gelijk aan een element van attributeDefinitions in de producttypedetails. dataType en scope zijn hier tekstwaarden. De bestaande aanmaakaanvraag en het aanmaakantwoord blijven hun numerieke enumwaarden gebruiken.

Een ontbrekend producttype geeft HTTP 404 met de titel "Product type not found". Een ontbrekende definitie, of een definitie van een ander producttype, geeft HTTP 404 met "Attribute not found". Na verwijderen van de definitie geeft dezelfde leesaanvraag dus 404.

## Gebruik van een categorie lezen

```http
GET /api/categories/11111111-1111-1111-1111-111111111111/usage
```

```json
{
  "categoryId": "11111111-1111-1111-1111-111111111111",
  "directChildCount": 2,
  "productAssignmentCount": 3,
  "isInUse": true
}
```

directChildCount telt alleen directe kinderen. productAssignmentCount telt directe producttoewijzingen aan deze categorie. Afstammelingen en hun producttoewijzingen worden niet opgeteld. isInUse is true wanneer minstens één van de twee aantallen groter is dan nul.

Een bestaande ongebruikte categorie geeft HTTP 200 met beide aantallen op nul en isInUse=false. Een ontbrekende categorie geeft HTTP 404.

## Gebruik van een producttype lezen

```http
GET /api/product-types/22222222-2222-2222-2222-222222222222/usage
```

```json
{
  "productTypeId": "22222222-2222-2222-2222-222222222222",
  "productCount": 5,
  "isInUse": true
}
```

productCount telt producten van dit producttype, ongeacht hun aantal varianten of attribuutwaarden. Attribuutdefinities zelf tellen niet als gebruik. isInUse is true wanneer productCount groter is dan nul.

Een bestaand ongebruikt producttype geeft HTTP 200 met productCount=0 en isInUse=false. Een ontbrekend producttype geeft HTTP 404.

## Gebruik in een beheerscherm

De gebruiksendpoints helpen om vóór een verwijderpoging te tonen welke afhankelijkheden bestaan. Het antwoord is informatief en verleent geen toestemming of garantie voor een latere verwijdering. De verwijderactie controleert het gebruik opnieuw en kan HTTP 409 teruggeven als er inmiddels afhankelijkheden zijn.

De bestaande opslagqueries worden hergebruikt. Het bestaan van een object en de verschillende aantallen worden afzonderlijk gelezen; deze aanvragen vormen geen transactionele momentopname. Gegevens kunnen tijdens of na het lezen veranderen.

Alle drie endpoints zijn uitsluitend leesacties. Ze wijzigen geen gegevens of revisies. Ze vereisen geldige, niet-lege GUID's. Een lege GUID geeft HTTP 400; een tekstwaarde die niet aan de guid-routeconstraint voldoet, matcht deze route niet.
