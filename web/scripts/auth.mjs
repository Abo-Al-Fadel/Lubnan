/**
 * Sessions, and the ways they used to end badly.
 *
 *   node scripts/auth.mjs        (needs the API on :5080 and Next on :3000)
 *
 * Run the API with a tiny access-token lifetime, so the expiry path is
 * reachable in a test rather than after fifteen minutes of waiting:
 *
 *   Auth__AccessTokenLifetime=00:00:08 dotnet run --project src/Lubnan.Api
 *
 * What this covers, and why each one is here rather than assumed:
 *
 *   - `?next=` cannot leave this origin. An open redirect on a sign-in page is
 *     the first half of a credential-phishing chain: our real form, then a
 *     destination of somebody else's choosing, at the moment the visitor has
 *     just proved they trust this site.
 *   - A guest pressing "like" is sent to sign in and comes back to the post,
 *     not to their profile.
 *   - A session outlives its access token. The refresh endpoint existed on the
 *     server and nothing in the browser ever called it, so every session died
 *     silently at fifteen minutes and looked exactly like being signed out.
 *   - No 5xx from anything. A 500 from /me used to render the signed-out page,
 *     which is how a migration that had not been applied in production
 *     presented itself as broken authentication.
 */

import { chromium } from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const WEB = 'http://localhost:3000';
const API = 'http://localhost:5080';
const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const MAIL = path.join(process.env.TEMP ?? '/tmp', 'lubnan-mail');

const out = [];
const ok = (name, pass, detail = '') => out.push([pass ? 'PASS' : 'FAIL', name, detail]);
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

/** The confirmation link, read out of the file the dev mail sender writes. */
async function confirmToken(email) {
  for (let i = 0; i < 120; i += 1) {
    const files = fs.existsSync(MAIL) ? fs.readdirSync(MAIL) : [];
    for (const f of files) {
      const body = fs.readFileSync(path.join(MAIL, f), 'utf8');
      if (body.includes(email) && body.includes('token=')) {
        return body.match(/token=([A-Za-z0-9_-]+)/)[1];
      }
    }
    await sleep(500);
  }
  throw new Error(`no confirmation mail for ${email}`);
}

// ── 0. safeReturnTo, exercised directly ─────────────────────────────────────
//
// Driving this through the sign-in form hits the login rate limiter after a few
// attempts, and a 429 tells you nothing about the function under test.
{
  const shim = path.join(ROOT, '.return-to.check.mjs');
  const src = fs
    .readFileSync(path.join(ROOT, 'lib', 'return-to.ts'), 'utf8')
    .replaceAll(': string', '')
    .replaceAll('export function', 'function');

  fs.writeFileSync(shim, `${src}\nexport { safeReturnTo, loginHref };`);
  const { safeReturnTo, loginHref } = await import(`file://${shim.replaceAll('\\', '/')}`);
  fs.unlinkSync(shim);

  const cases = [
    ['?next=%2Fcommunity%23abc', '/community#abc'],
    ['?next=%2Fprofile', '/profile'],
    ['', '/profile'],
    ['?next=https%3A%2F%2Fevil.example', '/profile'],
    ['?next=%2F%2Fevil.example', '/profile'], // protocol-relative
    ['?next=%2F%5Cevil.example', '/profile'], // several browsers read /\ as //
    ['?next=javascript%3Aalert(1)', '/profile'],
    ['?next=http%3A%2F%2Flocalhost%3A3000%2Fok', '/profile'], // absolute, even ours
  ];

  const wrong = cases.filter(([input, want]) => safeReturnTo(input) !== want);
  for (const [input, want] of wrong) {
    console.log(`  '${input}' -> ${safeReturnTo(input)}, wanted ${want}`);
  }

  ok('safeReturnTo keeps every redirect on this origin', wrong.length === 0, `${cases.length} cases`);
  ok('loginHref encodes the return path', loginHref('/community#x') === '/login?next=%2Fcommunity%23x');
}

const browser = await chromium.launch();
const ctx = await browser.newContext({ viewport: { width: 1280, height: 900 } });
const page = await ctx.newPage();

const errors = [];
page.on('console', (m) => {
  if (m.type() === 'error') errors.push(m.text());
});
page.on('response', (r) => {
  if (!r.ok()) errors.push(`HTTP ${r.status()} ${new URL(r.url()).pathname}`);
});

const like = () => page.getByRole('button', { name: /^(Like|Liked)$/ }).first();

// ── 1. A guest pressing like lands on sign-in, carrying where to return to ──
await page.goto(`${WEB}/community`, { waitUntil: 'networkidle' });
await like().waitFor({ timeout: 15000 });
await like().click();
await page.waitForURL(/\/login/, { timeout: 10000 });

