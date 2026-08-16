# Lubnān, server side

The API behind the Next.js app in the repository root. .NET 9, PostgreSQL,
vertical slices.

```bash
docker compose up -d                                    # postgres, on host port 5433
dotnet ef database update --project src/Lubnan.Infrastructure --startup-project src/Lubnan.Infrastructure
dotnet run --project src/Lubnan.Api -- seed             # eight destinations

dotnet watch run --project src/Lubnan.Api               # http://localhost:5080/scalar/v1
```

`dotnet watch run` is the one to use day to day: it rebuilds and restarts on
save. `dotnet run` is the same thing without the reload, and is what CI and the
container do.

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
├─ Dockerfile                   multi-stage, non-root, ICU for Arabic
├─ docker-compose.yml           postgres always; api behind the "full" profile
├─ scripts/export-seed.mjs      frontend editorial data -> seed JSON
│
├─ src/
│  ├─ Lubnan.Domain/            no packages, no project references, at all
│  │  ├─ Common/                Entity, AggregateRoot, ValueObject, Result, Locale
│  │  ├─ Places/                Place aggregate, Slug, Coordinates, PlateSet
│  │  └─ Users/                 User aggregate, sessions, tokens, audit trail
│  │
│  ├─ Lubnan.Application/       the slices
│  │  ├─ Abstractions/          IAppDbContext, IClock, ICurrentUser, ISender, IEndpoint
│  │  ├─ Behaviors/             validation and logging, applied to every request
│  │  └─ Features/
│  │     ├─ Places/             ListPlaces, GetPlaceBySlug
│  │     └─ Identity/           Register, ConfirmEmail, Login, Refresh, Logout,
│  │                            LogoutEverywhere, GetMe
│  │
│  ├─ Lubnan.Infrastructure/    EF Core, interceptors, outbox, migrations, seeder,
│  │                            password hashing, token minting, mail
│  └─ Lubnan.Api/               composition root, auth wiring, CSRF, security headers
│
└─ tests/
   ├─ Lubnan.Domain.Tests/          pure, no host, milliseconds
   ├─ Lubnan.Architecture.Tests/    the rules above, as failing builds
   └─ Lubnan.Api.IntegrationTests/  the real host, a real PostgreSQL
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
| An in-memory EF provider in tests | It has none of Postgres's types, constraints or query translation, so a suite built on it is green about a database nobody runs | never |
| Startup migrations | Two replicas starting together race. `dotnet ef database update` is a deploy step | never |
| Startup seeding | Same, plus a seeder that runs automatically eventually runs somewhere it should not. `dotnet run -- seed` | never |
| French and Arabic copy | It has not been written. Seeding the English body under an `ar` label would serve English prose while claiming otherwise — the response's `locale` field says what was actually served | translation |

The seed is generated from the frontend's editorial data by
`scripts/export-seed.mjs`, which reads `web/data/destinations.json` and
`web/data/places.ts`. That is the only coupling between the two halves, it
exists so eight articles are not typed twice, and it should be deleted on the
commit that makes this database the source of truth.

## The container

```bash
docker compose --profile full up -d --build   # api + postgres, as they deploy
curl http://localhost:5080/api/v1/places
```

Three things in the Dockerfile are worth knowing about, because each is a bug
that appears only in the image and never on your machine:

**The project files are copied and restored before the source is.** Docker
caches a layer until one of its inputs changes, and dependencies change far
less often than code does. Copy everything up front and editing one handler
re-downloads the entire NuGet graph.

**`icu-libs` is installed explicitly.** Alpine ships without ICU and .NET
silently falls back to invariant globalization, which breaks culture-aware
comparison and formatting. This application serves Arabic and French, so that
fallback is not survivable — and it produces wrong output rather than an error.

**It runs as a non-root user that does not own its own binaries.** The default
is root, and a root process that escapes its namespace is root on the host.
There is no reason for a web API to be able to overwrite its own DLLs.

The healthcheck hits `/health/live`, not `/health/ready`. A container that
calls itself unhealthy because Postgres blinked gets restarted, which turns a
thirty-second database failover into a restart loop across every replica.
Readiness is the orchestrator's question, not the container's.

## Sessions, and the reasoning behind them

