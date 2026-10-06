# Voorraadbeheer

Een beheerder kan per productvariant een niet-negatieve voorraad instellen. De waarde `0` betekent
uitverkocht. **Voorraad niet meer volgen** maakt de variant onbeperkt beschikbaar. Bestaande varianten
krijgen na de migratie deze onbeperkte stand, zodat bestaande verkoop niet onverwacht stopt.

De klantwinkel toont uitverkochte producten en varianten en kiest op een productpagina bij voorkeur
een beschikbare variant. De winkelmand controleert het gevraagde aantal op de server. Bij onvoldoende
voorraad verdwijnt het subtotaal en moet de klant het aantal verlagen of de regel verwijderen.

Bij bestellen wordt gevolgde voorraad in dezelfde databasetransactie als de bestelling afgeboekt.
Een gelijktijdige bestelling kan daardoor niet dezelfde laatste voorraad verkopen. Herhalen met
hetzelfde checkouttoken maakt geen tweede bestelling en boekt niet opnieuw af. Annuleren van een nog
niet betaalde bestelling geeft de afgeboekte gevolgde voorraad terug, zowel vanuit beheer als door
de klant zelf. Verzonden of terugbetaalde bestellingen wijzigen de voorraad niet automatisch.

De beheerroutes zijn `PUT /api/products/{productId}/variants/{variantId}/stock` met `quantity` en
`DELETE /api/products/{productId}/variants/{variantId}/stock`. Beide routes gebruiken de bestaande
beheerdersbeveiliging en productconcurrency. De migratie voegt een nullable `StockQuantity` toe met
een databasecontrole tegen negatieve waarden.

## Voorraadselecties in productbeheer

Het beveiligde productoverzicht kan filteren op Lage voorraad (0–5), Met uitverkochte variant (0) en Voorraad niet gevolgd (null). Een product voldoet zodra minstens één variant aan de selectie voldoet; een product met verschillende voorraadstanden kan dus in meerdere selecties voorkomen. De lijst telt producten, geen varianten, en het weergegeven variantenaantal blijft het totale aantal varianten van dat product.

De API gebruikt stock=low|out|untracked; weglaten toont alle voorraadstanden. Onbekende codes geven 400. Voorraadselecties combineren met published, search, categoryId en productTypeId. Filteren gebeurt vóór tellen en pagineren in SQL. Lage voorraad omvat ook nul; onbeperkte voorraad hoort daar niet bij. Er is geen migratie of nieuwe opslag.

De gekozen voorraadstand blijft in de URL behouden bij zoeken, bladeren en het openen van productdetails. Een andere selectie gaat naar pagina één; Alle voorraadstanden verwijdert alleen dit filter. De dashboardlink opent alle gepubliceerde producten met lage voorraad, zonder de beperking van twintig dashboardvarianten.
