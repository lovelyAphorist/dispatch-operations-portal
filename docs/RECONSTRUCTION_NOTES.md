# Reconstruction Notes

This file separates behavior recovered from prior development work from changes made only to make the public portfolio version safe and runnable.

## Recovered application behavior represented here

- `SelfDispatch` / `SelfDispatchModel` dashboard workflow.
- Controller route `/Dispatches/get-self-dispatches` with method name `GetPaginatedSelfDispatches`.
- Remote Tabulator pagination returning `last_page`, `data`, and `success`.
- Dashboard fields including reference number, first/last name, cancellation type, received/dispatch/cleared times, responding clinician team, and disposition.
- Provider-scoped access for `AGENCYSTAFF`, `AGENCYADMIN`, and `AUDITOR`-style users.
- Repository-side provider filtering, date filtering, column filtering, descending `ReceivedDateTime` ordering, and `Skip`/`Take` pagination.
- Nullable persisted appointment/follow-up flags mapped to non-nullable view-model booleans.
- Edit workflow with behavioral-health appointment fields, follow-up rows, and five-day follow-up fields.
- AJAX form submission with JSON success/error responses.
- `SelfDispatchAuditHistory` and `SelfDispatchAuditField` field-level history.
- `AddSelfDispatchHistoryAsync`, reflection-based change comparison, and `FindDispatchHistoryAsync` behavior.
- Audit-history table fields: changed date, changed by, event, field name, old value, new value.
- Explicit dependent-record cleanup before deleting a dispatch to avoid foreign-key violations.
- Completed dispatches treated as read-only in the edit workflow.

## Public reconstruction changes

These are intentionally different from the internal application:

- .NET 8 / ASP.NET Core MVC replaces the original ASP.NET MVC/.NET Framework host.
- EF Core + local SQLite replaces the internal database and database-first entity model.
- Demo users replace internal authentication and identity systems.
- Demo providers replace organization/provider records.
- All dispatch records are generated fictional data.
- Internal role names have been replaced where needed with generic equivalents.
- Internal libraries, endpoints, configuration, and integrations are omitted.
- Bootstrap/CDN versions are updated for a straightforward public demo.

## Why this is labeled a reconstruction

The repository is meant to demonstrate engineering work and patterns from the project without presenting proprietary source as public code. It should be described in interviews and on GitHub as a **sanitized portfolio reconstruction of an internal application maintained and extended by the developer**, not as the original production repository.
