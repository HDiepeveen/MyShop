# Technische naamswijziging in 0.2.53

Actieve technische identifiers gebruiken HDWebFramework. Productnaam: HD Web Framework.

- Solution, projecten, bronmappen, namespaces, databasecontext en ontwikkel-secret-id zijn hernoemd.
- Inlog- en beveiligingscookies heten HDWebFramework.Admin en HDWebFramework.Antiforgery. De standaardnaam XSRF-TOKEN blijft gelijk. Oude inlogsessies worden niet overgenomen; de oude cookies worden bij het volgende API-verzoek verwijderd.
- Browseropslag heet HDWebFramework.cart.v1 en HDWebFramework.pending-payment.v1. De omzetting neemt oude waarden eenmaal over als er nog geen nieuwe waarde is en verwijdert de oude sleutel. Een bestaande nieuwe waarde krijgt voorrang.
- De actieve Data Protection-toepassingsnaam en encryptiedoelen zijn hernoemd. SMTP- en Mollie-instellingen kunnen met de bestaande sleutelmap worden gelezen en worden bij gebruik of opslaan opnieuw versleuteld onder de nieuwe identiteit. Onleesbare waarden worden niet weggegooid. Back-ups en de oude sleutelmap blijven noodzakelijk.
- Configuratie schrijft voortaan ConnectionStrings:HDWebFramework. Een oudere productieconfiguratiesleutel wordt nog gelezen met behoud van de prioriteit van configuratiebestanden en omgevingsvariabelen. Pas bij de volgende hostingupdate de sleutel in het bestaande productieconfiguratiebestand aan; verander de database/server/gebruiker en de overige instellingen niet.
- De door SmarterASP.NET toegewezen database- en gebruikersnamen zijn externe identifiers. Die worden niet veranderd door de productnaam te wijzigen.
- Historische bestel- en e-mailinhoud wordt niet herschreven. Bestaande e-mails of bestelnummers blijven administratieve historie.

De paar oude naamverwijzingen in omzettingscode en bijbehorende tests zijn uitsluitend nodig voor een veilige overgang. Verwijder ze pas wanneer alle actieve installaties en browsergegevens zijn omgezet.

De juridische webteksten en downloads staan in frontend/public/legal en vermelden de actieve technische namen. De Markdown- en HTML-versies worden momenteel samen handmatig bijgewerkt; er is nog geen beheereditor of automatische Markdown-naar-HTML-pijplijn.

Voor bestaande installaties kan de omzetting expliciet worden uitgevoerd met: dotnet HDWebFramework.Api.dll --upgrade-framework. Gebruik dezelfde database en sleutelmap als voorheen. Deze opdracht start geen webserver en verstuurt geen e-mail. De lokale opgeslagen SMTP- en Mollie-instellingen zijn hiermee gecontroleerd en omgezet.
