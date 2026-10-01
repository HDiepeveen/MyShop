# Klantwinkel

De openbare winkel staat op `/winkel`. Bezoekers kunnen zonder account zoeken op productnaam, filteren op categorie, bladeren per twintig producten, productdetails bekijken en een variant kiezen. Een productdetail toont de direct gekoppelde categorieën als links naar het gefilterde assortiment. Teruggaan naar het assortiment behoudt de zoekterm, categorie en pagina. Ontbrekende of onbereikbare afbeeldingen krijgen een tekstuele vervanging.

Alleen gepubliceerde producten zijn zichtbaar. Ook een rechtstreekse detailaanvraag voor een concept retourneert 404. Intrekken van publicatie wordt bij de volgende aanvraag verwerkt. Een al geopende pagina wordt niet automatisch bijgewerkt.

De openbare leesroutes zijn `/api/shop/categories`, `/api/shop/products`, `/api/shop/products/{productId}`, `/api/shop/products/{productId}/prices`, `/api/shop/cart/quote` en `/api/shop/payment-options`. De productlijst accepteert offset (vanaf nul), limit (1–100, standaard 20), search (maximaal 200 tekens na trimmen) en een optionele categoryId. Zoeken en categorie kunnen worden gecombineerd. Een categorie filtert op directe productkoppelingen; onderliggende categorieën worden niet automatisch meegenomen. De categorielijst bevat alleen categorieën waaraan ten minste één gepubliceerd product direct is gekoppeld. Producten en categorieën worden op naam en daarna ID gesorteerd. De detailrespons bevat uitsluitend ID, naam, beschrijving, afbeelding met alternatieve tekst, direct gekoppelde categorieën en varianten met ID en naam. Beheerinformatie, basisprijzen en interne kenmerken worden niet gedeeld. Bestaande beheerroutes blijven beveiligd.

Publiceer producten, beheer variantvoorraad en beheer de betaalopties via het beveiligde beheer. Zie [Voorraadbeheer](inventory.md). Online betalen volgt in een afzonderlijk onderdeel.

Klanten kunnen als gast bestellen of een account gebruiken om hun standaardafleveradres en
bestelgeschiedenis te bewaren;
zie [Klantaccounts](customer-accounts.md).

## Actuele variantprijzen

De productpagina haalt prijzen afzonderlijk op en toont het bedrag en de valuta van de gekozen variant. De server berekent alle varianten op hetzelfde UTC-tijdstip met de bestaande domeinregels: alleen de actieve regel met de hoogste prioriteit, bij gelijke prioriteit het laagste regel-ID. Begintijd en eindtijd zijn inclusief. Er worden geen kortingen gestapeld. Bedragen worden volgens de bestaande Money-regels afgerond en nooit negatief.

De openbare prijsrespons bevat `at` en `variants` met uitsluitend `variantId`, `amount` en `currency`. Bedragen worden als exacte tekst met twee decimalen geleverd, zodat de browser geen centen afrondt; een ontbrekende basisprijs geeft null voor bedrag en valuta en nul is een geldige prijs. De client kan het rekentijdstip niet kiezen. Concepten, ingetrokken publicaties en onbekende producten geven 404, ook voor prijzen. De respons wordt niet gecachet.

De getoonde prijs is een momentopname. **Prijs vernieuwen** haalt opnieuw de actuele bedragen op met behoud van de variantkeuze. Tijdens ophalen en na een fout verdwijnt de oude prijs; er wordt nooit op een basisprijs teruggevallen. De pagina toont het ophaalmoment in lokale tijd. Een latere bestelling zal opnieuw moeten rekenen.

De assortimentslijst berekent alle kaartprijzen op één servermoment en geeft dat moment als `at` terug. Per product bevat `prices` een minimum en maximum per valuta na toepassing van dezelfde actieve kortingsregels als op de detailpagina en bij checkout. Ongeprijsde varianten tellen niet mee; als geen variant een prijs heeft, toont de kaart **Prijs niet beschikbaar**. Een gelijk minimum en maximum wordt als één prijs getoond. Bedragen worden als exacte tekst met twee decimalen geleverd, zodat ook waarden boven JavaScripts veilige getalgrens niet worden afgerond. Btw-berekening is nog niet opgenomen. Bezorgkosten worden bij checkout berekend op basis van de gekozen bezorgoptie.
## Winkelmand

`/winkel/winkelmand` is openbaar. Een geprijsde variant kan vanuit de productpagina worden toegevoegd; nogmaals toevoegen verhoogt het aantal. De navigatie toont het totale aantal artikelen. Er zijn maximaal twintig verschillende varianten en per variant maximaal 99 stuks. Aantallen worden pas toegepast met **Aantal bijwerken**; verwijderen werkt direct.

De winkelmand bewaart alleen product-ID, variant-ID en aantal in browseropslag (`myshop.cart.v1`). Er is geen klantenaccount of nieuwe serveropslag nodig. Ongeldige of dubbele bewaarde regels worden geweigerd. Als browseropslag niet beschikbaar is, blijft de winkelmand voor de huidige pagina werken met een melding. Meerdere tabbladen worden niet actief gesynchroniseerd; de laatst opgeslagen winkelmand wordt bij herladen ingelezen.

