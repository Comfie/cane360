# Repository Guidelines

## Project Structure & Module Organization

Cane360 is a .NET 10 modular monolith organized by dependency direction. `src/Domain` contains domain primitives and rules; `src/Application` contains use cases, behaviors, and interfaces; `src/Infrastructure` implements EF Core, PostgreSQL, and Identity; and `src/Web` hosts MVC controllers, OpenAPI, and the application entry point. The retained React 19/Vite client lives in `src/Web/ClientApp`. NUnit projects mirror production areas under `tests/Application.UnitTests` and `tests/Web.UnitTests`. Product and architecture references belong in `docs/`.

Keep dependencies inward-facing: Domain must not depend on other projects, Application depends on Domain, and Infrastructure/Web supply implementations at the boundary.

## Build, Test, and Development Commands

- `dotnet tool restore && dotnet restore Cane360.slnx` restores EF tooling and NuGet packages.
- `dotnet build Cane360.slnx` compiles the full solution; warnings fail the build.
- `dotnet test Cane360.slnx --no-build` runs all NUnit tests after a successful build.
- `dotnet run --project src/Web` starts the API; it does not apply pending migrations.
- From `src/Web/ClientApp`, run `npm install`, `npm start`, `npm run lint`, or `npm run build` to install, serve, lint, or bundle the client.
- `npm run generate-api` refreshes `src/web-api-client.ts` after API contract changes.

## Coding Style & Naming Conventions

Follow `.editorconfig`: four spaces for C#, two for JavaScript/JSON/XML, LF endings, and final newlines. Use file-scoped namespaces, braces, explicit types unless inference is obvious, and sorted `System` usings. Name types, methods, and properties in PascalCase; interfaces `IPascalCase`; locals and parameters camelCase; private fields `_camelCase`. ESLint governs JSX. Centralize NuGet versions in `Directory.Packages.props`; do not add versions to individual project files.

## Testing Guidelines

Use NUnit `[Test]` methods, Moq for collaborators, and Shouldly assertions. Name test classes `{Subject}Tests` and tests by observable behavior, for example `LoginReturnsUnauthorizedForInvalidCredentials`. Add focused tests beside the closest matching test namespace. Coverlet is configured, but no minimum coverage threshold is enforced.

## Commit & Pull Request Guidelines

Recent history uses concise Conventional Commit prefixes such as `feat:`, `refactor:`, `docs:`, and `chore:`. Keep commits scoped and imperative. Pull requests should explain intent and risks, link relevant issues, list verification commands, and include screenshots for client-visible changes. Call out migrations or API contract changes explicitly.

## Security & Configuration

Never commit new credentials. Override `ConnectionStrings__Cane360Db` or `VITE_API_URL` through environment variables. Database startup does not migrate automatically; inspect the target and pending migrations before running EF update commands.

Add durable engineering instructions for Cane360:

- Follow the SDK and package versions already pinned by the repository.
- The application uses ASP.NET Core, React with TypeScript, EF Core and PostgreSQL.
- Preserve Clean Architecture dependency direction.
- Domain contains business concepts and invariants.
- Application contains use cases, commands, queries and validation.
- Infrastructure contains EF Core, Identity and external integrations.
- Web contains endpoints, composition and the React application.
- Business rules must not be placed in React components or endpoint handlers.
- Use vertical slices and implement one working capability at a time.
- Do not scaffold the complete logical data model at once.
- Do not add microservices, GraphQL, Redis, message brokers or Kubernetes.
- Do not add mobile, PWA, offline sync, AI or IoT.
- Docker and Podman are unavailable.
- Do not run AppHost or start containers.
- PostgreSQL is hosted on Railway.
- Local development uses Railway public TCP access.
- Never put database credentials in source files, documentation, logs, tests,
  AGENTS.md or prompts.
- Never print complete connection strings.
- Do not run destructive tests against the development database.
- Integration tests require a separate test database.
- Before creating or applying migrations, show the target environment and list
  pending migrations.
- Never use EnsureDeleted against Railway.
- Preserve the existing authentication mechanism.
- Ask before adding production dependencies.
- Run relevant backend tests, frontend linting, type checking and production
  build after changes.
- Review git diff and avoid unrelated formatting or dependency churn.
