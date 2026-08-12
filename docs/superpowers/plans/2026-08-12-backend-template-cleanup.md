# Backend Template Cleanup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the generated Aspire/Minimal API demo foundation with a controller-based Cane360 API that connects directly to Railway PostgreSQL, migrates at startup, preserves Identity, and keeps a cleaned React/Vite client.

**Architecture:** Keep the Domain, Application, Infrastructure, and Web Clean Architecture projects. Web uses MVC controllers for Identity and health operations; Infrastructure owns EF Core/Npgsql, migrations, and Identity persistence. React remains hosted by Web but is decoupled from Aspire and stripped of Todo/Weather/Counter demos.

**Tech Stack:** .NET 10, ASP.NET Core MVC, ASP.NET Core Identity cookies, EF Core 10, Npgsql/PostgreSQL, OpenAPI/Scalar, NUnit/Moq/Shouldly, React 19, Vite 8.

## Global Constraints

- Do not use Aspire, Docker, Minimal APIs, endpoint groups, `MapIdentityApi`, `EnsureDeleted`, or `EnsureCreated`.
- Keep React/Vite under `src/Web/ClientApp`; remove only generated demo features and Aspire coupling.
- Preserve `/api/Users/register`, `/api/Users/login?useCookies=true`, `/api/Users/logout`, and `/api/Users/manage/info` for the React authentication client.
- Apply EF Core migrations automatically on every normal API startup, then seed the Administrator role and test administrator account.
- Store the approved test Railway PostgreSQL connection in `src/Web/appsettings.json`.
- Tests and builds must not require Aspire or Docker.

---

### Task 1: Establish a recoverable baseline and remove generated orchestration/demo code

**Files:**
- Modify: `Cane360.slnx`
- Modify: `Directory.Packages.props`
- Modify: `src/Application/Common/Interfaces/IApplicationDbContext.cs`
- Modify: `src/Infrastructure/Data/ApplicationDbContext.cs`
- Modify: `src/Infrastructure/Data/ApplicationDbContextInitialiser.cs`
- Modify: `src/Infrastructure/DependencyInjection.cs`
- Modify: `src/Infrastructure/Infrastructure.csproj`
- Modify: `src/Infrastructure/Infrastructure.csproj`
- Modify: `src/Web/DependencyInjection.cs`
- Modify: `src/Web/GlobalUsings.cs`
- Modify: `src/Web/Program.cs`
- Modify: `src/Web/Web.csproj`
- Delete: `src/AppHost/**`
- Delete: `src/ServiceDefaults/**`
- Delete: `src/Shared/**`
- Delete: `src/Domain/Entities/TodoItem.cs`
- Delete: `src/Domain/Entities/TodoList.cs`
- Delete: `src/Domain/Enums/PriorityLevel.cs`
- Delete: `src/Domain/Events/TodoItemCompletedEvent.cs`
- Delete: `src/Domain/Exceptions/UnsupportedColourException.cs`
- Delete: `src/Domain/ValueObjects/Colour.cs`
- Delete: `src/Application/TodoItems/**`
- Delete: `src/Application/TodoLists/**`
- Delete: `src/Application/WeatherForecasts/**`
- Delete: `src/Infrastructure/Data/Configurations/TodoItemConfiguration.cs`
- Delete: `src/Infrastructure/Data/Configurations/TodoListConfiguration.cs`
- Delete: `src/Web/Endpoints/**`
- Delete: `src/Web/Infrastructure/EndpointRouteBuilderExtensions.cs`
- Delete: `src/Web/Infrastructure/IEndpointGroup.cs`
- Delete: `src/Web/Infrastructure/IdentityApiOperationTransformer.cs`
- Delete: `src/Web/Infrastructure/MethodInfoExtensions.cs`
- Delete: `src/Web/Infrastructure/WebApplicationExtensions.cs`
- Delete: `tests/TestAppHost/**`
- Delete: `tests/Web.AcceptanceTests/**`
- Delete: `tests/Application.FunctionalTests/**`
- Delete: `tests/Domain.UnitTests/ValueObjects/ColourTests.cs`
- Delete: `tests/Application.UnitTests/Common/Mappings/MappingTests.cs`
- Modify: `tests/Application.UnitTests/Common/Behaviours/RequestLoggerTests.cs`

**Interfaces:**
- Consumes: Existing Clean Architecture base classes, MediatR behaviors, Identity services, exception handling, and current-user abstraction.
- Produces: A buildable four-project production solution using MVC registration/mapping with no demo domain model or Aspire references.

