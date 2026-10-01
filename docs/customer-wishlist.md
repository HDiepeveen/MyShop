# Verlanglijst

Een ingelogde klant kan een gepubliceerd product vanaf de productpagina op de eigen verlanglijst zetten. De navigatie geeft toegang tot `/winkel/account/verlanglijst`. Daar kan de klant opgeslagen producten openen en verwijderen.

De server koppelt ieder item aan het ingelogde klantaccount; een klant kan de lijst van een ander account niet lezen of wijzigen. Toevoegen en verwijderen zijn idempotent en schrijfacties gebruiken de bestaande CSRF-bescherming. Een concept of onbekend product kan niet worden toegevoegd.

Wanneer publicatie later wordt ingetrokken, blijft het item herkenbaar als tijdelijk niet beschikbaar en kan de klant het verwijderen. Bij definitieve productverwijdering wordt het gekoppelde verlanglijstitem automatisch verwijderd.

De beveiligde routes zijn `GET /api/customer/wishlist`, `GET /api/customer/wishlist/{productId}`, `POST /api/customer/wishlist/{productId}` en `DELETE /api/customer/wishlist/{productId}`. Het overzicht gebruikt offset/limit-paginering met een maximale paginagrootte van 100.
