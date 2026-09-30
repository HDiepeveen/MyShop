# Order management

Administrators can open **Bestellingen** to see orders placed through the public checkout. The overview shows the newest orders first and supports paging. The detail page shows the immutable customer, delivery address, product, price and total snapshots recorded when the order was placed.

The management endpoints are protected by the same administrator policy as catalog management:

- `GET /api/orders?offset=0&limit=20&status=awaitingPayment&search=MS-2026`
- `GET /api/orders/{id}`

Amounts are returned as two-decimal strings, together with their currency. Payment methods and statuses use stable API names (`payLater`, `awaitingPayment`, `paid`, `shipped`, `cancelled` and `refunded`).

The overview can be filtered by one status. Omitting `status` returns all orders. The filtered total controls paging, and an unsupported status returns 400.

The optional trimmed `search` value matches part of the order number, customer name or e-mail address and can be combined with the status filter. It has a maximum length of 200 characters. The management screen keeps both values while paging and when opening an order.

An administrator can mark an order awaiting payment as paid and then mark a paid order as shipped from its detail page. Recording a payment requires a payment reference of at most 100 characters; it is stored and shown with the payment time. Existing paid orders from before this addition may have no payment reference. Shipping requires a carrier and tracking code of at most 100 characters each; both are recorded and shown with the shipment time. Existing shipped orders from before this addition may have no tracking details. An order that still awaits payment can instead be cancelled with a required reason of at most 500 characters.

A paid order that has not been shipped can be marked as refunded after the full amount has been returned outside MyShop. This requires a refund reference of at most 100 characters and a reason of at most 500 characters. Both values and the UTC refund time are retained for the audit trail. MyShop records this manual action; automatic provider refunds and returns after shipment remain outside this slice.

`PUT /api/orders/{id}/status` accepts `paid`, `shipped`, `cancelled` or `refunded` with the revision returned by the detail endpoint. Payment requires `paymentReference`, shipping requires `carrier` and `trackingCode`, cancellation requires `reason`, and a refund requires both `refundReference` and `reason`. Each update records its UTC event time and replaces the revision atomically. A stale revision returns a conflict so concurrent changes are never overwritten silently.
