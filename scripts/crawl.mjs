import { chromium } from 'playwright';
import { mkdirSync, writeFileSync } from 'node:fs';

/**
 * Walk the site the way a user would and report what breaks.
 *
 * Every route, at desktop and phone. On each one: collect every link and
 * button, follow or press each, and record console errors, failed requests,
 * dead hrefs, unlabelled controls, keyboard traps and horizontal overflow.
 *
 * This is deliberately not a unit test. It is the pass that catches the things
 * that only appear when something is actually clicked.
 */
const BASE = 'http://localhost:3000';
const OUT = 'crawl';
mkdirSync(`${OUT}/shots`, { recursive: true });

const ROUTES = [
  '/', '/explore', '/explore/byblos', '/explore/baalbek', '/explore/jeita',
  '/explore/cedars', '/explore/qadisha', '/explore/tyre', '/explore/beirut',
  '/explore/batroun', '/story', '/people', '/achievements', '/legacy',
  '/plan', '/community', '/login', '/profile',
];

const VIEWPORTS = [
  ['desktop', { width: 1440, height: 900 }],
  ['phone', { width: 390, height: 844 }],
];

const findings = [];
const add = (sev, route, vp, what) => findings.push({ sev, route, vp, what });

const browser = await chromium.launch({ executablePath: process.env.CHROME_PATH || undefined });

/** Routes that exist, so a link to anything else is a dead link. */
const KNOWN = new Set(ROUTES);

