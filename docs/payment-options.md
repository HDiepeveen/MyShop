# Betaalopties en Mollie-testbetalingen

Beheerders beheren betaalopties via **Betaalopties**. Minimaal één betaaloptie moet actief blijven. **Later betalen** blijft beschikbaar; de instructies worden bij de bestelling vastgelegd.

## Mollie aansluiten

De Mollie-adapter ondersteunt nu uitsluitend **testmodus**, eenmalige betalingen in **EUR**. Live-sleutels worden geweigerd. Stel aan de backendzijde in:

- `Payments:Online:Provider`: `Mollie`.
- `Payments:Mollie:ApiKey`: de test-API-sleutel uit het Mollie-dashboard (begint met `test_`). Nooit in Git, de frontend, logs of een chat zetten.
- `Payments:Mollie:ReturnUrl`: de volledige URL van de klantpagina, bijvoorbeeld `https://test.example.nl/winkel/betaling`. Lokaal mag `http://localhost:4200/winkel/betaling`.
- `Payments:Mollie:WebhookUrl`: de volledige, publiek bereikbare HTTPS-URL `https://test.example.nl/api/payments/mollie/webhook`. Geen localhost, query of fragment. De adapter voegt zelf het checkoutkenmerk toe.

Voor lokale ontwikkeling kun je **scripts/configure-mollie-test.ps1** uitvoeren. Het script vraagt de testsleutel met verborgen invoer en slaat deze via standaardinvoer op, zodat de sleutel niet in de opdrachtregel of shellgeschiedenis staat. Het vraagt ook de twee adressen en wijzigt geen database. .NET user secrets is lokale opslag buiten Git, geen versleutelde kluis; gebruik de instellingen alleen op een vertrouwde ontwikkelmachine.

Voor lokale ontwikkeling laadt de API .NET user secrets met id `MyShop-Development`; instellingen en de sleutel blijven daarmee buiten de repository. Productieconfiguratie hoort in de secret store van de hostingomgeving. De database moet de migratie **MollieCustomerPaymentStarts** hebben voordat de API wordt gestart.

Herstart de backend na configuratie. Schakel daarna **Direct online betalen** in onder **Betaalopties**. Bij onvolledige configuratie blijft Mollie onbeschikbaar. Betaalmethoden worden aangeboden op Mollies betaalpagina op basis van je Mollie-profiel; MyShop dwingt geen methode af.

## Betaalcyclus

1. MyShop controleert winkelmand, actuele prijzen, voorraad, bezorgoptie en betaalopties en legt een snapshot vast.
2. Mollie ontvangt het exacte totaal, betalingskenmerk en checkouttoken. Een stabiele idempotency-sleutel voorkomt dubbele betaalstarts bij korte retries. Mollie bewaart deze sleutel één uur; een niet-opgeslagen betaalstart geeft nog geen betaalpagina aan de klant.
3. De klant opent de Mollie-betaalpagina. De UI vermeldt dat dit testmodus is.
4. Mollie meldt veranderingen via de webhook. De webhook vertrouwt geen meegestuurde betaalstatus: MyShop haalt deze zelf bij Mollie op en vergelijkt betaling-id, testmodus, bedrag, valuta en metadata met de opgeslagen start.
5. Alleen **paid** maakt een betaalde bestelling aan, boekt voorraad en zet één bestelbevestiging in de bestaande e-mailwachtrij. De bevestiging gebruikt de bestaande e-mailinstellingen; de koppeling voegt geen SMTP-account toe. Ingelogde klanten behouden de koppeling met hun account.
6. De klant keert terug naar **/winkel/betaling** en ziet de bevestiging of kan de betaalstatus opnieuw controleren. Een callback werkt ook als de klant de browser heeft gesloten.
7. Bij **failed**, **canceled** of **expired** blijft de winkelmand behouden en kan de klant opnieuw afrekenen. Open, pending en authorized geven geen bestelling. Bij een tijdelijk communicatieprobleem wordt geen tweede betaling gestart.
8. Herhaalde callbacks of statuscontroles leveren dezelfde bestelling op. De winkelmand wordt na terugkeer alleen leeggemaakt als deze nog overeenkomt met de betaalstart; wijzigingen in een andere tab blijven behouden.
9. Een beheerder kan met de bestaande factuurfunctie de gecontroleerde factuur uitgeven. De klant kan deze daarna bekijken en afdrukken. Facturen worden niet automatisch uitgegeven of gemaild.

Als artikelen tussen de betaalstart en de bevestiging niet meer beschikbaar zijn, kan de betaalde bestelling nog niet worden aangemaakt. De callback antwoordt met 503 zodat Mollie opnieuw probeert; de klant krijgt de instructie contact op te nemen en niet opnieuw te betalen. **Voorraad wordt bij betaalstart gecontroleerd maar nog niet vooraf gereserveerd.** Herstel voorraad/publicatie en controleer dezelfde betaalstatus opnieuw, of handel de betaling in het Mollie-dashboard af. Automatische refunds, chargebacks en een betaalreconciliatie-scherm zijn niet inbegrepen in deze adapter.

## Volledige echte test

Gebruik een aparte testdatabase en een testprofiel. Voor localhost moet het callback-adres via een HTTPS-tunnel of testhosting bereikbaar zijn. Gebruik geen echte klantgegevens of echte betaalmiddelen.

- Start een bestelling en kies op Mollies testpagina **betaald**; controleer bevestiging, beheerstatus, voorraad, klantaccount en bestelmail.
- Geef een factuur uit met de bestaande beheerderscontrole; controleer het totaal en btw en open die als dezelfde klant.
- Herhaal de callback en ververs de terugkeerpagina: één bestelling en één bestelmail moeten blijven bestaan.
- Test mislukt, geannuleerd en verlopen: geen betaalde bestelling, winkelmand behouden, opnieuw afrekenen mogelijk.
- Test een terugkeer vóór de callback en een callback zonder terugkeer; beide moeten correct werken.
- Controleer tijdelijk onbereikbare backend/provider en een voorraadwijziging tijdens de betaling. Start bij een onzekere betaalstatus geen nieuwe betaling.

De automatische tests simuleren Mollies HTTP-antwoorden en controleren de echte MyShop-API, SQL-opslag, e-mailwachtrij en factuurfunctie. Zij bewijzen niet dat jouw Mollie-account, tunnel, hosting of SMTP-server correct is ingesteld. Daarvoor is bovenstaande echte test nodig.

**TestPay** blijft een interne simulator, met `Payments:Online:Provider=TestPay` en een fictieve checkout-URL; dit is geen aansluiting op Mollie en geen echte betaling.

Bronnen: [Mollie betaalstart](https://docs.mollie.com/reference/create-payment), [webhooks](https://docs.mollie.com/reference/webhooks), [idempotency](https://docs.mollie.com/reference/api-idempotency), [testmodus](https://docs.mollie.com/reference/testing).
