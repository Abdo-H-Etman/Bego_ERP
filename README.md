# Enterprise Multi-Tenant Modular Monolith ERP Architecture

Production-grade, Clean Architecture modular monolith in **.NET 10** behind a **YARP API Gateway**, featuring **multiple database axes** (per-module and per-tenant sharding), dynamic connection resolution, **ASP.NET Core API Controllers** across all modules, **Feature-Based CQRS** (`Features/{Feature}/Commands` and `Queries`), direct **DbContext / UnitOfWork** (no repository anti-pattern), PostgreSQL timestamp concurrency tokens, Serilog structured logging, dynamic roles & permissions, metadata-driven UI engine, dynamic workflow state machines, HMAC authentication for Begohub ERP data pulls, barcode/label rendering, CircleCI automation, and an Angular front-end design.

---

## 1. Compliance Matrix (All Requirements)

| # | Requirement | Implementation Details |
|---|---|---|
| **1** | **Clean Architecture & Modular Monolith** | Monolith composition root (`Host/Erp.Api`) loading modules adhering to strict `Api -> Application -> Domain` & `Infrastructure -> Application, Domain` rules. |
| **2** | **ASP.NET Core API Controllers** | Replaced Minimal APIs with standard `[ApiController]` classes in every module, registered in `Erp.Api` via `.AddApplicationPart(...)` and routed through `app.MapControllers()`. |
| **3** | **Feature-Based CQRS Structure** | Each module's Application layer is organized by feature: `Features/{FeatureName}/Commands/` and `Features/{FeatureName}/Queries/` (e.g. `Features/Invoices/Commands/CreateInvoiceCommand.cs`). |
| **4** | **No Repository Pattern (Direct DbContext as Unit of Work)** | Module `DbContext` classes implement `IUnitOfWork` directly with `DbSet<T>`. Handlers execute queries and updates against the DbContext atomically without unnecessary repository wrappers. |
| **5** | **Dynamic Roles & Page/Action Permissions** | `Erp.Modules.Identity` with runtime dynamic roles, `HasPermissionAttribute`, and `PermissionAuthorizationHandler` for action and page-level security. Exposed via `RolesController` & `PermissionsController`. |
| **6** | **Metadata-Driven System** | `Erp.Modules.Metadata` defining `PageDefinition`, `FormDefinition`, `GridDefinition`, `AuthorizationGridDefinition` (column filtering/readonly by role), and JSONB `CustomFormData` exposed via `MetadataController`. |
| **7** | **Message Broker** | `IMessageBroker` abstraction with asynchronous topic-based pub/sub supporting RabbitMQ and resilient in-memory dispatching. |
| **8** | **PostgreSQL** | Configured across all modules using `Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3` with dedicated schemas (`tenancy`, `accounting`, `inventory`, `sales`, `purchasing`, `identity`, `metadata`, `workflow`, `notifications`). |
| **9** | **Angular Front-End System Design** | Complete architecture documented in `docs/ANGULAR_SYSTEM_DESIGN.md` and scaffolded in `frontend/angular-erp/` featuring metadata renderers (`DynamicFormComponent`, `DynamicGridComponent`) and interceptors. |
| **10** | **HMAC Authentication (Begohub ERP)** | `IHmacService` and `BegohubHmacMiddleware` implementing HMAC-SHA256 request signing, constant-time validation, and 5-minute anti-replay protection. Exposed via `BegohubController`. |
| **11** | **Multi-Tenant Identity** | `(AppId | LegalId, Country)` resolved in Gateway $\rightarrow$ `TenantId`, `Country`, `ShardKey` passed via trusted internal headers. Monolith enforces EF Core global query filters. |
| **12** | **Unit & Integration Testing** | Comprehensive xUnit & FluentAssertions test suite covering architectural boundaries, HMAC validation, workflow transitions, tax engines, and feature isolation (`20/20 tests passed`). |
| **13** | **Auditing System** | `IAuditableEntity` marker interface with EF Core `AuditDbContextInterceptor` capturing `CreatedAtUtc`, `CreatedBy`, `ModifiedAtUtc`, `ModifiedBy`, and automated JSON diff audit trail. |
| **14** | **Concurrency Validation (PostgreSQL Timestamp)** | Optimistic concurrency via `IVersionedEntity` using PostgreSQL `timestamp with time zone` (`ConcurrencyTimestamp` token with `CURRENT_TIMESTAMP` default and automatic value generation on add/update). |
| **15** | **Dynamic Workflow Engine** | `Erp.Modules.Workflow` providing `WorkflowDefinition`, `WorkflowState`, `WorkflowTransition`, and `WorkflowInstance` with role-restricted state machine transitions, audit history, and `WorkflowController`. |
| **16** | **Notification System** | `Erp.Modules.Notifications` providing multi-channel dispatch (In-App, Email, SMS, Webhook) with read status tracking and `NotificationsController`. |
| **17** | **Barcode and Label System** | `IBarcodeGenerator` and `LabelRenderer` supporting Code128, EAN-13, and QR Code SVG vector generation and thermal label layout rendering. |
| **18** | **CircleCI & Automated Deployment** | Automated `.circleci/config.yml` pipeline with restore, build, test, Docker image compilation (`Dockerfile` for Gateway and Monolith). |
| **19** | **Entity Framework Core 10** | EF Core 10 used across all 9 modules with schema segregation and dynamic connection resolution. |
| **20** | **DDD Pattern** | Rich DDD primitives in `Erp.SharedKernel` (`Entity<TId>`, `AggregateRoot<TId>`, `ValueObject`, `IDomainEvent`, `Result`, `Error`). |
| **21** | **Redis Caching for Metadata** | `IRedisCacheService` with prefix-based invalidation for caching compiled page, form, and grid metadata. |
| **22** | **Serilog Structured Logging & Error Handling** | Solution-wide `Serilog` integration (`Serilog.ILogger`, `SerilogRequestLogging`, `LoggingBehavior`), paired with `GlobalExceptionHandlingMiddleware` producing RFC 7807 `ProblemDetails`. |

