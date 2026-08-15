# Drop images here

Name each file after its plate ID from [`docs/image-prompts.md`](../../docs/image-prompts.md) and it
appears on the site. No code changes, no imports, no registration step.

```
public/img/A1.png      ← Cedar hero
public/img/A2.png      ← Raouche hero
public/img/A3.png      ← Limestone hero
public/img/C3r.png     ← Jeita Grotto, card rail
public/img/D2.png      ← Marc H. community post
public/img/E1.png      ← closing coast road
```

## Rules

- **`.png` is tried first, then `.jpg`.** Either works, so a mixed folder is fine.
- **Case-sensitive.** `C3r.png` — not `c3r.png` or `C3R.png`. The lookup is exactly
  `/img/<PLATE>.<ext>`.
- **Anything missing falls back** to the tonal placeholder, so the site never looks broken
  half-finished. Add plates one at a time and watch each land.
- **Press `I` on the site** and every slot prints its plate ID over itself, so you can match file to
  position without consulting the list.

## Phone crops — add `-m`

```
public/img/A1.png      ← 16:9 landscape
public/img/A1-m.png    ← 9:16 phone crop, used under 768px
```

Optional. Without it phones crop the landscape file centrally, which is fine for most plates. Only
the heroes and the closing plate have a phone variant in the prompt list.

This is resolved by *probing*: under 768px the browser loads `<plate>-m` off-DOM and swaps it in only
if it actually exists. An earlier version expressed it as `<source srcSet="…-m.png, ….png">`, which
cannot work — `srcset` picks its candidate before fetching, so a 404 never falls back, and a phone
missing the `-m` file would have shown the placeholder while the landscape plate sat unused beside
it. If a plate crops badly, `PhotoField` also takes `objectPosition="50% 30%"` to hold a horizon.

## Cut-outs — B1, B2, B3

Transparent PNGs that composite *in front* of the wordmark. Same folder, same naming:

```
public/img/B1.png      ← cedar        (Cedar variation)
public/img/B2.png      ← sea stacks   (Raouche variation)
public/img/B3.png      ← columns      (Limestone variation)
```

`public/img/cutouts/` also works if you prefer them separated — both locations are tried. When no
cut-out is present the hero falls back to the drawn SVG silhouette in
[`components/ui/Subjects.tsx`](../../components/ui/Subjects.tsx), so the page never breaks.

Two things matter for these:

- **Real alpha.** Corner pixels must be fully transparent, not white. A white box around the subject
  will show as a rectangle sitting on the photograph.
- **Subject touches the bottom edge** of its canvas. The cut-out is anchored to the ground line, so
  transparent padding under the trunk lifts it off the floor and it reads as floating.

## Sizes

Export at the resolution in the prompt list (hero 2688×1512, cards 1600×2000, posts 1600×1600).
PNG is lossless and large — run the photographic plates through a compressor before committing, or
export those as JPEG quality 80 and keep PNG for the cut-outs where the alpha matters. The hero is
the first thing that has to paint.
