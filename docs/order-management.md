# Order management

Administrators can open **Bestellingen** to see orders placed through the public checkout. The overview shows the newest orders first and supports paging. The detail page shows the immutable customer, delivery address, product, price and total snapshots recorded when the order was placed.

The management endpoints are protected by the same administrator policy as catalog management:

- `GET /api/orders?offset=0&limit=20`
- `GET /api/orders/{id}`

Amounts are returned as two-decimal strings, together with their currency. Payment methods and statuses use stable API names (`payLater`, `awaitingPayment` and `paid`).

An administrator can mark an order awaiting payment as paid from its detail page. `PUT /api/orders/{id}/status` accepts `paid` with the revision returned by the detail endpoint. The update records its UTC payment time and replaces the revision atomically. A stale revision returns a conflict so concurrent changes are never overwritten silently. Shipping and cancellation remain outside this slice.
