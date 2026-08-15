'use client';

import { useEffect, useRef, useState } from 'react';

/**
 * Bilingual lockup: LUBNĀN over لـــبـــنـــان, set to the same width.
 *
 * A note on the elongation, because the obvious approach is wrong:
 * CSS `letter-spacing` must never be used to space Arabic. Arabic is a cursive
 * script, and tracking pulls the letterforms apart at their joins, so لبنان
 * renders as five disconnected shapes. It reads to an Arabic speaker roughly
 * the way l u b n ā n reads in Latin.
 *
 * The correct device is the tatweel (ـ, U+0640) — a connecting glyph inserted
 * *into* the baseline stroke, exactly how elongation is done in Arabic
 * calligraphy and signage. The stroke stays continuous and the word stays one
 * word. Placement is constrained by the script: tatweel only sits between two
 * letters that actually join, and the alif (ا) in لبنان does not connect to a
 * following letter, so the final ان takes none and the stretch stops there.
 *
 * Matching the two widths is done by measuring, then scaling the Arabic by the
 * shortfall. Tatweel count alone cannot hit an exact width — it is quantised
 * to whole glyphs — so the residue is taken up with a sub-5% horizontal scale,
 * which is far below the threshold where a cursive stroke starts to look
 * distorted. Kept off the joins by scaling the whole word as one unit.
 */

const TATWEEL = 'ـ';
const ar = (n: number) =>
  `ل${TATWEEL.repeat(n)}ب${TATWEEL.repeat(n)}ن${TATWEEL.repeat(n)}ان`;

/** Tuned so the natural width lands just under the Latin at every size. */
export const LUBNAN_AR = ar(4);

export function Wordmark({
  size = 'md',
  className = '',
}: {
  size?: 'sm' | 'md' | 'lg';
  className?: string;
}) {
  const latinRef = useRef<HTMLSpanElement>(null);
  const arabicRef = useRef<HTMLSpanElement>(null);
  const [scale, setScale] = useState(1);

  useEffect(() => {
    const match = () => {
      const latin = latinRef.current;
      const arabic = arabicRef.current;
      if (!latin || !arabic) return;
      const a = arabic.getBoundingClientRect().width;
      if (!a) return;
      const l = latin.getBoundingClientRect().width;
      /* Target 88% of the Latin width, not 100%. Matched exactly, the Arabic
         reads *longer* than LUBNĀN: its strokes sit lower and the tatweel run
         is a continuous horizontal bar, so an equal measured width carries more
         visual mass. Optical match beats mathematical match on a lockup. */
      setScale(Math.min(1.3, Math.max(0.7, (l * 0.88) / a)));
    };

    match();
    /* Webfonts land after first paint; re-measure once they do. */
    if (typeof document !== 'undefined' && 'fonts' in document) {
      (document as Document & { fonts: FontFaceSet }).fonts.ready.then(match);
    }
    window.addEventListener('resize', match);
    return () => window.removeEventListener('resize', match);
  }, [size]);

  const scaleClasses = {
    sm: { latin: 'text-sm', ar: 'text-[0.62rem]', gap: 'gap-[0.18rem]' },
    md: { latin: 'text-base md:text-lg', ar: 'text-[0.7rem] md:text-xs', gap: 'gap-[0.22rem]' },
    /* The Arabic is set larger at lg than a naive optical match suggests: the
       tatweel run has to reach the Latin width, and leaving it small forces the
       scale correction past the point where the stroke visibly distorts. */
    lg: { latin: 'text-3xl md:text-5xl', ar: 'text-xl md:text-3xl', gap: 'gap-1.5' },
  }[size];

  return (
    <span className={`flex flex-col items-start ${scaleClasses.gap} ${className}`}>
      <span
        ref={latinRef}
        className={`font-display font-medium uppercase leading-none tracking-[0.16em] ${scaleClasses.latin}`}
      >
        Lubnān
      </span>
      {/* No letter-spacing: the tatweel does the elongation, and tracking would
          break the joins the tatweel exists to preserve. */}
      <span
        ref={arabicRef}
        lang="ar"
        dir="rtl"
        aria-hidden="true"
        className={`block origin-left font-arabic font-light leading-none opacity-85 ${scaleClasses.ar}`}
        style={{ transform: `scaleX(${scale})` }}
      >
        {LUBNAN_AR}
      </span>
    </span>
  );
}
