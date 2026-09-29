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

De interface en API hebben nog geen login of rollen. Deze opzet is voor lokaal ontwikkelen; beveiliging en productiehosting zijn volgende stappen. De frontend bindt standaard aan 127.0.0.1. Een productiebuild vereist een host met SPA-fallback en een passende `/api`-reverse proxy.

## Wat deze versie ondersteunt

- Overzicht met navigatie op desktop en smalle schermen.
- Producten zoeken en pagineren, details en bestaande varianten bekijken.
- Producttypen zoeken, pagineren en aanmaken.
- Categorieën zoeken, pagineren, bekijken, hernoemen en hoofdcategorieën aanmaken.
- Product aanmaken met een gekozen producttype en eerste variant.
- Productnaam wijzigen, varianten toevoegen en variantnamen wijzigen.
- Artikelnummers en basisprijzen per variant opslaan en wissen.
- Producten aan categorieën koppelen en ontkoppelen met een doorzoekbare, gepagineerde kiezer.
- Bestaande kenmerken, artikelnummers en basisprijzen bekijken.
- Kenmerkcontrole met begrijpelijke meldingen per product of variant.

De producttypekiezer en categoriekiezer hebben een eigen zoekfunctie en paginering; ze beperken de keuze niet tot de eerste pagina.
Categorie- en producttypelijsten hebben volgens het huidige backendcontract geen totaaltelling. Na een precies volle laatste pagina kan nog een lege pagina volgen; terugbladeren blijft mogelijk.

Nog niet opgenomen: attribuutdefinities/-waarden bewerken, kortingsregels wijzigen, categoriehiërarchie wijzigen, producten/varianten/categorieën verwijderen, inloggen, klantwinkel en checkout. De getoonde prijs is de basisprijs, niet de uitkomst van kortingsregels.

Basisprijzen accepteren een komma of punt en maximaal twee decimalen. Nul is een geldige prijs; wissen maakt de prijs afwezig. Bedragen die JavaScript niet exact op centen kan versturen worden geweigerd. De valuta bestaat uit drie letters, bijvoorbeeld EUR. Artikelnummers bevatten maximaal 64 tekens zonder spaties en worden in hoofdletters opgeslagen. Een mislukte wijziging behoudt de ingevoerde gegevens; er zijn geen automatische herhaalpogingen voor schrijfacties.

## Controle

```powershell
npm run build
npm run test:ci
```

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
