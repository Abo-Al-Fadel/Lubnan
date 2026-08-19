'use client';

import { useEffect, useRef, useState } from 'react';
import { useMediaQuery } from '@/lib/media';
import { PLATE_EXTENSIONS, platePath, plateDerivatives, videoCandidates } from '@/lib/plates';

/**
 * An image slot.
 *
 * Pass a `plate` ID and it resolves `/img/<plate>.png`, then `.jpg`. The same
 * file is used on every viewport — phones crop with `object-cover` rather than
 * swapping in a different plate. A separate phone still (A1M and the like)
 * made the first frame a different photograph from desktop, and leftover
 * inspect-mode state could lock that crop onto a wide window.
 *
 * If nothing resolves it falls back to a tonal field in the palette's
 * photographic range, so the page works with zero, some, or all plates present
 * and dropping a correctly named file into /public/img is the whole job.
 */
export function PhotoField({
  brief,
  showSlots,
  plate,
  className = '',
  variant = 'mid',
  priority = false,
  sizes = '(max-width: 767px) 100vw, (max-width: 1279px) 50vw, 33vw',
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
  /** Plate ID of an .mp4 to play over the still once it can. */
  video?: string;
  /**
   * How wide this plate will actually be painted, for picking a candidate.
   *
   * The default assumes a grid: full width on a phone, half on a tablet, a
   * third on a desktop. A full-bleed hero should pass "100vw" — getting this
   * wrong only costs the wrong candidate, never a broken image, which is why
   * a reasonable default beats requiring every caller to think about it.
   */
  sizes?: string;
  children?: React.ReactNode;
}) {
  const [extIndex, setExtIndex] = useState(0);
  const [loaded, setLoaded] = useState(false);
  const [videoSrc, setVideoSrc] = useState<string | null>(null);
  const [videoReady, setVideoReady] = useState(false);
  const imgRef = useRef<HTMLImageElement>(null);
  const videoRef = useRef<HTMLVideoElement>(null);
  const isPhone = useMediaQuery('(max-width: 767px)');
  const reduceMotion = useMediaQuery('(prefers-reduced-motion: reduce)');

  useEffect(() => {
    setExtIndex(0);
  }, [plate]);

  const exhausted = extIndex >= PLATE_EXTENSIONS.length;
  const src = plate && !exhausted ? platePath(plate, PLATE_EXTENSIONS[extIndex]) : null;
  const showImage = Boolean(src);

  /* Only the PNG has derivatives. Once the chain has fallen through to .jpg
     the source is a one-off that the optimiser never saw, so offering an AVIF
     that does not exist would make <picture> pick a 404 and stop - <source> has
     no error fallback the way <img> does. */
  const derivatives =
    plate && !exhausted && PLATE_EXTENSIONS[extIndex] === 'png' ? plateDerivatives(plate) : null;

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
    else setExtIndex((i) => i + 1);
  }, [src]);

  /**
   * The motion plate.
   *
   * The still is the first frame, so it is never *replaced* — the video fades
   * in on top of it once it has enough buffered to run, and if it stalls, is
   * refused autoplay, or the file is missing, what stays on screen is the
   * photograph that was already there. Nothing to fall back to because nothing
   * was taken away.
   *
   * Plays on phones too (muted, inline). An earlier gate at 768px left mobile
   * on the still forever, and because the query was read once, closing
   * DevTools inspect left the phone crop locked on a desktop frame.
   *
   * The source is chosen once per plate so toggling inspect does not reload
   * a several-hundred-megabyte file. Playback is kicked again on resize and
   * visibility — Chrome pauses media when the device toolbar opens.
   *
   * Mounting waits until the still has decoded so the two never compete for
   * bandwidth on the critical path. Reduced-motion users stay on the still.
   */
  useEffect(() => {
    if (!video || !loaded || reduceMotion) {
      if (!video || reduceMotion) {
        setVideoSrc(null);
        setVideoReady(false);
      }
      return;
    }
    if (videoSrc) return;

    let cancelled = false;
    const id = window.setTimeout(() => {
      const candidates = videoCandidates(video, isPhone);
      const probe = async (i: number) => {
        if (cancelled) return;
        if (i >= candidates.length) {
          setVideoSrc(candidates[candidates.length - 1] ?? null);
          return;
        }
        const url = candidates[i];
        try {
          const res = await fetch(url, { method: 'HEAD' });
          if (cancelled) return;
          if (res.ok || res.status === 405) {
            setVideoSrc(url);
            return;
          }
        } catch {
          /* HEAD can fail on odd static hosts; try the next name. */
        }
        await probe(i + 1);
      };
      void probe(0);
    }, 400);

    return () => {
      cancelled = true;
      window.clearTimeout(id);
    };
  }, [video, loaded, reduceMotion, videoSrc]);

  useEffect(() => {
    const el = videoRef.current;
    if (!el || !videoSrc) return;

    const arm = (node: HTMLVideoElement) => {
      node.muted = true;
      node.defaultMuted = true;
      node.setAttribute('playsinline', '');
      node.setAttribute('webkit-playsinline', '');
    };
    arm(el);

    const kick = () => {
      const node = videoRef.current;
      if (!node || document.visibilityState !== 'visible') return;
      arm(node);
      if (node.readyState >= 2) setVideoReady(true);
      void node.play().catch(() => {
        /* Autoplay refused: the still stays. */
      });
    };

    const onVis = () => {
      if (document.visibilityState === 'visible') kick();
    };

    kick();
    document.addEventListener('visibilitychange', onVis);
    window.addEventListener('pageshow', kick);
    window.addEventListener('resize', kick);
    window.addEventListener('orientationchange', kick);
    window.visualViewport?.addEventListener('resize', kick);

    const io = new IntersectionObserver(
      (entries) => {
        const entry = entries[0];
        if (!entry) return;
        if (entry.isIntersecting) kick();
        else videoRef.current?.pause();
      },
      { threshold: 0.15 },
    );
    io.observe(el);

    return () => {
      document.removeEventListener('visibilitychange', onVis);
      window.removeEventListener('pageshow', kick);
      window.removeEventListener('resize', kick);
      window.removeEventListener('orientationchange', kick);
      window.visualViewport?.removeEventListener('resize', kick);
      io.disconnect();
    };
  }, [videoSrc]);

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
        /* <picture>, not a bare <img>.
           The plates are archival PNG exports - ninety-nine of them come to
           233 MB, and PNG has no lossy mode, so there is no quality dial to
           turn. scripts/optimise-plates.mjs derives AVIF and WebP next to each
           source; the same files at 2560px wide come to 14 MB, a 94% saving,
           and that is the difference between a thirty-megabyte page and a
           two-megabyte one.
           The PNG stays last so a plate with no derivative still renders, and
           the browser picks the first format it understands - no JavaScript,
           no layout shift, and the existing fallback chain below is untouched. */
        <picture>
          {derivatives ? (
            <>
              <source srcSet={derivatives.avif} sizes={sizes} type="image/avif" />
              <source srcSet={derivatives.webp} sizes={sizes} type="image/webp" />
            </>
          ) : null}
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
            setExtIndex((i) => i + 1);
          }}
          loading={priority ? 'eager' : 'lazy'}
          fetchPriority={priority ? 'high' : 'auto'}
          decoding="async"
          className="absolute inset-0 h-full w-full object-cover transition-opacity duration-300 ease-out motion-reduce:transition-none"
        />
        </picture>
      ) : null}

      {videoSrc ? (
        <video
          ref={videoRef}
          src={videoSrc}
          autoPlay
          muted
          loop
          playsInline
          preload={isPhone ? 'metadata' : 'auto'}
          aria-hidden="true"
          tabIndex={-1}
          onCanPlay={() => {
            setVideoReady(true);
            void videoRef.current?.play().catch(() => undefined);
          }}
          onPlaying={() => setVideoReady(true)}
          onError={() => {
            setVideoReady(false);
            setVideoSrc(null);
          }}
          style={{
            opacity: videoReady ? 1 : 0,
            ...(objectPosition ? { objectPosition } : {}),
          }}
          className="pointer-events-none absolute inset-0 h-full w-full object-cover transition-opacity duration-1000 ease-out"
        />
      ) : null}

      {children}

      {showSlots ? (
        <div
          data-slot-overlay
          className="absolute inset-0 z-30 flex items-center justify-center bg-black/55 p-4"
        >
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
