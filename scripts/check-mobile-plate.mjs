import { chromium } from 'playwright';

const b = await chromium.launch({ executablePath: process.env.CHROME_PATH || undefined });
const page = await (await b.newContext({ viewport: { width: 375, height: 812 } })).newPage();

const reqs = [];
page.on('response', (r) => {
  if (r.url().includes('/img/')) reqs.push(`${r.status()}  ${r.url().split('/img/')[1]}`);
});

await page.goto('http://localhost:3000', { waitUntil: 'networkidle' });
await page.waitForTimeout(2500);

const hero = await page.evaluate(() => {
  const img = document.querySelector('[role="img"] img');
  if (!img) return { present: false };
  return {
    present: true,
    src: img.getAttribute('src'),
    currentSrc: img.currentSrc.split('/img/')[1],
    natural: `${img.naturalWidth}x${img.naturalHeight}`,
    rendered: `${Math.round(img.getBoundingClientRect().width)}x${Math.round(img.getBoundingClientRect().height)}`,
    matchesMobileMQ: window.matchMedia('(max-width: 767px)').matches,
  };
});

console.log('hero img:', JSON.stringify(hero, null, 2));
console.log('\n/img/ requests:');
reqs.forEach((r) => console.log('  ' + r));
await b.close();
