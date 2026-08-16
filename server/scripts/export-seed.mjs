// Turns the frontend's editorial data into the seed the API ships with.
//
// The frontend is, for now, the source of truth for this copy: it was written
// there and it renders from there. Rather than retype eight articles into C#
// and let the two drift, this reads web/data/destinations.json and
// web/data/places.ts and writes one JSON file the seeder embeds.
//
//   node server/scripts/export-seed.mjs
//
// Once the web app fetches /api/v1/places, the direction reverses: the database
// becomes the source and web/data/destinations.json goes away. This script is
// the bridge between those two states, and it should be deleted on the commit
// that removes the JSON.

import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const repo = join(here, '..', '..');
const web = join(repo, 'web');
const out = join(repo, 'server', 'src', 'Lubnan.Infrastructure', 'Persistence', 'Seed', 'places.seed.json');

/** The frontend spells these in prose; the domain spells them as enum members. */
const REGIONS = {
  Coast: 'Coast',
  'Mount Lebanon': 'MountLebanon',
  North: 'North',
  South: 'South',
  Bekaa: 'Bekaa',
};

const CATEGORIES = {
  ruins: 'Ruins',
  nature: 'Nature',
  mountains: 'Mountains',
  coast: 'Coast',
  city: 'City',
};

/**
 * places.ts is TypeScript, but the object literal inside it is plain JS: the
 * type annotations are all on the declaration, none inside the braces. So the
 * literal can be lifted out and evaluated rather than parsed.
 *
 * Brace counting rather than a regex, because the bodies contain braces in
 * prose and a lazy match would stop at the first one.
 */
function liftObjectLiteral(source, declaration) {
  const start = source.indexOf(declaration);
  if (start === -1) throw new Error(`Could not find "${declaration}" — has places.ts been restructured?`);

  const open = source.indexOf('{', start);
  let depth = 0;
  let inString = null;

  for (let i = open; i < source.length; i++) {
    const c = source[i];

    if (inString) {
      if (c === '\\') i++;
      else if (c === inString) inString = null;
      continue;
    }

    if (c === "'" || c === '"' || c === '`') inString = c;
    else if (c === '{') depth++;
    else if (c === '}' && --depth === 0) {
      // eslint-disable-next-line no-eval
      return eval(`(${source.slice(open, i + 1)})`);
    }
  }

  throw new Error('Unbalanced braces while lifting the object literal.');
}

// Several of these files were written on Windows and carry a byte-order mark,
// which JSON.parse treats as a syntax error rather than as whitespace.
const read = (...parts) => readFileSync(join(web, ...parts), 'utf8').replace(/^﻿/, '');

const destinations = JSON.parse(read('data', 'destinations.json'));
const extra = liftObjectLiteral(read('data', 'places.ts'), 'const extra');

const places = destinations.map((d, index) => {
  const e = extra[d.id];
  if (!e) throw new Error(`${d.id} is in destinations.json but not in places.ts.`);

  const region = REGIONS[d.region];
  const category = CATEGORIES[d.category];
  if (!region) throw new Error(`${d.id}: unmapped region "${d.region}".`);
  if (!category) throw new Error(`${d.id}: unmapped category "${d.category}".`);

  return {
    slug: d.id,
    region,
    category,
    displayOrder: index,
    latitude: e.lat,
    longitude: e.lon,
    plates: {
      hero: e.heroPlate ?? null,
      frame: e.framePlate ?? null,
      subject: e.subject ?? null,
      rail: d.plateRail ?? null,
      mosaic: d.plateMosaic ?? null,
    },
    // English only, and deliberately.
    //
    // The French and Arabic articles have not been written. Seeding them with
    // the English body under an "ar" label would be worse than leaving them
    // absent: the API would serve English prose while claiming to have served
    // Arabic. Absent, the fallback runs, the response says locale "en", and the
    // client can mark the page as untranslated — which is true.
    translations: {
      en: {
        name: d.name,
        localName: d.localName === d.name ? null : d.localName,
        note: d.note,
        standfirst: e.standfirst,
        body: e.body,
      },
    },
    callouts: (e.callouts ?? []).map((c) => ({
      x: c.x,
      y: c.y,
      text: { en: { label: c.label, body: c.body } },
    })),
    practical: (e.practical ?? []).map((p) => ({
      text: { en: { label: p.label, value: p.value } },
    })),
  };
});

mkdirSync(dirname(out), { recursive: true });
writeFileSync(out, `${JSON.stringify(places, null, 2)}\n`, 'utf8');

const callouts = places.reduce((n, p) => n + p.callouts.length, 0);
const facts = places.reduce((n, p) => n + p.practical.length, 0);
console.log(`${places.length} places, ${callouts} callouts, ${facts} practical facts -> ${out}`);
