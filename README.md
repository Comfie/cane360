# Cane360

Cane360 is an ASP.NET Core 10 modular monolith for Zimbabwean sugarcane farm management. The current development focus is the backend API, while the React/Vite client remains in the repository for later product work.

## Architecture

- `src/Domain` — domain primitives and rules.
- `src/Application` — use cases, application behaviors, and interfaces.
- `src/Infrastructure` — Entity Framework Core, PostgreSQL, and ASP.NET Core Identity persistence.
- `src/Web` — MVC controllers, OpenAPI/Scalar, and the production host for the React client.
- `src/Web/ClientApp` — retained React 19 and Vite client shell.
- `tests/Application.UnitTests` and `tests/Web.UnitTests` — current automated foundation coverage.

The project does not use .NET Aspire or Docker. API endpoints use MVC controllers rather than Minimal APIs.

## Requirements

- .NET SDK 10.0.101 or a compatible .NET 10 feature band.
- Node.js and npm for ClientApp development.
- A PostgreSQL database reachable from the machine running the API.

Restore repository tools and packages:

```bash
dotnet tool restore
dotnet restore Cane360.slnx
```

## Database

`src/Web/appsettings.json` currently contains the approved test Railway PostgreSQL connection. Railway's `postgres.railway.internal` hostname is private to Railway's network and may not resolve when the API runs locally.

For local development, override the connection without editing application code:

```bash
ConnectionStrings__Cane360Db='Host=localhost;Port=5432;Database=Cane360;Username=postgres;Password=postgres' dotnet run --project src/Web
```

On every normal startup, Cane360:

1. Applies all pending EF Core migrations with `MigrateAsync()`.
2. Creates the Administrator role when absent.
3. Creates the test `administrator@localhost` account when absent.

A migration or seed failure stops startup. The application never deletes or recreates an existing database.

Create a future migration with the repository-local EF 10 tool:

```bash
dotnet ef migrations add MigrationName \
  --project src/Infrastructure \
  --startup-project src/Web \
  --output-dir Data/Migrations
```

## Run the API

```bash
dotnet run --project src/Web
```

Default development URLs:

- API: `https://localhost:7001`
- Scalar API reference: `https://localhost:7001/scalar`
- OpenAPI document: `https://localhost:7001/openapi/v1.json`
- Database health: `https://localhost:7001/api/Health`

## Run the React client

In a second terminal:

```bash
cd src/Web/ClientApp
npm install
npm start
```

Vite starts at `http://localhost:5173` and proxies API requests to `https://localhost:7001`. Set `VITE_API_URL` to use a different API address.

Generate the TypeScript API client after an API contract change:

```bash
cd src/Web/ClientApp
npm run generate-api
```

## Authentication API

ASP.NET Core Identity is exposed through `UsersController`:

- `POST /api/Users/register`
- `POST /api/Users/login?useCookies=true`
- `POST /api/Users/logout`
- `GET /api/Users/manage/info`

Login uses the Identity application cookie. Logout and account information require authentication.

## Verify

```bash
dotnet build Cane360.slnx
dotnet test Cane360.slnx --no-build
cd src/Web/ClientApp
npm run lint
npm run build
npm audit
```
