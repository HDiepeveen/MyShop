# Klantwinkel

De openbare winkel staat op `/winkel`. Bezoekers kunnen zonder account zoeken op productnaam, bladeren per twintig producten, productdetails bekijken en een variant kiezen. Teruggaan naar het assortiment behoudt de zoekterm en pagina. Ontbrekende of onbereikbare afbeeldingen krijgen een tekstuele vervanging.

Alleen gepubliceerde producten zijn zichtbaar. Ook een rechtstreekse detailaanvraag voor een concept retourneert 404. Intrekken van publicatie wordt bij de volgende aanvraag verwerkt. Een al geopende pagina wordt niet automatisch bijgewerkt.

De openbare GET-routes zijn `/api/shop/products`, `/api/shop/products/{productId}` en `/api/shop/products/{productId}/prices`. De lijst accepteert offset (vanaf nul), limit (1–100, standaard 20) en search (maximaal 200 tekens na trimmen). Sortering is op naam en daarna ID. De detailrespons bevat uitsluitend ID, naam, beschrijving, afbeelding met alternatieve tekst en varianten met ID en naam. Beheerinformatie, basisprijzen en interne kenmerken worden niet gedeeld. Bestaande beheerroutes blijven beveiligd.

Er is geen nieuwe databasemigratie nodig. Publiceer producten via het bestaande beheer. Winkelmand, voorraad, bestellen en betalen vallen buiten dit onderdeel; de pagina vermeldt dat bestellen nog niet beschikbaar is.

## Actuele variantprijzen

De productpagina haalt prijzen afzonderlijk op en toont het bedrag en de valuta van de gekozen variant. De server berekent alle varianten op hetzelfde UTC-tijdstip met de bestaande domeinregels: alleen de actieve regel met de hoogste prioriteit, bij gelijke prioriteit het laagste regel-ID. Begintijd en eindtijd zijn inclusief. Er worden geen kortingen gestapeld. Bedragen worden volgens de bestaande Money-regels afgerond en nooit negatief.

De openbare prijsrespons bevat `at` en `variants` met uitsluitend `variantId`, `amount` en `currency`. Een ontbrekende basisprijs geeft null voor bedrag en valuta; nul is een geldige prijs. De client kan het rekentijdstip niet kiezen. Concepten, ingetrokken publicaties en onbekende producten geven 404, ook voor prijzen. De respons wordt niet gecachet.

De getoonde prijs is een momentopname. **Prijs vernieuwen** haalt opnieuw de actuele bedragen op met behoud van de variantkeuze. Tijdens ophalen en na een fout verdwijnt de oude prijs; er wordt nooit op een basisprijs teruggevallen. De pagina toont het ophaalmoment in lokale tijd. Een latere bestelling zal opnieuw moeten rekenen. De overzichtskaarten tonen in deze versie geen prijsbereik en er wordt nog geen btw- of verzendberekening toegevoegd.