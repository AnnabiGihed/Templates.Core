# Pivot.Framework

A collection of production-ready .NET 10 NuGet packages that provide plug-and-play infrastructure for Clean Architecture applications. Covers domain primitives, application plumbing, persistence, caching, messaging, scheduling, and Keycloak authentication for ASP.NET Core, Blazor Server, and MAUI.

Published to GitHub Packages: `https://nuget.pkg.github.com/AnnabiGihed/index.json`

---

## Packages

| Package | Description |
|---|---|
| `Pivot.Framework.Domain` | DDD primitives — aggregates, domain events, value objects, auditing, soft delete |
| `Pivot.Framework.Application` | CQRS with MediatR, FluentValidation pipeline, AutoMapper, ICurrentUser |
| `Pivot.Framework.Infrastructure.Abstraction` | Outbox interfaces, repository contracts |
| `Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore` | EF Core unit of work, outbox repository, audit interceptors |
| `Pivot.Framework.Infrastructure.Messaging.EntityFrameworkCore` | Outbox processor — dispatches persisted domain events via MediatR |
| `Pivot.Framework.Infrastructure.Scheduling` | Hangfire integration for background jobs |
| `Pivot.Framework.Tools.DependencyInjection` | `IServiceInstaller` convention — auto-discover and install DI modules by assembly scan |
| `Pivot.Framework.Containers.API` | ASP.NET Core API base — `ApiController`, exception middleware, Serilog setup |
| `Pivot.Framework.Caching` | Redis token cache + revocation blacklist wired into JWT bearer pipeline |
| `Pivot.Framework.Authentication` | Core Keycloak models, `KeycloakOptions`, `ICurrentUser`, `IKeycloakAuthService` |
| `Pivot.Framework.Authentication.AspNetCore` | JWT bearer setup for ASP.NET Core APIs (`AddKeycloakBackend`) |
| `Pivot.Framework.Authentication.Blazor` | PKCE login flow + Redis session store for Blazor Server (`AddKeycloakBlazor`) |
| `Pivot.Framework.Authentication.Maui` | PKCE login flow via `WebAuthenticator` for .NET MAUI (`AddKeycloakMaui`) |

---

## Installation

Add the GitHub Packages feed to your `NuGet.config`:

```xml
<configuration>
  <packageSources>
    <add key="github" value="https://nuget.pkg.github.com/AnnabiGihed/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <github>
      <add key="Username" value="YOUR_GITHUB_USERNAME" />
      <add key="ClearTextPassword" value="YOUR_GITHUB_PAT" />
    </github>
  </packageSourceCredentials>
</configuration>
```

Then install the packages you need:

```bash
dotnet add package Pivot.Framework.Domain
dotnet add package Pivot.Framework.Application
dotnet add package Pivot.Framework.Authentication.Blazor
# etc.
```

---

## Package Details

### Pivot.Framework.Domain

Provides the DDD building blocks your domain layer inherits from.

```csharp
// Aggregate root with domain events
public class Order : AggregateRoot<OrderId>
{
    public void Ship()
    {
        // ...
        RaiseDomainEvent(new OrderShippedEvent(Id));
    }
}

// Value object
public class Money : ValueObject<Money>
{
    public decimal Amount { get; }
    public string Currency { get; }
}

// Result type (via CSharpFunctionalExtensions)
Result<Order> result = Order.Create(customerId, items);
if (result.IsFailure)
    return result.Error;
```

Key types: `AggregateRoot<TId>`, `Entity<TId>`, `ValueObject<T>`, `IDomainEvent`, `Result<T>`, `Error`, `IAuditableEntity`, `ISoftDeletableEntity`.

---

### Pivot.Framework.Application

Wires up the application layer with MediatR, FluentValidation, and AutoMapper.

```csharp
// Command
public sealed record CreateOrderCommand(Guid CustomerId) : ICommand<OrderId>;

// Handler
internal sealed class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, OrderId>
{
    public async Task<Result<OrderId>> Handle(CreateOrderCommand command, CancellationToken ct)
    {
        // ...
    }
}

// Query
public sealed record GetOrderQuery(Guid OrderId) : IQuery<OrderDto>;
```

Key types: `ICommand<T>`, `IQuery<T>`, `ICommandHandler<,>`, `IQueryHandler<,>`, `IDomainEventHandler<T>`.

