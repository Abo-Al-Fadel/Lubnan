import { chromium } from 'playwright';

const browser = await chromium.launch({ executablePath: process.env.CHROME_PATH || undefined });
const ctx = await browser.newContext({ viewport: { width: 1600, height: 1000 } });
const page = await ctx.newPage();
await page.goto('http://localhost:3000/explore', { waitUntil: 'load' });
await page.waitForTimeout(4500);
const box = await page.locator('svg[role="img"]').first().boundingBox();
if (box) {
  await page.screenshot({
    path: 'ref/map.png',
    clip: {
      x: Math.max(0, box.x - 30),
      y: Math.max(0, box.y - 10),
      width: Math.min(box.width + 200, 1600 - box.x),
      height: Math.min(box.height + 20, 1000 - box.y),
    },
  });
  console.log('wrote ref/map.png');
} else {
  console.log('map not found');
}
await browser.close();
