import { chromium } from 'playwright';

const browser = await chromium.launch({ executablePath: process.env.CHROME_PATH || undefined });
const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
const page = await ctx.newPage();

const bad = [];
page.on('response', (r) => {
  if (r.status() >= 400) bad.push(`${r.status()} ${r.url()}`);
});
page.on('pageerror', (e) => bad.push(`PAGEERROR ${e.message.split('\n')[0]}`));
page.on('console', (m) => {
  if (m.type() === 'error') bad.push(`CONSOLE ${m.text().slice(0, 160)}`);
});

await page.goto('http://localhost:3000', { waitUntil: 'load' });
await page.waitForTimeout(4000);

console.log(bad.length ? bad.join('\n') : 'no failed requests / errors');
await page.screenshot({ path: 'ref/diag.png' });
await browser.close();
