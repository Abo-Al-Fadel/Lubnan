import { chromium } from 'playwright';

/**
 * Reproduces the reported bug: on a cold load the plates were invisible until
 * a variation switch forced a client-side remount. Runs twice — once with an
 * empty cache, once warm — because the race only shows when the image
 * completes before hydration, which a warm cache makes far more likely.
 */
const b = await chromium.launch({ executablePath: process.env.CHROME_PATH || undefined });

for (const pass of ['cold', 'warm (cached)']) {
  const ctx = await b.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  if (pass.startsWith('warm')) {
    await page.goto('http://localhost:3000', { waitUntil: 'networkidle' });
    await page.waitForTimeout(1200);
  }

  await page.goto('http://localhost:3000', { waitUntil: 'load' });
  await page.waitForTimeout(1800);

  const r = await page.evaluate(() => {
    const imgs = Array.from(document.querySelectorAll('[role="img"] img'));
    const cutout = document.querySelector('section img[src*="B1"]');
    return {
      total: imgs.length,
      visible: imgs.filter((i) => Number(getComputedStyle(i).opacity) > 0.9).length,
      invisible: imgs
        .filter((i) => Number(getComputedStyle(i).opacity) < 0.9)
        .map((i) => i.getAttribute('src')),
      heroComplete: imgs[0]?.complete,
      heroNatural: imgs[0] ? `${imgs[0].naturalWidth}x${imgs[0].naturalHeight}` : null,
      cutoutPresent: Boolean(cutout),
    };
  });

  console.log(`\n[${pass}]`);
  console.log(`  imgs in DOM      : ${r.total}`);
  console.log(`  visible (op>0.9) : ${r.visible}`);
  console.log(`  hero complete    : ${r.heroComplete}  natural ${r.heroNatural}`);
  console.log(`  cut-out rendered : ${r.cutoutPresent}`);
  if (r.invisible.length) console.log('  not yet decoded  :', r.invisible.join(', '));

  /* Below-fold slots are loading="lazy" and legitimately have not fetched yet.
     Scroll the page and they must all resolve. */
  await page.evaluate(async () => {
    const step = window.innerHeight * 0.8;
    for (let y = 0; y < document.body.scrollHeight; y += step) {
      window.scrollTo(0, y);
      await new Promise((res) => setTimeout(res, 200));
    }
  });
  await page.waitForTimeout(1500);

  const after = await page.evaluate(() => {
    const imgs = Array.from(document.querySelectorAll('[role="img"] img'));
    return {
      total: imgs.length,
      visible: imgs.filter((i) => Number(getComputedStyle(i).opacity) > 0.9).length,
      stuck: imgs
        .filter((i) => Number(getComputedStyle(i).opacity) < 0.9)
        .map((i) => `${i.getAttribute('src')} complete=${i.complete} w=${i.naturalWidth}`),
    };
  });
  console.log(`  after scroll     : ${after.visible}/${after.total} visible`);
  if (after.stuck.length) console.log('  STUCK            :', after.stuck.join(' | '));

  await ctx.close();
}

await b.close();
