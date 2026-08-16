# Lubnān

A tourism and culture platform for Lebanon. Two applications, deployed
independently, in one repository.

```
lubnan/
├─ web/       Next.js 14, TypeScript, Tailwind. Trilingual, RTL, three palettes.
├─ server/    .NET 9, PostgreSQL, vertical slices.
└─ .github/   CI
```

Each half has its own README with the detail: [web/](web/README.md) ·
[server/](server/README.md).

## Running both

```bash
# backend  — http://localhost:5080/scalar/v1
cd server
docker compose up -d
dotnet ef database update --project src/Lubnan.Infrastructure --startup-project src/Lubnan.Infrastructure
dotnet run --project src/Lubnan.Api -- seed
dotnet run --project src/Lubnan.Api

# frontend — http://localhost:3000
cd web
npm install
npm run dev
```

They do not talk to each other yet. The web app still reads `web/data/*.json`,
and the swap happens one feature at a time rather than in a single commit that
either works or does not.

> Postgres publishes on host port **5433**, not 5432, so it cannot collide with
> a natively installed PostgreSQL. See `server/docker-compose.yml`.

## Where the two meet

The API publishes OpenAPI at `/openapi/v1.json`. The frontend will generate its
client from that document rather than hand-writing fetch wrappers, so a
breaking change fails a build instead of a page.

`server/scripts/export-seed.mjs` is the one place the halves are currently
coupled: it reads the editorial copy out of `web/data/` and writes the seed the
API ships with. It exists so eight articles are not typed twice, and it should
be deleted on the commit that makes the database the source of truth.
