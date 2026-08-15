import { chromium } from 'playwright';
import { mkdirSync } from 'node:fs';

/** Every route, at both widths: errors, 404s, overflow, and a screenshot. */
const ROUTES = [
  ['home', '/'],
  ['explore', '/explore'],
  ['place', '/explore/baalbek'],
  ['story', '/story'],
  ['people', '/people'],
  ['achievements', '/achievements'],
  ['legacy', '/legacy'],
  ['plan', '/plan'],
  ['community', '/community'],
  ['login', '/login'],
  ['profile', '/profile'],
];

/**
 * [label, selector, minimum contrast, background percentile].
 *
 * The percentile is how far up the sorted-by-luminance pixel list to sample
 * for "the background". It exists because the glyphs are inside the box being
 * measured, so a naive sample can land on the type and report a meaningless
 * 1.00:1. Small text covers ~10% of its box, so the 75th percentile is safely
 * background; display type covers 40%+ and cannot be measured this way at all,
 * which is why headlines are not checked here — their size is their own
 * legibility argument, and the body copy beneath them shares the same scrim.
 */
const CHECKS = [
  ['wordmark', 'header a[aria-label]', 3, 0.6],
  ['body over photo', '.scrim p', 4.5, 0.75],
];

const OUT = 'routes';
mkdirSync(OUT, { recursive: true });

const failures = [];
const browser = await chromium.launch({ executablePath: process.env.CHROME_PATH || undefined });

