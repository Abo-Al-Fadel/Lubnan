import { chromium } from 'playwright';
const b = await chromium.launch({ executablePath: process.env.CHROME_PATH || undefined });
const p = await b.newPage();
await p.goto('http://localhost:3000/achievements', { waitUntil: 'load' });
await p.waitForTimeout(2500);
const out = await p.evaluate(() => {
  const sec = document.querySelectorAll('main section');
  const last = sec[sec.length - 1];
  const scrim = last.querySelector('.scrim') || last;
  const heading = scrim.querySelector('p');
  const cs = getComputedStyle(scrim);
  return {
    scrimClasses: scrim.className,
    palette: scrim.closest('[data-palette]')?.getAttribute('data-palette') ?? 'NONE',
    heroInk: cs.getPropertyValue('--hero-ink').trim(),
    scrimStrong: cs.getPropertyValue('--scrim-strong').trim(),
    headingColor: heading ? getComputedStyle(heading).color : null,
    beforeBg: getComputedStyle(scrim, '::before').backgroundImage.slice(0, 120),
  };
});
console.log(JSON.stringify(out, null, 1));
await b.close();
