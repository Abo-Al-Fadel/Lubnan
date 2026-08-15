import { chromium } from 'playwright';

/* Derive image ratios from the real rendered boxes rather than from
   photographic convention. Every [role="img"] on the page is a PhotoField. */

const browser = await chromium.launch({ executablePath: process.env.CHROME_PATH || undefined });
const out = {};

for (const [label, viewport] of [
  ['desktop-1920', { width: 1920, height: 1080 }],
  ['desktop-1440', { width: 1440, height: 900 }],
  ['mobile-375', { width: 375, height: 812 }],
]) {
  const ctx = await browser.newContext({ viewport, reducedMotion: 'reduce' });
  const page = await ctx.newPage();
  await page.goto('http://localhost:3000', { waitUntil: 'networkidle' });

  for (let v = 1; v <= 2; v++) {
    await page.keyboard.press(String(v));
    await page.waitForTimeout(500);
    const name = ['Cedar', 'Raouche'][v - 1];

    const boxes = await page.evaluate(() =>
      Array.from(document.querySelectorAll('[role="img"]')).map((el) => {
        const r = el.getBoundingClientRect();
        return {
          label: (el.getAttribute('aria-label') || '').slice(0, 48),
          w: Math.round(r.width),
          h: Math.round(r.height),
          ratio: +(r.width / r.height).toFixed(2),
        };
      }),
    );
    out[`${label} · ${name}`] = boxes;
  }
  await ctx.close();
}

await browser.close();

for (const [k, boxes] of Object.entries(out)) {
  console.log('\n=== ' + k + ' ===');
  boxes.forEach((b, i) =>
    console.log(
      `  ${String(i).padStart(2)}  ${String(b.w).padStart(5)}x${String(b.h).padStart(5)}  ratio ${String(b.ratio).padStart(5)}  ${b.label}`,
    ),
  );
}
