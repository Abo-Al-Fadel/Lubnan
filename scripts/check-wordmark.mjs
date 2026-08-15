import { chromium } from 'playwright';

const b = await chromium.launch({ executablePath: process.env.CHROME_PATH || undefined });

for (const [label, viewport] of [
  ['desktop', { width: 1440, height: 900 }],
  ['mobile', { width: 375, height: 812 }],
]) {
  const page = await (await b.newContext({ viewport })).newPage();
  await page.goto('http://localhost:3000', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1400);

  const r = await page.evaluate(() => {
    const out = [];
    document.querySelectorAll('header a[aria-label], footer span').forEach(() => {});
    // every lockup: latin span followed by the rtl arabic span
    document.querySelectorAll('[lang="ar"][dir="rtl"]').forEach((ar) => {
      const latin = ar.previousElementSibling;
      if (!latin) return;
      const lw = latin.getBoundingClientRect().width;
      const aw = ar.getBoundingClientRect().width;
      out.push({
        latin: +lw.toFixed(1),
        arabic: +aw.toFixed(1),
        deltaPct: +(((aw - lw) / lw) * 100).toFixed(1),
        transform: getComputedStyle(ar).transform,
      });
    });
    return out;
  });

  console.log(`\n=== ${label} ===`);
  r.forEach((x, i) =>
    console.log(
      `  lockup ${i}: latin ${x.latin}px  arabic ${x.arabic}px  delta ${x.deltaPct}%  ${x.transform}`,
    ),
  );

  // hero wordmark overflow check
  const hero = await page.evaluate(() => {
    const el = document.querySelector('section span[aria-hidden="true"]');
    if (!el) return null;
    const r = el.getBoundingClientRect();
    return { left: Math.round(r.left), right: Math.round(r.right), vw: window.innerWidth };
  });
  if (hero) {
    console.log(
      `  hero word: ${hero.left} → ${hero.right} of ${hero.vw}px  (bleed L ${-hero.left}, R ${hero.right - hero.vw})`,
    );
  }
}

await b.close();
