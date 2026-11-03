**Claude Opus 5.5 · October 28, 2026**

# WolfDesk (C#)

For further enquiry please contact Akin Kaldiroglu at akin@kaldiroglu.dev

WolfDesk is the running example of the Domain-Driven Design course, based on Vlad
Khononov's *Learning Domain-Driven Design* (O'Reilly, 2021). The course decks quote this
code, so every snippet on a slide is compiled and tested. The Java version is
`WolfDeskJava`; both have the same design and the same tests.

- **Project:** WolfDesk, C# version, started on 2026-10-28.
- **Stack:** .NET 10 (`net10.0`), xUnit. No framework.
- **Root namespace:** `dev.kaldiroglu.WolfDesk`.

## What WolfDesk is, and what it is not

- **A library with a demo.** The three bounded contexts are used in process. Each one's
  public interface is its application layer: `TicketService` in ticketing, `RegisterTenant`
  and `AddSupportedProduct` in tenants, `RecordTicketOpened` and `ChargeTenant` in billing.
  Other bounded contexts read ticketing's published language, which the outbox relay
  publishes.
- **One program: the demo.** `WolfDeskDemo` runs one story through the three contexts
  (`dotnet run --project src/WolfDesk.Demo`). Apart from it, the tests are the only callers.
- **No network API and no user interface.** There are no HTTP endpoints and no
  connection to a real message bus: billing subscribes to ticketing's messages through the
  in-memory bus, in the same process. In chapter 8's terms, WolfDesk has driven adapters,
  for storage and the message bus, and no driving adapter.
- **No database.** Every adapter keeps its data in memory and behaves like a database:
  tenants and billing have transactions, the ticket repository has optimistic concurrency,
  and the ticket's save writes its events to the outbox in the same transaction. Nothing
  needs Docker.
- **No packages but xUnit and the test SDK**, which only the tests use.

These are deliberate: the code stays about the domain model, and the in-memory adapters
show transactions, concurrency and the outbox without infrastructure to install.
`WolfDeskScopeTests` fails if the code stops matching this list: a package other than the
test frameworks, a project on another SDK than `Microsoft.NET.Sdk`, a network or database
type in `src`, or a program other than the demo.

## The business

WolfDesk provides a help desk ticket management system as a service (the book's Preface,
pp. xviii–xix). Tenants pay for the number of tickets opened per charging period, with
automatic volume discounts of 10% above 500 tickets a month, 20% above 750 and 30% above
1,000. Tenants configure their ticket categories and the products they support. Tickets are
assigned to support agents during their shifts, and escalated when the response deadline
passes.

## Expected benefits

- One code base that shows each of the book's business logic and architectural patterns in
  the bounded context where the book would use it.
- The same design in C# and Java, so participants work in their own language.
- Tags by chapter (`stage-ch05` to `stage-ch09`, …), so a slide can point to
  the code as it stood at that chapter.

## Bounded contexts and their patterns

| Namespace | Pattern | Chapter | What it does |
|---|---|---|---|
| `Billing` | Transaction script | 5 | Records opened tickets and charges a tenant per period with the volume discount |
| `Tenants` | Active record | 5 | A tenant with its ticket categories and supported products |
| `Ticketing.Domain` | Domain model | 6 | The `Ticket` aggregate: messages, assignment, escalation, reassignment, closing; its commands in `Domain.Commands`, its domain events in `Domain.Events` |
| `Ticketing.Application` | Application service | 6 | `TicketService`, the outbox relay and its event publisher port |
| `Ticketing.EventSourced` | Event-sourced domain model | 7 | The same ticket, its state rebuilt from its events |
| `Ticketing.ReadModels` | CQRS | 8 | The agents' queues, projected from the saved tickets |
| `Ticketing.PublishedLanguage` | Published language | 9 | The public messages ticketing publishes through its outbox |

Each context has its own small model; they share no classes. Storage is in-memory
adapters that mimic a database: a transaction works on a copy of the tables, which commit
keeps and rollback throws away.

## Architecture

Each module has the architecture its business logic needs (chapter 8):

- `Tenants` is layered: the `Tenant` active record depends on the concrete
  `TenantsDatabase`, and the transaction scripts `RegisterTenant` and `AddSupportedProduct`
  are its service layer.
- `Billing` is transaction scripts, which are a service layer already; they work on the
  database through the `IBillingDatabase` interface.
- `Ticketing` is ports & adapters, one namespace per role. The business logic layer
  defines the ports (chapter 8, p. 127): `ITicketRepository`, `IOutbox` and
  `IDepartmentRepository` are in `Ticketing.Domain`, and `Ticketing.InMemory` holds the
  adapters. The one port in `Ticketing.Application` is `IEventPublisher`: it publishes the
  published language, not the domain's events, and only the outbox relay uses it, so the
  domain stays independent of the published language. Only `InMemory` and the demo, the
  composition root, use an adapter, and the domain uses nothing from the application
  layer.
