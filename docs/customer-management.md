# Klantenbeheer

Beheerders kunnen onder **Klanten** klantaccounts zoeken op e-mailadres of opgeslagen naam. Het
overzicht is gepagineerd en toont de accountstatus en het aantal gekoppelde bestellingen. De
detailpagina toont het klantprofiel, het opgeslagen adres, het aantal bestellingen en het moment van
de laatste bestelling. Wachtwoorden en interne authenticatiegegevens worden nooit getoond.

Een beheerder kan een klantaccount blokkeren en deblokkeren. Blokkeren beëindigt bestaande sessies
en verhindert nieuwe aanmeldingen. Deblokkeren wist tevens mislukte inlogpogingen. Beide acties
gebruiken een revisie, zodat een gelijktijdige accountwijziging niet wordt overschreven. Bestellingen,
profielgegevens en het account zelf worden niet verwijderd.

De beheerroutes zijn `GET /api/customers`, `GET /api/customers/{id}` en
`PUT /api/customers/{id}/access`. Ze vereisen de rol `Administrator`; klant- en anonieme sessies
krijgen geen toegang. Schrijfacties vereisen daarnaast een antiforgerytoken.
