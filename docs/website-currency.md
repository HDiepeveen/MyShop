# Standaardvaluta website

Bij Website en SEO, onder Website, staat Valuta als laatste veld. Verplicht: drie letters, bijvoorbeeld EUR, SRD of USD; opslag normaliseert naar hoofdletters. Bestaande installaties krijgen EUR via migratie WebsiteDefaultCurrency.

De instelling vult de valuta in bij nieuwe product-/variantprijzen en het genereren van varianten. De productprijs kan een afwijkende valuta bevatten. Al opgeslagen prijzen behouden hun eigen valuta; aanpassen van de standaard wijzigt geen prijzen en rekent geen bedragen om. Handmatig ingevoerde valutakeuzen worden niet overschreven door een achtergrondverversing van website-instellingen.

Dit is geen uitbreiding van ondersteunde betaalvaluta’s bij Mollie of andere betaalproviders en verandert geen bezorgtarieven of bestaande bestellingen. Daar blijven hun eigen valuta en validaties gelden.

De waarde wordt met de bestaande SEO-revisie opgeslagen en openbaar teruggegeven in /api/shop/settings. Productbeheerders hoeven daardoor geen toegang tot Website en SEO te hebben om de standaard te gebruiken.
