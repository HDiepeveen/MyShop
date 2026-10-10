# SEO-instellingen voor assortiment en producten

Open **Beheer > SEO** om de zichtbare koptekst en de SEO-titel van het assortiment
te wijzigen. De titel wordt voor het browsertabblad en als aanwijzing voor
zoekmachines gebruikt. De bestaande standaardwaarden blijven beschikbaar.

Open bij een product **SEO voor dit product**. De drie velden zijn optioneel:
SEO-titel, SEO-omschrijving en webadres. Lege titel gebruikt de productnaam met
MyShop; lege omschrijving gebruikt een korte samenvatting van de beschrijving.
Een leeg webadres gebruikt de productnaam met een unieke product-ID. Vul voor een
kort eigen adres bijvoorbeeld `opel-corsa-2014` in, zonder `/winkel/`.

Adressen zijn uniek over huidige en eerdere eigen productadressen. Eerdere
adressen blijven bij dezelfde product-ID en verwijzen met HTTP 301 naar het actuele
adres. Ook oorspronkelijke GUID-links blijven werken. Automatische adressen kunnen
na een naamswijziging veranderen; hun product-ID zorgt dat oude links blijven
verwijzen. Technische routes zoals account, registratie en betalen zijn gereserveerd.
SEO-wijzigingen gebruiken een eigen revisiecontrole; productprijzen, voorraad en
presentatierevisie worden niet gewijzigd.

De klantinterface past titel, omschrijving en canonical-link toe bij navigatie.
In de gecombineerde ASP.NET-publicatie worden metadata en basisinhoud voor het
assortiment/product al in de eerste HTML-response opgenomen, voor alle bezoekers.
Angular bouwt daarna de interactieve pagina op. Dit is geen volledige Angular SSR.
Voor accounts, beheer, winkelmand en betaling wordt noindex gebruikt; toegang tot
privégegevens blijft geregeld door de bestaande authenticatie en autorisatie.
Conceptproducten en onbekende productpagina’s geven 404 en krijgen geen publieke
SEO-metadata. Canonical-links gebruiken het huidige HTTPS-hostadres op de hosting.

Deze slice bevat geen sitemap, categorie-SEO, sociale deelafbeeldingen of
Search Console-koppeling. SEO-velden garanderen geen positie in zoekresultaten.

De migraties `CatalogSeo` en `CatalogSeoNaming` voegen de configuratie, productmetadata
en adresgeschiedenis toe. De tweede migratie brengt de tabelnaam in lijn met de
bestaande meervoudconventie en bewaart de data. Beide moeten vóór een serverupdate
worden toegepast. Lokaal is dit uitgevoerd; de online webshop is niet aangepast.
