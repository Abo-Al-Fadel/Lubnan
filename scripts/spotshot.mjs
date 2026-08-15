import { chromium } from 'playwright';

/** [name, path, selector to frame] */
const TARGETS = [
  ['plan-banner', '/plan', 'section:first-of-type'],
  ['ach-close', '/achievements', 'main section:last-of-type'],
  ['cta', '/', 'main > div > div:last-of-type section'],
  ['secrets', '/', 'main section:nth-of-type(2)'],
];

const browser = await chromium.launch({ executablePath: process.env.CHROME_PATH || undefined });

for (const [name, path, sel] of TARGETS) {
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();
  await page.goto(`http://localhost:3000${path}`, { waitUntil: 'load' });
  await page.waitForTimeout(2500);

  const el = page.locator(sel).last();
  if (await el.count()) {
    await el.scrollIntoViewIfNeeded();
    await page.waitForTimeout(1600);
    const box = await el.boundingBox();
    if (box) {
      await page.screenshot({
        path: `ref/${name}.png`,
        clip: {
          x: 0,
          y: Math.max(0, box.y - 40),
          width: 1440,
          height: Math.min(900, 900 - Math.max(0, box.y - 40)),
        },
      });
      console.log(`wrote ref/${name}.png`);
    }
  } else {
    console.log(`${name}: selector missed`);
  }
  await ctx.close();
}
await browser.close();
