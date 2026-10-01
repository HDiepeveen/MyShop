# Beheerdersdashboard

Het beheerdersdashboard staat op `/` in de beheeromgeving en leest `GET /api/dashboard`. De route is alleen beschikbaar voor beheerders.

De tellingen zijn bedoeld als operationeel overzicht:

- `productCount` telt alle producten.
- `publishedProductCount` telt gepubliceerde producten.
- `draftProductCount` is het verschil tussen alle producten en gepubliceerde producten.
- `customerCount` telt accounts met de klantrol.
- `orders` groepeert bestellingen per status.
- `activeRevenue` telt betaalde en verzonden bestellingen per valuta. Bestellingen die wachten op betaling, geannuleerd zijn of zijn terugbetaald tellen niet mee. Valuta worden niet omgerekend.
- `lowStock` toont maximaal twintig gepubliceerde productvarianten waarvan voorraad wordt gevolgd en de voorraad maximaal vijf is.
- `recentOrders` toont de vijf nieuwste bestellingen, ongeacht status.

Het dashboard is een momentopname. De knop `Vernieuwen` haalt de actuele stand opnieuw op.