---

## 2. Feature-Based CQRS Architecture

Every module's `Application` layer follows a vertical-slice, feature-based CQRS structure:

```
{Module}.Application/
├── Features/
│   ├── {FeatureName}/
│   │   ├── Commands/
│   │   │   ├── Create{Entity}Command.cs
│   │   │   └── ...
│   │   └── Queries/
│   │       ├── Get{Entity}Queries.cs
│   │       └── ...
```

### Examples:
- **`Accounting.Application`**:
  - `Features/Invoices/Commands/CreateInvoiceCommand.cs`
  - `Features/Invoices/Queries/GetInvoiceQueries.cs`
- **`Tenancy.Application`**:
  - `Features/Tenants/Commands/RegisterTenantCommand.cs`
  - `Features/Tenants/Queries/GetTenantQueries.cs`
- **`Inventory.Application`**:
  - `Features/Products/Commands/CreateProductCommand.cs`
  - `Features/Products/Queries/GetProductsQuery.cs`
- **`Sales.Application`**:
  - `Features/Orders/Commands/CreateOrderCommand.cs`
  - `Features/Orders/Queries/GetOrdersQuery.cs`
- **`Purchasing.Application`**:
  - `Features/PurchaseOrders/Commands/CreatePurchaseOrderCommand.cs`
  - `Features/PurchaseOrders/Queries/GetPurchaseOrdersQuery.cs`

---

## 3. Controller-Based API Architecture

Each module's API layer exposes standard ASP.NET Core `[ApiController]` endpoints:

- **Tenancy**: `TenantsController` (`/api/tenants`, `/api/tenants/by-app`, `/api/tenants/by-legal`, `/api/tenants/resolve`)
- **Accounting**: `InvoicesController` (`/api/invoices`), `TaxController` (`/api/tax/calculate`)
- **Inventory**: `ProductsController` (`/api/inventory/products`)
- **Sales**: `OrdersController` (`/api/sales/orders`)
- **Purchasing**: `PurchaseOrdersController` (`/api/purchasing/purchase-orders`)
- **Identity**: `RolesController` (`/api/identity/roles`), `PermissionsController` (`/api/identity/permissions`)
- **Metadata**: `MetadataController` (`/api/metadata/pages`, `/api/metadata/data`)
- **Workflow**: `WorkflowController` (`/api/workflow/instances/...`)
- **Notifications**: `NotificationsController` (`/api/notifications`)
- **Host**: `BegohubController` (`/api/integrations/begohub/sync-pull`), `StatusController` (`/`)

