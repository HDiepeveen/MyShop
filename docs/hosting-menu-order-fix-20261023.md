# Herstel menuvolgorde — 23 oktober 2026 — 0.2.58

Broncommit 6348c12. Online installatie SITE1 (autoservice-ganpat.nl en tijdelijke hosting-URL).

Oorzaak live gereproduceerd met bedrijfsbeheerder: oude APK- en Reparatiepagina hadden Guid.Empty als PageId. Daardoor kregen beide dezelfde menu-sleutel; verplaatsen sorteerde ze samen en opslaan leverde HTTP 400 op. De generieke melding verscheen alleen bovenaan de lange instellingenpagina.

Herstel: oude pagina’s krijgen bij lezen een vaste, afzonderlijke ID op basis van legacy-adres en positie. Bij de eerstvolgende opslag worden deze IDs bewaard. Pagina-inhoud en webadressen blijven intact. Valideringsfouten en opslagbevestiging staan nu ook direct bij de opslagknop; specifieke SEO-fouttekst van de server wordt doorgegeven.

- Alle 954 frontendtests en zeven gerichte SQL/backendtests geslaagd, inclusief oude pagina’s en bedrijfsbeheerder met alleen seo-recht.
- Frontendbuild en Release publish geslaagd; bestaande Pkcs- en CSS-budgetwaarschuwingen ongewijzigd.
- Websiteback-up /hdwebframework-before-0.2.58-20261023.zip, paneeltaak 18:51 Succes. Oorspronkelijke HomePageJson afzonderlijk veiliggesteld buiten repository/pakket.
- Pakket C:/Users/hansd/Documents/Codex/HDWebFramework-online-fix-0.2.58-20261023/HDWebFramework-update-0.2.58.zip, geen productieconfig of bestaande sleutels opgenomen. Geen schemawijziging.
- Uitpakken 18:53 Succes. Onderhoud hernoemd naar maintenance-completed-0.2.58-20261023.htm.
- Rechtstreeks live getest in ingelogde bedrijfsbeheerderssessie: Banden één positie omhoog en terug, vervolgens Instellingen opslaan. Bevestiging Webshopinstellingen opgeslagen zichtbaar naast knop. Oorspronkelijke zichtbare volgorde behouden. Openbare instellingen bevatten acht unieke menulinks en twee unieke pagina-IDs. Naar Home en terug genavigeerd; volgorde blijft behouden.
- Actuele main-YOWIJRKJ.js bevat 0.2.58. Screenshot menu-order-saved.png in pakketmap. Lokale frontend en API herstart, 0.2.58 lokaal bevestigd.
