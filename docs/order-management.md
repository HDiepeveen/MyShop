# Order management

Administrators can open **Bestellingen** to see orders placed through the public checkout. The overview shows the newest orders first and supports paging. The detail page shows the immutable customer, delivery address, product, price and total snapshots recorded when the order was placed.

The management endpoints are protected by the same administrator policy as catalog management:

- `GET /api/orders?offset=0&limit=20&status=awaitingPayment&search=MS-2026`
- `GET /api/orders/{id}`

Amounts are returned as two-decimal strings, together with their currency. Payment methods and statuses use stable API names (`payLater`, `awaitingPayment`, `paid`, `shipped` and `cancelled`).

The overview can be filtered by one status. Omitting `status` returns all orders. The filtered total controls paging, and an unsupported status returns 400.

The optional trimmed `search` value matches part of the order number, customer name or e-mail address and can be combined with the status filter. It has a maximum length of 200 characters. The management screen keeps both values while paging and when opening an order.

An administrator can mark an order awaiting payment as paid and then mark a paid order as shipped from its detail page. Shipping requires a carrier and tracking code of at most 100 characters each; both are recorded and shown with the shipment time. Existing shipped orders from before this addition may have no tracking details. An order that still awaits payment can instead be cancelled with a required reason of at most 500 characters. Paid and shipped orders cannot be cancelled because refund handling is not yet available.

`PUT /api/orders/{id}/status` accepts `paid`, `shipped` or `cancelled` with the revision returned by the detail endpoint. Shipping also requires `carrier` and `trackingCode`; cancellation requires `reason`. Each update records its UTC event time and replaces the revision atomically. A stale revision returns a conflict so concurrent changes are never overwritten silently.
