# Database development

Run commands from the repository root. The backend uses SQL Server and EF Core 10.0.12.
The initial migration captures the existing persistence model; it does not change domain or repository behavior.

## Restore the migration tool

The root `dotnet-tools.json` pins the local EF tool to the backend's EF Core version.

```powershell
dotnet tool restore
dotnet build backend/MyShop.sln
```

The design-time factory in Infrastructure lets EF inspect the model without starting the API.
Its default connection names `MyShopDesignTime` on LocalDB; constructing the context and generating SQL do not open that connection.
Always provide `--connection` when applying migrations so the intended database is explicit.

## Create a new development database

Install SQL Server LocalDB on Windows, or use a development SQL Server instance.
The following example creates or updates a dedicated local development database:

```powershell
$developmentConnection = 'Server=(localdb)\MSSQLLocalDB;Database=MyShopDevelopment;Integrated Security=True;TrustServerCertificate=True'
dotnet ef database update --project backend/src/MyShop.Infrastructure --startup-project backend/src/MyShop.Infrastructure --connection $developmentConnection
$env:ConnectionStrings__MyShop = $developmentConnection
dotnet run --project backend/src/MyShop.Api
```

The API reads `ConnectionStrings:MyShop`; it does not automatically migrate the database.
Use your own server and authentication settings when LocalDB is unavailable. Keep credentials out of tracked files.

The initial migration is intended for an empty database. An existing database created without migration history needs a separately reviewed baseline plan; do not apply the initial migration blindly to existing catalog tables.
An idempotent script uses migration history to decide what to apply, not a comparison of existing table shapes.

## Change the schema

After deliberately updating the persistence mappings, create and review a migration:

```powershell
dotnet ef migrations add DescriptiveChangeName --project backend/src/MyShop.Infrastructure --startup-project backend/src/MyShop.Infrastructure --output-dir Persistence/Migrations
dotnet ef migrations has-pending-model-changes --project backend/src/MyShop.Infrastructure --startup-project backend/src/MyShop.Infrastructure
dotnet ef migrations script --idempotent --project backend/src/MyShop.Infrastructure --startup-project backend/src/MyShop.Infrastructure
```

Review both directions of the migration, generated SQL, and any data conversion or deletion before applying it.
Commit the migration, its designer file, and the model snapshot together.
The offline migration tests verify that the snapshot matches the model and that SQL can be generated.

## Run real SQL Server integration tests

The integration tests are opt-in. Without `MYSHOP_TEST_SQLSERVER`, only those tests are reported as skipped; the other backend tests still run.
For a full validation including real SQL Server:

```powershell
$env:MYSHOP_TEST_SQLSERVER = 'Server=(localdb)\MSSQLLocalDB;Integrated Security=True;TrustServerCertificate=True'
dotnet build backend/MyShop.sln -m:1 --disable-build-servers
dotnet test backend/MyShop.sln -m:1 --disable-build-servers
git diff --check
```

The test account needs permission to create and drop databases on that development instance.
Each fixture replaces the supplied database name with a fresh `MyShopTests_<random GUID>` database, applies migrations, and deletes only that database on completion.
Tests use separate contexts to verify persisted results. The migration rollback test uses its own additional database.
The fixture rejects attached database files; supply a server connection instead.
Tests cover migration application and rollback, category hierarchy constraints, product type attributes, full product graphs, tracked removals, ordered choices, concurrency rollback, cascading deletes, paging, and unique SKUs.

A forcibly terminated test process can leave its temporary database behind. Identify the exact database owned by that interrupted run before removing it; do not delete all databases matching the prefix.
To return to tests without a live database in the current PowerShell session:

```powershell
Remove-Item Env:MYSHOP_TEST_SQLSERVER
```

See Microsoft's [design-time context documentation](https://learn.microsoft.com/ef/core/miscellaneous/cli/dbcontext-creation) and [migration application guidance](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying?tabs=dotnet-core-cli) for the underlying tooling.
