# Varianten uit opties

Open een product in beheer en gebruik **Varianten uit opties aanmaken**.
Het producttype bepaalt de variantkenmerken; de opties verschillen per product.
Vul één optie per regel in, bijvoorbeeld bij Maat: S, M, L en bij Kleur: blauw, zwart.
**Combinaties bekijken** toont de zes uitvoeringen. Vink combinaties uit die je niet verkoopt.

Je kunt een gezamenlijke beginprijs exclusief btw (EUR), beschikbaar btw-percentage en
beginvoorraad invullen. Lege beginprijs betekent nog geen prijs; lege voorraad betekent
voorraad nog niet volgen. Bewerk later de prijs, voorraad en het artikelnummer per variant.

Er worden maximaal 100 combinaties per keer en 10 variantkenmerken ondersteund.
Elke optie bevat maximaal 100 tekens. Voor getallen gebruik je geen eenheid;
voor boolean gebruik je true/false en voor datums jjjj-mm-dd.
Een kenmerk met meerdere keuzes tegelijk (MultiChoice) vul je na het aanmaken apart in.

Bestaande combinaties worden herkend aan hun kenmerkwaarden, ook wanneer hun naam
anders is. Hun naam, prijs, voorraad en artikelnummer blijven behouden.
De oorspronkelijke variant zonder kenmerken wordt ook behouden; vul die apart in
of verwijder haar nadat er andere varianten zijn.
Bij opnieuw openen komen opties uit de opgeslagen varianten terug in de lijsten.
Opties die je niet aanmaakt worden niet apart opgeslagen.

De gekozen combinaties worden samen via het bestaande productaggregate opgeslagen.
Een ongeldig verzoek wordt volledig afgewezen. Een gewijzigde productrevisie geeft 409:
vernieuw de pagina en controleer opnieuw. Er is geen nieuwe database-migratie nodig.
