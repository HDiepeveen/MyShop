# MyShop

MyShop is an e-commerce application with an ASP.NET Core catalog API and a modular backend. The Angular frontend is planned.

## Local Development Requirements

Use the following versions for local development:

- Windows x64
- .NET SDK 10.0.400
- Node.js 24.19.0
- npm 12.0.2
- Angular CLI 22.1.4

The .NET SDK version is pinned in `global.json`, and the Node.js version is
recorded in `.nvmrc` for Node version managers that support it.

## Planned Technology Stack

- Angular and TypeScript for the frontend
- ASP.NET Core Web API and C# for the backend
- Entity Framework Core with SQL Server for database access
- Git and GitHub for source control

## Planned Structure

- `frontend/`: Angular application
- `backend/src/`: ASP.NET Core backend projects, separated into Api, Application, Domain, and Infrastructure
- `backend/tests/`: Automated backend tests

The backend contains catalog management, product variants, attributes, pricing, and persistence tests. See [Catalog browsing API](docs/catalog-browsing.md) for list endpoints, paging, filters, and response examples.

See [Catalog management queries](docs/catalog-management-queries.md) for individual attribute definitions and category/product type usage counts.
