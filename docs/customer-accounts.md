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
gekoppeld. Het overzicht toont de nieuwste eerst, met paginering, status en totalen. De detailpagina
toont de onveranderlijke artikel-, klant- en adressnapshot, betaalinstructies en beschikbare
betaal- en verzendmomenten. Interne beheerreferenties en redenen worden niet via deze klant-API
gedeeld. Een onbekende bestelling en een bestelling van een ander account geven beide 404.

De routes zijn `POST /api/customer/auth/register`, `POST /api/customer/auth/login`,
`GET /api/customer/profile`, `PUT /api/customer/profile`, `GET /api/customer/orders` en
`GET /api/customer/orders/{id}`. Alle profiel- en bestelroutes vereisen de rol `Customer`.
E-mailbevestiging en wachtwoordherstel per e-mail volgen in afzonderlijke onderdelen.
