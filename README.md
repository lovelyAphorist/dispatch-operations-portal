# Colorado Dispatch Portal — Portfolio Reconstruction

A sanitized reconstruction of an internal ASP.NET MVC dispatch-management application I maintained and extended. The public repository preserves the workflows and engineering patterns from that work while replacing company infrastructure, authentication, database schemas, names, and operational data with demo-safe equivalents.

> **Portfolio scope:** This is not the production source code and contains no proprietary source, credentials, PHI, internal endpoints, or real dispatch records. The original application used ASP.NET MVC/.NET Framework and Entity Framework. This public reconstruction uses **ASP.NET Core MVC on .NET 8 + EF Core + SQLite** so reviewers can run it locally without internal dependencies.

## What it demonstrates

- C# / ASP.NET Core MVC
- Entity Framework Core and LINQ
- Repository pattern and async data access
- AutoMapper entity/view-model mapping
- Tabulator remote pagination and filtering
- Role- and provider-scoped data access
- AJAX/fetch-based edit workflow
- Field-level audit history
- One-to-many follow-up management
- Foreign-key-safe dependent record deletion
- Razor views, Bootstrap, JavaScript, and Luxon date formatting

## Reconstructed workflow

The dashboard displays self-dispatch records and loads data remotely through the original-style route:

```text
GET /Dispatches/get-self-dispatches
```

The controller action is intentionally named `GetPaginatedSelfDispatches`, matching the recovered application behavior. Server-side processing applies provider access, date ranges, search text, Tabulator column filters, descending `ReceivedDateTime` ordering, and `Skip`/`Take` pagination before returning:

```json
{
  "last_page": 4,
  "data": [],
  "success": true
}
```

Agency roles see only records assigned to providers linked to their demo user. Internal roles can see all demo providers.

## Audit history

Updates create a `SelfDispatchAuditHistory` row and then field-level `SelfDispatchAuditField` rows. Changes are detected using the same reflection-based pattern recovered from the original implementation. The dashboard history modal displays:

- Changed date
- Changed by
- Event
- Field name
- Old value
- New value

Dates are formatted as `MM/dd/yyyy HH:mm:ss` in the audit layer.

## Safe delete behavior

The original work included troubleshooting foreign-key failures while deleting dispatches. This reconstruction explicitly removes dependent records in safe order:

1. Audit fields
2. Audit history rows
3. Follow-ups
4. Five-day follow-up
5. Self-dispatch

That logic is deliberately visible in `SelfDispatchRepo.DeleteDispatchAsync` rather than hidden behind database cascade behavior.

## Demo access profiles

The app seeds several fake users to make authorization behavior easy to review:

| Demo user | Role | Scope |
|---|---|---|
| Avery Agency Admin | `AGENCYADMIN` | Demo Providers North + Central |
| Bailey Agency Staff | `AGENCYSTAFF` | Demo Provider North |
| Casey Auditor | `AUDITOR` | Demo Provider Central |
| Drew Internal Admin | `INTERNALADMIN` | All providers |
| Ellis Dispatch | `INTERNALDISPATCH` | All providers |

Role names and provider names are demo-safe; no internal user or organization identifiers are included.

## Run locally

Prerequisites:

- .NET 8 SDK

Then:

```bash
dotnet restore
dotnet run --project ColoradoDispatchPortal
```

Open the URL printed by ASP.NET Core. The SQLite database is created and populated with fictional dispatch records automatically on first launch.

## Project structure

```text
ColoradoDispatchPortal/
├── Controllers/
│   └── DispatchController.cs
├── Data/
│   ├── DispatchPortalContext.cs
│   ├── DemoDataSeeder.cs
│   └── Entities/
├── Mapping/
│   └── MappingProfile.cs
├── Models/
├── Repositories/
│   ├── ISelfDispatchRepo.cs
│   └── SelfDispatchRepo.cs
├── Services/
│   └── DemoAccessService.cs
├── Views/Dispatch/
│   ├── Dashboard.cshtml
│   └── EditDispatch.cshtml
└── wwwroot/
    ├── css/site.css
    └── js/
        ├── dispatch-dashboard.js
        └── edit-dispatch.js
```

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) for the request and persistence flows.

## Notable implementation details

### Provider filtering

`SelfDispatchRepo.GetSelfDispatches` first resolves the current demo user's provider IDs. `AGENCYSTAFF`, `AGENCYADMIN`, and `AUDITOR` users are provider-scoped. Internal demo roles are not.

### Nullable Boolean compatibility

The persisted entity keeps several appointment/follow-up flags nullable to mirror the kind of legacy database shape encountered in the original project. The public model exposes normal booleans and maps null to `false`.

### Completed records

Completed dispatches are displayed as read-only in the edit page, preserving the recovered workflow where updates were only presented for incomplete dispatches.

## Testing

`ColoradoDispatchPortal.Tests` includes focused repository tests for:

- provider-scoped pagination
- audit history creation during updates
- dependent-record deletion

Run:

```bash
dotnet test
```

## Portfolio talking points

This project is useful for discussing how I handled more than UI CRUD: server-side table performance, role-aware data access, entity/view-model mismatches, nullable-value mapping, auditability, relational constraints, and AJAX-driven MVC workflows in an existing production application.
