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

De routes zijn `POST /api/customer/auth/register`, `POST /api/customer/auth/login`,
`GET /api/customer/profile` en `PUT /api/customer/profile`. De profielroutes vereisen de rol
`Customer`. E-mailbevestiging, wachtwoordherstel per e-mail en een bestelgeschiedenis voor klanten
volgen in afzonderlijke onderdelen.
