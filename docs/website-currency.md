# Websitevaluta

Bij Website en SEO, onder Website, staat Valuta als laatste veld. Verplicht: drie letters, bijvoorbeeld EUR, SRD of USD; opslag normaliseert naar hoofdletters. Bestaande installaties krijgen EUR via migratie WebsiteDefaultCurrency.

Een wijziging van de websitevaluta vervangt de valuta van alle bestaande product-/variantprijzen, ook van eerder afwijkend ingestelde valuta. Alleen de valutacode verandert: brutoprijs, nettoprijs, btw-percentage, voorraad en prijsregels blijven intact. Bijvoorbeeld EUR 10 wordt SRD 10; er is geen wisselkoersomrekening. Het ongewijzigd opslaan van de websitevaluta laat afwijkende productvaluta’s staan. Daarna kan elke productprijs weer een afwijkende valuta krijgen.

De wijziging van instellingen en productvaluta’s gebeurt in één transactie. Productrevisies worden vernieuwd zodat oude geopende bewerkschermen de wijziging niet stilzwijgend terugdraaien.

Nieuwe prijzen en gegenereerde varianten gebruiken de websitevaluta als standaard. Handmatig ingevoerde valutakeuzen in een open formulier worden niet overschreven door een achtergrondverversing van website-instellingen.

Bestaande bestellingen en betalingen behouden hun historische bedragen en valuta. Dit breidt de ondersteunde betaalvaluta’s bij betaalproviders niet uit en wijzigt geen bezorgtarieven; daarvoor blijven de eigen valuta en validaties gelden.
