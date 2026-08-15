'use client';

import { useEffect, useRef, useState } from 'react';
import { subjects } from '@/components/ui/Subjects';
import type { Variation } from '@/data/variations';

/**
 * Cut-outs need an alpha channel, so only formats that carry one. Both
 * locations are tried: alongside the plates in /img/ (where they naturally
 * get dropped) and in /img/cutouts/ for anyone who prefers them separated.
 */
const CANDIDATES = (plate: string) => [
  `/img/${plate}.png`,
  `/img/${plate}.webp`,
  `/img/cutouts/${plate}.png`,
  `/img/cutouts/${plate}.webp`,
];

/**
 * The subject that occludes the wordmark.
 *
 * Looks for `/img/cutouts/<plate>.png` (then `.webp`) and falls back to the
 * drawn SVG silhouette when neither is there. Same contract as PhotoField, so
 * dropping B1/B2/B3 into public/img/cutouts is the whole integration step.
 *
 * Height is set and width left auto, so the file's own aspect ratio drives the
 * shape. That means the cut-out does not have to be square — whatever crop the
 * generator produced will sit correctly as long as the subject touches the
 * bottom edge of the canvas, since this is anchored to the ground line.
 *
 * The drawn fallback is tinted with `--subject`; a real photographic cut-out
 * carries its own colour and is left alone.
 */
export function HeroSubject({
  variation,
  className = '',
}: {
  variation: Variation;
  className?: string;
}) {
  const [index, setIndex] = useState(0);
  const imgRef = useRef<HTMLImageElement>(null);
  const Drawn = subjects[variation.subject];
  const urls = CANDIDATES(variation.cutoutPlate);

  /* A candidate that 404s from cache is already `complete` with zero width by
     the time React hydrates, and fires no `error` event — so the chain would
     stall on a dead URL and never reach the drawn fallback. */
  useEffect(() => {
    const el = imgRef.current;
    if (!el || !el.complete) return;
    if (el.naturalWidth === 0) setIndex((i) => i + 1);
  }, [index]);

  if (index >= urls.length) {
    return <Drawn className={`${className} text-subject`} />;
  }

  return (
    <img
      ref={imgRef}
      key={urls[index]}
      src={urls[index]}
      alt=""
      onError={() => setIndex((i) => i + 1)}
      decoding="async"
      fetchPriority="high"
      className={`${className} w-auto object-contain object-bottom`}
    />
  );
}