const guestUrl = new URL(page.url());
ok('guest like goes to /login', guestUrl.pathname === '/login', page.url());

const next = guestUrl.searchParams.get('next');
ok('it carries a return path to the post', !!next && next.startsWith('/community#'), String(next));

// ── 2. An account to sign in with ───────────────────────────────────────────
const email = `e2e-${Date.now()}@example.com`;
const password = 'a long enough passphrase';

const registered = await fetch(`${API}/api/v1/auth/register`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ email, password, displayName: 'E2E Reader' }),
});
ok('register accepted', registered.ok || registered.status === 202, String(registered.status));

if (!(registered.ok || registered.status === 202)) {
  // Say so here rather than waiting a minute for a mail that was never going to
  // be sent. 429 means this script has run several times over, not that
  // anything is broken - restart the API to clear the limiter.
  console.log(`\nregister returned ${registered.status}: no account, so no mail.`);
  process.exit(1);
}

const confirmed = await fetch(`${API}/api/v1/auth/confirm-email`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ token: await confirmToken(email) }),
});
ok('email confirmed', confirmed.ok || confirmed.status === 204, String(confirmed.status));

// ── 3. Signing in returns to that post, not to the profile ──────────────────
await page.goto(`${WEB}/login?next=${encodeURIComponent(next)}`, { waitUntil: 'networkidle' });
await page.fill('#email', email);
await page.fill('#password', password);
await page.getByRole('button', { name: /log in|sign in/i }).first().click();
await page.waitForURL(/\/community/, { timeout: 20000 });
ok('sign-in returns to the post', new URL(page.url()).pathname === '/community', page.url());

// ── 4. The profile shows the account ────────────────────────────────────────
await page.goto(`${WEB}/profile`, { waitUntil: 'networkidle' });
await page.waitForTimeout(1500);
const profile = await page.locator('main').innerText();
ok('profile shows the signed-in account', /e2e reader/i.test(profile), profile.slice(0, 90).replace(/\n/g, ' | '));
ok('profile does not show the guest wall', !profile.includes('Sign in to keep saved places'));

// ── 5. Past the access token's lifetime, the session must survive ───────────
await page.goto(`${WEB}/community`, { waitUntil: 'networkidle' });
await page.waitForTimeout(11000);

let refreshes = 0;
page.on('response', (r) => {
  if (r.url().includes('/auth/refresh')) refreshes += 1;
});

const before = await like().innerText();
await like().click();
await page.waitForTimeout(3000);
const after = await like().innerText();

ok('like works after the access token expired', before !== after, `${before.trim()} -> ${after.trim()}`);
ok('the session was renewed, not ended', refreshes >= 1, `refresh calls: ${refreshes}`);
ok('still on /community, not bounced to login', new URL(page.url()).pathname === '/community', page.url());

await page.goto(`${WEB}/profile`, { waitUntil: 'networkidle' });
await page.waitForTimeout(1500);
ok('profile survives token expiry', /e2e reader/i.test(await page.locator('main').innerText()));

// ── 6. An off-origin ?next= is refused even with valid credentials ──────────
await page.goto(`${WEB}/login?next=${encodeURIComponent('https://evil.example/steal')}`, {
  waitUntil: 'networkidle',
});
await page.fill('#email', email);
await page.fill('#password', password);
await page.getByRole('button', { name: /log in|sign in/i }).first().click();
await page.waitForTimeout(4000);
ok('off-origin ?next= is refused', new URL(page.url()).host === 'localhost:3000', page.url());

await browser.close();

// A 401 on a write is the mechanism working: the token had expired, the client
// refreshed, the retry succeeded - which the assertions above already proved.
// The 429s are this script signing in several times in a minute. A 5xx is not
// explainable that way, and is the shape the avatars bug took.
const serverErrors = errors.filter((e) => /HTTP 5\d\d/.test(e));
ok('no 5xx from any request', serverErrors.length === 0, serverErrors.slice(0, 3).join(' ~ '));

const unexplained = errors.filter(
  (e) => !/favicon|404 \(Not Found\)|HTTP 401|HTTP 429|401 \(Unauthorized\)|429/i.test(e),
);
ok('no other console errors', unexplained.length === 0, unexplained.slice(0, 3).join(' ~ '));

for (const [status, name, detail] of out) {
  console.log(`${status}  ${name}${detail ? `   [${detail}]` : ''}`);
}

const failed = out.filter(([status]) => status === 'FAIL').length;
console.log(failed ? `\n${failed} FAILED` : '\nALL PASSED');
process.exit(failed ? 1 : 0);
