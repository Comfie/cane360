# Vercel and Railway deployment

Cane360 deploys as a Vercel-hosted React single-page application, a
Railway-hosted ASP.NET Core API, and the existing Railway PostgreSQL database.
The frontend proxies `/api/*` through Vercel to keep ASP.NET Core Identity
cookies on the frontend origin. This avoids cross-site cookie and permissive
CORS configuration.

## 1. Check the database before deployment

Normal API startup does not create or apply migrations. Before changing the
database, use the existing local secret or a temporary shell environment
variable to target the intended Railway environment, then run:

```bash
dotnet run --project src/Web -- --database-status
dotnet ef migrations list \
  --project src/Infrastructure \
  --startup-project src/Web
```

Review the reported environment, server, database, and pending migration count.
Do not apply a migration until those details identify the intended database.
Never run destructive tests or `EnsureDeleted` against Railway.

## 2. Deploy the API to Railway

Create an application service from the GitHub repository in the same Railway
project and environment as PostgreSQL. Keep the service root directory at the
repository root. Railway reads `railway.json` and builds the root `Dockerfile`.

Configure these service variables in Railway:

- `ConnectionStrings__Cane360Db`: an Npgsql keyword/value connection string
  assembled with Railway references to the PostgreSQL service's private host,
  port, database, username, and password variables. Do not use or expose the
  database's public TCP endpoint from the deployed API.
- `ASPNETCORE_FORWARDEDHEADERS_ENABLED`: `true`, so ASP.NET Core respects the
  HTTPS scheme forwarded by Railway's TLS-terminating proxy.

`ASPNETCORE_ENVIRONMENT` is already set to `Production` in the runtime image.
Railway injects `PORT`, and the application binds to it. Do not create a manual
`PORT` variable unless Railway support directs you to do so.

The deployment health check is `/api/Health`. It deliberately returns a failure
when the API cannot reach PostgreSQL, preventing Railway from activating an
unhealthy deployment. After the deployment succeeds, generate a public Railway
domain for the API and confirm:

```text
https://<railway-api-domain>/api/Health
```

Do not include `/api` when recording the API origin for Vercel.

## 3. Deploy the frontend to Vercel

Import the same GitHub repository as a Vercel project and configure:

- Root Directory: `src/Web/ClientApp`
- Framework Preset: Vite
- Environment Variable: `API_ORIGIN` set to the API's HTTPS Railway origin,
  without a trailing `/api` path

The checked-in `vercel.ts` selects the frontend-only build, publishes the
`build` directory, proxies `/api/*` to Railway, disables caching for API
responses, and falls back to `index.html` for React Router deep links.

`API_ORIGIN` is not a database secret; it is the public origin Vercel must proxy.
Do not add any database variable to Vercel. `VITE_API_URL` remains a local Vite
development-proxy setting and is not required in Vercel.

Deploy Railway first, then Vercel. If the Railway domain changes, update
`API_ORIGIN` and redeploy the Vercel project.

## 4. Verify the live deployment

Use the Vercel URL for browser verification:

1. Open `/login`, register or sign in, and confirm the dashboard loads.
2. Refresh a protected route and confirm the session remains authenticated.
3. Create or read a farm record to verify an authenticated API call.
4. Sign out and confirm a protected route returns to login.
5. Open a deep link such as `/farm` directly and confirm Vercel serves the SPA.

In browser developer tools, application requests should target the Vercel
origin under `/api/*`, not the Railway domain. Never log or share cookie values,
database variables, or complete connection strings while troubleshooting.

## Operational note

Keep the API at one replica for the initial deployment. The current application
does not persist ASP.NET Core Data Protection keys outside the container, so an
API redeployment can require users to sign in again. Persisting and sharing
those keys should be a separate hardening change before enabling multiple API
replicas; it may require either Railway volume configuration or an approved
production package.
