# Hostingupdate 23 oktober 2026 — 0.2.55

Broncommit: 7fb286a. Uitgevoerd rond 16:46–16:53 CEST. Doel: SmarterASP.NET SITE1; zowel hansdiepeveen-001-site1.ktempurl.com als autoservice-ganpat.nl gebruiken dezelfde installatie.

- Alle 945 frontendtests geslaagd. Productionbuild en .NET Release publish uitgevoerd. Bestaande Pkcs- en CSS-budgetwaarschuwingen ongewijzigd.
- Websiteback-up /hdwebframework-before-0.2.55-20261023-1646.zip: taak Succes, circa 168 MB.
- Databaseback-up /db/db_acf835_myshop.bak: paneeltaak 23 oktober 16:47 Succes.
- Migratie 20261023122436_WebsiteDefaultCurrency toegepast: extra Currency-kolom met EUR als standaard. SQL in afzonderlijke batches binnen dezelfde transactie uitgevoerd. Vijf producten en twee accounts voor en na migratie; geen productbedragen gewijzigd.
- Uploadpakket C:/Users/hansd/Documents/Codex/HDWebFramework-testupdate-0.2.55-20261023-1640/HDWebFramework-update-0.2.55.zip (circa 28 MB). Bevat wwwroot/legal en de actuele frontend. Geen appsettings.Production.json of bestaande beveiligingssleutels opgenomen.
- Bestanden via paneel naar /site1 uitgepakt; taak Succes. Bestaande productieconfig, uploads in de database en App_Data/keys behouden.
- Tijdelijk app_offline.htm gebruikt; na update hernoemd naar maintenance-completed-0.2.55-20261023.htm. Onderhoudsstatus eerst 503, daarna beide websites HTTP 200.
- Gecontroleerd: Home, Assortiment, Contact, APK, drie juridische pagina’s en openbare instellingen HTTP 200. Actuele frontend main-P7TWMEE4.js; versie 0.2.55. Productkaarten met valutatekens en kleinere fotopadding visueel gecontroleerd. Screenshot deployment-proof.png in pakketmap.
- Beheerendpoint zonder login HTTP 401. Geen beheerlogin, testbestelling of testmail uitgevoerd; bestaande betaalinstellingen niet aangepast. Lokale frontend en API blijven beschikbaar op 4200 en 5032.