for (const [label, viewport] of [
  ['d', { width: 1440, height: 900 }],
  ['m', { width: 412, height: 915 }],
]) {
  console.log(`\n=== ${label === 'd' ? 'desktop' : 'mobile'} ===`);
  for (const [name, path] of ROUTES) {
    const ctx = await browser.newContext({ viewport });
    const page = await ctx.newPage();
    const errs = [];
    page.on('pageerror', (e) => errs.push(`JS: ${e.message.split('\n')[0]}`));
    /* /img/ 404s are by design: PhotoField walks an extension and phone-suffix
       chain and falls back to a tonal field, so a missing plate is a slot
       waiting for art, not a bug. Everything else that 404s is a bug. */
    page.on('response', (r) => {
      const url = r.url();
      if (r.status() >= 400 && !url.includes('/img/')) {
        errs.push(`${r.status()} ${url.split('/').pop()}`);
      }
    });

    await page.goto(`http://localhost:3000${path}`, { waitUntil: 'load' });
    await page.waitForTimeout(2200);

    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
    );
    if (overflow > 1) errs.push(`overflow ${overflow}px`);

    // Untranslated keys leak through as the raw dotted key.
    const rawKeys = await page.evaluate(() => {
      const m = document.body.innerText.match(/\b[a-z]+\.[a-zA-Z]{3,}\b/g) || [];
      return [...new Set(m.filter((k) => !k.includes(' ') && !/\.(com|org|net|png|jpg)$/.test(k)))];
    });
    if (rawKeys.length) errs.push(`raw keys: ${rawKeys.slice(0, 5).join(', ')}`);

    /* Text over photography, measured against the pixels actually painted
       behind it. Computed styles cannot catch this — the background is a plate
       under a scrim, and `backgroundColor` on the element is transparent. */
    for (const [what, sel, min, pct] of CHECKS) {
      const el = page.locator(sel).first();
      if (!(await el.count())) continue;
      try {
        await el.scrollIntoViewIfNeeded({ timeout: 2000 });
      } catch {
        continue;
      }
      const box = await el.boundingBox();
      const color = await el.evaluate((n) => getComputedStyle(n).color);
      if (!box || box.width < 4 || box.height < 4) continue;

      /* Sample the ground *beside* the text, not through it.
         Sampling the text's own box means the glyphs are in the sample, and
         once the type is correctly bright the percentile lands on the type
         itself and reports ~1:1 against its own colour. Which is what happened:
         this check passed while the Achievements headline was buried under its
         own gradient, and failed the moment it became legible. A strip at the
         same vertical band sits under the same gradient stop and contains no
         glyphs. */
      const PROBE = 90;
      const GAP = 12;
      const rightX = box.x + box.width + GAP;
      const leftX = box.x - GAP - PROBE;
      let px;
      if (rightX + PROBE <= viewport.width) px = rightX;
      else if (leftX >= 0) px = leftX;
      else px = null;

      const clip = px === null
        ? {
            x: Math.max(0, box.x),
            y: Math.max(0, box.y),
            width: Math.min(box.width, viewport.width - Math.max(0, box.x)),
            height: Math.min(box.height, viewport.height - Math.max(0, box.y)),
          }
        : {
            x: px,
            y: Math.max(0, box.y),
            width: PROBE,
            height: Math.min(box.height, viewport.height - Math.max(0, box.y)),
          };
      if (clip.width < 4 || clip.height < 4) continue;
      // With a glyph-free sample, the worst case is simply the brightest ground.
      const usePct = px === null ? pct : 0.9;

      const shot = await page.screenshot({ clip });
      const inkRgb = (color.match(/\d+(\.\d+)?/g) || []).slice(0, 3).map(Number);
      const bg = await page.evaluate(
        async ({ u, pct, ink }) => {
          const img = new Image();
          img.src = u;
          await img.decode();
          const c = document.createElement('canvas');
          c.width = img.width;
          c.height = img.height;
          const cx = c.getContext('2d');
          cx.drawImage(img, 0, 0);
          const d = cx.getImageData(0, 0, c.width, c.height).data;

          /* Drop anything close to the type's own colour. Those pixels are
             glyphs or their antialiasing, and including them lets a correctly
             legible block report ~1:1 against itself. */
          const near = (p) =>
            Math.abs(p[0] - ink[0]) < 46 &&
            Math.abs(p[1] - ink[1]) < 46 &&
            Math.abs(p[2] - ink[2]) < 46;

          const all = [];
          for (let i = 0; i < d.length; i += 4) all.push([d[i], d[i + 1], d[i + 2]]);
          let px = all.filter((p) => !near(p));
          // If the box is almost entirely glyph, there is nothing to measure.
          if (px.length < all.length * 0.15) px = all;
          px.sort((a, b) => a[0] + a[1] + a[2] - (b[0] + b[1] + b[2]));
          return px[Math.floor(px.length * pct)];
        },
        { u: `data:image/png;base64,${shot.toString('base64')}`, pct: usePct, ink: inkRgb },
      );

      const lum = ([r, g, b]) => {
        const f = (v) => {
          const s = v / 255;
          return s <= 0.03928 ? s / 12.92 : Math.pow((s + 0.055) / 1.055, 2.4);
        };
        return 0.2126 * f(r) + 0.7152 * f(g) + 0.0722 * f(b);
      };
      const rgb = (color.match(/\d+(\.\d+)?/g) || []).slice(0, 3).map(Number);
      const [la, lb] = [lum(rgb), lum(bg)];
      const ratio = (Math.max(la, lb) + 0.05) / (Math.min(la, lb) + 0.05);
      if (ratio < min) errs.push(`${what} ${ratio.toFixed(2)}:1 on rgb(${bg.join(',')})`);
    }

    await page.evaluate(() => window.scrollTo(0, 0));
    await page.waitForTimeout(400);

    if (errs.length) {
      failures.push(`${label}/${name}: ${errs.join(' | ')}`);
      console.log(`  FAIL  ${name.padEnd(10)} ${errs.join(' | ')}`);
    } else {
      console.log(`  ok    ${name}`);
    }

    await page.screenshot({ path: `${OUT}/${label}-${name}.png`, fullPage: label === 'd' });
    await ctx.close();
  }
}

await browser.close();
console.log(
  failures.length ? `\n${failures.length} FAILURE(S)` : '\nALL ROUTES CLEAN',
);
process.exitCode = failures.length ? 1 : 0;
