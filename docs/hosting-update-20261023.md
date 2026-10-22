# Hostingupdate 23 oktober 2026 — 0.2.54

Broncommit: 406a638. Doel: SmarterASP.NET SITE1, hansdiepeveen-001-site1.ktempurl.com. autoservice-ganpat.nl wijst naar dezelfde installatie en toont dus dezelfde update.

- Frontend productionbuild en .NET Release publish uitgevoerd. Bestaande Pkcs dependencywaarschuwingen en CSS-budgetwaarschuwing blijven open.
- Siteback-up via bestandsbeheer: /hdwebframework-before-0.2.54-20261023.zip (succes).
- Databaseback-up via paneel: /db/db_acf835_myshop.bak, taak 23 oktober circa 00:36 (succes).
- Drie schema-uitbreidingen toegepast van CategoryPageSeo naar 20261021203426_AssortmentBackground. Vijf producten en twee accounts voor en na update.
- Bestaande appsettings.Production.json en App_Data/keys behouden. Oude productie-connectionstringsleutel werkt via compatibiliteitscode; geen lokale database geïmporteerd.
- Onderhoud met app_offline.htm tijdens uitpakken; daarna reversibel hernoemd naar maintenance-completed-20261023.htm.
- Nieuwe applicatienaam HDWebFramework; oude applicatiebestanden zijn niet verwijderd. web.config start HDWebFramework.Api.dll. Bestaande sessies worden niet overgenomen.
- Eerste bestandskopie na mislukte publish had de juridische bestanden afgevlakt. Hersteld met aanvullend archief naar wwwroot/legal; HTTP en browsercontrole bevestigen actuele documenten.
- Volledig gecorrigeerd lokaal pakket: C:/Users/hansd/Documents/Codex/HDWebFramework-testupdate-0.2.54-20261023/HDWebFramework-complete-0.2.54.zip. Geen productiegeheimen of sleutels in dit archief.

Gecontroleerd: Home, Contact, Assortiment, categorie occasions, APK, Reparatie, inlogpagina, drie juridische pagina’s en privacydownload HTTP 200. Privacytekst heeft website-scope en eenmanszaak. Frontend bevat versie 0.2.54. Openbare betaalopties: checkoutEnabled=false. Beheerendpoint zonder login HTTP 401. Bestaande beheerlogin en SMTP-verzending niet opnieuw uitgevoerd; geen testmail verstuurd. Lokale API na publish weer gestart op 5032; Angular draait op 4200.

Aandacht: hostingdashboard toont unieke bezoekers en dagelijkse bezoekersaantallen. Dat bewijst geen opslag van ruwe IP-adressen, maar vraagt nadere uitleg naast de eerdere helpdeskverklaring over uitgeschakelde Visitor Stats. Niet op basis hiervan privacyteksten als definitief beschouwd. Leveranciers-/doorgiftecontrole blijft open.