- [ ] **Step 1: Commit the untracked starter as a recoverable baseline**

Run:

```bash
git add .aspire .devcontainer .editorconfig .gitignore Cane360.slnx Directory.Build.props Directory.Packages.props README.md global.json docs/product docs/architecture src tests
git commit -m "chore: capture generated project baseline"
```

Expected: the generated starter is recoverable before authorized deletions; `.idea`, `.DS_Store`, and build artifacts remain unstaged.

- [ ] **Step 2: Record the failing structural checks**

Run:

```bash
rg -n "Aspire|AddServiceDefaults|MapIdentityApi|IEndpointGroup|Todo|WeatherForecast|Counter" Cane360.slnx Directory.Packages.props src tests
```

Expected: matches demonstrate all generated dependencies that this task must remove.

- [ ] **Step 3: Remove orchestration and sample files, then simplify project topology**

Update `Cane360.slnx` to contain only:

```xml
<Solution>
  <Folder Name="/Solution Items/">
    <File Path=".editorconfig" />
    <File Path=".gitignore" />
    <File Path="Directory.Build.props" />
    <File Path="Directory.Packages.props" />
    <File Path="global.json" />
    <File Path="README.md" />
  </Folder>
  <Folder Name="/src/">
    <Project Path="src/Application/Application.csproj" />
    <Project Path="src/Domain/Domain.csproj" />
    <Project Path="src/Infrastructure/Infrastructure.csproj" />
    <Project Path="src/Web/Web.csproj" DefaultStartup="true" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/Application.UnitTests/Application.UnitTests.csproj" />
    <Project Path="tests/Domain.UnitTests/Domain.UnitTests.csproj" />
    <Project Path="tests/Infrastructure.IntegrationTests/Infrastructure.IntegrationTests.csproj" />
  </Folder>
</Solution>
```

Remove all Aspire package versions and unused acceptance-test packages from `Directory.Packages.props`. Replace `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` with the already-present `Npgsql.EntityFrameworkCore.PostgreSQL` package in Infrastructure. Remove Web's ServiceDefaults project reference.

Reduce `IApplicationDbContext` to its stable persistence contract:

```csharp
namespace Cane360.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
```

Remove Todo `DbSet` properties and configurations from `ApplicationDbContext`. Rewrite `RequestLoggerTests` around a test-local `TestRequest : IRequest` so behavior coverage no longer depends on Todo commands.

- [ ] **Step 4: Register MVC and remove all Minimal API mapping**

In Web dependency injection, replace endpoint explorer setup and Minimal API Identity transformer registration with:

```csharp
builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
    options.AddOperationTransformer<ApiExceptionOperationTransformer>());
```

In `Program.cs`, remove service defaults and endpoint-group calls, then map controllers:

```csharp
builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors(static policy => policy
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowAnyOrigin());
app.UseFileServer();
app.MapOpenApi();
app.MapScalarApiReference();
app.UseExceptionHandler(options => { });
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");
app.Run();
```

Keep database initialization out of `Program.cs` until Task 3 introduces safe migrations.

- [ ] **Step 5: Verify the cleaned foundation**

Run:

```bash
dotnet build Cane360.slnx --no-restore
dotnet test Cane360.slnx --no-build --no-restore
rg -n "Aspire|AddServiceDefaults|MapIdentityApi|IEndpointGroup|Todo|WeatherForecast|Counter" Cane360.slnx Directory.Packages.props src tests -g '!src/Web/ClientApp/**'
```

Expected: build and remaining tests pass; repository search returns no backend matches.

- [ ] **Step 6: Commit the foundation cleanup**

```bash
git add Cane360.slnx Directory.Packages.props src tests
git commit -m "refactor: remove Aspire and generated API demos"
```

### Task 2: Add controller-based Identity endpoints with tests

**Files:**
- Create: `src/Web/Controllers/UsersController.cs`
- Create: `src/Web/Models/Auth/RegisterRequest.cs`
- Create: `src/Web/Models/Auth/LoginRequest.cs`
- Create: `src/Web/Models/Auth/UserInfoResponse.cs`
- Create: `tests/Web.UnitTests/Web.UnitTests.csproj`
- Create: `tests/Web.UnitTests/GlobalUsings.cs`
- Create: `tests/Web.UnitTests/Controllers/UsersControllerTests.cs`
- Create: `tests/Web.UnitTests/Infrastructure/IdentityManagerMocks.cs`
- Modify: `Cane360.slnx`

