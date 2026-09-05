# Architecture

## Dashboard request flow

```mermaid
flowchart LR
    Browser[Tabulator dashboard] -->|GET /Dispatches/get-self-dispatches| Controller[DispatchController]
    Controller --> Repo[SelfDispatchRepo]
    Repo --> Access[DemoAccessService / provider IDs]
    Repo --> EF[Entity Framework Core]
    EF --> SQLite[(SQLite demo DB)]
    Repo --> Mapper[AutoMapper]
    Mapper --> Controller
    Controller -->|last_page + data + success| Browser
```

## Update and audit flow

```mermaid
flowchart TD
    Edit[EditDispatch.cshtml] -->|FormData POST| Controller[UpdateDispatch]
    Controller --> Repo[SelfDispatchRepo.UpdateDispatchAsync]
    Repo --> Old[Snapshot existing model]
    Repo --> Save[Update entity + follow-ups]
    Save --> Audit[AddSelfDispatchHistoryAsync]
    Audit --> History[(SelfDispatchAuditHistory)]
    History --> Fields[(SelfDispatchAuditField)]
    Controller -->|JSON success| Edit
```

## Delete flow

```mermaid
flowchart TD
    Delete[Delete request] --> Fields[Remove audit fields]
    Fields --> Histories[Remove audit histories]
    Histories --> FollowUps[Remove follow-ups]
    FollowUps --> FiveDay[Remove five-day follow-up]
    FiveDay --> Dispatch[Remove self-dispatch]
```

## Reconstruction boundaries

The public version intentionally does **not** reproduce internal authentication, company databases, proprietary libraries, organization names, production routes beyond the recovered public-facing MVC route shape, or any operational/clinical records. Demo identities and records are fictional.