Bij openen, wijzigen en **Winkelmand vernieuwen** wordt de hele winkelmand in één aanvraag op de server gecontroleerd. Elk product wordt daar één keer geladen; alle regels gebruiken hetzelfde servermoment voor de bestaande kortingsberekening. Alleen gepubliceerde, bestaande varianten met een prijs en voldoende gevolgde voorraad tellen mee. Bij een ontbrekend artikel, prijs of voorraad verschijnt geen subtotaal; de gebruiker kan het aantal verlagen of de regel verwijderen. Bij een netwerkfout verdwijnen alle oude bedragen totdat een nieuwe controle slaagt. Achterhaalde aanvragen worden afgebroken.

Regelbedragen en subtotalen worden op de server met decimalen berekend. De API levert bedragen als decimale tekst met twee cijfers achter de punt; de browser toont die met een decimale komma zonder opnieuw te rekenen. Zo blijven ook bedragen boven de veilige JavaScript-getalgrens exact. Elke valuta krijgt een eigen subtotaal; valuta worden nooit opgeteld of omgerekend. Nulprijzen blijven geldig. Bezorgkosten worden na de keuze bij checkout toegevoegd. Gevolgde voorraad wordt pas bij bestellen afgeboekt.
### Servercontrole van de winkelmand

`GET /api/shop/cart/quote` accepteert herhaalde `lines`-parameters in de vorm `productId:variantId:quantity`, bijvoorbeeld `?lines=PRODUCT-GUID:VARIANT-GUID:3`. Er worden alleen identifiers en aantallen geaccepteerd als berekeningsinvoer; het rekentijdstip en bedragen komen van de server. De route is anoniem, alleen-lezen en geeft `Cache-Control: no-store` terug. Er is geen nieuwe opslag of migratie.

Maximaal twintig unieke product/variant-combinaties zijn toegestaan, met aantallen 1–99 en niet-lege GUIDs. Ongeldige invoer geeft 400 voordat er producten worden gelezen. Een lege aanvraag geeft een lege winkelmand terug. De respons bevat `at`, `lines` en `totals`. Per regel komen de identifiers, het aantal, openbare namen, `amount`, `currency`, `total` en `failure` terug. `failure` is null, `unavailable`, `priceMissing` of `outOfStock`. Bij een niet-gepubliceerd product of ontbrekende variant worden ook de namen weggelaten. Zodra een regel niet berekend kan worden, zijn de subtotalen leeg. Geldige regels blijven zichtbaar zodat de bezoeker de winkelmand kan herstellen.

## Bestellen met later betalen

Wanneer **later betalen** in het beheer beschikbaar is, kan een gast naam, e-mailadres en afleveradres invullen en de bestelling plaatsen. De browser stuurt alleen de gekozen betaalcode, klantinvoer, artikelidentiteiten, aantallen en de zojuist getoonde prijzen naar `POST /api/shop/orders`. Deze openbare schrijfactie vereist een antiforgerytoken.

De server controleert de betaaloptie en berekent alle regels opnieuw op één actueel tijdstip. Een verdwenen product, ontbrekende prijs of prijswijziging geeft een conflict; de klant moet dan eerst de winkelmand vernieuwen. Een clientprijs bepaalt nooit het bestelbedrag. Na een geslaagde controle worden klantgegevens, afleveradres, product- en variantnamen, eenheidsprijzen, regelbedragen, totalen en de op dat moment ingestelde betaalinstructies als onveranderlijke bestelsnapshot opgeslagen. De winkelmand wordt geleegd en de klant ziet het bestelnummer en, indien ingesteld, de instructies voor achteraf betalen. Wijzigingen in de beheerinstellingen veranderen bestaande bestellingen niet.

Iedere poging gebruikt een willekeurig checkouttoken. Opnieuw verzenden met hetzelfde token retourneert dezelfde bestelling en maakt geen duplicaat. De initiële status is `AwaitingPayment` en de betaalmethode is `PayLater`. Gevolgde voorraad wordt atomair met de bestelling afgeboekt en komt terug wanneer deze bestelling vóór betaling wordt geannuleerd.

De klant kiest daarnaast een door beheer ingeschakelde bezorgoptie. De server neemt de actuele naam, toelichting en kosten over in de bestelling en telt de kosten bij het juiste valutaatotaal op; zie [Bezorgopties](delivery-methods.md). Automatische e-mail valt buiten deze slice. Online betalen is voorbereid: de openbare betaalopties tonen deze keuze alleen wanneer `Payments:Online:Provider` op een ondersteunde provider staat en beheer de optie heeft ingeschakeld. `POST /api/shop/online-payments` controleert de actuele winkelmand, bezorgoptie en betaalinstellingen en retourneert de providernaam, providerbetaling, checkout-url, betalingskenmerk en gevalideerde totalen. De huidige TestPay-adapter geeft al een veilige test-checkout-url terug en de betaalstart wordt opgeslagen als basis voor return/webhook-verwerking; de daadwerkelijke Mollie-providerredirect volgt in een volgende slice.
