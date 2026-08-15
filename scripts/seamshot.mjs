import { chromium } from 'playwright';

const browser = await chromium.launch({ executablePath: process.env.CHROME_PATH || undefined });

for (const [label, viewport] of [
  ['1920', { width: 1920, height: 925 }],
  ['412', { width: 412, height: 915 }],
]) {
  const ctx = await browser.newContext({ viewport });
  const page = await ctx.newPage();
  await page.goto('http://localhost:3000', { waitUntil: 'load' });
  await page.waitForTimeout(5000);

  const m = await page.evaluate(() => {
    const sec = document.querySelector('section');
    const seam = document.querySelector('.seam');
    return {
      sectionHeight: Math.round(sec.getBoundingClientRect().height),
      viewport: innerHeight,
      seamTopAbs: Math.round(seam.getBoundingClientRect().top + scrollY),
      seamHeight: Math.round(seam.getBoundingClientRect().height),
    };
  });
  console.log(label, JSON.stringify(m));

  // Frame the seam: scroll so it sits mid-screen.
  await page.evaluate((y) => window.scrollTo(0, y), m.seamTopAbs - Math.round(m.viewport * 0.45));
  await page.waitForTimeout(1400);
  await page.screenshot({ path: `ref/seam-${label}.png` });
  await ctx.close();
}
await browser.close();
