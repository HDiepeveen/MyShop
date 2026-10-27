# Openbare zoekmachinebestanden — 0.2.70 — 27 oktober 2026

- Broncommit 0762287: eigen AllowAnonymous endpoints /robots.txt en /sitemap.xml; voorheen geen endpoints waardoor fallbackbeheerbeveiliging HTTP 401 gaf.
- Sitemap verzamelt ingeschakelde Home/extra pagina's/Contact, assortiment, openbare categorieën en uitsluitend gepubliceerde producten. XML via XDocument, unieke absolute URL's gebaseerd op het aangevraagde domein. Geen verzonnen lastmod-datums. Cache maximaal 5 minuten.
- Twee tests geslaagd: robots HTTP 200 met authenticatiefallback, beide endpoints AllowAnonymous, XML namespace/escaping/deduplicatie. Lokale sitemap met echte database HTTP 200.
- Productiebuild en Release-publicatie geslaagd.
- Hostingback-up /hdwebframework-before-0.2.70-20261027.zip: 15:05 Succes; uitpakken /site1/hdwebframework-update-0.2.70.zip: 15:07 Succes.
- Onderhoudsbestand hernoemd naar maintenance-completed-0.2.70-20261027.htm.
- Online https://autoservice-ganpat.nl/robots.txt geeft 200 text/plain en verwijst naar /sitemap.xml.
- Online sitemap geeft 200 application/xml en 12 URL's. Alle 12 URL's gecontroleerd: HTTP 200 en geen noindex-meta.
- /api/products blijft 401 zonder beheerlogin. Voor/na 4 producten en 4 accounts. Geen migratie/configuratie/sleutels vervangen.
- Domein autoservice-ganpat.nl is sinds eerder op 27 oktober weer actief gekoppeld.
- Pakket en robots-public-proof.png: C:/Users/hansd/Documents/Codex/HDWebFramework-online-update-0.2.70-20261027.
- Nog niet ingediend bij Google Search Console; geen indexering gegarandeerd.