---

### Pivot.Framework.Tools.DependencyInjection

Convention-based DI registration. Implement `IServiceInstaller` in each project and call `InstallServices` from the composition root.

```csharp
// Define an installer in your project
public class InfrastructureServiceInstaller : IServiceInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration, bool includeConventions = true)
    {
        services.AddScoped<IOrderRepository, OrderRepository>();
        // ...
    }
}

// Register all installers found in the given assemblies
services.InstallServices(configuration, includeConventionBasedRegistration: true,
    typeof(InfrastructureServiceInstaller).Assembly);
```

---

### Pivot.Framework.Containers.API

Base class and middleware for ASP.NET Core API projects.

```csharp
// Controller base
[ApiController]
public sealed class OrdersController : ApiController
{
    public OrdersController(ISender sender) : base(sender) { }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderCommand command, CancellationToken ct)
        => HandleResult(await Sender.Send(command, ct));
}
```

Includes `ExceptionHandlerMiddleware` that maps domain exceptions to RFC 7807 `ProblemDetails` responses, and Serilog setup.

---

### Pivot.Framework.Authentication.AspNetCore

JWT bearer authentication for ASP.NET Core APIs backed by Keycloak.

**appsettings.json:**
```json
{
  "Keycloak": {
    "BaseUrl": "https://auth.example.com",
    "Realm": "my-realm",
    "ClientId": "my-api",
    "Audience": "my-api",
    "RequireHttpsMetadata": true
  }
}
```

**Program.cs:**
```csharp
// Registers JWT bearer, ICurrentUser, and IHttpContextAccessor
builder.Services.AddKeycloakBackend(builder.Configuration);
```

**Usage in a controller or handler:**
```csharp
public class MyService(ICurrentUser currentUser)
{
    public void DoSomething()
    {
        Guid? userId   = currentUser.UserId;
        string? name   = currentUser.DisplayName;
        string? email  = currentUser.Email;
        bool isAdmin   = currentUser.IsInRole("admin");
    }
}
```

---

### Pivot.Framework.Caching

Redis-backed JWT token cache and revocation blacklist. Reduces repeated token parsing and enables immediate logout before token expiry.