**Cookies, not an `Authorization` header.** A value JavaScript can read is a
value an XSS payload can read, and a frontend has hundreds of dependencies. An
httpOnly cookie can be *used* by a compromised page but not *stolen* from it,
which is the difference between one bad session and every session.

| Cookie | Contents | Attributes |
| --- | --- | --- |
| `lubnan_at` | JWT, 15 min | httpOnly, Secure, Lax, `/` |
| `lubnan_rt` | opaque, 30 days, rotating | httpOnly, Secure, Lax, **`/api/v1/auth`** |
| `lubnan_csrf` | double-submit token | **readable**, Secure, Lax, `/` |

The refresh cookie's path is deliberate: an ordinary request would otherwise
carry the long-lived credential as well as the short-lived one, so any log,
proxy or crash dump capturing one request captures the token that mints all the
others.

The trade for cookies is CSRF, and it is answered twice over. `SameSite=Lax`
stops the browser sending the session on a cross-site POST — but that is
enforced by the browser. The double-submit check is enforced by us: only script
on our own origin can read `lubnan_csrf` and echo it in `X-CSRF-Token`, and a
cross-site form can cause a request but cannot set a header. Compared in
constant time, because an early-returning comparison leaks the token one
character at a time.

**Refresh rotation with reuse detection.** Rotation does not prevent theft — a
stolen token works once. It makes theft *detectable*: the moment either party
presents a spent token, the whole family is revoked and the real holder finds
out because they were signed out. Revoking only the presented token would leave
the thief's current one working.

**Enumeration resistance.** Registration answers identically whether or not the
address is known; the *email* carries the difference. Sign-in answers identically
for no account, wrong password, unconfirmed, locked and suspended. Login hashes
a decoy password when no user matches, so "no such account" takes as long as
"wrong password" — otherwise the timing difference is a working oracle over the
network.

**Password rules: length only.** Twelve characters minimum, 256 maximum, no
character classes. Class rules push people towards `Password1!` and reject
passphrases that are orders of magnitude stronger; NIST SP 800-63B and the NCSC
both now say the same. The maximum exists because hashing is deliberately slow,
so an unbounded password is a way to make the server do unbounded work.

### Not yet done

Named, rather than left to be discovered:

- **Breached-password check** against Have I Been Pwned's k-anonymity API on
  registration and password change.
- **The append-only guarantee on `account_events` is application-level.** No
  code path updates or deletes a row. The database-level half — a role holding
  `INSERT, SELECT` and nothing else — is a grant in a migration and is what
  makes the log survive an attacker who has the application's own credentials.
- **Distributed rate limiting.** Counters are in process memory, so N replicas
  means N times the limit. Correct for one instance; the comment sits on the
  code that has to change.
- **The security stamp is issued but not checked per request.** "Sign out
  everywhere" stops refresh instantly and access tokens within 15 minutes. The
  claim is in the token from the first release specifically so that checking it
  against a cached value later is a change to validation, not to the token
  format — the latter would sign every user out on deploy.

## Verification

```bash
dotnet test                                      # 67 tests, three suites
dotnet build -p:ContinuousIntegrationBuild=true  # warnings become errors, as in CI
```

| Suite | What it proves | Needs |
| --- | --- | --- |
| `Lubnan.Domain.Tests` | the rules the aggregate enforces | nothing |
| `Lubnan.Architecture.Tests` | the dependency direction still holds | nothing |
| `Lubnan.Api.IntegrationTests` | HTTP in, PostgreSQL and back | Docker |

The integration tests start a **disposable PostgreSQL per run** with
Testcontainers, apply the migration, seed through the domain, and drive the
real host. Nothing is stubbed. The alternatives are an in-memory provider,
which has none of Postgres's types, constraints or query translation and so
cannot catch the bugs that actually happen, or a shared development database,
which makes the suite order-dependent and fails for whoever runs it second.

The response DTOs are **written out again** in the test project rather than
referenced from `Lubnan.Application`. The duplication is the point: a client
sharing types with its server cannot detect a breaking change, because renaming
a property on both sides at once leaves every test green and every real
consumer broken.

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

All of it is now asserted by `Lubnan.Api.IntegrationTests` rather than checked
by hand, so it stays true without anybody remembering to look.
