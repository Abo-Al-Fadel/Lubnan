import { chromium } from 'playwright';

const BASE = 'http://localhost:3000';
const out = [];
const ok = (name, pass, detail) => out.push({ name, pass, detail });

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
const errors = [];
page.on('pageerror', (e) => errors.push(e.message.split('\n')[0]));

await page.goto(BASE + '/profile', { waitUntil: 'load' });
await page.waitForTimeout(800);
const profileH1 = await page.locator('h1').innerText();
ok('profile is guest, not a fake user', !/Rania/i.test(profileH1), profileH1);

await page.goto(BASE + '/login', { waitUntil: 'load' });
await page.waitForTimeout(400);
await page.click('button[type="submit"]');
const emailInvalid = await page.evaluate(() => document.getElementById('email')?.validity.valueMissing);
ok('login requires email', Boolean(emailInvalid), `valueMissing=${emailInvalid}`);

await page.goto(BASE + '/explore?region=Coast', { waitUntil: 'load' });
await page.waitForTimeout(1200);
const regionNote = await page.locator('main').innerText();
ok('explore honours ?region=Coast', /Coast/i.test(regionNote) && !/Bekaa/i.test(await page.locator('section').nth(2).innerText().catch(() => '')), 'region filter applied');

await page.goto(BASE + '/this-route-does-not-exist', { waitUntil: 'load' });
await page.waitForTimeout(400);
ok('unknown route renders not-found', (await page.locator('h1').count()) > 0, await page.locator('h1').innerText());

await page.goto(BASE, { waitUntil: 'load' });
await page.waitForTimeout(600);
await page.keyboard.press('Tab');
const skip = await page.evaluate(() => document.activeElement?.getAttribute('href'));
ok('skip link is first focus', skip === '#main', `focused href=${skip}`);

await page.goto(BASE + '/plan', { waitUntil: 'load' });
await page.waitForTimeout(500);
const before = await page.evaluate(() => document.documentElement.dataset.theme);
await page.getByRole('button', { name: /theme|switch theme|changer de thème|تبديل/i }).first().click();
await page.waitForTimeout(200);
const after = await page.evaluate(() => document.documentElement.dataset.theme);
ok('theme toggle flips data-theme', before !== after, `${before} -> ${after}`);

await page.getByRole('button', { name: /search/i }).first().click();
await page.waitForTimeout(300);
await page.keyboard.type('Byblos');
await page.waitForTimeout(200);
const searchHit = await page.locator('a[href="/explore/byblos"]').count();
ok('search finds Byblos', searchHit > 0, `hits=${searchHit}`);
await page.keyboard.press('Escape');

const phone = await browser.newPage({ viewport: { width: 390, height: 844 } });
await phone.goto(BASE, { waitUntil: 'load' });
await phone.waitForTimeout(800);
const overflow = await phone.evaluate(
  () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
);
ok('home has no horizontal overflow on phone', overflow <= 1, `overflow=${overflow}px`);
await phone.getByRole('button', { name: /menu|القائمة/i }).click();
await phone.waitForTimeout(200);
const drawer = await phone.locator('#mobile-drawer').count();
ok('phone menu opens', drawer === 1, `drawer=${drawer}`);

await page.goto(BASE, { waitUntil: 'load' });
await page.waitForTimeout(1800);
const desktopVideo = await page.locator('section video').count();
ok('desktop hero mounts a video', desktopVideo > 0, `videos=${desktopVideo}`);

await page.setViewportSize({ width: 390, height: 844 });
await page.waitForTimeout(800);
const afterPhone = await page.evaluate(() => ({
  vw: getComputedStyle(document.documentElement).getPropertyValue('--app-vw').trim(),
  inner: window.innerWidth,
  videos: document.querySelectorAll('section video').length,
}));
ok(
  'inspect-to-phone updates --app-vw',
  Number.parseInt(afterPhone.vw, 10) <= 430,
  `--app-vw=${afterPhone.vw} inner=${afterPhone.inner}`,
);

await page.setViewportSize({ width: 1440, height: 900 });
await page.waitForTimeout(800);
const afterDesktop = await page.evaluate(() => ({
  vw: getComputedStyle(document.documentElement).getPropertyValue('--app-vw').trim(),
  videos: document.querySelectorAll('section video').length,
}));
ok(
  'cancel inspect restores desktop width',
  Number.parseInt(afterDesktop.vw, 10) >= 1200,
  `--app-vw=${afterDesktop.vw}`,
);
ok('hero video survives inspect cancel', afterDesktop.videos > 0, `videos=${afterDesktop.videos}`);

await phone.goto(BASE, { waitUntil: 'load' });
await phone.waitForTimeout(2200);
const phoneVideo = await phone.locator('section video').count();
ok('phone hero mounts a video', phoneVideo > 0, `videos=${phoneVideo}`);

await page.goto(BASE, { waitUntil: 'load' });
await page.waitForTimeout(400);
await page.keyboard.press('i');
await page.waitForTimeout(200);
const slotsOn = await page.locator('[data-slot-overlay]').count();
await page.keyboard.press('Escape');
await page.waitForTimeout(200);
const slotsOff = await page.locator('[data-slot-overlay]').count();
ok('Escape closes the I-key slot overlay', slotsOn > 0 && slotsOff === 0, `on=${slotsOn} off=${slotsOff}`);

ok('no pageerrors during pass', errors.length === 0, errors.join(' | ') || 'none');

await browser.close();
console.log(JSON.stringify(out, null, 2));
process.exitCode = out.some((r) => !r.pass) ? 1 : 0;
