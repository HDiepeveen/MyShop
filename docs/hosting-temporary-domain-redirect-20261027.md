# Tijdelijke hosting-URL doorsturen — 27 oktober 2026

- Online /site1/web.config: rewrite-blok toegevoegd direct onder system.webServer. Bestaande handlers en aspNetCore ongewijzigd.
- Exacte hostvoorwaarde hansdiepeveen-001-site1.ktempurl.com, bestemming https://autoservice-ganpat.nl/{R:1}, HTTP 301, zoekparameters behouden.
- Vooraf kopie: C:/Users/hansd/Documents/Codex/web.config-before-host-redirect-20261027.txt.
- Hoofdpad en /apk?bron=test&waarde=1 online gecontroleerd: beide 301 naar overeenkomstige hoofdadres. Hoofdsite en sitemap blijven HTTP 200.
- Browser bevestigt overgang naar https://autoservice-ganpat.nl/apk?bron=test&waarde=1.
- Publicatie bewaart regel via scripts/hosting/production-rewrite.xml en Publish-Hosting.ps1. Andere implementatie: eigen -RewriteRulesFile meegeven; aparte testsite: -RewriteRulesFile ''.
- Alleen tijdelijke hostinghost doorgestuurd; www is niet aangepast.
- Publicatiescript syntactisch gecontroleerd; XML-samenvoeging gecontroleerd met behoud hostingelementen. Geen volledige applicatierelease of databasewijziging nodig, versie blijft 0.2.70.
- Bewijs: C:/Users/hansd/Documents/Codex/hosting-redirect-proof-20261027.png.
