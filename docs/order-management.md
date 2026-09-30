# Order management

Administrators can open **Bestellingen** to see orders placed through the public checkout. The overview shows the newest orders first and supports paging. The detail page shows the immutable customer, delivery address, product, price and total snapshots recorded when the order was placed.

The management endpoints are protected by the same administrator policy as catalog management:

- `GET /api/orders?offset=0&limit=20`
- `GET /api/orders/{id}`

Amounts are returned as two-decimal strings, together with their currency. Payment methods and statuses use stable API names (`payLater` and `awaitingPayment`). This first management slice is read-only; changing payment or fulfilment status is outside its scope.
