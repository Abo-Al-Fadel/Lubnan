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
 * The motion plate, if one has been dropped in.
 *
 * This used to try `-web`, `-m` and `M` suffixes first, on the theory that
 * someone might supply a lighter encode. Nobody ever did, so every visit spent
 * three requests collecting three 404s before reaching the file that exists —
 * on the homepage, for every visitor, in production too, where the mp4s are
 * gitignored and *none* of the four resolve.
 *
 * One candidate. A lighter encode belongs at `/vid/<id>.mp4`, replacing the
 * heavy one, rather than as a naming convention nothing writes.
 */
export function videoCandidates(id: string): string[] {
  return [videoPath(id)];
}
