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
niet betaalde bestelling geeft de afgeboekte gevolgde voorraad terug. Verzonden of terugbetaalde
bestellingen wijzigen de voorraad niet automatisch.

De beheerroutes zijn `PUT /api/products/{productId}/variants/{variantId}/stock` met `quantity` en
`DELETE /api/products/{productId}/variants/{variantId}/stock`. Beide routes gebruiken de bestaande
beheerdersbeveiliging en productconcurrency. De migratie voegt een nullable `StockQuantity` toe met
een databasecontrole tegen negatieve waarden.
