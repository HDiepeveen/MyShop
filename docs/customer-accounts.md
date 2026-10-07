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
E-mailbevestiging en wachtwoordherstel voor klanten zijn beschikbaar; zie [Klantmail](email.md) voor de schermen, tokenregels en SMTP-configuratie.

## Zoeken en filteren in Mijn bestellingen

Klanten kunnen zoeken op een deel van het bestelnummer (maximaal 200 tekens na trimmen) en filteren op Wacht op betaling, Betaald, Verzonden, Geannuleerd of Terugbetaald. Filters combineren met elkaar. Zoeken en status wijzigen gaan naar pagina één; bladeren en opnieuw proberen behouden de toegepaste filters. Filters wissen toont weer alle eigen bestellingen. Een leeg gefilterd overzicht wordt onderscheiden van een account zonder bestellingen.

De klant-API accepteert aanvullend search en status met de codes awaitingPayment, paid, shipped, cancelled en refunded. Onbekende codes of een te lange zoekterm geven 400. Filteren gebeurt vóór tellen en pagineren, met behoud van de accountcontrole. Gastbestellingen of bestellingen van andere accounts worden ook bij een exact bestelnummer nooit getoond. De bestaande snapshotgegevens en sortering op nieuwste bestelling blijven behouden.

Bestellingslinks bewaren zoekterm, status en pagina als queryparameters. De teruglink vanuit een bestelling herstelt die keuzes in Mijn bestellingen. Er is geen migratie of nieuwe opslag.

## Artikelen opnieuw bestellen

Op een eigen bestellingsdetail kan de klant de historische artikelen en aantallen opnieuw in de winkelmand zetten. Er wordt nog geen nieuwe bestelling geplaatst. De winkelmand haalt actuele prijzen, namen en voorraad op; historische bedragen, betaalwijze, bezorgkosten en adres worden niet gekopieerd. Een verdwenen of ingetrokken variant verschijnt als niet beschikbaar en kan uit de winkelmand worden verwijderd.

Een bestaande winkelmand wordt na expliciete bevestiging samengevoegd. Gelijke product/variant-combinaties krijgen opgetelde aantallen. De limieten van twintig varianten en 99 stuks per variant blijven gelden. Bij ongeldige regels of overschrijding verandert geen enkel deel van de winkelmand. Dubbel klikken op dezelfde geopende bestelling voegt niet nogmaals toe.

Na toevoegen opent de winkelmand. Als navigatie niet lukt, toont de detailpagina een link om die alsnog te openen; de artikelen worden daardoor niet opnieuw toegevoegd. Browseropslag bewaart uitsluitend identifiers en aantallen. Als bewaren niet lukt, blijft de winkelmand in de huidige sessie werken met de bestaande opslagmelding.

## Besteloverzicht afdrukken

Een geladen bestelling kan worden afgedrukt met Besteloverzicht afdrukken / PDF. De browser biedt afdrukken of opslaan als PDF. Het overzicht gebruikt de bestaande bestelsnapshot met bestelnummer, datum, artikelen, bedragen per valuta, klantgegevens, bezorging en beschikbare statusmomenten. Navigatie, actieknoppen en tijdelijke actiemeldingen worden bij afdrukken verborgen. Afdrukken is geblokkeerd tijdens annuleren en doet geen serverwijzigingen. Dit is een besteloverzicht; er wordt geen factuurnummer of btw-berekening toegevoegd.

## Kenmerken kopiëren

Bestelnummers en aanwezige trackingcodes kunnen via een knop worden gekopieerd. Het beheer biedt daarnaast kopiëren voor aanwezige betalings- en terugbetalingskenmerken. De originele tekst blijft zichtbaar. De browser vraagt zo nodig om toegang tot het klembord. Bij ontbrekende toegang verschijnt een melding om de tekst handmatig te selecteren en kopiëren. Tijdens een lopende kopieeractie wordt dubbel klikken geblokkeerd. Kopieerknoppen en feedback worden niet afgedrukt; er zijn geen serverwijzigingen.

## Bladeren in Mijn bestellingen

Het eigen besteloverzicht toont het totale aantal binnen de toegepaste filters en het huidige paginanummer. Eerste pagina, Laatste pagina en Ga naar pagina vullen Vorige en Volgende aan. Direct springen accepteert alleen hele paginanummers binnen het huidige aantal pagina's; een lege latere pagina biedt een terugweg naar pagina één. Het overzicht blijft twintig bestellingen per pagina ophalen.

De actieve zoekterm en status staan zichtbaar boven het resultaat en kunnen afzonderlijk worden gewist, met behoud van het andere filter. Beide acties gaan naar pagina één. Filters wissen verwijdert beide keuzes. Bestellingen vernieuwen haalt de huidige pagina met de toegepaste filters opnieuw op; ongestuurde zoekinvoer wordt niet toegepast. Tijdens ophalen zijn nieuwe leesacties geblokkeerd. Na een fout kan de klant opnieuw ophalen; verlaten van de pagina annuleert de aanvraag.

Detail- en teruglinks bewaren de bestaande zoek-, status- en paginacontext. De nieuwe bediening gebruikt de bestaande klantgebonden leesroute, zonder nieuwe opslag of wijziging van bestellingen. Aantallen en pagina-indeling kunnen tussen aanvragen veranderen.
