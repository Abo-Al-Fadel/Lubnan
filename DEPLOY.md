# Deploying Lubnān

Three free accounts, roughly forty minutes, and one decision you cannot undo
cheaply (the database region). Everything here is a step you have to take
yourself, because each needs an account only you can create.

```
        browser
           │  everything, one origin
           ▼
   ┌───────────────────┐
   │  Vercel           │   Next.js. /api/* is proxied by
   │  lubnan.vercel.app│   app/api/[...path]/route.ts
   └─────────┬─────────┘
             │  server-to-server
             ▼
   ┌───────────────────┐
   │  Render           │   the .NET container, from server/Dockerfile
   │  lubnan-api...    │
   └─────────┬─────────┘
             │
             ▼
   ┌───────────────────┐
   │  Neon             │   PostgreSQL 17
   └───────────────────┘
```

The browser only ever talks to Vercel. Render and Neon are never in a URL a
reader can see, which is why there is no CORS policy to maintain and why the
session cookies are first-party.

---

## Before you start: two secrets

Run this twice and keep both. They must be **different** — one key, one job, so
that anything leaking a hash does not also weaken token signing.

```bash
openssl rand -base64 48    # → Auth__SigningKey
openssl rand -base64 48    # → Auth__HashKey
```

No `openssl`? PowerShell:

```powershell
[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Max 256 }))
```

Neither of these ever goes in a file. They are pasted into dashboards.

---

## 1. Neon — the database

1. <https://neon.tech> → sign up with GitHub.
2. **Create project.** Name `lubnan`, Postgres **17**, region **AWS eu-central-1
   (Frankfurt)**.
   *Match this to Render's region in step 2.* Every query crosses that gap, and
   Frankfurt↔Virginia adds ~90 ms to each one.
3. Dashboard → **Connection string** → choose the **Pooled connection**.
   It has `-pooler` in the host. Take that one: the direct string opens a real
   backend per connection, and Render's container will exhaust the free tier's
   limit under any concurrency.

Keep it. This is `ConnectionStrings__Database`.

### The format does not matter

Neon prints a URI:

```
postgresql://lubnan_owner:npg_xxx@ep-xxx-pooler.eu-central-1.aws.neon.tech/neondb?sslmode=require
```

Npgsql natively wants ADO.NET keywords (`Host=…;Database=…;Username=…`) and
**does not parse the URI** — pasting it raw used to fail with *"Format of the
initialization string does not conform to specification starting at index 0"*,
a message that names neither the setting nor the fix.

The app now converts it for you, at startup and in `dotnet ef`. **Paste
whatever Neon gives you.** Percent-encoded passwords (`p%40ss` for `p@ss`) are
decoded too, which otherwise fails as "wrong password" rather than as
"mangled password".

If you prefer the explicit form, Neon's connection panel has a **.NET** option
in its dropdown that prints the keyword format directly. Either works.

**Free tier reality:** 0.5 GB, and the compute autosuspends after ~5 minutes
idle then resumes in about a second. That auto-resume is why this is Neon and
not Supabase — Supabase's free tier *pauses after 7 days* and needs a manual
click to come back, which for a portfolio nobody visits for a fortnight means a
dead site.

---

## 2. Render — the API

> **Three things have to be true before this works, and all three fail
> confusingly.**
>
> `rootDir` scopes `dockerfilePath` and `dockerContext` — they are relative to
> **it**, not to the repository root. Getting that backwards produces
> `lstat /opt/render/project/src/server/server: no such file or directory`,
> where the doubled `server/server` is the entire diagnosis. If you set the
> service up from the dashboard instead, **Root Directory** `server` pairs with
> **Dockerfile Path** `./Dockerfile` — not `./server/Dockerfile`.
>
>
> `render.yaml` must be on the branch Render is building — `main` — or you get
> `failed to read dockerfile: open Dockerfile: no such file or directory`,
> which blames a missing Dockerfile when the real problem is a stale checkout.
>
> And it must be at the **repository root**. Render only looks there by
> default; under `server/` it reports *"Blueprint file render.yaml not found on
> main branch"*, which reads like the file does not exist rather than like it is
> one directory away. It now sits at the root, so leave **Blueprint Path**
> empty.

1. <https://render.com> → sign up with GitHub → authorise the `Lubnan` repo.
2. **New → Blueprint** → pick the repo. It reads `server/render.yaml` and will
   prompt for every value marked `sync: false`.

   Leave **Branch** as `main` and **Blueprint Path** empty.

   *Blueprint, not "Web Service".* A plain web service ignores `render.yaml`
   entirely. If you already made one, delete it and start from Blueprint —
   there is nothing to salvage, and the settings you would have to enter by
   hand are the ones the blueprint already carries.
3. Fill them in:

   | Key | Value |
   |---|---|
   | `ConnectionStrings__Database` | the Neon **pooled** string |
   | `Auth__SigningKey` | first `openssl rand` |
   | `Auth__HashKey` | second `openssl rand` |
   | `Auth__WebBaseUrl` | `https://<your-project>.vercel.app` — see step 4 |
   | `Mail__ApiKey` | from step 3 |
   | `Mail__From` | `Lubnan <onboarding@resend.dev>` to start |
   | `KnownProxies__0` | leave blank for now — step 5 |

4. Region **Frankfurt**, to match Neon.
5. Deploy. First build takes ~5 minutes.

