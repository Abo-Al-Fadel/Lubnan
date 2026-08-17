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

/**
 * Phone-crop suffixes.
 *
 * Three conventions, because all three are natural to type and guessing wrong
 * silently serves the desktop crop instead of failing loudly. `A1M` is what the
 * exports actually arrived as.
 */
export const PHONE_SUFFIXES = ['M', '-m', '_m', 'm'] as const;

/** The series folder for a plate ID: the leading alphabetic run, upper-cased. */
export function plateSeries(id: string): string {
  return (id.match(/^[A-Za-z]+/)?.[0] ?? 'misc').toUpperCase();
}

/** `/img/J/J4.png` */
export function platePath(id: string, ext: string): string {
  return `/img/${plateSeries(id)}/${id}.${ext}`;
}

/** Every phone-crop candidate for a plate, in probe order. */
export function phoneCandidates(id: string): string[] {
  const out: string[] = [];
  for (const suffix of PHONE_SUFFIXES) {
    for (const ext of PLATE_EXTENSIONS) out.push(platePath(`${id}${suffix}`, ext));
  }
  return out;
}

/** Cut-out candidates: alpha formats only, series folder then a flat fallback. */
export function cutoutCandidates(id: string): string[] {
  return [
    ...CUTOUT_EXTENSIONS.map((ext) => platePath(id, ext)),
    ...CUTOUT_EXTENSIONS.map((ext) => `/img/cutouts/${id}.${ext}`),
  ];
}

/** `/vid/A1.mp4`. Video is not an image and does not live under /img. */
export function videoPath(id: string): string {
  return `/vid/${id}.mp4`;
}

/**
 * Motion plates, lightest first.
 *
 * The raw A1 export is hundreds of megabytes. Prefer a web encode or a phone
 * cut when someone has dropped one in; otherwise the full file still plays.
 */
export function videoCandidates(id: string): string[] {
  return [`/vid/${id}-web.mp4`, `/vid/${id}-m.mp4`, `/vid/${id}M.mp4`, videoPath(id)];
}
