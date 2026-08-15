# Lubnān — image plate list

Complete, copy-and-run prompts with a palette selector and copy buttons:
<https://claude.ai/code/artifact/191d62a4-a109-42b2-bac4-0602691aad5b>

Each prompt on that page is one self-contained block — house style, depth construction, grade,
exclusions and `--ar` already assembled. Nothing to prepend.

## Ratios are measured, not assumed

From the rendered boxes in a real browser via `node scripts/measure.mjs`. Re-run it whenever a
section layout changes.

| Slot | 1920 | 1440 | 375 | Generate at |
|---|---|---|---|---|
| Hero, desktop | 1.78 | 1.60 | — | **16:9** |
| Hero, mobile | — | — | 0.46 | **9:16** |
| Destination card | 0.80 | 0.80 | 0.80 | **4:5** |
| Mosaic tile 1 | 2.67 | 1.97 | 1.33 | **16:9** |
| Mosaic tiles 2–3 | 1.89 | 1.39 | 1.60 | **16:9** |
| Mosaic tile 4 | 1.51 | 1.11 | 0.80 | **4:3** |
| Mosaic tile 5 | 1.12 | 0.81 | 0.80 | **1:1** |
| Community post | 1.00 | 1.00 | 1.00 | **1:1** |
| Closing, desktop | 2.47 | 2.16 | — | **5:2** |
| Closing, mobile | — | — | 0.64 | **2:3** |

The hero is a full-bleed `100svh` section, so at 1920×1080 it measures exactly 16:9. Where a slot
swings across viewports the widest number governs, and the subject wants to sit inside the central
60% so the crop never eats it.

## Depth in the hero

The hero carries the first impression and is the hardest plate, because a cut-out subject gets
composited on top of it. A flat backdrop makes that subject look pasted on. Every hero prompt
therefore specifies **three named depth planes** — a close foreground entering the lower corners and
slightly soft, a midground carrying the terrain, a far background of receding forms — plus:

- **Aerial perspective.** Each plane paler and lower in contrast than the one in front. This single
  relationship is what makes a photograph feel deep, and it is the first thing a generator drops if
  you do not ask for it.
- **An open but not empty centre.** The wordmark spans it and the cut-out lands in it, so nothing
  structural can sit there — but it still needs a receding ground plane, so the subject has floor to
  stand on rather than sky to float against.
- **Matched light direction between plate and cut-out.** Each B prompt repeats its A prompt's light.
  A cedar lit from the left against a valley lit from the right reads as fake instantly, and grading
  will not fix it.

## How many plates you actually need

Only the ones for the palette you pick. The three variations use different section layouts, so the
count differs:

| Palette | Hero | Cut-out | Destinations | Community | Closing | **Total** |
|---|---|---|---|---|---|---|
| Cedar | 2 | 1 | 5 landscape (mosaic) | 4 | 2 | **14** |
| Raouche | 2 | 1 | 8 portrait (rail) | 4 | 2 | **17** |
| Limestone | 2 | 1 | 8 portrait (rail) | 4 | 2 | **17** |

## Dropping them in

Name files by plate ID into [`public/img/`](../public/img/) — `A1.png`, `C3r.png`, `D2.png`.
`.png` is tried first, then `.jpg`. Missing plates fall back to the tonal placeholder, so the site
works with any subset present. See [`public/img/README.md`](../public/img/README.md) for the full
contract.
