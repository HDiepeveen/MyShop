# Facturen, bedrijfsgegevens en btw-percentages

Beheer beheert bedrijfsgegevens en btw-percentages via `/instellingen/facturatie`. De bedrijfsnaam, straat/huisnummer, postcode, plaats, Nederlands btw-id, eventueel KvK-nummer en factuurprefix zijn instelbaar. Opslaan gebruikt revisiecontrole. Voor uitgifte moeten de bedrijfsgegevens compleet zijn.

De btw-lijst begint met 21%, 9%, 0% en Vrijgesteld. Beheer kan percentages van 0 tot 100 met maximaal twee decimalen toevoegen, aanpassen of uitschakelen. Er is geen automatische koppeling aan verkooplanden of VIES: een ingevoerd buitenlands percentage maakt de fiscale toepassing niet automatisch correct. 0% en Vrijgesteld blijven afzonderlijke behandelingen. De beheerder controleert zelf de voorwaarden en noodzakelijke factuurvermelding, ook bij zakelijke afnemers en buitenlandse verkopen.

De prijzediteur haalt beschikbare keuzes op bij openen. Nieuwe prijzen of nieuwe btw-keuzes vereisen een actief percentage. Een bestaand vastgelegd percentage blijft bij het product behouden, ook als het later wordt uitgeschakeld. Wijzigingen in de percentageslijst rekenen bestaande prijzen, orders of facturen niet om. De preview ondersteunt ook bijvoorbeeld 12,5% met exacte gehele centen.

## Uitgeven en raadplegen

Het beheerdetail van een bestelling biedt Factuur bekijken / uitgeven. Een beheerder controleert het factuuradres, de naam of bedrijfsnaam van de afnemer, eventueel diens btw-id en de leveringsdatum of datum van vooruitbetaling. De controle van de btw-behandeling moet expliciet bevestigd worden. Bij 0% of Vrijgesteld is een btw-vermelding/toelichting verplicht. Uitgifte controleert de orderrevisie en geeft voor dezelfde bestelling steeds dezelfde factuur terug.

Factuurnummers komen uit een databasecounter die samen met het document wordt vastgelegd. Ze zijn uniek en oplopend, bijvoorbeeld `INV-2026-000001`. Het jaar is UTC; de counter wordt niet automatisch jaarlijks teruggezet. Bij twee gelijktijdige aanvragen voor dezelfde order ontstaat één factuur. Een gewijzigde, geannuleerde of terugbetaalde order krijgt geen nieuwe gewone factuur. Reeds uitgegeven facturen blijven raadpleegbaar.

De factuur bewaart bedrijfsgegevens, afnemergegevens, nummer, uitgiftetijdstip, opgegeven leveringsdatum, omschrijvingen, aantallen, nettobedragen, btw en totalen. Bedragen worden als exacte decimale tekst aan de browser geleverd. De opgeslagen gegevens veranderen niet door latere wijzigingen van prijzen, percentages of bedrijfsgegevens. Bezorgkosten worden naar verhouding over de bekende btw-groepen verdeeld; het totaal inclusief btw blijft exact gelijk aan de bestelling. Bij onvoldoende gegevens wordt uitgifte geweigerd.

De browser biedt Factuur afdrukken / PDF. Hiermee kan de factuur worden afgedrukt of als PDF opgeslagen, met Unicode-tekst en zonder navigatie of bewerkingsknoppen. Het is een vastgelegde factuur, afzonderlijk van het bestaande besteloverzicht. Klanten kunnen alleen de factuur van een aan hun account gekoppelde bestelling raadplegen. Gastfacturen kunnen door beheer worden afgedrukt of als PDF worden verstrekt; er is geen openbare link met persoonsgegevens.

Deze versie ondersteunt facturen in EUR. Oude orders zonder vastgelegde btw worden niet stilzwijgend van tarieven voorzien en kunnen nog geen factuur krijgen. Automatische factuurmail, creditnota's, vreemde-valuta-omrekening, btw-aangiften en automatische buitenlandse fiscale regels behoren niet tot deze uitbreiding. Correcties op uitgegeven facturen worden niet gedaan door het oorspronkelijke document te wijzigen.

De routes zijn `GET/PUT /api/billing/company`, `GET/POST /api/billing/vat-rates`, `GET/POST /api/orders/{orderId}/invoice` en `GET /api/customer/orders/{orderId}/invoice`. Instellingen en uitgifte zijn uitsluitend voor beheer; alle schrijfacties gebruiken CSRF. Pas de migratie `BillingInvoices` toe op de echte applicatiedatabase vóór starten. Integratietests gebruiken eigen geïsoleerde databases.

Controleer het toepasselijke tarief en de factuurgegevens tegen de [factuureisen van de Belastingdienst](https://www.belastingdienst.nl/wps/wcm/connect/bldcontentnl/belastingdienst/zakelijk/btw/administratie_bijhouden/facturen_maken/factuureisen/). De percentagelijst vervangt geen fiscale beoordeling.