**Free tier reality:** the instance sleeps after 15 minutes idle, and the next
request pays roughly **50 seconds** of cold start. This is the trade you chose
over Cloud Run, and it is the right one for a portfolio: Render's ceiling is
fixed, so a traffic spike — friendly or hostile — degrades the site instead of
generating an invoice.

### Run the migrations

Render's free tier has no shell, so run them from your machine against Neon:

```bash
cd server
ConnectionStrings__Database="<neon pooled string>" \
  dotnet ef database update \
    --project src/Lubnan.Infrastructure \
    --startup-project src/Lubnan.Infrastructure

ConnectionStrings__Database="<neon pooled string>" \
  dotnet run --project src/Lubnan.Api -- seed
```

On PowerShell, set it first:

```powershell
$env:ConnectionStrings__Database = "<neon pooled string>"
dotnet ef database update --project src/Lubnan.Infrastructure --startup-project src/Lubnan.Infrastructure
dotnet run --project src/Lubnan.Api -- seed
```

This stays a manual deploy step on purpose. Migrating at startup means two
replicas racing each other on the same schema.

---

## 3. Resend — email

Without this, nobody can confirm an address or reset a password.

1. <https://resend.com> → sign up. Free tier: 3,000 messages/month, **no card,
   no expiry**.
2. **API Keys → Create.** Permission: **Sending access**, not Full access.
   The application only ever posts a message; full access additionally allows
   managing domains and minting more API keys, so a leaked key would let
   somebody redirect your mail rather than merely send some. Domain is
   *All domains*.

   Copy it once; it is never shown again. This is `Mail__ApiKey`.
3. To start, leave `Mail__From` as `onboarding@resend.dev` — Resend's shared
   sender, which works immediately and only delivers to *your own* signup
   address.
4. When you want mail to reach anyone else, add a domain and let Resend walk
   you through the SPF, DKIM and DMARC records. Until those verify, messages
   from your own domain are accepted by the API and then silently dropped by
   the receiving side — the failure looks like "mail never arrives", not like
   an error.

---

## 4. Vercel — the frontend

1. <https://vercel.com> → sign up with GitHub → **Import** the `Lubnan` repo.
2. **Root Directory: `web`.** Everything else auto-detects.
3. Environment variables:

   | Key | Value |
   |---|---|
   | `API_ORIGIN` | `https://lubnan-api-xxxx.onrender.com` (from Render, no trailing slash) |
   | `WEB_ORIGIN` | `https://<your-project>.vercel.app` |

4. Deploy.
5. Go back to Render and set `Auth__WebBaseUrl` to the Vercel URL. Confirmation
   and reset links are built from it, so a wrong value emails people a live
   token pointing at a dead host.

**Free tier reality:** 100 GB/month bandwidth. At the current ~2 MB per page
that is roughly 45,000 views — before the image work it would have been 2,800.
Hobby is **non-commercial only**; if this ever earns money you need Pro.

---

## 5. The step everyone forgets: `KnownProxies`

Render sits behind its own load balancer, so every request arrives at the API
from an internal address. Until you tell the API which proxy to trust, it
ignores `X-Forwarded-For` entirely and **rate-limits every visitor on earth into
a single bucket** — ten sign-in attempts per five minutes, shared globally.

Find the address in Render's logs after the first real request (look for the
`RemoteIpAddress` on an incoming request), then set `KnownProxies__0` to it and
redeploy.

If that proves awkward, the honest fallback is to trust Render's private range:

```
KnownProxies__0 = 10.0.0.0
```

It is deliberately not set by default. An empty list ignores a spoofable
header; a wrong list trusts one.

---

## 6. Verify the deployment

```bash
curl -i https://<project>.vercel.app/health/ready          # 200
curl -s https://<project>.vercel.app/api/v1/places | head  # eight destinations
curl -i https://<project>.vercel.app/api/v1/me             # 401, and that is correct
```

Then in a browser: register, check the mail arrives, confirm, sign in, save a
place, post to the community, sign out. If the first request takes fifty
seconds, that is the cold start, not a fault.

---

## What is still not done

Stated rather than left to be discovered.

| | Why it matters | What it costs |
|---|---|---|
| **Error tracking** | You will learn about outages from users. Sentry's free tier is 5k events/month | 20 min |
| **Uptime monitoring** | UptimeRobot free, 5-minute checks on `/health/ready`. Also keeps Render awake | 10 min |
| **Backups** | Neon keeps a 7-day history on free, but **an untested restore is not a backup**. Restore into a branch once and look at it | 30 min |
| **Distributed rate limits** | Counters are in this process's memory. Correct for Render's single free instance, wrong the moment there are two | — |
| **Breached-password check** | Have I Been Pwned's k-anonymity API on registration and password change | 1 hour |
| **The flight board** | Scrapes `beirutairport.gov.lb`. Cached and with a fallback, but it will break when they change their HTML | — |
| **`A1.mp4` is 612 MB** | Gitignored, so deploys are safe and the hero falls back to the still. A web encode (~5 MB) is needed before motion ships | 15 min |

---

## Costs, honestly

Zero, for a portfolio, indefinitely — with real limits: a ~50 s cold start on
the first visit after idle, 0.5 GB of database, 100 GB/month of bandwidth, and
3,000 emails.

The first thing to break under real use is Vercel bandwidth, and the first
thing to break under real *scrutiny* is the Hobby licence, which forbids
commercial use. Neither is a code change; both are a plan upgrade.
