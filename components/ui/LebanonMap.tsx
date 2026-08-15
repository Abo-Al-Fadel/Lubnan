'use client';

import { useEffect, useId, useRef, useState } from 'react';
import { BORDER, COASTLINE } from '@/data/lebanon-border';

/**
 * The country, drawn.
 *
 * yosemite-route-line-overlay: a hairline graphic laid over the photograph
 * that encodes the actual journey rather than decorating the frame. Here the
 * hairline is Lebanon's own border and coastline, so the one graphic element
 * on the page carries information.
 *
 * The outline is the real national boundary, from geoBoundaries ADM0 (public
 * domain), simplified to 361 points and projected here. It replaces a 26-point
 * outline I had picked by eye, which read as a tracing: straight runs where
 * the coast actually bends, and a southern border in the wrong place.
 * Equirectangular with a cos(φ) correction is accurate at this scale and keeps
 * the projection legible as three lines of arithmetic.
 */

export type Waypoint = {
  id: string;
  name: string;
  lon: number;
  lat: number;
  /** Which side to hang the label on, so labels never cross the outline. */
  side?: 'left' | 'right';
};

/* Bounds taken from the data itself, with a small margin, so the country fills
   the frame instead of floating inside guessed extents. */
const LON = [35.06, 36.67] as const;
const LAT = [33.0, 34.74] as const;
const W = 420;
const H = 760;
const PAD = 26;
/** Space reserved either side of the projection for waypoint labels. */
const LABEL_ROOM = 170;

/** Equirectangular, x compressed by cos of the mid-latitude. */
function project([lon, lat]: [number, number]): [number, number] {
  const midLat = ((LAT[0] + LAT[1]) / 2) * (Math.PI / 180);
  const spanLon = (LON[1] - LON[0]) * Math.cos(midLat);
  const spanLat = LAT[1] - LAT[0];
  const x = (((lon - LON[0]) * Math.cos(midLat)) / spanLon) * (W - PAD * 2) + PAD;
  const y = H - PAD - ((lat - LAT[0]) / spanLat) * (H - PAD * 2);
  return [Number(x.toFixed(1)), Number(y.toFixed(1))];
}

const toPath = (ring: [number, number][], close = false) =>
  ring.map(project).map(([x, y], i) => `${i ? 'L' : 'M'}${x} ${y}`).join(' ') + (close ? ' Z' : '');

