# Lubnān

A tourism and culture platform for Lebanon. Next.js 14 App Router, TypeScript,
Tailwind. Trilingual (English / French / Arabic with RTL), light and dark
themes, and a design system where every colour is a token.

```bash
npm install
npm run dev        # http://localhost:3000
npm run build
```

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

`components/ui/PhotoField.tsx` resolves a plate ID to `/img/<ID>.png`, then
`.jpg`, and on phones probes for `<ID>M.png` first. If nothing resolves it
falls back to a tonal field in the palette's photographic range, so the site
works with zero, some, or all plates present. Dropping a correctly named file
into `public/img/` is the whole job.

The hero videos (`public/img/*.mp4`) are gitignored — they are ~500MB each,
past GitHub's per-file limit. The hero shows the still without them.

## Verification

Playwright scripts under `scripts/`. Both are assertions, not screenshots:

```bash
node scripts/verify.mjs verify   # landing page: overflow, contrast, i18n, images
node scripts/routes.mjs          # all 11 routes at 1440 and 412
```

`routes.mjs` screenshots each text block and measures its contrast against the
pixels **actually painted behind it** — computed styles cannot answer this when
the background is a photograph under a gradient, and every serious visual bug
in this project so far has been of that kind.

## Known gaps

- `L8` (Batroun annotated frame) and `S1`–`S8` (People work objects) are not
  yet generated; those slots show the tonal placeholder.
- `/people` deliberately shows each person's **work** rather than a generated
  portrait — inventing a likeness of a real, named, often living person and
  presenting it as photography is not something this project does. Each entry
  keeps a slot for a licensed portrait.
- Plates are full-resolution PNGs. They should be converted to WebP at display
  size before this is deployed anywhere real.
