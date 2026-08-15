'use client';

import { useEffect, useRef, useState } from 'react';

/** Tried in order. PNG first — that is what the plates are exported as. */
const EXTENSIONS = ['png', 'jpg'] as const;

/**
 * An image slot.
 *
 * Pass a `plate` ID and it resolves `/img/<plate>.png`, then `.jpg`. On phones
 * it also looks for `<plate>-m.png` / `.jpg` and uses that instead when it
 * exists. If nothing resolves it falls back to a tonal field in the palette's
 * photographic range, so the page works with zero, some, or all plates present
 * and dropping a correctly named file into /public/img is the whole job.
 *
 * The phone variant is *probed* with an off-DOM Image() and only swapped in on
 * a successful load. An earlier version tried to express this as
 * `<source srcSet="…-m.png, ….png">`, which cannot work: srcset picks its
 * candidate before fetching, so a 404 never re-selects and a phone missing the
 * -m file would show the placeholder while the landscape plate sat unused.
 * Probing costs one extra request on phones and is honest about what exists.
 */
export function PhotoField({
  brief,
  showSlots,
  plate,
  className = '',
  variant = 'mid',
  priority = false,
  objectPosition,
  video,
  children,
}: {
  brief: string;
  showSlots: boolean;
  /** Plate ID from docs/image-prompts.md, e.g. "A1", "C3r", "D2". */
  plate?: string;
  className?: string;
  variant?: 'mid' | 'low' | 'high';
  /** Set on the hero so the browser fetches it first. */
  priority?: boolean;
  /** Crop anchor, e.g. "50% 30%" to hold a horizon on tall crops. */
  objectPosition?: string;
  /** Plate ID of an .mp4 to play over the still once it can. Desktop only. */
  video?: string;
  children?: React.ReactNode;
}) {
  const [extIndex, setExtIndex] = useState(0);
  const [phoneSrc, setPhoneSrc] = useState<string | null>(null);
  const [loaded, setLoaded] = useState(false);
  const [videoSrc, setVideoSrc] = useState<string | null>(null);
  const [videoReady, setVideoReady] = useState(false);
  const imgRef = useRef<HTMLImageElement>(null);

  useEffect(() => {
    setExtIndex(0);
    setPhoneSrc(null);
    if (!plate || typeof window === 'undefined') return;
    if (!window.matchMedia('(max-width: 767px)').matches) return;

    /* Three naming conventions, because all three are natural to type and
       guessing wrong just silently serves the desktop crop. `A1M` is what the
       exports actually arrived as. */
    const candidates: string[] = [];
    for (const suffix of ['M', '-m', '_m', 'm']) {
      for (const ext of EXTENSIONS) candidates.push(`/img/${plate}${suffix}.${ext}`);
    }

    let cancelled = false;
    const probe = (i: number) => {
      if (cancelled || i >= candidates.length) return;
      const url = candidates[i];
      const img = new window.Image();
      img.onload = () => {
        if (!cancelled) setPhoneSrc(url);
      };
      img.onerror = () => probe(i + 1);
      img.src = url;
    };
    probe(0);
    return () => {
      cancelled = true;
    };
  }, [plate]);

  const exhausted = extIndex >= EXTENSIONS.length;
  const src = phoneSrc ?? (plate ? `/img/${plate}.${EXTENSIONS[extIndex]}` : null);
  const showImage = Boolean(src) && (!exhausted || Boolean(phoneSrc));

  /**
   * Catch images that finished before React attached its handlers.
   *
   * The element is server-rendered and the browser starts fetching immediately,
   * so on a cold load the `load` event routinely fires *before* hydration —
   * `onLoad` never runs, the image stays at opacity 0, and the page looks
   * empty until something forces a client-side remount. Switching variations
   * did exactly that, which is why the images "came back".
   *
   * `complete` plus a non-zero `naturalWidth` is the only reliable way to ask
   * an <img> whether it already succeeded. A cached 404 is `complete` with
   * width 0, and that never fires `error` after hydration either, so the
   * fallback chain has to be advanced here too.
   */
  useEffect(() => {
    setLoaded(false);
    if (!src) return;
    const el = imgRef.current;
    if (!el || !el.complete) return;
    if (el.naturalWidth > 0) setLoaded(true);
    else if (phoneSrc) setPhoneSrc(null);
    else setExtIndex((i) => i + 1);
  }, [src, phoneSrc]);

  /**
   * The motion plate.
   *
   * The still is the first frame, so it is never *replaced* — the video fades
   * in on top of it once it has enough buffered to run, and if it stalls, is
   * refused autoplay, or the file is missing, what stays on screen is the
   * photograph that was already there. Nothing to fall back to because nothing
   * was taken away.
   *
   * Gated to wide viewports and to users who have not asked for reduced motion.
   * A1.mp4 is ~40MB; that is fine on a desktop connection and hostile on a
   * phone, which gets the still and the portrait crop instead. Mounting is
   * deferred until the still has decoded so the two never compete for
   * bandwidth on the critical path.
   */
  useEffect(() => {
    if (!video || !loaded || typeof window === 'undefined') return;
    if (!window.matchMedia('(min-width: 768px)').matches) return;
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;
    const id = window.setTimeout(() => setVideoSrc(`/img/${video}.mp4`), 400);
    return () => window.clearTimeout(id);
  }, [video, loaded]);

  const tone =
    variant === 'low'
      ? 'linear-gradient(168deg, var(--photo-c) 0%, var(--photo-a) 62%, var(--photo-b) 100%)'
      /* `high` is what the heroes use, and hero type is eggshell. The old
         ramp opened on the lightest photographic tone, so an unfilled hero
         slot was light-on-light and unreadable — which made empty layouts
         impossible to judge. It now opens and closes on the darkest tone. */
      : variant === 'high'
        ? 'linear-gradient(196deg, var(--photo-c) 0%, var(--photo-a) 55%, var(--photo-c) 100%)'
        : 'linear-gradient(184deg, var(--photo-a) 0%, var(--photo-b) 44%, var(--photo-c) 100%)';

  /* `grain::after` needs a positioned ancestor, so this once hardcoded
     `relative` in the base classes. That silently broke every caller passing
     `absolute inset-0`: Tailwind emits `.relative` after `.absolute`, so the
     later rule won regardless of class order and the field collapsed to zero
     height. Only supply a position when the caller has not chosen one. */
  const positioned = /\b(absolute|fixed|sticky)\b/.test(className);

  return (
    <div
      className={`grain overflow-hidden ${positioned ? '' : 'relative'} ${className}`}
      style={{ background: tone }}
      role="img"
      aria-label={brief}
    >
      {showImage && src ? (
        <img
          ref={imgRef}
          key={src}
          src={src}
          alt=""
          /* Hidden until it decodes: Chrome paints its broken-image glyph the
             moment a src 404s, so walking the extension chain flashed a
             torn-page icon on every slot with no plate yet. Driven by React
             state rather than by mutating style directly, so the effect above
             can rescue an image that loaded before hydration. */
          onLoad={() => setLoaded(true)}
          style={{
            opacity: loaded ? 1 : 0,
            ...(objectPosition ? { objectPosition } : {}),
          }}
          onError={() => {
            if (phoneSrc) setPhoneSrc(null);
            else setExtIndex((i) => i + 1);
          }}
          loading={priority ? 'eager' : 'lazy'}
          fetchPriority={priority ? 'high' : 'auto'}
          decoding="async"
          className="absolute inset-0 h-full w-full object-cover transition-opacity duration-500 ease-out"
        />
      ) : null}

      {videoSrc ? (
        <video
          src={videoSrc}
          autoPlay
          muted
          loop
          playsInline
          preload="auto"
          aria-hidden="true"
          tabIndex={-1}
          onCanPlay={() => setVideoReady(true)}
          onError={() => setVideoSrc(null)}
          style={{
            opacity: videoReady ? 1 : 0,
            ...(objectPosition ? { objectPosition } : {}),
          }}
          className="pointer-events-none absolute inset-0 h-full w-full object-cover transition-opacity duration-1000 ease-out"
        />
      ) : null}

      {children}

      {showSlots ? (
        <div className="absolute inset-0 z-30 flex items-center justify-center bg-black/55 p-4">
          <p className="micro max-w-[44ch] text-center leading-[1.9] text-white">
            <span className="mr-2 inline-block border border-white/40 px-1.5 py-0.5">
              {plate ?? 'Image'}
            </span>
            {brief}
          </p>
        </div>
      ) : null}
    </div>
  );
}
