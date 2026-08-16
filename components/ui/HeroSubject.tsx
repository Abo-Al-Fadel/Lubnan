'use client';

import { useEffect, useRef, useState } from 'react';
import { subjects } from '@/components/ui/Subjects';
import { cutoutCandidates } from '@/lib/plates';
import type { Variation } from '@/data/variations';

/**
 * The subject that occludes the wordmark.
 *
 * Resolves through lib/plates, so `K4` becomes `/img/K/K4.png`, and falls back
 * to the drawn SVG silhouette when no file resolves.
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
  const urls = cutoutCandidates(variation.cutoutPlate);

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