The host composition root (`Host/Erp.Api`) dynamically binds all controller assemblies:

```csharp
builder.Services.AddControllers()
    .AddApplicationPart(typeof(TenantsController).Assembly)
    .AddApplicationPart(typeof(InvoicesController).Assembly)
    .AddApplicationPart(typeof(ProductsController).Assembly)
    .AddApplicationPart(typeof(OrdersController).Assembly)
    .AddApplicationPart(typeof(PurchaseOrdersController).Assembly)
    .AddApplicationPart(typeof(RolesController).Assembly)
    .AddApplicationPart(typeof(MetadataController).Assembly)
    .AddApplicationPart(typeof(WorkflowController).Assembly)
    .AddApplicationPart(typeof(NotificationsController).Assembly);

// Routing pipeline
app.MapControllers();
```

---

## 4. Solution Layout

```
Erp.slnx
Directory.Build.props
.circleci/config.yml
docs/ANGULAR_SYSTEM_DESIGN.md
frontend/angular-erp/
src/
├── Gateway/
│   └── Erp.Gateway                       (YARP Reverse Proxy + ITenantResolver + Header Sanitization)
├── Host/
│   └── Erp.Api                           (Composition root, AddControllers + ApplicationParts, Serilog, HMAC)
├── BuildingBlocks/
│   ├── Erp.SharedKernel                  (DDD primitives, IAuditableEntity, IVersionedEntity, Result, Error)
│   ├── Erp.Application.Abstractions      (IUnitOfWork, CQRS, LoggingBehavior, ITenantContext, HMAC)
│   └── Erp.Infrastructure.Abstractions   (ITenantConnectionResolver, IModule, Caching, Barcodes)
├── Modules/
│   ├── Tenancy/                          (Domain, Application [Features/Tenants], Infrastructure, Api [Controllers])
│   ├── Accounting/                       (Domain, Application [Features/Invoices], Infrastructure, Api [Controllers])
│   ├── Inventory/                        (Domain, Application [Features/Products], Infrastructure, Api [Controllers])
│   ├── Sales/                            (Domain, Application [Features/Orders], Infrastructure, Api [Controllers])
│   ├── Purchasing/                       (Domain, Application [Features/PurchaseOrders], Infrastructure, Api [Controllers])
│   ├── Identity/                         (Erp.Modules.Identity: Dynamic roles, permissions, RolesController)
│   ├── Metadata/                         (Erp.Modules.Metadata: UI metadata engine, MetadataController)
│   ├── Workflow/                         (Erp.Modules.Workflow: State machine engine, WorkflowController)
│   └── Notifications/                    (Erp.Modules.Notifications: Multi-channel dispatch, NotificationsController)
└── Localization/
    ├── Tax.Egypt                         (Egypt ETA standard VAT: 14%)
    ├── Tax.Ksa                           (KSA ZATCA standard VAT: 15%)
    └── Tax.Uae                           (UAE FTA standard VAT: 5%)
tests/
├── ArchitectureTests/
│   └── Erp.ArchitectureTests             (NetArchTest boundary & layer dependency enforcement)
├── UnitTests/
│   └── Erp.UnitTests                     (Pure in-memory unit tests: Tax engines, Barcodes, Workflow, Concurrency)
└── IntegrationTests/
    └── Erp.IntegrationTests              (Integration tests: HMAC signatures, anti-replay validation)
```

---

## 5. Verification & Testing

```bash
# Build the entire solution (0 warnings, 0 errors)
dotnet build

# Run all tests across all suites (20 tests: Architecture, Unit, and Integration)
dotnet test

# Run specific test suites independently
dotnet test tests/ArchitectureTests/Erp.ArchitectureTests/Erp.ArchitectureTests.csproj
dotnet test tests/UnitTests/Erp.UnitTests/Erp.UnitTests.csproj
dotnet test tests/IntegrationTests/Erp.IntegrationTests/Erp.IntegrationTests.csproj

# Run Host Monolith (port 5050)
dotnet run --project src/Host/Erp.Api/Erp.Api.csproj

# Run API Gateway (port 5051)
dotnet run --project src/Gateway/Erp.Gateway/Erp.Gateway.csproj
```
