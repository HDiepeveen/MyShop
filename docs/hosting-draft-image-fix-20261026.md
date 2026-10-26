# Conceptafbeeldingen bedrijfsbeheerder — 26 oktober 2026 — 0.2.62

Broncommit e75a727. Doel SITE1 via hansdiepeveen-001-site1.ktempurl.com; eigen domein blijft losgekoppeld.

Oorzaak: /api/shop/product-images/{imageId} gaf niet-gepubliceerde foto’s alleen aan de volledige hoofdbeheerder vrij. Een bedrijfsbeheerder mocht wel uploaden, maar kreeg bij het opvragen van de foto een 404. Herstel controleert daarnaast het actuele products-recht via ICompanyAccess. Accounts met andere rollen en bezoekers blijven uitgesloten van conceptfoto’s. Privévoorbeelden krijgen Cache-Control private, no-store.

- Vijf ProductImagesHttpTests geslaagd, inclusief upload door bedrijfsbeheerder, afbeelding ophalen, afwijzen van bezoeker en andere rol, onmiddellijke intrekking van toegang en openbaar worden na publicatie. Productiebuild en Release publish geslaagd.
- Websiteback-up /hdwebframework-before-0.2.62-20261026.zip: paneeltaak 17:34 Succes.
- Pakket C:/Users/hansd/Documents/Codex/HDWebFramework-online-fix-0.2.62-20261026/HDWebFramework-update-0.2.62.zip; bevat geen productieconfig of bestaande sleutels. Geen schemawijziging of database-import. Voor update drie producten en vier accounts.
- Tijdelijk onderhoud met app_offline.htm.
- Uitpakken paneeltaak 17:36 Succes. Onderhoudsbestand hernoemd naar maintenance-completed-0.2.62-20261026.htm.
- Home, Assortiment, Contact en openbare instellingen HTTP 200. main-FODFTKI2.js bevestigt 0.2.62. Een bestaande conceptfoto geeft zonder login nog 404; gepubliceerd assortiment en foto visueel gecontroleerd. Geen ingelogde live-previewtest uitgevoerd.
- Drie producten en vier accounts na update behouden. Geen bestanden opnieuw geüpload naar producten, geen productpublicaties gewijzigd en geen domeinkoppelingen aangepast. Screenshot deployment-proof.png in pakketmap. Lokaal API en frontend opnieuw gestart met 0.2.62.