- `Message` stays in `Ticketing.Domain` with `Ticket`. Its constructor and `MarkAsRead`
  are `internal`, which in C# opens them to the whole assembly, not only to the namespace.
- The tags up to `stage-ch09` keep the earlier layout, with these types directly in
  `Ticketing`; the decks quote the code at those tags.
- `Ticketing.EventSourced` is ports & adapters too: `IEventStore` is its port, and
  `InMemoryEventStore` in `Ticketing.InMemory` its adapter.
- `Ticketing.ReadModels` adds CQRS: the `Ticket` aggregate is the command execution model,
  and `AgentQueueProjection` projects the saved tickets into each agent's queue. Each
  catch-up reads the tickets saved after its last checkpoint, which the repository stamps
  on every save, like a `rowversion` column. Resetting the checkpoint rebuilds the queues.
- Ticketing and billing communicate through messages (chapter 9): ticketing's `IOutbox` is a
  port, `OutboxRelay` publishes through the `IEventPublisher` port, and billing's
  `TicketingAnticorruptionLayer` subscribes. `TicketingToBillingTests` wires the two.

## The ticket's rules

The `Ticket` aggregate follows the book's listings 06-08 to 06-17 for its shape, and the
ticket policies of the TypeScript `eventually-wolfdesk` for its rules:

- Only the customer or the assigned agent writes messages; only a message's receiver can
  acknowledge it.
- The customer can request an escalation only after the response deadline, and only once.
- A missed deadline escalates the ticket automatically.
- An escalated ticket with less than half its time left and unread messages for its agent
  goes to another agent (listing 06-13).
- The response deadline counts the agent's working time only, from the shift schedule
  (listing 06-18, a domain service).
- Saving a ticket that changed since it was loaded fails (optimistic concurrency, listings
  06-10 to 06-12).

## The event-sourced ticket

`Ticketing.EventSourced` implements the same `Ticket` aggregate as an event-sourced domain
model (chapter 7, pp. 108–110). It has the same commands and rules; only the way its state
is persisted differs:

- No state is stored. `IEventStore` keeps each ticket's events, append-only: it can fetch a
  ticket's events and append new ones, nothing else.
- `new Ticket(events)` rehydrates the ticket: `TicketState` projects the events, oldest
  first, into the current state. A command that passes the rules appends a new event, and
  the event changes the state through the same projection.
- `TicketService` follows the book's script: load the events, rehydrate, execute one
  command, append the new events at the version the ticket was loaded at. If other events
  were appended since, the append fails (optimistic concurrency).
- The events are the source of truth, so they carry everything: the title, the response
  timeframe, the message body. Chapter 6's events do not; they were never meant to rebuild
  a ticket.
- `LoadAt(id, version)` rebuilds a ticket as it was at an earlier version, and
  `TicketAnalysisProjection` projects the same events into a second model for analysis.
- The event-sourced types have the names of chapter 6's (`Ticket`, `TicketService`,
  `TicketOpened`, …) in their own namespace; a file that uses both namespaces names them
  with a using alias.

## Publishing events: the outbox and the published language

Chapter 9's patterns connect ticketing to billing (pp. 137–147):

- **Outbox.** `TicketService` no longer publishes events. `InMemoryTicketRepository.Save`
  writes the ticket and its new domain events to the outbox in the same transaction, so
  both are saved or neither is. A save rejected for concurrency writes no events.
- **Relay.** `OutboxRelay` pulls the unpublished events, publishes them, and marks them as
  published. If it stops between publishing and marking, the next run publishes the event
  again: delivery is at least once.
- **Published language.** The relay publishes `Ticketing.PublishedLanguage` messages, not
  the ticket's domain events: `PublishedLanguageTranslator` translates the public events
  and leaves the private ones (assignment, messages, reassignment) inside. The messages
  carry only plain values, so consumers depend on nothing else in ticketing.
- **Anticorruption layer.** Billing translates `TicketOpenedMessage` into its own model:
  it finds the tenant from the ticket's product and calls `RecordTicketOpened`, which
  counts a ticket once however often the message arrives.

## The demo

`dev.kaldiroglu.WolfDesk.Demo.WolfDeskDemo` runs one story through the three bounded
contexts and prints each step: a tenant registers a product, a customer opens a ticket, an
early escalation is refused and a later one succeeds, the agent closes the ticket, the
outbox relay publishes, and billing charges the tenant. It is the composition root: the
only class that wires the contexts to their in-memory adapters. No database is needed.
The console project `src/WolfDesk.Demo` runs it; its output is the same as the Java
version's.

## Credits

The ticket's rules follow `wolfdesk/eventually-wolfdesk`, MIT License, Copyright (c) 2021
Vladik Khononov; its license is in `THIRD-PARTY-LICENSE-eventually-wolfdesk`. The code here
is written from scratch in C#; nothing is translated line by line.

## Run it with

```bash
dotnet run --project src/WolfDesk.Demo             # the demo
dotnet test                                        # all tests
dotnet test --filter FullyQualifiedName~ChargeTenantTests   # one test class
```
