# Backend Template Cleanup Design

## Purpose

Turn the generated Clean Architecture starter into a clean Cane360 foundation. The result keeps the existing layered architecture, React/Vite client, ASP.NET Core Identity, OpenAPI, and PostgreSQL support while removing Aspire, Minimal APIs, and demo features.

## Scope

### Keep

- `Domain`, `Application`, `Infrastructure`, and `Web` projects.
- The React/Vite `ClientApp`, including its layout, theme, home page, registration page, and login page.
- ASP.NET Core Identity, Identity roles, cookie authentication, authorization, and the existing administrator role/user seed.
- Clean Architecture base entities, application behaviors, exception mapping, current-user abstraction, OpenAPI, and Scalar.
- Unit and integration test projects that remain useful after demo removal.

### Remove

- Aspire `AppHost`, `ServiceDefaults`, and `TestAppHost` projects.
- The `Shared` project, whose service-name constants only support Aspire orchestration.
- Aspire packages, configuration, solution references, service discovery, telemetry defaults, resource health mapping, and Docker-dependent test setup.
- Minimal API endpoint groups, endpoint mapping extensions, and Minimal API-specific OpenAPI transformers.
- Todo, TodoList, TodoItem, Weather, Counter, Colour, and Priority sample code across Domain, Application, Infrastructure, Web, ClientApp, and tests.
- The Aspire-dependent browser acceptance-test project.

## Solution Architecture

The solution remains an ASP.NET Core 10 modular monolith with four production projects:

- `Domain` contains domain primitives, constants, entities, value objects, and events belonging to Cane360.
- `Application` contains use cases, behaviors, application exceptions, and abstractions.
- `Infrastructure` implements persistence and Identity using EF Core and Npgsql.
- `Web` is the composition root, hosts MVC controllers, exposes OpenAPI/Scalar, and serves the React application in production.

The React/Vite application remains under `src/Web/ClientApp`. It is not redesigned during this cleanup. It is only stripped of generated demo features and decoupled from Aspire-specific development environment variables.

## HTTP API

The API uses MVC controllers exclusively. `AddControllers()` registers controllers, and `MapControllers()` maps them. No route is registered with `MapGet`, `MapPost`, `MapGroup`, `MapIdentityApi`, or the template's endpoint-group abstraction.

### Users controller

`UsersController` preserves the route prefix `/api/Users` so the existing generated React client remains compatible. It provides:

| Method | Route | Authorization | Behavior |
| --- | --- | --- | --- |
| POST | `/api/Users/register` | Anonymous | Creates an Identity user from email and password. Returns success or a structured validation problem containing Identity errors. |
| POST | `/api/Users/login?useCookies=true` | Anonymous | Validates email/password and creates the existing Identity application cookie. Invalid credentials return `401`. |
| POST | `/api/Users/logout` | Authenticated | Signs out the current cookie session. |
| GET | `/api/Users/manage/info` | Authenticated | Returns the current user's email and email-confirmation state. |

Authentication continues to use ASP.NET Core Identity, `UserManager<ApplicationUser>`, `SignInManager<ApplicationUser>`, Identity roles, EF Core stores, and application cookies. The cleanup does not introduce JWTs, external providers, email delivery, password reset, refresh tokens, or two-factor management.

### Health controller

`HealthController` exposes `GET /api/Health`. It calls EF Core's database connectivity check. A reachable database returns `200` with a small status payload; an unavailable database returns `503` without exposing credentials or internal exception details.

### Error handling and API documentation

- `[ApiController]` supplies automatic request-model validation.
- The existing exception handler continues mapping application validation, not-found, unauthorized, and forbidden exceptions to Problem Details responses.
- Unhandled exceptions remain handled by ASP.NET Core's exception pipeline.
- Runtime OpenAPI and Scalar remain available.
- The Minimal API Identity operation transformer is removed because controller annotations provide operation metadata directly.

## PostgreSQL and Migrations

EF Core connects directly through `UseNpgsql`; no Aspire database registration or Npgsql enrichment is used.

The Railway PostgreSQL value is stored under `ConnectionStrings:Cane360Db` in `src/Web/appsettings.json`. Because Npgsql consumes its native key/value format, the supplied Railway URI is represented as the equivalent `Host`, `Port`, `Database`, `Username`, and `Password` settings. No source-controlled production-secret mechanism is introduced during this test phase.

The hostname `postgres.railway.internal` is treated as the selected deployment address. Local API startup requires a network-reachable override if that private hostname is unavailable outside Railway.

On every normal application startup:

1. Create a dependency-injection scope.
2. Call `Database.MigrateAsync()` to apply all pending migrations.
3. Seed the Administrator role and test administrator account when absent.
4. Complete startup and begin serving requests.

Startup migration or seed failure is logged and stops the process. The application must never delete or recreate an existing database. The cleanup creates an initial migration containing the retained Identity schema.

## React/Vite Cleanup

The ClientApp remains part of the Web project and remains publishable with the API.

- Remove Todo, Weather, and Counter components.
- Remove their routes, navigation links, styles, generated API dependencies, and sample wording.
- Retain Home, Layout, Theme, Login, Register, ProtectedRoute, and authentication context components.
- Keep the `/api/Users` authentication integration.
- Replace Aspire service-discovery environment variables in Vite configuration with `VITE_API_URL`, falling back to the locally configured Web URL.
- Give Vite a local default port so it can start without Aspire-provided `PORT` configuration.
- Remove obsolete Weather proxy entries.

## Testing and Verification

Testing must not require Aspire or Docker.

- Remove Todo/Weather/Counter-specific unit, functional, and acceptance tests.
- Remove the Aspire-dependent browser acceptance-test and TestAppHost projects from the solution.
- Retain focused Domain, Application, Infrastructure, and functional test projects where they still contain valid foundation tests.
- Add focused tests for controller authentication behavior where practical without an external database.
- Verify the entire `.NET` solution restores, builds, and runs its remaining automated tests.
- Verify the React application lints and builds after demo removal and API-client regeneration.
- Confirm with repository-wide searches that production code no longer references Aspire, Minimal API endpoint-group types, Todo, Weather, or Counter.

Database startup and health behavior cannot be fully exercised locally when the Railway private hostname is unreachable. Build/test verification must distinguish that environmental limitation from compilation or test failures.

## Documentation

Update the root README to describe:

- The backend-only development focus while retaining the React project.
- `dotnet run --project src/Web` as the API startup command.
- Automatic EF Core migrations at startup.
- Direct Railway PostgreSQL configuration.
- Scalar/OpenAPI URLs.
- Separate ClientApp installation and Vite startup commands.
- Docker and Aspire are not required.

## Success Criteria

- The solution has no Aspire production or test project references.
- The Web API registers and maps MVC controllers only.
- Identity registration, cookie login, logout, and current-user information are controller actions under `/api/Users`.
- The configured Railway PostgreSQL database receives pending migrations automatically during normal startup.
- Startup never calls `EnsureDeleted` or `EnsureCreated`.
- Todo, Weather, and Counter demo features are absent from backend, frontend, and tests.
- The React/Vite project remains present and buildable.
- OpenAPI, Scalar, Problem Details, Identity, authorization, and administrator seeding remain operational.
- Remaining tests and builds require neither Docker nor Aspire.
