import { chromium } from 'playwright';

const browser = await chromium.launch({ executablePath: process.env.CHROME_PATH || undefined });

for (const [label, viewport] of [
  ['desktop', { width: 1440, height: 900 }],
  ['mobile', { width: 412, height: 915 }],
]) {
  const ctx = await browser.newContext({ viewport });
  const page = await ctx.newPage();
  await page.goto('http://localhost:3000', { waitUntil: 'load' });
  await page.waitForTimeout(6000);

  const v = await page.evaluate(() => {
    const el = document.querySelector('section video');
    if (!el) return null;
    return {
      src: el.getAttribute('src'),
      readyState: el.readyState,
      paused: el.paused,
      currentTime: Number(el.currentTime.toFixed(2)),
      opacity: getComputedStyle(el).opacity,
      w: el.videoWidth,
      h: el.videoHeight,
    };
  });
  console.log(label, JSON.stringify(v));
  await ctx.close();
}
await browser.close();
