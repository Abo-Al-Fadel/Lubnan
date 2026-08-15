import { chromium } from 'playwright';
import { mkdirSync } from 'node:fs';
mkdirSync('spot', { recursive: true });
const b = await chromium.launch({ executablePath: process.env.CHROME_PATH || undefined });
for (const [label, vp] of [['desktop',{width:1440,height:900}],['mobile',{width:412,height:915}]]) {
  const page = await (await b.newContext({ viewport: vp })).newPage();
  await page.goto('http://localhost:3000', { waitUntil: 'load' });
  await page.waitForTimeout(2200);
  // seam + sticky navbar
  await page.evaluate(() => window.scrollTo(0, window.innerHeight * 0.86));
  await page.waitForTimeout(900);
  await page.screenshot({ path: `spot/${label}-seam.png` });
  // closing CTA
  await page.evaluate(async () => {
    const step = window.innerHeight * 0.8;
    for (let y = 0; y < document.body.scrollHeight; y += step) {
      window.scrollTo(0, y); await new Promise(r => setTimeout(r, 200));
    }
    const cta = [...document.querySelectorAll('a')].find(a => /plan your trip|خطّط|préparez/i.test(a.textContent||''));
    cta?.scrollIntoView({ block: 'center' });
  });
  await page.waitForTimeout(1400);
  await page.screenshot({ path: `spot/${label}-cta.png` });
  const nav = await page.evaluate(() => {
    const h = document.querySelector('header');
    const cs = getComputedStyle(h);
    return { position: cs.position, top: h.getBoundingClientRect().top, bg: cs.backgroundColor };
  });
  console.log(`${label}: header ${nav.position} top=${Math.round(nav.top)} bg=${nav.bg}`);
}
await b.close();
