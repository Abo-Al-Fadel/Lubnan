import { chromium } from 'playwright';
import { mkdirSync } from 'node:fs';

const OUT = process.argv[2] ?? 'ref';
mkdirSync(OUT, { recursive: true });

const browser = await chromium.launch({ executablePath: process.env.CHROME_PATH || undefined });

for (const [label, viewport] of [
  ['w1920', { width: 1920, height: 925 }],
  ['w412', { width: 412, height: 915 }],
]) {
  const ctx = await browser.newContext({ viewport });
  const page = await ctx.newPage();
  await page.goto('http://localhost:3000', { waitUntil: 'load' });
  await page.waitForTimeout(2600);

  const m = await page.evaluate(() => {
    const box = (el) => {
      if (!el) return null;
      const r = el.getBoundingClientRect();
      return {
        l: Math.round(r.left),
        r: Math.round(r.right),
        t: Math.round(r.top),
        b: Math.round(r.bottom),
        w: Math.round(r.width),
        h: Math.round(r.height),
      };
    };
    const wordEl = document.querySelector('span.anim-word');
    const range = document.createRange();
    range.selectNodeContents(wordEl);
    const wr = range.getBoundingClientRect();
    const subj = document.querySelector('.anim-subject img, .anim-subject svg');
    return {
      vw: innerWidth,
      vh: innerHeight,
      fontSize: getComputedStyle(wordEl).fontSize,
      wordGlyphs: {
        l: Math.round(wr.left),
        r: Math.round(wr.right),
        t: Math.round(wr.top),
        b: Math.round(wr.bottom),
        w: Math.round(wr.width),
        h: Math.round(wr.height),
      },
      subject: box(subj),
      statBar: box(document.querySelector('.scrim > div + div, .scrim .border-t')),
    };
  });
  console.log(label, JSON.stringify(m, null, 1));

  await page.screenshot({ path: `${OUT}/${label}.png` });
  await ctx.close();
}
await browser.close();
