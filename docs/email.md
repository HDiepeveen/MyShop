# Klantmail

MyShop ondersteunt e-mailbevestiging, wachtwoordherstel voor klanten en bestelbevestigingen. Verzending verloopt via SMTP met STARTTLS of via lokale `.eml`-bestanden in Development. Standaard staat verzending uit; nieuwe berichten worden wel duurzaam klaargezet.

## Configuratie

Pas eerst de migraties `EmailMessages` en `ManagedEmailSettings` toe op de applicatiedatabase. De integratietests doen dit uitsluitend in hun eigen testdatabases. Beheerders stellen SMTP vervolgens in via **E-mailinstellingen** op `/instellingen/email`. SMTP-wachtwoorden horen niet in Git.

Het scherm biedt server, STARTTLS-poort, gebruikersnaam, afzenderadres, afzendernaam en winkel-URL. Een leeg wachtwoordveld behoudt het opgeslagen wachtwoord. Een nieuw wachtwoord vervangt het; Opgeslagen wachtwoord wissen is een afzonderlijke keuze. De API retourneert uitsluitend of een wachtwoord is ingesteld. ASP.NET Core Data Protection versleutelt het wachtwoord vóór opslag in de database. Bewaar de sleutels duurzaam en afgeschermd en deel dezelfde sleutelring bij meerdere instances. Na sleutelverlies moet de beheerder een nieuw SMTP-wachtwoord instellen.

Opslaan gebruikt revisiecontrole; een oude revisie geeft een conflict zonder instellingen of wachtwoord te overschrijven. Het scherm houdt de invoer dan vast. Opgeslagen instellingen opnieuw ophalen vervangt de invoer door de actuele instellingen en maakt het wachtwoordveld leeg. Alleen beheerders mogen instellingen lezen, opslaan en een testmail klaarzetten; schrijfacties vereisen CSRF.

Verzending aan- of uitzetten wordt zonder herstart door de verzendwerker opgepakt. Na inschakelen worden ook al klaargezette berichten verwerkt. De testmail gebruikt de laatst opgeslagen instellingen, vereist ingeschakelde verzending en een actuele revisie en is begrensd door de beheerlimiet. De melding Testmail klaargezet bevestigt de wachtrijopname; controleer ontvangst en spammap om aflevering te controleren. De routes zijn `GET/PUT /api/email-settings` en `POST /api/email-settings/test`.

De onderstaande serverconfiguratie blijft als startconfiguratie ondersteund totdat een beheerder voor het eerst opslaat. Bij die eerste opslag wordt een eventueel bestaand SMTP-wachtwoord versleuteld overgenomen wanneer het veld leeg blijft. Daarna zijn de database-instellingen leidend, ook voor de URL in nieuwe accountmails. In Development kan de startconfiguratie nog Pickup gebruiken; opslaan via het beheerscherm kiest SMTP.

| Instelling | Betekenis |
|---|---|
| `Email__Enabled` | `true` om verzending in de startconfiguratie in te schakelen |
| `Email__Mode` | `Smtp` of `Pickup`; Pickup is uitsluitend voor Development |
| `Email__PublicBaseUrl` | Publieke winkel-URL, bijvoorbeeld `https://shop.example.com`; geen query, fragment of gebruikersgegevens |
| `Email__From` | Afzenderadres dat de mailprovider toestaat |
| `Email__Smtp__Host` | SMTP-server |
| `Email__Smtp__Port` | Standaard 587; STARTTLS is verplicht. Impliciete TLS op poort 465 wordt niet ondersteund |
| `Email__Smtp__User` | Optionele SMTP-gebruikersnaam |
| `Email__Smtp__Password` | SMTP-wachtwoord via beveiligde instellingen |
| `Email__PickupDirectory` | Optionele lokale map; standaard `.mail` onder de backend-applicatie |

In Development is de standaard linkbasis `http://127.0.0.1:4200`. Een andere lokale HTTP-URL mag uitsluitend loopback zijn. Productie vereist HTTPS. Links worden nooit opgebouwd uit een door de bezoeker aangeleverde Host-header. Configureer duurzame Data Protection-sleutels, anders verliezen accountlinks hun geldigheid na sleutelverlies.

