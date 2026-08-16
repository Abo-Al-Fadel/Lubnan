# Lubnān, server side

The API behind the Next.js app in the repository root. .NET 9, PostgreSQL,
vertical slices.

```bash
docker compose up -d
dotnet ef database update --project src/Lubnan.Infrastructure --startup-project src/Lubnan.Infrastructure
dotnet run --project src/Lubnan.Api -- seed
dotnet run --project src/Lubnan.Api          # http://localhost:5080/scalar/v1
```

> **The container publishes Postgres on host port 5433, not 5432.** A container
> should not squat on the canonical port: anyone who has installed PostgreSQL
> natively — which is what happens when you install pgAdmin from the EDB
> bundle — already has a service there, and the collision is silent. Docker
> binds IPv6 only, `localhost` resolves to IPv4 first, and your connection
> lands on the other database. The error is `password authentication failed`,
> which sends you looking at credentials that were never wrong.

## Why the folders look like this

Organised by **feature**, not by technical role. The instinct on a portfolio
project is `Controllers/`, `Services/`, `Repositories/`, `Entities/` — four
folders touched to add one behaviour, and a pile of interfaces with exactly one
implementation each. Here, everything one use case needs is in one folder:

```
Features/Places/GetPlaceBySlug/
  Query.cs       what is being asked
  Validator.cs   what would make the question invalid
  Handler.cs     the answer
  Endpoint.cs    how it is reached over HTTP
```

Four small files, one folder, one behaviour. Nothing to navigate.

```
server/
├─ Directory.Build.props        settings every project inherits
├─ Directory.Packages.props     every package version, in one file
├─ docker-compose.yml           postgres, and nothing that is not used yet
├─ scripts/export-seed.mjs      frontend editorial data -> seed JSON
│
├─ src/
│  ├─ Lubnan.Domain/            no packages, no project references, at all
│  │  ├─ Common/                Entity, AggregateRoot, ValueObject, Result, Locale
│  │  └─ Places/                Place aggregate, Slug, Coordinates, PlateSet, events
│  │
│  ├─ Lubnan.Application/       the slices
│  │  ├─ Abstractions/          IAppDbContext, IClock, ICurrentUser, ISender, IEndpoint
│  │  ├─ Behaviors/             validation and logging, applied to every request
│  │  └─ Features/Places/       ListPlaces, GetPlaceBySlug
│  │
│  ├─ Lubnan.Infrastructure/    EF Core, interceptors, outbox, migrations, seeder
│  └─ Lubnan.Api/               composition root: ~150 lines, all of it wiring
│
└─ tests/
   ├─ Lubnan.Domain.Tests/      pure, no host, milliseconds
   └─ Lubnan.Architecture.Tests the rules above, as failing builds
```

### The dependency direction

```
Api  ──────────────►  Application  ──────────►  Domain
 │                         ▲                      ▲
 └──►  Infrastructure  ────┘──────────────────────┘
```

Infrastructure points **up**: it implements interfaces Application declares,
and only `Program.cs` ever mentions it. That is what lets Postgres be replaced
without touching a handler, and it is enforced by
`tests/Lubnan.Architecture.Tests`, not by a paragraph like this one.

## What is worth defending in an interview

**Why vertical slices.** Layered architecture organises by technical role, so
one feature touches four folders and accumulates single-implementation
interfaces. Slices organise by reason to change.

**Why architecture tests.** A diagram describes month one. These fail the build
in month eighteen, when the person adding the twentieth feature has not read the
diagram and a `using` statement is one keystroke away. Six rules, currently:
the domain depends on nothing, Application never names Npgsql, Infrastructure
never names the API, handlers never touch HTTP, handlers are sealed and
internal, entities are not records.

**Why `Result<T>` and not exceptions.** A slug nobody published is not a bug.
Exceptions are for bugs; making an ordinary 404 into one costs a stack capture
on a hot path and hides the control flow from the compiler.

**Why the transactional outbox.** "Save the row" and "publish the event" are two
operations, and any two operations can fail between the first and the second.
Writing the event to the same database in the same transaction makes the pair
atomic. `DomainEventInterceptor` does it in forty lines.

**Why a table for translations and `jsonb` for callout labels.** One rule,
applied consistently: *a column for anything you filter, sort, constrain or
search on; JSON for prose that is only ever read as part of its parent.* A
place's article is searched, needs a per-locale stemmer, and has to answer
"which places lack Arabic" — so it is a table with a unique index on
`(place_id, locale)`. A callout label is read exactly when its callout is read
and nothing queries it — so it is `jsonb`, and moving a dot stays one write
instead of one per language.

**Why partial indexes.** `WHERE published_at IS NOT NULL` keeps the list index
the size of the catalogue rather than the size of the drafts folder.
`WHERE processed_at IS NULL` keeps the outbox index the size of the backlog
rather than of all history. Both stay small permanently.

**Why not microservices.** At this size they add network failure between things
that belong in one transaction. Being able to say why you did *not* reach for
something is a stronger signal than a diagram with eight boxes.

## Deliberate omissions

Each of these is a decision, not an oversight.

| Not here | Why | Arrives with |
| --- | --- | --- |
| PostGIS | Nothing queries geography yet, and `CREATE EXTENSION` in the first migration makes "does this host offer PostGIS" a condition of the schema existing at all | the near-me endpoint |
| Redis | Rate limits count in process memory. Correct for one instance, wrong for two — see the comment in `Program.cs` | the second replica |
| MinIO / S3 | No uploads yet | community posts |
| A mediator package | The obvious one moved to a paid licence for commercial use. `Sender.cs` is sixty lines and explains itself | never |
| Startup migrations | Two replicas starting together race. `dotnet ef database update` is a deploy step | never |
| Startup seeding | Same, plus a seeder that runs automatically eventually runs somewhere it should not. `dotnet run -- seed` | never |
| French and Arabic copy | It has not been written. Seeding the English body under an `ar` label would serve English prose while claiming otherwise — the response's `locale` field says what was actually served | translation |

The seed is generated from the frontend's editorial data by
`scripts/export-seed.mjs`, which reads `web/data/destinations.json` and
`web/data/places.ts`. That is the only coupling between the two halves, it
exists so eight articles are not typed twice, and it should be deleted on the
commit that makes this database the source of truth.

## Verification

```bash
dotnet test                                    # 41 tests: domain rules + architecture rules
dotnet build -p:ContinuousIntegrationBuild=true    # warnings become errors, as in CI
```

CI additionally asserts that the migrations match the model
(`ef migrations has-pending-model-changes`) and that the seed file matches the
frontend data it is generated from. Both are drifts that are invisible until
they are expensive.

### Run against a real database

The migration applies, the seed loads through the domain, and both endpoints
answer from Postgres 17 in the container:

```
8 places, 24 callouts, 32 practical facts, 8 outbox rows
```

The outbox count is the one to look at. Eight `PlacePublished` events were
drained onto the outbox table by `DomainEventInterceptor` inside the same
transaction as the inserts — which is the whole pattern, demonstrated rather
than described.

Also confirmed against live data: editorial ordering, region and category
filtering, callout coordinates surviving the `jsonb` round trip, the locale
fallback (a request for `ar` returns English **and reports `"locale": "en"`**,
so a client can mark the page untranslated), and a 404 with a stable
`place.notFound` code for an unknown slug.

### What still has not been run

**Automated integration tests.** Everything above was verified by hand. The next
piece of real work is `Lubnan.Api.IntegrationTests` with Testcontainers, so a
disposable Postgres comes up per test run and none of this depends on somebody
remembering to check. That should exist before a third slice is written.