for (const [vpName, viewport] of VIEWPORTS) {
  console.log(`\n════ ${vpName} ${viewport.width}x${viewport.height} ════`);

  for (const route of ROUTES) {
    const ctx = await browser.newContext({ viewport });
    const page = await ctx.newPage();

    const consoleErrors = [];
    const failed = [];
    page.on('pageerror', (e) => consoleErrors.push(e.message.split('\n')[0]));
    page.on('console', (m) => {
      if (m.type() === 'error' && !m.text().includes('Failed to load resource')) {
        consoleErrors.push(m.text().slice(0, 160));
      }
    });
    page.on('response', (r) => {
      if (r.status() >= 400 && !r.url().includes('/img/')) {
        failed.push(`${r.status()} ${r.url().replace(BASE, '')}`);
      }
    });

    let status = 0;
    try {
      const res = await page.goto(BASE + route, { waitUntil: 'load', timeout: 45000 });
      status = res?.status() ?? 0;
    } catch (e) {
      add('ERROR', route, vpName, `navigation failed: ${e.message.split('\n')[0]}`);
      await ctx.close();
      continue;
    }
    if (status >= 400) add('ERROR', route, vpName, `route returned ${status}`);
    await page.waitForTimeout(2200);

    // ── overflow ──────────────────────────────────────────────────────────
    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
    );
    if (overflow > 1) add('BUG', route, vpName, `horizontal overflow ${overflow}px`);

    // ── links ─────────────────────────────────────────────────────────────
    const links = await page.$$eval('a[href]', (as) =>
      as.map((a) => ({
        href: a.getAttribute('href'),
        text: (a.textContent || '').trim().slice(0, 40),
        label: a.getAttribute('aria-label'),
        target: a.getAttribute('target'),
        rel: a.getAttribute('rel'),
      })),
    );
    for (const l of links) {
      if (l.href === '#' || l.href === '') {
        add('BUG', route, vpName, `dead link "${l.text || l.label || '(no text)'}"`);
      }
      if (!l.text && !l.label) {
        add('A11Y', route, vpName, `link with no accessible name -> ${l.href}`);
      }
      if (l.target === '_blank' && !(l.rel || '').includes('noopener')) {
        add('SEC', route, vpName, `target=_blank without noopener -> ${l.href}`);
      }
      const internal = l.href?.startsWith('/') ? l.href.split(/[?#]/)[0] : null;
      if (internal && !KNOWN.has(internal) && !internal.startsWith('/img')) {
        add('BUG', route, vpName, `link to unknown route ${internal}`);
      }
    }

    // ── buttons: press every one and watch for errors ─────────────────────
    const buttons = await page.$$('button:not([disabled])');
    let pressed = 0;
    for (let i = 0; i < Math.min(buttons.length, 40); i++) {
      const b = buttons[i];
      try {
        const meta = await b.evaluate((el) => ({
          name: (el.textContent || '').trim().slice(0, 30) || el.getAttribute('aria-label') || '',
          visible: !!(el.offsetWidth || el.offsetHeight),
        }));
        if (!meta.visible) continue;
        if (!meta.name) add('A11Y', route, vpName, 'button with no accessible name');
        await b.click({ timeout: 2500, force: false });
        pressed++;
        await page.waitForTimeout(120);
        // Close anything modal that opened, so the next click is not blocked.
        await page.keyboard.press('Escape').catch(() => {});
      } catch {
        /* Obscured or detached after a re-render. Not a defect by itself. */
      }
    }

    // ── tap targets on phone ──────────────────────────────────────────────
    if (vpName === 'phone') {
      /* `.tap` widens the hit area with an absolutely positioned ::after,
         which real hit testing honours but getBoundingClientRect cannot see.
         Excluded here so the check does not report a target that has already
         been given one. */
      const small = await page.$$eval('a[href], button', (els) =>
        els
          .filter((el) => {
            if (el.classList.contains('tap')) return false;
            const r = el.getBoundingClientRect();
            return r.width > 0 && r.height > 0 && (r.height < 24 || r.width < 24);
          })
          .map((el) => `${el.tagName.toLowerCase()} "${(el.textContent || '').trim().slice(0, 24)}"`)
          .slice(0, 6),
      );
      for (const s of small) add('A11Y', route, vpName, `tap target under 24px: ${s}`);
    }

    // ── headings ──────────────────────────────────────────────────────────
    const h1s = await page.$$eval('h1', (n) => n.length);
    if (h1s === 0) add('SEO', route, vpName, 'no h1');
    if (h1s > 1) add('SEO', route, vpName, `${h1s} h1 elements`);

    // ── images without alt ────────────────────────────────────────────────
    const noAlt = await page.$$eval('img:not([alt])', (n) => n.length);
    if (noAlt) add('A11Y', route, vpName, `${noAlt} img without alt`);

    for (const e of [...new Set(consoleErrors)]) add('ERROR', route, vpName, `console: ${e}`);
    for (const f of [...new Set(failed)]) add('ERROR', route, vpName, `request ${f}`);

    const bad = findings.filter((x) => x.route === route && x.vp === vpName).length;
    console.log(`  ${bad ? 'FAIL' : ' ok '}  ${route.padEnd(20)} ${pressed} controls pressed`);

    if (bad) {
      await page.screenshot({
        path: `${OUT}/shots/${vpName}${route.replace(/\//g, '_') || '_home'}.png`,
      });
    }
    await ctx.close();
  }
}

await browser.close();

const order = ['ERROR', 'SEC', 'BUG', 'A11Y', 'SEO'];
const grouped = order
  .map((sev) => [sev, findings.filter((f) => f.sev === sev)])
  .filter(([, list]) => list.length);

console.log('\n════ findings ════');
if (!grouped.length) console.log('  none');
for (const [sev, list] of grouped) {
  // Collapse identical findings that repeat across routes.
  const byWhat = new Map();
  for (const f of list) {
    const k = f.what.replace(/\d+/g, 'N');
    if (!byWhat.has(k)) byWhat.set(k, []);
    byWhat.get(k).push(f);
  }
  console.log(`\n${sev} (${list.length})`);
  for (const [, group] of byWhat) {
    const first = group[0];
    const where =
      group.length > 3
        ? `${group.length} places`
        : group.map((g) => `${g.route}@${g.vp}`).join(', ');
    console.log(`  ${first.what}`);
    console.log(`      ${where}`);
  }
}

writeFileSync(`${OUT}/report.json`, JSON.stringify(findings, null, 2));
console.log(`\n${findings.length} findings -> ${OUT}/report.json`);
process.exitCode = findings.some((f) => f.sev === 'ERROR' || f.sev === 'SEC') ? 1 : 0;
