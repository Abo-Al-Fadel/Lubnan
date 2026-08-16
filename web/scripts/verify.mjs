import { chromium } from 'playwright';
import { mkdirSync } from 'node:fs';

/**
 * Full acceptance pass for the landing page.
 *
 * Screenshots, a scroll-through video, and hard assertions for the things that
 * have actually broken in this project before: horizontal overflow, invisible
 * images after a cold load, text that fails contrast on a photograph, buttons
 * whose label matches their own fill, and untranslated strings.
 */

const OUT = process.argv[2] ?? 'verify';
mkdirSync(`${OUT}/video`, { recursive: true });

const failures = [];
const fail = (m) => {
  failures.push(m);
  console.log('  FAIL  ' + m);
};
const pass = (m) => console.log('  ok    ' + m);

const browser = await chromium.launch({ executablePath: process.env.CHROME_PATH || undefined });

const VIEWPORTS = [
  ['desktop', { width: 1440, height: 900 }],
  ['mobile', { width: 412, height: 915 }],
];

/** sRGB relative luminance + WCAG contrast. */
function contrast(rgb1, rgb2) {
  const lum = ([r, g, b]) => {
    const f = (c) => {
      const s = c / 255;
      return s <= 0.03928 ? s / 12.92 : Math.pow((s + 0.055) / 1.055, 2.4);
    };
    return 0.2126 * f(r) + 0.7152 * f(g) + 0.0722 * f(b);
  };
  const [a, b] = [lum(rgb1), lum(rgb2)];
  return (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05);
}
const parse = (s) => (s.match(/\d+(\.\d+)?/g) || []).slice(0, 3).map(Number);