**Program.cs:**
```csharp
// Registers Redis + JWT bearer + claims cache + revocation cache in one call
builder.Services.AddKeycloakRedisCache(builder.Configuration);
```

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  },
  "TokenRevocation": {
    "DefaultTtlDays": 30
  }
}
```

**Revoking a token on logout:**
```csharp
app.MapPost("/auth/logout", async (HttpContext ctx, ITokenRevocationCache revocation) =>
{
    var token = ctx.GetBearerToken();
    if (token is not null)
        await revocation.RevokeAsync(token);

    return Results.Ok();
}).RequireAuthorization();
```

---

### Pivot.Framework.Authentication.Blazor

Full PKCE login flow for Blazor Server. Tokens are stored server-side in Redis — the browser only receives an opaque `HttpOnly` session cookie. Compatible with `<AuthorizeView>` and `[Authorize]` out of the box.

**appsettings.json:**
```json
{
  "Keycloak": {
    "BaseUrl": "https://auth.example.com",
    "Realm": "my-realm",
    "ClientId": "my-blazor-app",
    "Audience": "my-blazor-app",
    "Scopes": "openid profile email offline_access",
    "RequireHttpsMetadata": true
  },
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

**Program.cs:**
```csharp
builder.Services.AddAuthentication();
builder.Services.AddStackExchangeRedisCache(o => o.Configuration = "localhost:6379");
builder.Services.AddKeycloakBlazor(builder.Configuration);
```

**Routes.razor** — wrap `RouteView` inside `CascadingAuthenticationState` (must be inside the interactive boundary):
```razor
<Router AppAssembly="typeof(Program).Assembly">
    <Found Context="routeData">
        <CascadingAuthenticationState>
            <RouteView RouteData="routeData" DefaultLayout="typeof(MainLayout)" />
            <FocusOnNavigate RouteData="routeData" Selector="h1" />
        </CascadingAuthenticationState>
    </Found>
</Router>
```

**MainLayout** — rehydrate the session on every circuit start:
```csharp
protected override async Task OnInitializedAsync()
{
    await Auth.InitialiseFromCookieAsync();
}
```

**Required pages** — create these three pages in your Blazor app:

`/auth/callback` — completes the PKCE exchange:
```razor
@page "/auth/callback"
@inject IBlazorKeycloakAuthService Auth
@inject NavigationManager Nav

@code {
    [SupplyParameterFromQuery(Name = "code")]  private string? Code  { get; set; }
    [SupplyParameterFromQuery(Name = "state")] private string? State { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (string.IsNullOrEmpty(Code) || string.IsNullOrEmpty(State))
        {
            Nav.NavigateTo("/", forceLoad: true);
            return;
        }
        var returnUrl = await Auth.HandleCallbackAsync(Code, State);
        Nav.NavigateTo(returnUrl ?? "/auth/login-failed", forceLoad: true);
    }
}
```

`/auth/logout` — revokes tokens and redirects to Keycloak end-session:
```razor
@page "/auth/logout"
@inject IBlazorKeycloakAuthService Auth

@code {
    protected override async Task OnInitializedAsync() => await Auth.LogoutAsync();
}
```

`/auth/login-failed` — shown when the callback fails (state mismatch, expired session, etc.).

**Login / logout button:**
```razor
@inject IBlazorKeycloakAuthService Auth

<AuthorizeView>
    <Authorized>
        <span>Hello, @context.User.Identity?.Name!</span>
        <button @onclick="() => Nav.NavigateTo('/auth/logout', forceLoad: true)">Logout</button>
    </Authorized>
    <NotAuthorized>
        <button @onclick="Login">Login</button>
    </NotAuthorized>
</AuthorizeView>

@code {
    async Task Login() => await Auth.LoginAsync();
}
```

Add your callback URL to Keycloak's **Valid Redirect URIs**: `https://your-app/auth/callback`

**Authenticated API calls** — inject a Bearer token automatically via `AddKeycloakHandler`:
```csharp
builder.Services.AddHttpClient("MyApi", c => c.BaseAddress = new Uri("https://api.example.com"))
    .AddKeycloakHandler();
```

---

### Pivot.Framework.Authentication.Maui

PKCE login flow for .NET MAUI Blazor Hybrid via the system browser (`WebAuthenticator`). Tokens are stored in OS secure storage.

**MauiProgram.cs:**
```csharp
builder.Services.AddKeycloakMaui(builder.Configuration);
builder.Services.AddHttpClient("MyApi", c => c.BaseAddress = new Uri("https://api.example.com"))
    .AddKeycloakHandler();
```

**Usage:**
```csharp
@inject IKeycloakAuthService Auth

<button @onclick="Login">Login</button>

@code {
    async Task Login() => await Auth.LoginAsync();
}
```

---

### Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore

EF Core unit of work with automatic audit stamping and domain event → outbox persistence.

```csharp
// Inherit your DbContext from the base
public class AppDbContext : BaseDbContext<AppDbContext>
{
    public AppDbContext(DbContextOptions<AppDbContext> options, IUnitOfWork uow)
        : base(options, uow) { }

    public DbSet<Order> Orders => Set<Order>();
}

// Use the unit of work
public class CreateOrderCommandHandler(IUnitOfWork<AppDbContext> unitOfWork)
{
    public async Task<Result<OrderId>> Handle(CreateOrderCommand cmd, CancellationToken ct)
    {
        var order = Order.Create(cmd.CustomerId);
        await unitOfWork.Repository<Order>().AddAsync(order, ct);
        await unitOfWork.SaveChangesAsync(ct); // audits + flushes domain events to outbox
        return order.Id;
    }
}
```

---

### Pivot.Framework.Infrastructure.Scheduling

Hangfire integration for background jobs.

```csharp
services.AddScheduling(configuration);

// Enqueue a job
IBackgroundJobClient jobs = ...;
jobs.Enqueue<IMyJob>(j => j.RunAsync());
```

---

## Versioning and Publishing

Packages are published automatically to GitHub Packages when a tag matching `v*.*.*` is pushed:

```bash
git tag v1.2.3
git push origin v1.2.3
```

The CI workflow in `.github/workflows/publish.yml` updates the version in all `.csproj` files, builds, packs, and pushes to `https://nuget.pkg.github.com/AnnabiGihed/index.json`.

---

## Author

Gihed Annabi — [github.com/AnnabiGihed](https://github.com/AnnabiGihed)
