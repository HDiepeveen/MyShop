# MyShop beheerinterface

Eerste lokale Angular-beheerinterface voor de bestaande catalogusbackend. De onderdelen zijn standalone en ingedeeld per catalogusfunctie. Angular en CLI zijn lokaal vastgezet op 22.2.0; de volledige afhankelijkheden staan in `package-lock.json`.

## Lokaal starten

Gebruik de .NET-versie uit `../global.json` en Node uit `../.nvmrc`. Deze versie is ook getest met Node 24.21.0 en npm 11.19.0. Een globale Angular CLI is niet nodig.

Maak eerst een ontwikkelingsdatabase met de instructies in [Database development](../docs/database-development.md). Start daarna de backend vanuit de repository-root, met jouw ontwikkelingsdatabase:

```powershell
$env:ConnectionStrings__MyShop = 'Server=(localdb)\MSSQLLocalDB;Database=MyShopDevelopment;Integrated Security=True;TrustServerCertificate=True'
dotnet run --project backend/src/MyShop.Api --launch-profile http
```

Open een tweede terminal in `frontend`:

```powershell
npm ci
npm start
```

Open [MyShop beheer](http://127.0.0.1:4200). De ontwikkelserver stuurt `/api/**` door naar `http://localhost:5032` volgens `proxy.conf.json`. Hiervoor zijn geen wijzigingen aan de CORS-instellingen van de backend nodig.

De interface en API vereisen een MyShop-beheerdersaccount. Volg eerst [Beveiligd beheer](../docs/admin-security.md) om de accountmigratie toe te passen en je eigen account lokaal aan te maken. Productiehosting blijft een afzonderlijke stap. De frontend bindt standaard aan 127.0.0.1. Een productiebuild vereist een host met SPA-fallback en een passende `/api`-reverse proxy.

## Wat deze versie ondersteunt

Vanuit een categorie of producttype kun je de bijbehorende producten openen. De filters staan in de URL als `categoryId` en `productTypeId` en kunnen samen worden gebruikt. Naamzoeken, pagineren en opnieuw proberen behouden deze filters. Ieder filter kan afzonderlijk worden verwijderd. Categoriefilters tellen alleen directe productkoppelingen, geen subcategorieën. Zoeken op artikelnummer blijft het hele assortiment doorzoeken.

- Overzicht met navigatie op desktop en smalle schermen.
- Producten zoeken en pagineren, details en bestaande varianten bekijken. Een product is ook rechtstreeks op artikelnummer (SKU) te vinden.
- Producttypen zoeken, pagineren, bekijken, aanmaken en hernoemen; gebruik ophalen en ongebruikte typen na bevestiging verwijderen.
- Kenmerken definiëren met naam, code, soort, product-/variantniveau en verplicht/filterbaar. Bestaande namen en instellingen wijzigen en definities na bevestiging verwijderen.
- Achtergebleven waarden van verwijderde kenmerken per product of variant bekijken en na bevestiging wissen.
- Kenmerkwaarden voor producten en varianten invoeren en wissen: tekst, gehele getallen, decimalen, ja/nee, datum en enkele/meerdere keuzes.
- Categorieën zoeken, pagineren, bekijken, hernoemen en hoofdcategorieën aanmaken.
- Product aanmaken met een gekozen producttype en eerste variant.
- Productnaam wijzigen, varianten toevoegen en variantnamen wijzigen.
- Artikelnummers en basisprijzen per variant opslaan en wissen.
- Kortingsregels per variant bekijken, toevoegen, wijzigen en na bevestiging wissen.
- Producten aan categorieën koppelen en ontkoppelen met een doorzoekbare, gepagineerde kiezer.
- Categorieën naar een ander bovenliggend niveau verplaatsen of terugbrengen naar hoofdniveau.
- Categorieën, producten en niet-laatste varianten na bevestiging verwijderen met gebruiks- en veiligheidscontroles.
- Bestaande kenmerken, artikelnummers en basisprijzen bekijken.
- Kenmerkcontrole met begrijpelijke meldingen per product of variant.

De producttypekiezer en categoriekiezer hebben een eigen zoekfunctie en paginering; ze beperken de keuze niet tot de eerste pagina.
Categorie- en producttypelijsten hebben volgens het huidige backendcontract geen totaaltelling. Na een precies volle laatste pagina kan nog een lege pagina volgen; terugbladeren blijft mogelijk.

De openbare klantwinkel op `/winkel` biedt zoeken, bladeren, productdetails en variantkeuze. De winkelmand bewaart varianten en aantallen in deze browser en laat de server de beschikbaarheid, actuele prijzen en subtotalen opnieuw bepalen. Nog niet opgenomen: checkout. In het beheer wordt de basisprijs getoond. De klantwinkel toont op de productpagina de actuele variantprijs na toepassing van de bestaande kortingsregels; zie [Klantwinkel](../docs/storefront.md).

Basisprijzen accepteren een komma of punt en maximaal twee decimalen. Nul is een geldige prijs; wissen maakt de prijs afwezig. Bedragen die JavaScript niet exact op centen kan versturen worden geweigerd. De valuta bestaat uit drie letters, bijvoorbeeld EUR. Artikelnummers bevatten maximaal 64 tekens zonder spaties en worden in hoofdletters opgeslagen. Een mislukte wijziging behoudt de ingevoerde gegevens; er zijn geen automatische herhaalpogingen voor schrijfacties.

Kenmerkgetallen worden exact als JSON-getallen verstuurd. Gehele getallen ondersteunen het volledige Int64-bereik; decimalen worden gecontroleerd tegen het .NET-decimalbereik en maximaal 28 decimalen, zonder afronding. De uitlezing gebruikt de broncontext van JSON.parse om numerieke kenmerkwaarden zonder precisieverlies weer te geven; een browser zonder die ondersteuning meldt een leesfout in plaats van afgeronde waarden te tonen. Basisprijzen gebruiken de afzonderlijke bestaande prijsinvoer.

Keuzes zijn vrije waarden volgens het bestaande backendcontract, zonder vooraf beheerde keuzelijst. Meerdere keuzes behouden volgorde en inhoud en mogen niet leeg of dubbel zijn. Ja/nee onderscheidt een ontbrekende waarde van nee; datums worden als kalenderdatum verstuurd zonder tijdzone. Het wissen van een verplicht kenmerk is toegestaan en levert daarna een aandachtspunt bij de kenmerkcontrole op.

## Controle

Ook categorie- en producttypeoverzichten bewaren `search` en `offset` in de URL. De teruglink vanuit details behoudt de zoekterm en pagina. Na verwijderen blijft de zoekterm behouden en start de lijst op pagina één. Na aanmaken wordt de lijst zonder zoekterm op pagina één vernieuwd.

Het productoverzicht bewaart de toegepaste naamzoekterm (`search`) en pagina (`offset`) naast de filters in de URL. Vernieuwen en browsernavigatie herstellen dit overzicht. Links naar details en nieuw product nemen deze context mee; de teruglink brengt je terug naar het overzicht. Na verwijderen gaat het overzicht naar pagina één met dezelfde filters en zoekterm. Ongeldige offsets vallen terug op pagina één. Met 'Zoekterm wissen' wis je alleen de naamzoekterm.

```powershell
npm run build
npm run test:ci
```

De Vitest-configuratie gebruikt maximaal twee testprocessen tegelijk om opstart-time-outs op een drukke ontwikkelcomputer te voorkomen.

De componenttests gebruiken de echte Angular HTTP-client met een gecontroleerde testbackend. Ze controleren aanvraagcontracten, paginering, annuleren van verouderde reads, foutafhandeling en dubbel-submitbescherming. Browsercontrole met de echte .NET-backend en SQL Server vult dit aan.

Voor de backend, vanuit de repository-root:

```powershell
$env:MYSHOP_TEST_SQLSERVER = 'Server=(localdb)\MSSQLLocalDB;Integrated Security=True;TrustServerCertificate=True'
dotnet build backend/MyShop.sln -m:1 --disable-build-servers
dotnet test backend/MyShop.sln -m:1 --disable-build-servers
git diff --check
```

De startuphandleiding schrijft geen voorbeeldgegevens automatisch naar jouw database. Bij een lege database kun je in het scherm eerst een producttype en vervolgens een product aanmaken.

Referenties: [Angular standalone-projecten](https://angular.dev/cli/new), [ontwikkelproxy](https://angular.dev/tools/cli/serve) en [testen met Vitest](https://angular.dev/guide/testing).

Kenmerkbeheer gebruikt de bestaande backendregels: code, soort en niveau blijven vast. Hernoemen en verplicht/filterbaar wijzigen gelden voor alle producten van het type. Een definitie verwijderen wist geen ingevulde waarden; die blijven zichtbaar bij het product en kunnen daar per waarde worden opgeruimd. De kenmerkcontrole wordt daarna opnieuw opgehaald.

Het scherm verwerkt één schrijfactie tegelijk per product of producttype. Verwijderen van een producttype is alleen beschikbaar na een geslaagde gebruikscontrole met nul producten en vraagt bevestiging. De backend controleert gebruik opnieuw; bij een conflict haalt de interface het gebruik opnieuw op. Een leesfout wordt nooit als nul gebruik geïnterpreteerd.

Categorieën tonen gebruik in productkoppelingen en directe subcategorieën. Verplaatsen gebruikt de backendcontrole tegen hiërarchische cycli. Een categorie kan alleen worden verwijderd wanneer beide aantallen nul zijn. Een product en een variant worden altijd met een expliciete bevestiging verwijderd; de laatste variant van een product blijft beschermd door het backendcontract.

Kortingsregels gebruiken het bestaande prijsregelcontract. Er zijn percentagekortingen en vaste kortingen, met een naam, positieve waarde, prioriteit en optionele start- en einddatum. De einddatum mag niet vóór de startdatum liggen. De getoonde basisprijs wordt door deze beheerinterface niet vooraf berekend; de backend blijft verantwoordelijk voor de uiteindelijke actieve kortingsuitkomst.

De drie catalogusoverzichten kunnen handmatig worden ververst met behoud van de toegepaste zoekterm, pagina en productfilters. Een nog niet ingediende zoekterm wordt daarbij niet toegepast. Vanaf vervolgpagina's is er een knop Eerste pagina; een lege vervolgpagina krijgt een eigen melding. Opnieuw zoeken naar dezelfde productnaam op pagina één haalt de resultaten opnieuw op.

Productdetails bieden een beschrijving, een hoofdafbeelding via HTTPS met alternatieve tekst, een voorbeeld van de opgeslagen presentatie en expliciet opslaan als concept of gepubliceerd. De productlijst toont de status. Zie [Productpresentatie](../docs/product-presentation.md) voor voorwaarden en grenzen.
