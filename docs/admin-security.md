# Beveiligd beheer

MyShop gebruikt ASP.NET Core Identity met EF Core en SQL Server voor lokale beheerdersaccounts. Alle catalogusroutes vereisen standaard de rol `Administrator`. Alleen sessiecontrole, het ophalen van een CSRF-token en inloggen zijn openbaar. Er is geen openbare registratie.

## Eerste account aanmaken

Stop de oude API. Voer vanuit de repository-root in een interactieve PowerShell-terminal uit:

```powershell
$developmentConnection = 'Server=(localdb)\MSSQLLocalDB;Database=MyShopDevelopment;Integrated Security=True;TrustServerCertificate=True'
dotnet ef database update --project backend/src/MyShop.Infrastructure --startup-project backend/src/MyShop.Infrastructure --connection $developmentConnection
$env:ConnectionStrings__MyShop = $developmentConnection
dotnet run --project backend/src/MyShop.Api -- --create-admin
```

Gebruik de bestaande database met migratiehistorie; zie [Database development](database-development.md) voor databases zonder migratiehistorie. De nieuwe migratie voegt alleen Identity-tabellen toe. De catalogusdomeinmodellen en repositories veranderen niet.

Het commando vraagt een gebruikersnaam en tweemaal een verborgen wachtwoord: 12–128 tekens met een hoofdletter, kleine letter, cijfer en symbool. Daarna stopt het; het start geen webserver. Een bestaande naam wordt nooit overschreven of gepromoveerd. Een mislukte aanmaak wordt teruggedraaid. Geef wachtwoorden nooit mee in argumenten, scripts, Git of chat.

Start daarna API en frontend volgens de frontendhandleiding en log in op `/inloggen`. Er worden geen automatische migraties of standaardaccounts aangemaakt.

## Gebruik en herstel

- De sessie vervalt na 30 minuten, ook bij actief gebruik. De sessiecookie is HttpOnly en SameSite=Strict; er staan geen toegangstokens in browseropslag.
- Wachtwoord wijzigen vereist het huidige wachtwoord. Daarna moet je opnieuw inloggen; eerdere sessies vervallen.
- Uitloggen trekt alle sessies van dit account in. Een netwerkfout wordt niet als succesvol uitloggen getoond.
- Vijf onjuiste wachtwoorden blokkeren een account vijftien minuten. Maximaal tien loginverzoeken per bron-IP per minuut, per serverproces. De foutmelding onderscheidt geen onbekende naam, onjuist wachtwoord, ontbrekende rol of blokkade.
- Vergeten wachtwoord: voer lokaal `dotnet run --project backend/src/MyShop.Api -- --reset-admin` uit met dezelfde verbindingsinstelling. Dit reset alleen een bestaand beheerdersaccount, heft de loginblokkade op en trekt sessies in.
- Extra beheerders kunnen met `--create-admin` worden toegevoegd. Deze batch bevat geen webscherm voor gebruikers- of rollenbeheer, e-mailherstel of MFA.

## API en browser

`GET /api/auth/session` geeft `authenticated`, `administrator` en `name`. `GET /api/auth/csrf` plaatst antiforgerycookies. Angular stuurt automatisch `X-XSRF-TOKEN` bij schrijfacties naar dezelfde oorsprong. Na inloggen en vóór uitloggen of wachtwoordwijziging wordt een nieuw token opgehaald. Alle POST/PUT/PATCH/DELETE-aanvragen, inclusief login, vereisen een geldig token. Schrijfacties worden bij sessieproblemen nooit automatisch herhaald.

`POST /api/auth/login` ontvangt `userName` en `password`; `POST /api/auth/logout` beëindigt sessies; `POST /api/auth/password` ontvangt `currentPassword` en `newPassword`. De API geeft 401/403 in plaats van redirects; antiforgeryfouten geven 400. Account- en catalogusantwoorden worden niet gecachet. Rollen en beveiligingsstempels worden bij iedere aanvraag opnieuw gecontroleerd.

## Productievoorwaarden

Gebruik HTTPS en bied frontend en `/api` onder dezelfde oorsprong aan. Buiten Development hebben beveiligingscookies altijd de Secure-vlag. Bij TLS-beëindiging op een reverse proxy moet de API de oorspronkelijke HTTPS-verbinding betrouwbaar herkennen; configureer uitsluitend vertrouwde proxies voor ingebruikname.

Bewaar ASP.NET Core Data Protection-sleutels duurzaam en afgeschermd voor het serviceaccount; deel ze bij meerdere instances. Voor meerdere instances is een gezamenlijke loginlimiet op de proxy nodig. Hosting, back-ups en monitoring horen bij de aparte productiemijlpaal.

## Tests

De beveiligingstests gebruiken een echte HTTP-server en eigen tijdelijke `MyShopTests_<guid>` SQL Server-databases. Ze testen alle catalogusroutes zonder login, CSRF, rolintrekking, wachtwoordwijziging, cookiehergebruik, sessieverloop, accountaanmaak/herstel en loginlimieten. Stel `MYSHOP_TEST_SQLSERVER` in, zoals bij de bestaande databasetests. De ontwikkelingsdatabase wordt niet gewijzigd.

Bronnen: [ASP.NET Core cookie authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0), [antiforgery](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0).