**Interfaces:**
- Consumes: `UserManager<ApplicationUser>`, `SignInManager<ApplicationUser>`, Identity application cookies, and MVC `ProblemDetails` responses.
- Produces: `UsersController.Register`, `UsersController.Login`, `UsersController.Logout`, and `UsersController.Info` actions under `/api/Users` with React-compatible request fields and routes.

- [ ] **Step 1: Read the good-test rules before changing tests**

Read `superpowers:test-driven-development/writing-good-tests.md` completely. Name the production change that makes each test fail: controller removal, route/action behavior change, or an incorrect Identity result mapping.

- [ ] **Step 2: Create the Web unit-test project and write failing controller tests**

Reference Web and use NUnit, Moq, and Shouldly. Cover one behavior per test:

```csharp
[Test]
public async Task RegisterReturnsBadRequestWhenIdentityRejectsUser()
{
    _userManager
        .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), "weak"))
        .ReturnsAsync(IdentityResult.Failed(new IdentityError
        {
            Code = "PasswordTooShort",
            Description = "Password is too short."
        }));

    var result = await _controller.Register(new RegisterRequest("user@example.com", "weak"));

    result.ShouldBeOfType<BadRequestObjectResult>();
}

[Test]
public async Task LoginReturnsUnauthorizedForInvalidCredentials()
{
    _signInManager
        .Setup(x => x.PasswordSignInAsync("user@example.com", "wrong", true, true))
        .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

    var result = await _controller.Login(
        useCookies: true,
        useSessionCookies: null,
        new LoginRequest("user@example.com", "wrong"));

    result.ShouldBeOfType<UnauthorizedResult>();
}

[Test]
public async Task LogoutSignsOutAndReturnsOk()
{
    var result = await _controller.Logout();

    _signInManager.Verify(x => x.SignOutAsync(), Times.Once);
    result.ShouldBeOfType<OkResult>();
}
```

Add success coverage for registration and current-user information, using real request/response types and only mocking Identity managers at their unavoidable external boundary.

- [ ] **Step 3: Run the tests to verify RED**

Run:

```bash
dotnet restore tests/Web.UnitTests/Web.UnitTests.csproj
dotnet test tests/Web.UnitTests/Web.UnitTests.csproj --no-restore
```

Expected: FAIL because `UsersController` and auth models do not exist.

- [ ] **Step 4: Implement the minimal controller and DTOs**

Use records with validation attributes:

```csharp
public sealed record RegisterRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password);

public sealed record LoginRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password);

public sealed record UserInfoResponse(string Email, bool IsEmailConfirmed);
```

Implement `[ApiController]`, `[Route("api/[controller]")]`, `[AllowAnonymous]`, and `[Authorize]` explicitly. Preserve both generated Identity query parameters so the existing React call remains source-compatible. `Login` calls:

```csharp
var isPersistent = useCookies && useSessionCookies != true;
var result = await signInManager.PasswordSignInAsync(
    request.Email,
    request.Password,
    isPersistent,
    lockoutOnFailure: true);
```

Return `Ok()` on success and `Unauthorized()` otherwise. `Register` groups Identity errors into a `ValidationProblemDetails`; `Info` uses `UserManager.GetUserAsync(User)` and returns `Unauthorized()` if no current Identity user exists.

- [ ] **Step 5: Verify GREEN and controller metadata**

Run:

```bash
dotnet test tests/Web.UnitTests/Web.UnitTests.csproj
dotnet build Cane360.slnx --no-restore
```

Expected: all controller tests and solution build pass.

- [ ] **Step 6: Commit the controller API**

```bash
git add Cane360.slnx src/Web/Controllers src/Web/Models tests/Web.UnitTests
git commit -m "feat: expose Identity through MVC controllers"
```

### Task 3: Configure Railway PostgreSQL, safe startup migrations, and controller health

**Files:**
- Modify: `src/Web/appsettings.json`
- Modify: `src/Web/Program.cs`
- Modify: `src/Infrastructure/DependencyInjection.cs`
- Modify: `src/Infrastructure/Data/ApplicationDbContextInitialiser.cs`
- Create: `src/Infrastructure/Data/ApplicationDbContextFactory.cs`
- Create: `src/Infrastructure/Data/Migrations/**`
- Create: `src/Web/Services/IDatabaseHealthCheck.cs`
- Create: `src/Web/Services/DatabaseHealthCheck.cs`
- Create: `src/Web/Controllers/HealthController.cs`
- Create: `tests/Web.UnitTests/Controllers/HealthControllerTests.cs`
- Modify: `src/Web/DependencyInjection.cs`

