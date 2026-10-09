# MyShop

MyShop is an e-commerce application with an ASP.NET Core catalog API and a modular backend. An Angular interface provides the first local catalog management screens.

## Webshop version

The webshop version is stored centrally in `version.json`. The administration
sidebar displays this value at the bottom to signed-in administrators. It is not displayed in
the customer storefront or on the sign-in page.

Use `MAJOR.MINOR.PATCH`: increase PATCH for fixes, MINOR for new functionality,
and MAJOR for incompatible changes. Change the value before building a release;
the displayed version is embedded in the frontend build. The initial recorded
version is `0.1.0`. This webshop version is separate from dependency versions
and the frontend package's internal version.

## Local Development Requirements

Use the following versions for local development:

- Windows x64
- .NET SDK 10.0.400
- Node.js 24.19.0
- npm 11.19.0 (used for the frontend lockfile)
- Angular CLI 22.2.0 (local frontend dependency; no global CLI required)

The .NET SDK version is pinned in `global.json`, and the Node.js version is
recorded in `.nvmrc` for Node version managers that support it.

## Technology Stack

- Angular and TypeScript for the management frontend
- ASP.NET Core Web API and C# for the backend
- Entity Framework Core with SQL Server for database access
- Git and GitHub for source control

## Structure

- `frontend/`: Angular catalog management interface
- `backend/src/`: ASP.NET Core backend projects, separated into Api, Application, Domain, and Infrastructure
- `backend/tests/`: Automated backend tests

The backend contains catalog management, product variants, attributes, pricing, and persistence tests. See [Catalog browsing API](docs/catalog-browsing.md) for list endpoints, paging, filters, and response examples.

See [Catalog management queries](docs/catalog-management-queries.md) for individual attribute definitions and category/product type usage counts.

See [Database development](docs/database-development.md) for migrations, local database setup, and real SQL Server integration tests.

See [Product attribute validation](docs/product-attribute-validation.md) for missing required values and mismatches against current product type definitions.

See [Frontend setup and scope](frontend/README.md) to start the management interface and see which operations it supports.

See [Beveiligd beheer](docs/admin-security.md) for the account migration, initial administrator setup, login and password recovery.

See [Productpresentatie](docs/product-presentation.md) for descriptions, image links, draft/publication status and revision checks.

See [Klantwinkel](docs/storefront.md) for public browsing, publication rules and current scope.

See [Order management](docs/order-management.md) for the protected order overview and recorded order details.

See [Klantenbeheer](docs/customer-management.md) for searching, viewing, blocking and unblocking customer accounts.

See [Beheerdersdashboard](docs/admin-dashboard.md) for the protected management overview and operational counters.

See [Bezorgopties](docs/delivery-methods.md) for managed checkout choices and immutable order delivery snapshots.

See [Verlanglijst](docs/customer-wishlist.md) for customer product saving and availability behavior.
See [Klantmail](docs/email.md) for email confirmation, customer password recovery, order confirmations and SMTP configuration.
