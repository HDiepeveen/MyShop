# WWW-doorverwijzing — 27 oktober 2026

- Aan online /site1/web.config een tweede host-specifieke regel toegevoegd: www.autoservice-ganpat.nl naar https://autoservice-ganpat.nl/{R:1}, HTTP 301, zoekparameters behouden.
- Tijdelijke-hostregel, handlers en aspNetCore behouden.
- Back-up vooraf: C:/Users/hansd/Documents/Codex/web.config-before-www-redirect-20261027.txt.
- Getest: HTTPS-www hoofdpad en /apk met twee zoekparameters, HTTP-www /apk, tijdelijke host /apk: 301 naar overeenkomstig HTTPS-hoofdadres zonder www.
- Hoofdwebsite en sitemap blijven HTTP 200.
- scripts/hosting/production-rewrite.xml bevat beide regels, behouden via bestaande publicatieproces. XML gecontroleerd. Versie blijft 0.2.70; geen applicatierelease of databasewijziging.
- Screenshot C:/Users/hansd/Documents/Codex/www-redirect-proof-20261027.png.
