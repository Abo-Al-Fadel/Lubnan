/**
 * Derive web formats from the PNG plates.
 *
 * The plates are exported as PNG, which is the right archival choice and the
 * wrong delivery one: ninety-nine of them come to 233 MB, and a single page
 * pulling six is a thirty-megabyte page. PNG has no lossy mode, so there is no
 * quality dial to turn — the only fix is a different format.
 *
 * Writes AVIF and WebP next to each source. The PNG stays as the last fallback
 * in the <picture>, so nothing breaks if a derivative is missing, and this
 * script is safe to re-run: it skips work whose output is newer than its input.
 *
 *   node scripts/optimise-plates.mjs [--force]
 */
import sharp from 'sharp';
import { readdir, stat, mkdir } from 'node:fs/promises';
import { existsSync } from 'node:fs';
import path from 'node:path';

const ROOT = path.join(process.cwd(), 'public', 'img');
const FORCE = process.argv.includes('--force');

// 2560 covers a 2x ultrawide. The sources are larger than anything a browser
// will ever paint, and downscaling is most of the saving.
const MAX_WIDTH = 2560;

async function* pngs(dir) {
  for (const entry of await readdir(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) yield* pngs(full);
    else if (entry.name.toLowerCase().endsWith('.png')) yield full;
  }
}

async function fresh(src, out) {
  if (FORCE || !existsSync(out)) return false;
  const [a, b] = await Promise.all([stat(src), stat(out)]);
  return b.mtimeMs >= a.mtimeMs;
}

let before = 0, after = 0, done = 0, skipped = 0;

for await (const src of pngs(ROOT)) {
  const { size } = await stat(src);
  before += size;

  const base = src.replace(/\.png$/i, '');
  const avif = `${base}.avif`;
  const webp = `${base}.webp`;

  if (await fresh(src, avif) && await fresh(src, webp)) {
    skipped++;
    after += (await stat(avif)).size;
    continue;
  }

  const pipeline = sharp(src, { limitInputPixels: false })
    .rotate()
    .resize({ width: MAX_WIDTH, withoutEnlargement: true });

  // effort 4 rather than the default 4->9 sweep: the last few points of
  // compression cost minutes per image and buy single-digit percentages.
  await pipeline.clone().avif({ quality: 55, effort: 4 }).toFile(avif);
  await pipeline.clone().webp({ quality: 78, effort: 4 }).toFile(webp);

  after += (await stat(avif)).size;
  done++;
  if (done % 10 === 0) process.stdout.write(`  ${done} converted\n`);
}

const mb = (n) => (n / 1048576).toFixed(1);
console.log(`\nplates:   ${done} converted, ${skipped} already current`);
console.log(`png:      ${mb(before)} MB`);
console.log(`avif:     ${mb(after)} MB  (${(100 - (after / before) * 100).toFixed(1)}% smaller)`);