for (const [label, viewport] of VIEWPORTS) {
  console.log(`\n=== ${label} ${viewport.width}x${viewport.height} ===`);
  const ctx = await browser.newContext({
    viewport,
    recordVideo: { dir: `${OUT}/video`, size: viewport },
  });
  const page = await ctx.newPage();

  const consoleErrors = [];
  page.on('pageerror', (e) => consoleErrors.push(e.message));

  await page.goto('http://localhost:3000', { waitUntil: 'load' });
  await page.waitForTimeout(2400);

  // 1 — no horizontal overflow
  const overflow = await page.evaluate(
    () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
  );
  overflow > 1 ? fail(`horizontal overflow ${overflow}px`) : pass('no horizontal overflow');

  // 2 — hero wordmark fits inside the frame
  /* Measure the glyphs, not the box. The wordmark's container is deliberately
     116vw so the text can bleed past the container margins; measuring the
     element just re-measures that container and tells you nothing. A Range
     over the text node gives the actual painted extent. */
  const word = await page.evaluate(() => {
    const el = document.querySelector('span.anim-word');
    if (!el || !el.firstChild) return null;
    const range = document.createRange();
    range.selectNodeContents(el);
    const r = range.getBoundingClientRect();
    return { left: r.left, right: r.right, vw: window.innerWidth };
  });
  if (!word) fail('wordmark not found');
  else if (word.left < -2 || word.right > word.vw + 2)
    fail(`wordmark clipped: ${Math.round(word.left)}..${Math.round(word.right)} of ${word.vw}`);
  else pass('wordmark fits the viewport');

  // 3 — cedar must cut the word, not bury it
  const cover = await page.evaluate(() => {
    const w = document.querySelector('span.anim-word')?.getBoundingClientRect();
    const c = document.querySelector('.anim-subject img, .anim-subject svg')?.getBoundingClientRect();
    if (!w || !c) return null;
    return Math.round((100 * (Math.min(w.right, c.right) - Math.max(w.left, c.left))) / w.width);
  });
  /* The cut-out is behind SHOW_HERO_SUBJECT in Hero.tsx. Absent is a valid
     state, so this reports rather than fails — but it still fails loudly if a
     subject *is* rendered and lands wrong, which is the bug this check exists
     for. */
  if (cover === null) pass('hero subject parked (no occlusion to check)');
  else if (cover > 42) fail(`cedar covers ${cover}% of the wordmark (max 42)`);
  else if (cover < 12) fail(`cedar covers only ${cover}% — occlusion does not read`);
  else pass(`cedar covers ${cover}% of the wordmark`);

  /* 3b — type over photography must clear its ACTUAL backdrop.
     Computed styles cannot answer this: the background is a video frame under
     a scrim under a wash, and `backgroundColor` on the <p> is transparent. So
     screenshot the text's box, hand the PNG back to the page, and measure the
     mean luminance the browser really painted. This is the check that would
     have caught the 2.54:1 headline and the standfirst washing out when the
     hero became video. */
  for (const [name, sel] of [
    ['standfirst', '.scrim p.anim-lift'],
    ['stat label', '.scrim .micro'],
  ]) {
    const el = page.locator(sel).first();
    if (!(await el.count())) continue;
    const box = await el.boundingBox();
    const color = await el.evaluate((n) => getComputedStyle(n).color);
    if (!box || box.width < 4 || box.height < 4) continue;

    const shot = await page.screenshot({
      clip: {
        x: Math.max(0, box.x),
        y: Math.max(0, box.y),
        width: Math.min(box.width, viewport.width - box.x),
        height: Math.min(box.height, viewport.height - box.y),
      },
    });

    /* Mean of the *darker half* of the pixels: the glyphs themselves are light
       and would drag a plain mean upward, hiding a bright backdrop. */
    const backdrop = await page.evaluate(async (dataUrl) => {
      const img = new Image();
      img.src = dataUrl;
      await img.decode();
      const c = document.createElement('canvas');
      c.width = img.width;
      c.height = img.height;
      const ctx = c.getContext('2d');
      ctx.drawImage(img, 0, 0);
      const d = ctx.getImageData(0, 0, c.width, c.height).data;
      const px = [];
      for (let i = 0; i < d.length; i += 4) px.push([d[i], d[i + 1], d[i + 2]]);
      px.sort((a, b) => a[0] + a[1] + a[2] - (b[0] + b[1] + b[2]));
      const half = px.slice(0, Math.max(1, Math.floor(px.length / 2)));
      return half
        .reduce((acc, p) => [acc[0] + p[0], acc[1] + p[1], acc[2] + p[2]], [0, 0, 0])
        .map((v) => Math.round(v / half.length));
    }, `data:image/png;base64,${shot.toString('base64')}`);

    const c = contrast(parse(color), backdrop);
    c < 4.5
      ? fail(
          `${name} contrast ${c.toFixed(2)}:1 over its real backdrop rgb(${backdrop.join(',')})`,
        )
      : pass(`${name} contrast ${c.toFixed(2)}:1 over photography`);
  }

  // 4 — scroll, then every image must be visible
  await page.evaluate(async () => {
    const step = window.innerHeight * 0.8;
    for (let y = 0; y < document.body.scrollHeight; y += step) {
      window.scrollTo(0, y);
      await new Promise((r) => setTimeout(r, 240));
    }
  });
  await page.waitForTimeout(1600);

  const imgs = await page.evaluate(() => {
    const list = Array.from(document.querySelectorAll('[role="img"] img'));
    return {
      total: list.length,
      hidden: list
        .filter((i) => Number(getComputedStyle(i).opacity) < 0.9)
        .map((i) => i.getAttribute('src')),
    };
  });
  imgs.hidden.length
    ? fail(`${imgs.hidden.length} image(s) not visible: ${imgs.hidden.join(', ')}`)
    : pass(`all ${imgs.total} images visible after scroll`);

  // 5 — no button whose text matches its own background
  const invisibleButtons = await page.evaluate(() => {
    const out = [];
    document.querySelectorAll('a, button').forEach((el) => {
      const cs = getComputedStyle(el);
      const bg = cs.backgroundColor;
      if (!bg || bg === 'rgba(0, 0, 0, 0)' || bg === 'transparent') return;
      const text = (el.textContent || '').trim();
      if (!text) return;
      out.push({ text: text.slice(0, 28), color: cs.color, bg });
    });
    return out;
  });
  let btnBad = 0;
  for (const b of invisibleButtons) {
    const c = contrast(parse(b.color), parse(b.bg));
    if (c < 3) {
      fail(`button "${b.text}" contrast ${c.toFixed(2)}:1 against its own fill`);
      btnBad++;
    }
  }
  if (!btnBad) pass(`${invisibleButtons.length} filled buttons all clear 3:1`);

  // 6 — page error free
  consoleErrors.length ? fail(`page errors: ${consoleErrors.join(' | ')}`) : pass('no page errors');

  await page.evaluate(() => window.scrollTo(0, 0));
  await page.waitForTimeout(600);
  await page.screenshot({ path: `${OUT}/${label}-hero.png` });
  await page.evaluate(async () => {
    const step = window.innerHeight * 0.8;
    for (let y = 0; y < document.body.scrollHeight; y += step) {
      window.scrollTo(0, y);
      await new Promise((r) => setTimeout(r, 200));
    }
    window.scrollTo(0, 0);
  });
  await page.waitForTimeout(900);
  await page.screenshot({ path: `${OUT}/${label}-full.png`, fullPage: true });

  await ctx.close();
}

// 7 — translation coverage
console.log('\n=== i18n ===');
const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
const page = await ctx.newPage();
/* Not `networkidle`: the hero video streams continuously, so the network never
   goes idle and this timed out at 30s. */
await page.goto('http://localhost:3000', { waitUntil: 'load' });
await page.waitForTimeout(1200);

const ENGLISH_MARKERS = [
  'Two ranges run the length',
  'Walk in under the old ones',
  'Things nobody puts on the map',
  'Plan your trip',
  'Coast, ruin, snowline',
];

for (const [code, expectDir] of [
  ['FR', 'ltr'],
  ['ع', 'rtl'],
]) {
  await page.locator('header button', { hasText: code }).first().click();
  await page.waitForTimeout(700);
  const state = await page.evaluate(() => ({
    dir: document.documentElement.dir,
    lang: document.documentElement.lang,
    body: document.body.innerText,
  }));
  state.dir === expectDir ? pass(`${code}: dir=${state.dir}`) : fail(`${code}: dir=${state.dir}`);
  const leaked = ENGLISH_MARKERS.filter((m) => state.body.includes(m));
  leaked.length
    ? fail(`${code}: untranslated — ${leaked.join(' / ')}`)
    : pass(`${code}: no English copy leaked`);
}
await ctx.close();

await browser.close();

console.log(
  failures.length
    ? `\n${failures.length} FAILURE(S)\n` + failures.map((f) => '  - ' + f).join('\n')
    : '\nALL CHECKS PASSED',
);
process.exitCode = failures.length ? 1 : 0;