## Accounts

Registratie slaat het account en de eerste bevestigingsmail in één transactie op. De profielpagina toont of het adres bevestigd is en biedt een link om opnieuw een bevestigingsmail aan te vragen. Het bestaande inloggedrag blijft behouden: bevestiging is in deze uitbreiding geen voorwaarde voor aanmelden. Bestaande klanten krijgen geen automatische bulkmail en hun bevestigingsstatus wordt niet stilzwijgend gewijzigd.

De inlogpagina biedt Wachtwoord vergeten. Aanvragen geven steeds dezelfde neutrale respons, ook voor onbekende, ongeschikte of beheerdersadressen. Een link is gekoppeld aan één klant en het juiste token-doel. Klantlinks wijzigen geen beheerdersaccounts, ook geen accounts met beide rollen. Geblokkeerde klanten kunnen hun blokkade niet opheffen via wachtwoordherstel. Herstel voldoet aan de bestaande wachtwoordregels, maakt oude sessies ongeldig en maakt de gebruikte herstellink ongeldig. Alle vier schrijfroutes vereisen CSRF en delen de bestaande limiet voor klant-authenticatie.

Bevestigings- en herstellinks zijn twee uur geldig. De linkpagina verandert niets door alleen openen: bevestigen of herstellen vereist een expliciete actie. Verlopen of ongeldige links bieden een nieuwe aanvraag. Herstel logt niet automatisch in. De vier routes zijn `POST /api/customer/auth/request-confirmation`, `confirm-email`, `request-password-reset` en `reset-password`. De profielrespons bevat aanvullend `emailConfirmed`.

De frontend gebruikt de `no-referrer`-metatag. Stel bij de frontendhost ook `Referrer-Policy: no-referrer` in en sla accountlink-querystrings niet in toegangslogs of analytics op. Bescherm testmailbestanden: ze bevatten werkende accountlinks en vallen via `.mail/` buiten Git.

## Bestelbevestigingen

De orderrepository slaat de bestelbevestiging in dezelfde transactie als bestelling en voorraadwijziging op. Dit geldt voor gast- en klantbestellingen en voor later betalen en opgeslagen online bestellingen. De mail gebruikt de opgeslagen namen, aantallen, totalen per valuta, bezorgkosten en betaalinstructies. Checkouttokens of accounttokens worden niet meegestuurd. Het is een bevestiging, geen fiscale factuur.

Herhaling met hetzelfde checkouttoken voegt geen tweede wachtrijbericht toe. Een mislukte ordertransactie laat geen bericht achter. Een SMTP-storing draait een opgeslagen bestelling niet terug.

## Verzending en beheer

De werker verwerkt elke tien seconden een bericht. Een databaselease voorkomt gelijktijdige verwerking door meerdere workers. Verzenden stopt uiterlijk na twee minuten; een claim vervalt na vijf minuten. Fouten plannen een nieuwe poging met een oplopende wachttijd tot maximaal zestig minuten. Er worden geen tokens, adressen of SMTP-foutteksten gelogd. Na succes worden `SentAt` en `Attempts` bewaard en wordt de berichtinhoud uit de database gewist.

SMTP kan geen strikt eenmalige aflevering garanderen: als de mailserver een bericht accepteert en het proces vóór de databasebevestiging stopt, kan een retry hetzelfde bericht opnieuw afleveren. De vaste Message-ID helpt identificeren; er wordt niet beloofd dat een mailprovider hierop dedupliceert. Er is geen automatische bulkverzending voor oude orders. Na inschakelen worden reeds klaargezette berichten verwerkt; verlopen accountlinks moeten opnieuw worden aangevraagd.

Controleer bij ingebruikname afzenderrechten en SPF/DKIM/DMARC bij de gekozen mailprovider en test een echte mailbox. Verzorg toegangsbeperking en bewaartermijnen voor mailbestanden en wachtrijmetadata. Een beheerpagina voor de mailwachtrij en bounceverwerking vallen buiten deze uitbreiding. De geautomatiseerde tests gebruiken lokale mailbestanden en een falende SMTP-configuratie; ze versturen geen echte klantmail.
