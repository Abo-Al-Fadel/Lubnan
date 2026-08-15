import { chromium } from 'playwright';
import { mkdirSync } from 'node:fs';

const OUT = process.argv[2] ?? 'shots';
const BASE = 'http://localhost:3000';
const VARIATIONS = ['Cedar', 'Raouche'];

mkdirSync(OUT, { recursive: true });

/* Uses Playwright's own Chromium. CHROME_PATH overrides it, which is only
   needed if `npx playwright install chromium` has not run. */
const browser = await chromium.launch({
  executablePath: process.env.CHROME_PATH || undefined,
});
const problems = [];

for (const [label, size] of [
  ['desktop', { width: 1440, height: 900 }],
  ['mobile', { width: 375, height: 812 }],
]) {
  const ctx = await browser.newContext({
    viewport: size,
    deviceScaleFactor: 1,
    reducedMotion: 'no-preference',
  });
  const page = await ctx.newPage();

  page.on('console', (m) => {
    if (m.type() === 'error') problems.push(`[console ${label}] ${m.text()}`);
  });
  page.on('pageerror', (e) => problems.push(`[pageerror ${label}] ${e.message}`));

  await page.goto(BASE, { waitUntil: 'networkidle' });

  for (let i = 0; i < VARIATIONS.length; i++) {
    const name = VARIATIONS[i];
    await page.keyboard.press(String(i + 1));
    await page.waitForTimeout(1600);

    // Above the fold
    await page.screenshot({ path: `${OUT}/${label}-${i + 1}-${name}-hero.png` });

    /* Scroll the whole page before the full-page shot. Without this the
       capture lies: lazy images below the fold never request, and scroll
       reveals never intersect, so the screenshot shows placeholders and blank
       headings that a real visitor would never see. */
    await page.evaluate(async () => {
      const step = window.innerHeight * 0.8;
      for (let y = 0; y < document.body.scrollHeight; y += step) {
        window.scrollTo(0, y);
        await new Promise((r) => setTimeout(r, 220));
      }
      window.scrollTo(0, document.body.scrollHeight);
      await new Promise((r) => setTimeout(r, 700));
    });
    await page.evaluate(() => window.scrollTo(0, 0));
    await page.waitForTimeout(500);

    // Whole page
    await page.screenshot({
      path: `${OUT}/${label}-${i + 1}-${name}-full.png`,
      fullPage: true,
    });

    // Horizontal overflow is a hard floor, not a wish list
    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
    );
    if (overflow > 1) problems.push(`[overflow ${label} ${name}] ${overflow}px`);

    await page.evaluate(() => window.scrollTo(0, 0));
  }

  // Search overlay, captured once per breakpoint
  await page.keyboard.press('1');
  await page.waitForTimeout(400);
  /* Both a desktop and a mobile trigger exist in the DOM at all times; only
     one is visible per breakpoint, so the locator has to filter on that. */
  /* Desktop shows a labelled field; mobile shows an icon-only button with an
     aria-label. Match on accessible name so both resolve. */
  const trigger = page
    .locator('header button:visible')
    .filter({ has: page.locator('svg') })
    .first();
  await trigger.click();
  await page.waitForTimeout(350);
  await page.fill('input[type="search"]', 'cedar');
  await page.waitForTimeout(350);
  await page.screenshot({ path: `${OUT}/${label}-search.png` });

  await ctx.close();
}

await browser.close();

if (problems.length) {
  console.log('PROBLEMS:');
  for (const p of problems) console.log('  ' + p);
} else {
  console.log('No console errors, no page errors, no horizontal overflow.');
}
