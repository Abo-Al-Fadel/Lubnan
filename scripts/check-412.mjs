import { chromium } from 'playwright';
const b = await chromium.launch({ executablePath: process.env.CHROME_PATH || undefined });
for (const w of [375, 412, 430]) {
  const page = await (await b.newContext({ viewport: { width: w, height: 915 } })).newPage();
  await page.goto('http://localhost:3000', { waitUntil: 'networkidle' });
  await page.waitForTimeout(2200);
  const r = await page.evaluate(() => {
    const img = document.querySelector('[role="img"] img');
    const word = document.querySelector('span[aria-hidden="true"].pointer-events-none');
    const cut = document.querySelector('section img[src*="/img/B"]');
    const wr = word?.getBoundingClientRect();
    const cr = cut?.getBoundingClientRect();
    return {
      mq: window.matchMedia('(max-width: 767px)').matches,
      heroSrc: img?.currentSrc.split('/img/')[1],
      wordBox: wr ? `${Math.round(wr.left)}..${Math.round(wr.right)} w${Math.round(wr.width)}` : null,
      cutBox: cr ? `${Math.round(cr.left)}..${Math.round(cr.right)} w${Math.round(cr.width)}` : null,
      coverPct: wr && cr ? Math.round(100 * (Math.min(wr.right,cr.right)-Math.max(wr.left,cr.left)) / wr.width) : null,
    };
  });
  console.log(`[${w}px] mq=${r.mq} hero=${r.heroSrc}`);
  console.log(`        word ${r.wordBox}`);
  console.log(`        cut  ${r.cutBox}  covers ${r.coverPct}% of the word`);
}
await b.close();