export function LebanonMap({
  waypoints,
  activeId,
  onSelect,
  className = '',
}: {
  waypoints: Waypoint[];
  activeId?: string | null;
  onSelect?: (id: string) => void;
  className?: string;
}) {
  const uid = useId().replace(/:/g, '');
  const ref = useRef<SVGSVGElement>(null);
  const [drawn, setDrawn] = useState(false);

  /* Draw the border once it is on screen, and fail visible: if the observer
     never fires the outline is simply already there rather than permanently
     invisible — the same failure mode that left the landing reveals blank. */
  useEffect(() => {
    const el = ref.current;
    if (!el) return;
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
      setDrawn(true);
      return;
    }
    const io = new IntersectionObserver(
      (entries) => {
        if (entries.some((e) => e.isIntersecting)) {
          setDrawn(true);
          io.disconnect();
        }
      },
      { threshold: 0.15 },
    );
    io.observe(el);
    const failsafe = window.setTimeout(() => setDrawn(true), 2600);
    return () => {
      io.disconnect();
      window.clearTimeout(failsafe);
    };
  }, []);

  const border = toPath(BORDER, true);
  const coast = toPath(COASTLINE);

  /**
   * Label layout.
   *
   * Two things go wrong without it. Qadisha and the Cedars are eleven minutes
   * of latitude apart, so their labels printed on top of each other; and a
   * long name hung off an eastern pin ran past the frame and was clipped by
   * the section's overflow. So: choose the side that has room, then walk down
   * each side pushing any label that would touch its neighbour.
   */
  const CHAR = 7.4; // approx px per uppercase glyph at 12px with tracking
  const MIN_GAP = 17;

  const placed = waypoints
    .map((w) => {
      const [x, y] = project([w.lon, w.lat]);
      const width = w.name.length * CHAR;
      // Prefer the requested side, but flip if the label would leave the frame.
      let right = w.side !== 'left';
      if (right && x + 32 + width > W + LABEL_ROOM) right = false;
      if (!right && x - 32 - width < -LABEL_ROOM) right = true;
      return { w, x, y, width, right, labelY: y };
    })
    .sort((a, b) => a.y - b.y);

  for (const side of [true, false]) {
    let lastY = -Infinity;
    for (const p of placed) {
      if (p.right !== side) continue;
      if (p.labelY - lastY < MIN_GAP) p.labelY = lastY + MIN_GAP;
      lastY = p.labelY;
    }
  }

  return (
    /* The viewBox is wider than the projection so labels live *inside* it.
       With `overflow: visible` they hung outside the box and were cut off by
       the first ancestor with overflow hidden — "The Cedars of God" lost its
       last word. */
    <svg
      ref={ref}
      viewBox={`${-LABEL_ROOM} 0 ${W + LABEL_ROOM * 2} ${H}`}
      className={className}
      role="img"
      aria-label="Map of Lebanon with the destinations marked"
    >
      <defs>
        <clipPath id={`${uid}-land`}>
          <path d={border} />
        </clipPath>
      </defs>

      {/* Land wash, so the country reads as a body and not just an edge. */}
      <path d={border} fill="var(--hero-ink)" opacity={drawn ? 0.07 : 0} className="map-fade" />

      {/* The mountain ranges used to be drawn here as two dashed strokes. They
          are gone: at a thin dash they read as a scattering of specks over the
          photograph, and thickened they read as a smear. Neither carried
          information the border and the pins do not already give, and the rule
          on this project is that the one graphic element on a page has to be
          content rather than decoration. */}

      {/* Border, then the coastline drawn heavier over it. */}
      <path
        d={border}
        pathLength={1}
        fill="none"
        stroke="var(--hero-ink)"
        strokeWidth={1}
        opacity={0.45}
        className={drawn ? 'map-draw map-draw--border' : 'map-draw'}
      />
      <path
        d={coast}
        pathLength={1}
        fill="none"
        stroke="var(--hero-ink)"
        strokeWidth={2}
        strokeLinecap="round"
        className={drawn ? 'map-draw map-draw--coast' : 'map-draw'}
      />

      {placed.map(({ w, x, y, labelY, right }, i) => {
        const active = activeId === w.id;
        return (
          <g
            key={w.id}
            className="map-pin"
            style={{ transitionDelay: `${900 + i * 70}ms`, opacity: drawn ? 1 : 0 }}
            onClick={onSelect ? () => onSelect(w.id) : undefined}
            role={onSelect ? 'button' : undefined}
            tabIndex={onSelect ? 0 : undefined}
            onKeyDown={
              onSelect
                ? (e) => {
                    if (e.key === 'Enter' || e.key === ' ') {
                      e.preventDefault();
                      onSelect(w.id);
                    }
                  }
                : undefined
            }
          >
            {/* Elbow, not a straight tick: the label may have been pushed off
                the pin's own latitude to clear its neighbour, and the leader
                has to still point at the place it names. */}
            <path
              d={`M${x} ${y} H${right ? x + 14 : x - 14} V${labelY} H${right ? x + 26 : x - 26}`}
              fill="none"
              stroke="var(--hero-ink)"
              strokeWidth={1}
              opacity={active ? 0.95 : 0.5}
            />
            <circle
              cx={x}
              cy={y}
              r={active ? 5 : 3.2}
              fill={active ? 'var(--accent)' : 'var(--hero-ink)'}
            />
            <text
              x={right ? x + 32 : x - 32}
              y={labelY + 4}
              textAnchor={right ? 'start' : 'end'}
              fill="var(--hero-ink)"
              opacity={active ? 1 : 0.86}
              style={{
                font: '500 12px var(--font-body), sans-serif',
                letterSpacing: '0.1em',
                textTransform: 'uppercase',
                paintOrder: 'stroke',
                stroke: 'var(--scrim-strong)',
                strokeWidth: 3,
                strokeLinejoin: 'round',
              }}
            >
              {w.name}
            </text>
          </g>
        );
      })}
    </svg>
  );
}
