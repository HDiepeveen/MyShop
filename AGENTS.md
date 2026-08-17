# MyShop Project Instructions

## Project Context

MyShop is an e-commerce application. Its frontend will use Angular and TypeScript. Its backend will use ASP.NET Core Web API and C#. SQL Server and Entity Framework Core will provide database access.

## Architecture

- Start as a modular monolith.
- Separate the backend into `Api`, `Application`, `Domain`, and `Infrastructure` projects.
- Organize the Angular application by feature and use standalone components.
- Use DTOs for API contracts; do not expose Entity Framework Core entities directly.
- Keep controllers thin. Do not place business logic in controllers.
- Use dependency injection.
- Use `async`/`await` for asynchronous APIs.
- Do not introduce architectural patterns without first explaining why they are needed.

## Delivery Practices

- Include appropriate automated tests with new functionality.
- Prefer small, focused changes and do not change unrelated files.
- Never commit secrets, passwords, API keys, or connection strings containing credentials.
- Before making large changes, explain the plan.
- Run appropriate tests and validation after code changes.
- Keep the repository in a clean Git state after completing a task.
