# Cane360

Cane360 is an ASP.NET Core 10 and React application for individual Zimbabwean sugarcane growers. The current vertical slices cover farm and field setup plus the Draft-to-Closed crop-cycle lifecycle, harvest results, and chronological field history.

## Architecture

- `src/Domain` — domain primitives and rules.
- `src/Application` — use cases, application behaviors, and interfaces.
- `src/Infrastructure` — Entity Framework Core, PostgreSQL, and ASP.NET Core Identity persistence.
- `src/Web` — MVC controllers, OpenAPI/Scalar, and the production host for the React client.
- `src/Web/ClientApp` — React 19, Vite, and the responsive Cane360 client.
- `tests/Application.UnitTests` and `tests/Web.UnitTests` — current automated foundation coverage.

The project does not use .NET Aspire or Docker. API endpoints use MVC controllers rather than Minimal APIs.

## Requirements

- .NET SDK 10.0.101 or a compatible .NET 10 feature band.
- Node.js and npm for ClientApp development.
- A Railway PostgreSQL database reachable through its public TCP endpoint.

Restore repository tools and packages:

```bash
dotnet tool restore
dotnet restore Cane360.slnx
```

## Database

The backend reads the Railway connection from the .NET user-secrets key
`ConnectionStrings:Cane360Db`. The connection string must not be added to React,
`appsettings.json`, source control, command output, or logs.

In Rider, right-click `src/Web/Web.csproj`, choose **Tools > .NET User Secrets**,
and ensure that key is present. Alternatively, set it without printing the value:

```bash
dotnet user-secrets set "ConnectionStrings:Cane360Db" "<Railway public connection string>" \
  --project src/Web
```

Check the configured target and pending migrations without changing the database:

```bash
dotnet run --project src/Web -- --database-status
```

This reports only the environment, provider, server, database name, and migration
counts. Normal startup does not apply migrations automatically. Before any future
migration is applied, review the target above and list migrations with:

```bash
dotnet ef migrations list --project src/Infrastructure --startup-project src/Web
```

Create a future migration with the repository-local EF 10 tool:

```bash
dotnet ef migrations add MigrationName \
  --project src/Infrastructure \
  --startup-project src/Web \
  --output-dir Data/Migrations
```

## Run the API

```bash
dotnet run --project src/Web --launch-profile https
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

Run both processes directly from Rider or separate terminals. Do not start the
AppHost or use Docker/Podman. Database settings remain backend-only.

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

## Farm and crop-cycle API

All farm endpoints require the existing Identity application cookie and derive the
grower tenant from the authenticated user; React never supplies a tenant ID.

- `GET /api/FarmSetup`
- `POST /api/FarmSetup/farm`
- `POST /api/FarmSetup/fields`
- `GET|POST /api/fields/{fieldId}/crop-cycles`
- `GET /api/fields/{fieldId}/crop-cycles/{cropCycleId}`
- `POST /api/fields/{fieldId}/crop-cycles/{cropCycleId}/transitions/{action}`
- `GET|POST /api/CropVarieties`

Supported transition actions are `activate`, `cancel`, `ready-for-harvest`,
`harvest`, and `close`. The `/farm` and `/fields` screens guide setup; cycle
overviews show the current status, permanent harvest result, and real lifecycle
events. Activities, labour, inputs, and costs remain explicitly unavailable until
their modules are implemented.

## Verify

```bash
dotnet build Cane360.slnx
dotnet test Cane360.slnx --no-build
cd src/Web/ClientApp
npm run lint
npm run typecheck
npm test
npm run build
```

## Deploy

The supported split deployment is:

- React/Vite on Vercel.
- ASP.NET Core API on Railway.
- PostgreSQL on Railway.

Vercel proxies `/api/*` to Railway so the Identity cookie remains same-origin in
the browser. See [docs/deployment.md](docs/deployment.md) for the required
Railway and Vercel settings, the safe migration gate, and post-deploy checks.
