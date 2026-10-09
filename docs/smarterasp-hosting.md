# MyShop op SmarterASP.NET

Hostnaam: `hansdiepeveen-001-site1.ktempurl.com`. SQL-doel: `sql6033.site4now.net`,
`db_acf835_myshop`, gebruiker `db_acf835_myshop_admin`. Bewaar wachtwoorden buiten Git.

## Pakket maken

Voer vanuit de repository uit met een nieuwe uitvoermap buiten Git:

```powershell
./scripts/Publish-Hosting.ps1 -OutputDirectory C:/Users/hansd/Documents/Codex/MyShop-upload
./scripts/Export-HostingDatabase.ps1 -PackageDirectory C:/Users/hansd/Documents/Codex/MyShop-upload
```

Het pakket bevat de ASP.NET Core API en de Angular-productiebouw in `website/wwwroot`.
Gebruik op de hosting ASP.NET Core/.NET 10. Een geinstalleerde .NET 10-runtime en de
ASP.NET Core Hosting Bundle zijn nodig voor deze framework-dependent publicatie.
De gegenereerde `web.config` start de API in Production. Gebruik HTTPS.

## Lokale invoer van het databasewachtwoord

Voer in de uitvoermap `./Configure-Hosting.ps1` uit. Het script vraagt het
wachtwoord afgeschermd op, schrijft `website/appsettings.Production.json` en maakt
`MyShop-upload.zip`. Configuratie en zip bevatten het databasewachtwoord. Bewaar ze
prive, buiten Git, en stuur ze niet in een chat. Het configuratiebestand mag niet
in `wwwroot` staan. Upload geen backups of gegevensscripts naar de websitemap.

## Database overzetten

De lokale SQL Server 2025-backup kan niet naar de hosting-SQL Server 2022 worden
teruggezet. Gebruik daarom `database-schema.sql` en `database-data.sql` via
`./Install-HostingDatabase.ps1` in de uitvoermap. Dat script gebruikt de ingestelde
SQL-login. De verbinding gebruikt encryptie met certificaatcontrole. De installer
weigert een database met bestaande gebruikerstabellen voordat hij wijzigingen doet.
Het gegevensscript verwerkt de rijen in een transactie en controleert alle constraints.
Bij een fout wordt niet automatisch gewist of opnieuw geprobeerd. Laat de fout eerst
beoordelen: de structuur kan al zijn aangemaakt, maar gegevens worden teruggedraaid.

De export bevat producten, foto-bytes, accounts, instellingen en overige opgeslagen
gegevens. Identity-wachtwoordhashes blijven bruikbaar; lokale sessies en bestaande
bevestigings-/resetlinks worden niet overgenomen. SMTP wordt uitsluitend in de
hostingkopie uitgeschakeld; de lokaal versleutelde SMTP-sleutel wordt verwijderd en
het openbare websiteadres wordt vervangen door de hosting-URL. Stel in online beheer
de Brevo-sleutel opnieuw in en verstuur een testmail voordat je e-mail inschakelt.
De lokale database blijft ongewijzigd. Houd de export prive en bewaar een backup.

## Upload en controle

Upload alleen de INHOUD van `website` naar `/site1` (of upload de zip en pak hem daar
uit). Verwijder de standaard `index.html` van de provider uit `/site1`; de echte
Angular-index staat in `/site1/wwwroot/index.html`. Upload `web.config` mee. Zet de
site op .NET CORE-modus en bevestig de .NET 10-runtime. Zorg voor schrijfrechten op
`/site1/App_Data/keys`. De applicatie bewaart daar haar nieuwe Data Protection-sleutels;
bewaar deze map bij updates en backups. Verwijder hem niet bij herpublicatie.

Controleer `/winkel`, een product met fotos, rechtstreeks openen van een diepe
Angular-link, beheerlogin met het bestaande account, en klantlogin. Controleer
product- en fototellingen, bedrijfs-/btw-instellingen en de winkelmandinstelling.
Controleer e-mail na herinvoer van de SMTP-sleutel en na aanpassen van het openbare
adres. Online betalen wordt pas geconfigureerd na koppelen van de juiste provider
en HTTPS-callback; lokale user-secrets worden nooit automatisch geupload.

De SQL2022-import en SMTP-verzending moeten nog op de echte hosting worden getest.
IIS en gedeelde hosting kunnen achtergrondtaken bij inactiviteit pauzeren; controleer
bij de provider Always On of een scheduler voordat je op tijdige e-mails vertrouwt.
