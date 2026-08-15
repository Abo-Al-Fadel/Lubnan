'use client';

import { useEffect, useId, useRef, useState } from 'react';

/**
 * The country, drawn.
 *
 * yosemite-route-line-overlay: a hairline graphic laid over the photograph
 * that encodes the actual journey rather than decorating the frame. Here the
 * hairline is Lebanon's own border and coastline, so the one graphic element
 * on the page carries information.
 *
 * The outline is real geography — a simplified lon/lat ring projected here
 * rather than hand-authored path data, so the shape is honest and the
 * waypoints land where the places actually are. Equirectangular with a cos(φ)
 * correction is accurate enough at this scale and keeps the projection legible
 * as three lines of arithmetic.
 */

/** Simplified national boundary, clockwise from the north-west coast. */
const BOUNDARY: [number, number][] = [
  [35.98, 34.63],
  [35.83, 34.48],
  [35.78, 34.42],
  [35.72, 34.3],
  [35.66, 34.25],
  [35.65, 34.12],
  [35.6, 34.0],
  [35.55, 33.95],
  [35.48, 33.9],
  [35.45, 33.82],
  [35.42, 33.7],
  [35.38, 33.56],
  [35.3, 33.4],
  [35.2, 33.27],
  [35.11, 33.09],
  [35.3, 33.06],
  [35.55, 33.25],
  [35.62, 33.25],
  [35.82, 33.42],
  [35.9, 33.62],
  [36.0, 33.82],
  [36.22, 34.0],
  [36.33, 34.2],
  [36.42, 34.42],
  [36.32, 34.55],
  [36.1, 34.63],
];

/** Where the coastline ends and the land border begins, as ring indices. */
const COAST_END = 14;

export type Waypoint = {
  id: string;
  name: string;
  lon: number;
  lat: number;
  /** Which side to hang the label on, so labels never cross the outline. */
  side?: 'left' | 'right';
};

const LON = [35.05, 36.5] as const;
const LAT = [33.0, 34.72] as const;
const W = 420;
const H = 760;
const PAD = 26;

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

  const border = toPath(BOUNDARY, true);
  const coast = toPath(BOUNDARY.slice(0, COAST_END + 1));

  return (
    <svg
      ref={ref}
      viewBox={`0 0 ${W} ${H}`}
      className={className}
      role="img"
      aria-label="Map of Lebanon with the eight destinations marked"
      style={{ overflow: 'visible' }}
    >
      <defs>
        <clipPath id={`${uid}-land`}>
          <path d={border} />
        </clipPath>
      </defs>

      {/* Land wash, so the country reads as a body and not just an edge. */}
      <path d={border} fill="var(--hero-ink)" opacity={drawn ? 0.07 : 0} className="map-fade" />

      {/* The two ranges, clipped to the border. Content: this is why the
          country has a snowline and a coastline an hour apart. */}
      <g clipPath={`url(#${uid}-land)`} opacity={drawn ? 0.5 : 0} className="map-fade">
        {[
          [
            [35.62, 34.42],
            [35.8, 34.2],
            [35.9, 33.98],
            [35.62, 33.7],
            [35.5, 33.4],
          ],
          [
            [36.28, 34.42],
            [36.18, 34.1],
            [36.02, 33.85],
            [35.86, 33.5],
          ],
        ].map((ridge, i) => (
          <path
            key={i}
            d={toPath(ridge as [number, number][])}
            fill="none"
            stroke="var(--hero-ink)"
            strokeWidth={11}
            strokeLinecap="round"
            strokeDasharray="1 15"
            opacity={0.55}
          />
        ))}
      </g>

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

      {waypoints.map((w, i) => {
        const [x, y] = project([w.lon, w.lat]);
        const right = w.side !== 'left';
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
            <line
              x1={x}
              y1={y}
              x2={right ? x + 26 : x - 26}
              y2={y}
              stroke="var(--hero-ink)"
              strokeWidth={1}
              opacity={active ? 0.9 : 0.42}
            />
            <circle
              cx={x}
              cy={y}
              r={active ? 5 : 3.2}
              fill={active ? 'var(--accent)' : 'var(--hero-ink)'}
            />
            <text
              x={right ? x + 32 : x - 32}
              y={y + 4}
              textAnchor={right ? 'start' : 'end'}
              fill="var(--hero-ink)"
              opacity={active ? 1 : 0.82}
              style={{
                font: '500 12px var(--font-body), sans-serif',
                letterSpacing: '0.12em',
                textTransform: 'uppercase',
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
