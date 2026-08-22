# Lubnān

A tourism and culture platform for Lebanon. Next.js 14 App Router, TypeScript,
Tailwind. Trilingual (English / French / Arabic with RTL), light and dark
themes, and a design system where every colour is a token.

```bash
npm install
npm run dev        # http://localhost:3000
npm run build
```

The API lives beside it in [../server/](../server/) — .NET 9, PostgreSQL,
vertical slices — and is deployed separately. It has its own
[README](../server/README.md). The web app does not call it yet: the frontend
still reads `data/*.json`, and the swap happens per feature rather than all at
once.

## Routes

| Route | What it is |
| --- | --- |
| `/` | Landing page — type-behind-subject hero, secrets, mosaic, community, CTA |
| `/explore` | Drawn map of Lebanon, region picker, filtered destination mosaic |
| `/explore/[place]` | Per-destination page with annotated callouts on the photograph |
| `/story` | Seven centuries as a horizontal scroll, one era per screen |
| `/people` | Eight Lebanese figures, indexed by their work |
| `/achievements` | A ledger of nine claims, from the alphabet to the UDHR |
| `/legacy` | The six UNESCO sites as a technical archive with drawn diagrams |
| `/plan` | Climate band, numbered accordion, transfer sheet, itinerary builder |
| `/community` | Single-column social feed |
| `/login`, `/profile` | Account pages, in the Raouche palette |

## Design system

Everything visual comes from CSS custom properties in `app/globals.css`,
scoped by `[data-palette]` and `[data-theme]`. **A raw hex value in a component
is a bug.**

Three palettes:

- **Cedar** — snow ground, near-black ink. Landing, Explore, Place, Community,
  People, Achievements.
- **Gentle** — pale ground for sustained reading. Legacy, Plan. Its scrim is
  *light* and its hero ink is *dark*, inverting the usual relationship.
- **Raouche** — deep teal ground. Login and Profile, so the account area reads
  as a distinct room.

Key tokens: `--ground` / `--band` (surfaces), `--ink` / `--ink-dim` /
`--ink-ghost` (type on those surfaces), `--hero-ink` and friends (type on a
*photograph*, which is a different problem), `--scrim` / `--scrim-strong`
(what makes that legible), `--accent`, `--btn-solid`.

### Images

Plates are grouped by series letter, so a folder listing answers "what is the
J series" without opening anything:

```
public/
  img/A/  A1.png A1M.png      hero plate and its phone crop
  img/J/  J1..J8.png          place heroes
  img/K/  K1..K8.png          place cut-outs (alpha)
  img/S/  S1..S8.png          People, the work
  …
  vid/    A1.mp4 A2.mp4       gitignored, ~500MB each
  brand/  favicon.png
```

Every path is resolved through `lib/plates.ts` — nothing composes an asset URL
inline, so the layout on disk can change again without a search across the
codebase. `PhotoField` tries `.png` then `.jpg`, probes for a phone crop
(`<ID>M`, `-m`, `_m`, `m`) on narrow viewports, and falls back to a tonal field
in the palette's photographic range when nothing resolves. The site works with
zero, some, or all plates present.

The hero videos are gitignored: past GitHub's 100MB per-file limit. The hero
layers video *over* the still and only reveals it once it can play, so with no
file present the photograph simply stays.

## Verification

Three Playwright suites under `scripts/`. All assert; none are just screenshots.

```bash
node scripts/verify.mjs verify   # landing page: overflow, contrast, i18n, images
node scripts/routes.mjs          # 11 routes at 1440 and 412, contrast over photography
node scripts/crawl.mjs           # 18 routes at 1440 and 390, pressing every control
node scripts/build-border.mjs    # regenerate data/lebanon-border.ts from GeoJSON
```

Two of these are worth knowing about:

**`routes.mjs`** screenshots each text block and measures contrast against the
pixels *actually painted behind it*. Computed styles cannot answer this when
the background is a photograph under a gradient, and most of the real visual
bugs in this project have been of that kind. It samples beside the text and
filters glyph-coloured pixels, because sampling through the text reports type
against itself.

**`crawl.mjs`** walks every route and **presses every visible control**, then
reports errors, dead links, links to unknown routes, missing accessible names,
`target="_blank"` without `noopener`, tap targets under 24px, heading structure
and horizontal overflow. Its first run found 239 problems.

## Known gaps

- **Password reset exists on the server and has no front end.** The API half is
  finished and correct — `POST /api/v1/auth/forgot-password` and
  `/api/v1/auth/reset-password`, single-use hashed tokens, one-hour expiry, and
  every session ended on success. What is missing is entirely on this side: no
  `/reset-password` page, and no "forgot password?" link on `/login`. So nobody
  can enter the flow through the site, and anyone who called the endpoint
  directly would be mailed a link to a 404. `app/robots.ts` already disallows
  `/reset-password`, which is where the intent is recorded.
  Finishing it means a page that reads `?token=`, posts it with a new password,
  and handles `token.invalid` and `password.breached`; a link on `/login`; and
  translation keys in all three of `en`/`fr`/`ar`, since a half-translated
  account page is worse than none. Until then the endpoints are reachable but
  unreferenced.
- `/people` deliberately shows each person's **work** rather than a generated
  portrait. Inventing a likeness of a real, named, often living person and
  presenting it as photography is not something this project does. Six entries
  use licence-checked Wikimedia images (see [CREDITS.md](CREDITS.md)); Elie
  Saab and Taleb use object plates because no clearly free photograph exists.
- Story, People, Achievements, Legacy, Community and Profile open on type
  rather than a banner. Plates `T1`–`T6` exist for banners if wanted.
- Plates are full-resolution PNGs, ~233MB total. They should be converted to
  WebP at display size before this is deployed anywhere real.
- `/plan` loads today's BEY board from `/api/v1/flights` (the airport's public
  page, cached a few minutes). If that feed is down, `data/flights.ts` is the
  fallback and the board says so.
