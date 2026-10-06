# Klantaccounts

Klanten kunnen in de winkel een eigen account registreren met hun e-mailadres en een sterk
wachtwoord. Afrekenen als gast blijft beschikbaar. Een klantaccount krijgt uitsluitend de rol
`Customer` en geeft geen toegang tot het beheer.

Een ingelogde klant kan onder **Mijn account** een naam en standaardafleveradres opslaan. Het profiel
gebruikt een revisie, zodat gelijktijdige wijzigingen niet stilzwijgend worden overschreven. Na
inloggen in dezelfde browsersessie vult de checkout het opgeslagen profiel in; de klant kan de
gegevens voor de bestelling nog aanpassen. De bestelling bewaart zoals voorheen een onveranderlijke
snapshot van de gebruikte klant- en adresgegevens.

Registratie en klantlogin zijn begrensd tot tien pogingen per bron-IP per minuut. Dezelfde beveiligde,
HttpOnly sessiecookie, CSRF-bescherming, wachtwoordeisen, blokkering na mislukte pogingen en sessieduur
als voor beheerders worden gebruikt. Wachtwoorden en adressen worden nooit in browseropslag bewaard.

## Bestelgeschiedenis

Een bestelling die tijdens een ingelogde klantsessie wordt geplaatst, wordt aan dat klantaccount
gekoppeld. Gastbestellingen blijven los van accounts en worden niet achteraf op basis van een
e-mailadres gekoppeld. Daardoor kan een andere gebruiker van hetzelfde e-mailadres geen oude
gastbestellingen overnemen.

Onder **Mijn bestellingen** ziet de klant uitsluitend bestellingen die aan het eigen account zijn
gekoppeld. Het overzicht toont de nieuwste eerst, met paginering, status, totalen en beschikbare
trackinginformatie voor verzonden bestellingen. De detailpagina toont de onveranderlijke artikel-,
klant- en adressnapshot, betaalinstructies en een tijdlijn met beschikbare bestel-, betaal-,
verzend-, annulerings- en terugbetalingsmomenten. Interne
beheerreferenties en redenen worden niet via deze klant-API gedeeld. Een onbekende bestelling en
een bestelling van een ander account geven beide 404.

Een klant kan een eigen bestelling annuleren zolang deze nog op betaling wacht. De interface vraagt
eerst om bevestiging. De server controleert nogmaals het account, de status en de revisie en legt als
reden **Geannuleerd door klant** vast. Een gelijktijdige betaling of eerdere wijziging geeft een
conflict en wordt nooit overschreven. Gevolgde voorraad wordt in dezelfde transactie teruggegeven;
onbeperkte voorraad blijft onbeperkt. Betaalde, verzonden, geannuleerde en terugbetaalde bestellingen
kunnen door de klant niet worden geannuleerd.

De routes zijn `POST /api/customer/auth/register`, `POST /api/customer/auth/login`,
`GET /api/customer/profile`, `PUT /api/customer/profile`, `GET /api/customer/orders` en
`GET /api/customer/orders/{id}`. Annuleren gebruikt
`POST /api/customer/orders/{id}/cancel` met de actuele revisie en vereist een antiforgerytoken.
Alle profiel- en bestelroutes vereisen de rol `Customer`.

Klanten kunnen bovendien gepubliceerde producten op een persoonlijke verlanglijst bewaren; zie
[Verlanglijst](customer-wishlist.md).
E-mailbevestiging en wachtwoordherstel per e-mail volgen in afzonderlijke onderdelen.

## Zoeken en filteren in Mijn bestellingen

Klanten kunnen zoeken op een deel van het bestelnummer (maximaal 200 tekens na trimmen) en filteren op Wacht op betaling, Betaald, Verzonden, Geannuleerd of Terugbetaald. Filters combineren met elkaar. Zoeken en status wijzigen gaan naar pagina één; bladeren en opnieuw proberen behouden de toegepaste filters. Filters wissen toont weer alle eigen bestellingen. Een leeg gefilterd overzicht wordt onderscheiden van een account zonder bestellingen.

De klant-API accepteert aanvullend search en status met de codes awaitingPayment, paid, shipped, cancelled en refunded. Onbekende codes of een te lange zoekterm geven 400. Filteren gebeurt vóór tellen en pagineren, met behoud van de accountcontrole. Gastbestellingen of bestellingen van andere accounts worden ook bij een exact bestelnummer nooit getoond. De bestaande snapshotgegevens en sortering op nieuwste bestelling blijven behouden.

Bestellingslinks bewaren zoekterm, status en pagina als queryparameters. De teruglink vanuit een bestelling herstelt die keuzes in Mijn bestellingen. Er is geen migratie of nieuwe opslag.
