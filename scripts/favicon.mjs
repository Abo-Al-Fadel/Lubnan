import { chromium } from 'playwright';
import { writeFileSync } from 'node:fs';

/**
 * Trim the transparent margin off the supplied cedar and re-export it at the
 * sizes a browser actually asks for.
 *
 * A 1254px source is not a "large" favicon — it is downscaled to 16 or 32px in
 * the tab strip, and whatever transparent padding the export carries is scaled
 * down with it. Cropping to the ink is the only thing that makes the mark
 * bigger at the size it is seen.
 */
const browser = await chromium.launch({ executablePath: process.env.CHROME_PATH || undefined });
const page = await browser.newPage();
await page.goto('http://localhost:3000');

const result = await page.evaluate(async () => {
  const img = new Image();
  img.src = '/brand/favicon.png';
  await img.decode();

  const c = document.createElement('canvas');
  c.width = img.naturalWidth;
  c.height = img.naturalHeight;
  const ctx = c.getContext('2d', { willReadFrequently: true });
  ctx.drawImage(img, 0, 0);
  const { data } = ctx.getImageData(0, 0, c.width, c.height);

  // Alpha bounding box. Threshold above 8 to ignore export fringing.
  let minX = c.width, minY = c.height, maxX = -1, maxY = -1;
  for (let y = 0; y < c.height; y++) {
    for (let x = 0; x < c.width; x++) {
      if (data[(y * c.width + x) * 4 + 3] > 8) {
        if (x < minX) minX = x;
        if (x > maxX) maxX = x;
        if (y < minY) minY = y;
        if (y > maxY) maxY = y;
      }
    }
  }

  const bw = maxX - minX + 1;
  const bh = maxY - minY + 1;
  const report = {
    source: `${c.width}x${c.height}`,
    inkBox: `${bw}x${bh} at ${minX},${minY}`,
    fillsWidth: `${Math.round((100 * bw) / c.width)}%`,
    fillsHeight: `${Math.round((100 * bh) / c.height)}%`,
  };

  // Square crop around the ink, with a small even breathing margin.
  const side = Math.max(bw, bh);
  const pad = Math.round(side * 0.04);
  const box = side + pad * 2;
  const sx = minX + bw / 2 - box / 2;
  const sy = minY + bh / 2 - box / 2;

  const out = {};
  for (const size of [512, 180, 32]) {
    const o = document.createElement('canvas');
    o.width = size;
    o.height = size;
    const octx = o.getContext('2d');
    octx.imageSmoothingQuality = 'high';
    octx.drawImage(img, sx, sy, box, box, 0, 0, size, size);
    out[size] = o.toDataURL('image/png').split(',')[1];
  }
  return { report, out };
});

console.log(JSON.stringify(result.report, null, 1));
for (const [size, b64] of Object.entries(result.out)) {
  const path =
    size === '512'
      ? 'public/brand/favicon-trimmed.png'
      : size === '180'
        ? 'app/apple-icon.png'
        : 'app/icon.png';
  writeFileSync(path, Buffer.from(b64, 'base64'));
  console.log(`wrote ${path} (${size}px)`);
}
await browser.close();