**Interfaces:**
- Consumes: `ConnectionStrings:Cane360Db`, `ApplicationDbContext`, EF Core migrations, and Identity role/user managers.
- Produces: `InitialiseDatabaseAsync(WebApplication)`, `IDatabaseHealthCheck.CanConnectAsync(CancellationToken)`, and `GET /api/Health` returning `200` or `503`.

- [ ] **Step 1: Write failing health-controller tests**

```csharp
[TestCase(true, typeof(OkObjectResult))]
[TestCase(false, typeof(ObjectResult))]
public async Task GetReflectsDatabaseAvailability(bool canConnect, Type resultType)
{
    _healthCheck
        .Setup(x => x.CanConnectAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(canConnect);

    var result = await _controller.Get(CancellationToken.None);

    result.ShouldBeOfType(resultType);
    if (!canConnect) ((ObjectResult)result).StatusCode.ShouldBe(503);
}
```

- [ ] **Step 2: Run the tests to verify RED**

Run:

```bash
dotnet test tests/Web.UnitTests/Web.UnitTests.csproj --no-restore
```

Expected: FAIL because the health controller and service contract do not exist.

- [ ] **Step 3: Configure direct Npgsql and implement safe initialization**

Set `ConnectionStrings:Cane360Db` to:

```json
"Host=postgres.railway.internal;Port=5432;Database=railway;Username=postgres;Password=yNqwbPUhvZmucFGTqNjFrqamsbIpHNwM"
```

Infrastructure registration uses only:

```csharp
var connectionString = builder.Configuration.GetConnectionString("Cane360Db");
Guard.Against.NullOrWhiteSpace(connectionString);

builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
    options.UseNpgsql(connectionString);
});
```

Rewrite initialization to call `await _context.Database.MigrateAsync()` and retain only role/admin seeding. Remove all destructive database operations and Todo seeding. Add `Microsoft.EntityFrameworkCore.Design` to Infrastructure with private assets, then add a design-time factory with `Host=localhost;Port=5432;Database=Cane360Design;Username=postgres;Password=postgres` so `dotnet ef` can build the model without connecting.

- [ ] **Step 4: Implement the health abstraction and controller**

```csharp
public interface IDatabaseHealthCheck
{
    Task<bool> CanConnectAsync(CancellationToken cancellationToken);
}

public sealed class DatabaseHealthCheck(ApplicationDbContext context) : IDatabaseHealthCheck
{
    public Task<bool> CanConnectAsync(CancellationToken cancellationToken) =>
        context.Database.CanConnectAsync(cancellationToken);
}
```

`HealthController.Get` returns `Ok(new { status = "healthy" })` when true and `StatusCode(503, new { status = "unhealthy" })` when false. Register the service as scoped.

- [ ] **Step 5: Verify GREEN**

Run:

```bash
dotnet test tests/Web.UnitTests/Web.UnitTests.csproj
```

Expected: health and auth controller tests pass.

- [ ] **Step 6: Generate and inspect the initial Identity migration**

Run:

```bash
dotnet ef migrations add InitialIdentitySchema --project src/Infrastructure --startup-project src/Web --output-dir Data/Migrations
rg -n "AspNetUsers|AspNetRoles|Todo|Weather" src/Infrastructure/Data/Migrations
```

Expected: migration contains Identity tables and no Todo/Weather schema.

- [ ] **Step 7: Enable automatic startup migration**

Immediately after `builder.Build()` and before request-pipeline mapping, add:

```csharp
await app.InitialiseDatabaseAsync();
```

Run:

```bash
rg -n "MigrateAsync|EnsureDeleted|EnsureCreated|InitialiseDatabaseAsync" src
dotnet build Cane360.slnx --no-restore
```

Expected: `MigrateAsync` and startup initialization are present; destructive initialization is absent; build passes. Do not attempt local API startup against `postgres.railway.internal` unless that hostname is reachable.

- [ ] **Step 8: Commit persistence and health**

```bash
git add src/Infrastructure src/Web/appsettings.json src/Web/Program.cs src/Web/DependencyInjection.cs src/Web/Controllers/HealthController.cs src/Web/Services tests/Web.UnitTests/Controllers/HealthControllerTests.cs
git commit -m "feat: add direct PostgreSQL startup migration"
```

### Task 4: Clean and decouple the retained React/Vite client

