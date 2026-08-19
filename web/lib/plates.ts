/**
 * Where a plate lives.
 *
 * Plates are grouped by series letter under `public/img/`, so `J4` resolves to
 * `/img/J/J4.png`. The grouping keeps a hundred-odd files navigable: a folder
 * listing answers "what is the J series" without opening anything, and a new
 * series is a new folder rather than another eighty entries in one directory.
 *
 * Every consumer resolves paths through here rather than composing strings
 * inline, so the layout on disk can change again without a search across the
 * codebase.
 */

/** Formats tried in order. PNG first: that is what the plates are exported as. */
export const PLATE_EXTENSIONS = ['png', 'jpg'] as const;

/** Formats that carry an alpha channel, for cut-outs. */
export const CUTOUT_EXTENSIONS = ['png', 'webp'] as const;

/** The series folder for a plate ID: the leading alphabetic run, upper-cased. */
export function plateSeries(id: string): string {
  return (id.match(/^[A-Za-z]+/)?.[0] ?? 'misc').toUpperCase();
}

/** `/img/J/J4.png` */
export function platePath(id: string, ext: string): string {
  return `/img/${plateSeries(id)}/${id}.${ext}`;
}

/** Cut-out candidates: alpha formats only, series folder then a flat fallback. */
export function cutoutCandidates(id: string): string[] {
  return [
    ...CUTOUT_EXTENSIONS.map((ext) => platePath(id, ext)),
    ...CUTOUT_EXTENSIONS.map((ext) => `/img/cutouts/${id}.${ext}`),
  ];
}

/**
 * The AVIF and WebP derived from a plate's PNG.
 *
 * Written by `scripts/optimise-plates.mjs` alongside the source. The PNG is
 * kept as the `<img>` fallback inside a `<picture>`, so a plate whose
 * derivatives have not been generated still renders — but every browser in
 * use for a decade takes one of these instead, at roughly a fifteenth of the
 * bytes.
 */
export function plateDerivatives(id: string): { avif: string; webp: string } {
  const base = `/img/${plateSeries(id)}/${id}`;

  // Two candidates, so the browser can decline to download and decode a 2560px
  // plate onto a 390px screen. The saving is bytes twice over: fewer to
  // transfer, and a quarter of the pixels to decode — and AVIF decoding is main
  // thread work, which is the part that reads as slowness even on a fast
  // connection.
  return {
    avif: `${base}-1280.avif 1280w, ${base}.avif 2560w`,
    webp: `${base}-1280.webp 1280w, ${base}.webp 2560w`,
  };
}

/** `/vid/A1.mp4`. Video is not an image and does not live under /img. */
export function videoPath(id: string): string {
  return `/vid/${id}.mp4`;
}

/**
 * The motion plate, lightest usable encode first.
 *
 * Two files, because a hero video is the one asset where the phone/desktop gap
 * actually matters: the desktop encode is more than twice the bytes, and a
 * phone is both the slowest connection and the smallest screen to spend them
 * on. `-mobile` is offered first on a narrow viewport and `-final` everywhere
 * else, and PhotoField probes with HEAD so a missing file costs one request
 * rather than a broken hero.
 *
 * The raw exports are hundreds of megabytes and stay gitignored. Only the two
 * encoded files are committed, which is why the names are specific rather than
 * a convention nothing writes - the last version of this function guessed at
 * three suffixes that had never existed and collected three 404s per visit.
 */
export function videoCandidates(id: string, phone = false): string[] {
  const base = `/vid/${id}`;

  return phone
    ? [`${base}-mobile.mp4`, `${base}-final.mp4`]
    : [`${base}-final.mp4`, `${base}-mobile.mp4`];
}
