import { chromium } from 'playwright';

const b = await chromium.launch({ executablePath: process.env.CHROME_PATH });
const ctx = await b.newContext({ viewport: { width: 1440, height: 900 } });
const page = await ctx.newPage();

const requested = [];
page.on('response', (r) => {
  if (r.url().includes('/img/')) requested.push(`${r.status()}  ${r.url().split('/img/')[1]}`);
});

await page.goto('http://localhost:3000', { waitUntil: 'networkidle' });
/* Long enough for the extension chain to walk png -> jpg -> placeholder and
   for React to commit each step. A short wait races the fallback. */
await page.waitForTimeout(1800);

const hero = await page.evaluate(() => {
  const img = document.querySelector('[role="img"] img');
  if (!img) return { present: false };
  return {
    present: true,
    src: img.getAttribute('src'),
    currentSrc: img.currentSrc.split('/img/')[1] || img.currentSrc,
    naturalW: img.naturalWidth,
    naturalH: img.naturalHeight,
    renderedW: Math.round(img.getBoundingClientRect().width),
    renderedH: Math.round(img.getBoundingClientRect().height),
  };
});

console.log('hero <img>:', JSON.stringify(hero, null, 2));
console.log('\n/img/ requests:');
requested.forEach((r) => console.log('  ' + r));

// Switch to a palette whose plate does NOT exist, to prove the fallback
await page.keyboard.press('2');
await page.waitForTimeout(700);
const fallback = await page.evaluate(() => {
  const field = document.querySelector('[role="img"]');
  return { hasImg: !!field.querySelector('img'), bg: getComputedStyle(field).backgroundImage.slice(0, 44) };
});
console.log('\nRaouche (no A2.jpg on disk):', JSON.stringify(fallback));

await b.close();
