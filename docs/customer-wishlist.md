# Verlanglijst

Een ingelogde klant kan een gepubliceerd product vanaf de productpagina op de eigen verlanglijst zetten. De navigatie geeft toegang tot `/winkel/account/verlanglijst`. Daar kan de klant opgeslagen producten openen en verwijderen.

De server koppelt ieder item aan het ingelogde klantaccount; een klant kan de lijst van een ander account niet lezen of wijzigen. Toevoegen en verwijderen zijn idempotent en schrijfacties gebruiken de bestaande CSRF-bescherming. Een concept of onbekend product kan niet worden toegevoegd.

Wanneer publicatie later wordt ingetrokken, blijft het item herkenbaar als tijdelijk niet beschikbaar en kan de klant het verwijderen. Bij definitieve productverwijdering wordt het gekoppelde verlanglijstitem automatisch verwijderd.

De beveiligde routes zijn `GET /api/customer/wishlist`, `GET /api/customer/wishlist/{productId}`, `POST /api/customer/wishlist/{productId}` en `DELETE /api/customer/wishlist/{productId}`. Het overzicht gebruikt offset/limit-paginering met een maximale paginagrootte van 100.

## Zoeken en sorteren

De klant kan zoeken op een deel van de productnaam en sorteren op Laatst toegevoegd of Naam: A–Z. Zoeken en sortering wijzigen gaan naar pagina één; bladeren, verwijderen en opnieuw proberen behouden de toegepaste keuzes. Zoekterm wissen behoudt de gekozen sortering. Een lege zoekselectie wordt onderscheiden van een lege verlanglijst.

GET /api/customer/wishlist accepteert search (maximaal 200 tekens na trimmen) en sort=newest|name. Filteren en sorteren gebeuren vóór tellen en pagineren, uitsluitend binnen het eigen account. Bij gelijke sorteerwaarden bepaalt het product-ID de vaste volgorde. Ingetrokken producten blijven op de eigen lijst vindbaar, zoals voorheen. Er is geen migratie of nieuwe opslag.

Productlinks bewaren zoekterm, sortering en pagina als verlanglijstcontext. De productpagina biedt daarvoor een vaste teruglink naar Mijn verlanglijst; de bestaande link naar het assortiment blijft beschikbaar. Een ontbrekend of ingetrokken product kan via die teruglink nog steeds vanuit de lijst worden verwijderd.
