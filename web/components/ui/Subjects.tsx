/**
 * Hero subjects, drawn rather than masked.
 *
 * impeccable bans a geometric mask approximating a photographic subject's
 * contour. Nothing photographic sits under these — they are authored artwork
 * that occludes the hero word, so the depth relationship is real. When real
 * photography or a rendered plate arrives, each of these is replaced by a
 * cut-out asset with a true alpha matte, not by a polygon over the image.
 */

type SubjectProps = { className?: string };

/** Raouche — the Pigeon Rocks sea stacks, arch cut with evenodd. */
export function ArchStack({ className = '' }: SubjectProps) {
  return (
    <svg
      viewBox="0 0 320 260"
      className={className}
      fill="currentColor"
      aria-hidden="true"
      preserveAspectRatio="xMidYMax meet"
    >
      {/* Near stack, arch cut with evenodd. Angular rather than rounded — the
          curved version read as a blob at hero scale. */}
      <path
        fillRule="evenodd"
        d="M46 260l7-118 15-36 24-28 32-11 36 9 24 26 15 42 9 116zM98 260l3-80 13-36 23-15 25 15 12 36 4 80z"
      />
      {/* Far stack, offset and shorter so the pair reads as two rocks. */}
      <path d="M214 260l4-92 12-30 18-16 22 10 12 30 6 98z" />
      {/* Waterline chop at the base. */}
      <path d="M0 260v-18l40 6 46-4 40 8 44-6 42 6 48-8 60 6v10z" opacity="0.5" />
    </svg>
  );
}

/** Lebanese cedar — flat crown, stacked horizontal boughs. */
export function CedarSilhouette({ className = '' }: SubjectProps) {
  return (
    <svg
      viewBox="0 0 320 260"
      className={className}
      fill="currentColor"
      aria-hidden="true"
      preserveAspectRatio="xMidYMax meet"
    >
      <path d="M152 260h16v-52h-16z" />
      <path d="M160 24c-14 0-26 5-34 12 10-1 18 0 24 3-16 2-30 9-39 18 13-3 25-3 34-1-22 5-41 16-53 29 20-6 39-8 54-6-28 8-52 22-66 38 26-10 51-15 72-14-34 11-62 29-78 48 32-15 63-23 90-24-25 12-45 26-57 40 40-20 78-29 112-29v-14z" />
      <path d="M160 24c14 0 26 5 34 12-10-1-18 0-24 3 16 2 30 9 39 18-13-3-25-3-34-1 22 5 41 16 53 29-20-6-39-8-54-6 28 8 52 22 66 38-26-10-51-15-72-14 34 11 62 29 78 48-32-15-63-23-90-24 25 12 45 26 57 40-40-20-78-29-112-29v-14z" />
    </svg>
  );
}

/** Baalbek — the six standing columns of the Temple of Jupiter. */
export function ColumnRow({ className = '' }: SubjectProps) {
  const xs = [42, 82, 122, 162, 202, 242];
  return (
    <svg
      viewBox="0 0 320 260"
      className={className}
      fill="currentColor"
      aria-hidden="true"
      preserveAspectRatio="xMidYMax meet"
    >
      <path d="M22 34h276v22H22z" />
      <path d="M30 56h260v14H30z" />
      {xs.map((x) => (
        <g key={x}>
          <path d={`M${x - 3} 70h34v10h-34z`} />
          <path d={`M${x + 1} 80h26v168h-26z`} />
          <path d={`M${x - 4} 248h36v12h-36z`} />
        </g>
      ))}
    </svg>
  );
}

/** A headland dropping into the sea. */
export function Headland({ className = '' }: SubjectProps) {
  return (
    <svg
      viewBox="0 0 320 260"
      className={className}
      fill="currentColor"
      aria-hidden="true"
      preserveAspectRatio="xMidYMax meet"
    >
      <path d="M320 260H26q34-22 62-58 26-34 52-76 22-36 56-52 46-22 124-16z" />
      <path d="M0 260v-18q52 10 104 4l-6 14z" opacity="0.5" />
    </svg>
  );
}

export const subjects = {
  arch: ArchStack,
  cedar: CedarSilhouette,
  columns: ColumnRow,
  headland: Headland,
} as const;

/**
 * Like affordance. Drawn rather than a ♥/♡ glyph, which impeccable bans as
 * unicode standing in for an icon system. One stroke weight, filled state
 * swaps fill rather than swapping character.
 */
export function Heart({ filled = false, className = '' }: SubjectProps & { filled?: boolean }) {
  return (
    <svg
      viewBox="0 0 20 20"
      width="14"
      height="14"
      className={className}
      fill={filled ? 'currentColor' : 'none'}
      stroke="currentColor"
      strokeWidth="1.5"
      aria-hidden="true"
    >
      <path d="M10 16.5S2.75 12.2 2.75 7.4A3.9 3.9 0 0 1 10 5.3a3.9 3.9 0 0 1 7.25 2.1c0 4.8-7.25 9.1-7.25 9.1Z" />
    </svg>
  );
}

/** Print registration crosshair, reused as a hairline joint marker. */
export function Crosshair({ className = '' }: SubjectProps) {
  return (
    <svg
      viewBox="0 0 16 16"
      className={`pointer-events-none ${className}`}
      width="14"
      height="14"
      aria-hidden="true"
    >
      <path d="M8 1v14M1 8h14" stroke="currentColor" strokeWidth="0.75" opacity="0.65" />
    </svg>
  );
}
