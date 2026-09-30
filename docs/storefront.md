# Klantwinkel

De openbare winkel staat op `/winkel`. Bezoekers kunnen zonder account zoeken op productnaam, bladeren per twintig producten, productdetails bekijken en een variant kiezen. Teruggaan naar het assortiment behoudt de zoekterm en pagina. Ontbrekende of onbereikbare afbeeldingen krijgen een tekstuele vervanging.

Alleen gepubliceerde producten zijn zichtbaar. Ook een rechtstreekse detailaanvraag voor een concept retourneert 404. Intrekken van publicatie wordt bij de volgende aanvraag verwerkt. Een al geopende pagina wordt niet automatisch bijgewerkt.

De twee openbare GET-routes zijn `/api/shop/products` en `/api/shop/products/{productId}`. De lijst accepteert offset (vanaf nul), limit (1–100, standaard 20) en search (maximaal 200 tekens na trimmen). Sortering is op naam en daarna ID. De detailrespons bevat uitsluitend ID, naam, beschrijving, afbeelding met alternatieve tekst en varianten met ID en naam. Beheerinformatie, basisprijzen en interne kenmerken worden niet gedeeld. Bestaande beheerroutes blijven beveiligd.

Er is geen nieuwe databasemigratie nodig. Publiceer producten via het bestaande beheer. Winkelprijzen, winkelmand, voorraad, bestellen en betalen vallen buiten dit onderdeel; de pagina vermeldt dat bestellen nog niet beschikbaar is.
