import { writeFileSync, existsSync } from 'node:fs';

/**
 * Pull plates from Wikimedia Commons, licence-checked.
 *
 * The repository is public, so "found it online" is not good enough: every
 * file here is confirmed public domain or Creative Commons through the API
 * before it is written, and every author and licence is recorded in
 * CREDITS.md. Anything that does not come back clean is skipped and reported
 * rather than downloaded and hoped about.
 */
const UA = 'LubnanDemo/1.0 (https://github.com/Abo-Al-Fadel/Lubnan; educational)';
const API = 'https://commons.wikimedia.org/w/api.php';

/** [plate id, Commons File: title, what it stands for] */
const WANTED = [
  // Gibran painted; this is his own work, not a likeness invented for him.
  ['S1', 'Self Portrait by Kahlil Gibran.jpg', 'Khalil Gibran'],
  ['S2', 'The universal declaration of human rights 10 December 1948.jpg', 'Charles Malik'],
  ['S3', 'Baalbek Lebanon.JPG', 'Fairuz'],
  ['S5', 'Election sous la coupole de MM de Nolhac et du général Goyau (M. Richepin) - btv1b9038736n.jpg', 'Amin Maalouf'],
  ['S6', 'Hassan Kamel Al-Sabbah - Al-Alam, V2, P 268 (01).jpg', 'Hassan Kamel Al-Sabbah'],
  ['S8', 'RFM, Kurhaus Wiesbaden, stage with grand piano for Sokolov recital.jpg', 'Mika'],
];

const OK = /public domain|^cc[ -]|creative commons|cc0/i;

async function info(title) {
  const url =
    `${API}?action=query&format=json&origin=*` +
    `&titles=${encodeURIComponent('File:' + title)}` +
    `&prop=imageinfo&iiprop=url|extmetadata|size&iiurlwidth=1600`;
  const r = await fetch(url, { headers: { 'User-Agent': UA } });
  const j = await r.json();
  const page = Object.values(j.query?.pages ?? {})[0];
  if (!page || page.missing !== undefined || !page.imageinfo) return null;
  const ii = page.imageinfo[0];
  const strip = (s) => (s ?? '').replace(/<[^>]+>/g, '').trim();
  return {
    url: ii.thumburl || ii.url,
    width: ii.thumbwidth || ii.width,
    height: ii.thumbheight || ii.height,
    licence: strip(ii.extmetadata?.LicenseShortName?.value) || 'unknown',
    artist: strip(ii.extmetadata?.Artist?.value) || 'Unknown',
    descUrl: ii.descriptionurl,
  };
}

async function search(term) {
  const url =
    `${API}?action=query&format=json&origin=*&generator=search` +
    `&gsrsearch=${encodeURIComponent('filetype:bitmap ' + term)}` +
    `&gsrnamespace=6&gsrlimit=6&prop=imageinfo&iiprop=url|extmetadata|size&iiurlwidth=1600`;
  const r = await fetch(url, { headers: { 'User-Agent': UA } });
  const j = await r.json();
  return Object.values(j.query?.pages ?? {}).map((p) => p.title);
}

const mode = process.argv[2];

if (mode === 'search') {
  for (const term of process.argv.slice(3)) {
    console.log(`\n── ${term} ──`);
    for (const t of await search(term)) {
      const i = await info(t.replace(/^File:/, ''));
      if (i) console.log(`  ${i.licence.padEnd(22)} ${i.width}x${i.height}  ${t}`);
    }
  }
} else {
  const credits = [];
  for (const [plate, title, who] of WANTED) {
    const i = await info(title);
    if (!i) {
      console.log(`SKIP  ${plate}  not found: ${title}`);
      continue;
    }
    if (!OK.test(i.licence)) {
      console.log(`SKIP  ${plate}  licence "${i.licence}" not clearly free`);
      continue;
    }
    const out = `public/img/${plate.match(/^[A-Za-z]+/)[0].toUpperCase()}/${plate}.png`;
    const res = await fetch(i.url, { headers: { 'User-Agent': UA } });
    if (!res.ok) {
      console.log(`SKIP  ${plate}  download ${res.status}`);
      continue;
    }
    writeFileSync(out, Buffer.from(await res.arrayBuffer()));
    console.log(`OK    ${plate}  ${i.licence}  ${i.width}x${i.height}  ${title}`);
    credits.push({ plate, who, title, ...i });
  }

  if (credits.length) {
    writeFileSync(
      'CREDITS.md',
      `# Image credits\n\n` +
        `Plates sourced from Wikimedia Commons. Every file below was licence-checked\n` +
        `through the Commons API before download. Everything else in \`public/img/\`\n` +
        `was generated for this project.\n\n` +
        credits
          .map(
            (c) =>
              `## ${c.plate} — ${c.who}\n\n` +
              `- **File:** [${c.title}](${c.descUrl})\n` +
              `- **Licence:** ${c.licence}\n` +
              `- **Author:** ${c.artist}\n`,
          )
          .join('\n'),
    );
    console.log('\nwrote CREDITS.md');
  }
}
