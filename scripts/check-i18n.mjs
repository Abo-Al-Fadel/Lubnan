import { chromium } from 'playwright';

const b = await chromium.launch({ executablePath: process.env.CHROME_PATH || undefined });
const page = await (await b.newContext({ viewport: { width: 1440, height: 900 } })).newPage();
await page.goto('http://localhost:3000', { waitUntil: 'networkidle' });
await page.waitForTimeout(900);

const read = async () => {
  await page.evaluate(() => window.scrollTo(0, 1400));
  await page.waitForTimeout(500);
  return page.evaluate(() => ({
    lang: document.documentElement.lang,
    dir: document.documentElement.dir,
    theme: document.documentElement.dataset.theme,
    nav: Array.from(document.querySelectorAll('header nav a')).map((a) => a.textContent.trim()),
    login: document.querySelector('header a[href="#"]:last-of-type')?.textContent?.trim(),
    footerHead: Array.from(document.querySelectorAll('footer p.micro'))
      .slice(0, 4)
      .map((p) => p.textContent.trim()),
  }));
};

for (const code of ['EN', 'FR', 'ع']) {
  await page.evaluate(() => window.scrollTo(0, 0));
  await page.waitForTimeout(300);
  await page.locator('header button', { hasText: code }).first().click();
  await page.waitForTimeout(600);
  const r = await read();
  console.log(`\n[${code}] lang=${r.lang} dir=${r.dir} theme=${r.theme}`);
  console.log('  nav   :', r.nav.join(' · '));
  console.log('  footer:', r.footerHead.join(' · '));
}

// theme toggle — back to EN first so the accessible name is predictable
await page.evaluate(() => window.scrollTo(0, 0));
await page.locator('header button', { hasText: 'EN' }).first().click();
await page.waitForTimeout(400);

const before = await page.evaluate(() => ({
  theme: document.documentElement.dataset.theme,
  ground: getComputedStyle(document.querySelector('[data-palette]'))
    .getPropertyValue('--ground')
    .trim(),
}));

await page.getByRole('button', { name: 'Switch theme' }).click();
await page.waitForTimeout(600);

const after = await page.evaluate(() => ({
  theme: document.documentElement.dataset.theme,
  ground: getComputedStyle(document.querySelector('[data-palette]'))
    .getPropertyValue('--ground')
    .trim(),
  ink: getComputedStyle(document.querySelector('[data-palette]'))
    .getPropertyValue('--ink')
    .trim(),
}));

console.log('\ntheme before:', JSON.stringify(before));
console.log('theme after :', JSON.stringify(after));

await b.close();