**Files:**
- Delete: `src/Web/ClientApp/src/components/Todo.jsx`
- Delete: `src/Web/ClientApp/src/components/Weather.jsx`
- Delete: `src/Web/ClientApp/src/components/Counter.jsx`
- Modify: `src/Web/ClientApp/src/AppRoutes.jsx`
- Modify: `src/Web/ClientApp/src/components/NavMenu.jsx`
- Modify: `src/Web/ClientApp/src/components/Home.jsx`
- Modify: `src/Web/ClientApp/src/styles.scss`
- Modify: `src/Web/ClientApp/vite.config.ts`
- Modify: `src/Web/ClientApp/README.md`
- Generate: `src/Web/ClientApp/src/web-api-client.ts`

**Interfaces:**
- Consumes: controller OpenAPI document and `/api/Users` cookie endpoints.
- Produces: a buildable React shell with Home/Login/Register routes and a generated `UsersClient` compatible with `AuthContext.jsx`.

- [ ] **Step 1: Record failing structural checks**

Run:

```bash
rg -n "Todo|Tasks|Weather|Counter|services__webapi|weatherforecast" src/Web/ClientApp
```

Expected: matches identify demo UI and Aspire coupling.

- [ ] **Step 2: Remove demo UI and simplify navigation**

`AppRoutes.jsx` must contain only Home, Login, and Register routes. `NavMenu.jsx` must show the Cane360 brand, Home, authentication actions, and theme toggle. Rewrite Home copy to describe Cane360 without starter-template instructions. Remove Todo-only style rules while retaining generic authentication, navigation, theme, dialog, and button rules.

- [ ] **Step 3: Replace Aspire Vite configuration**

Use:

```ts
const target = process.env.VITE_API_URL || 'https://localhost:7001';

export default defineConfig({
  plugins: [react()],
  server: {
    port: Number(process.env.PORT) || 5173,
    proxy: {
      '/api': { target, secure: false, changeOrigin: true },
      '/openapi': { target, secure: false, changeOrigin: true },
      '/scalar': { target, secure: false, changeOrigin: true },
    },
  },
  build: { outDir: 'build' },
});
```

- [ ] **Step 4: Generate the controller client and verify React**

Run:

```bash
dotnet build src/Web/Web.csproj --no-restore
npm run generate-api
npm run lint
npm run build
```

Working directory for npm commands: `src/Web/ClientApp`.

Expected: `web-api-client.ts` contains `UsersClient` methods for register, login, logout, and info; lint and production build pass.

- [ ] **Step 5: Confirm demo removal and commit**

Run:

```bash
rg -n "Todo|Tasks|Weather|Counter|services__webapi|weatherforecast" src/Web/ClientApp -g '!package-lock.json'
```

Expected: no matches.

```bash
git add src/Web/ClientApp
git commit -m "refactor: clean retained React client"
```

### Task 5: Document and verify the complete foundation

**Files:**
- Modify: `README.md`
- Modify: `src/Web/Web.http`

**Interfaces:**
- Consumes: completed controller routes, Railway configuration, migration lifecycle, and Vite scripts.
- Produces: accurate local/deployment instructions and a final evidence-backed verification report.

- [ ] **Step 1: Rewrite run and development documentation**

Document these exact commands:

```bash
dotnet restore Cane360.slnx
dotnet run --project src/Web
cd src/Web/ClientApp
npm install
npm start
```

State that startup applies migrations, Railway's private hostname may not resolve locally, Scalar is `/scalar`, OpenAPI is `/openapi/v1.json`, and neither Aspire nor Docker is required. Replace `Web.http` samples with health and controller-auth requests.

- [ ] **Step 2: Run complete verification**

Run:

```bash
dotnet restore Cane360.slnx
dotnet build Cane360.slnx --no-restore
dotnet test Cane360.slnx --no-build --no-restore
rg -n "Aspire|AddServiceDefaults|MapIdentityApi|IEndpointGroup|EnsureDeleted|EnsureCreated|Todo|WeatherForecast|Counter" Cane360.slnx Directory.Packages.props src tests -g '!src/Web/ClientApp/package-lock.json'
npm run lint
npm run build
git status --short
```

Working directory for the final two npm commands: `src/Web/ClientApp`.

Expected: restore/build/tests/lint/frontend build pass; repository search returns no forbidden production references; Git shows only intentional changes or is clean after the final commit.

- [ ] **Step 3: Commit documentation**

```bash
git add README.md src/Web/Web.http
git commit -m "docs: update controller API setup"
```

- [ ] **Step 4: Review the final diff and report the Railway limitation**

Run:

```bash
git log --oneline --decorate -8
git status --short
git diff HEAD~5..HEAD --stat
```

Report all verification commands and their outcomes. If local runtime connection is impossible because `postgres.railway.internal` is private, state that explicitly without treating successful build/test evidence as a successful Railway connectivity test.
