'use client';

import { useEffect, useRef, useState } from 'react';

/**
 * Counts a figure up when it scrolls into view.
 *
 * Takes the display string, not a number, so "10,452", "1,000+", "2,000m" and
 * "6" all work: the numeric run is animated and whatever wraps it — thousands
 * separators, a trailing +, a unit — is preserved exactly.
 *
 * pallet-ross-card-fan-scroll: motion tied to scroll position rather than to a
 * timer, so nothing plays before it is looked at. It also runs once and stops;
 * a figure that re-counts every time it scrolls past is a toy.
 *
 * The element reserves its final width before animating, so a counting figure
 * cannot reflow the hairline grid it sits in.
 */
export function Counter({
  value,
  className = '',
  duration = 1400,
}: {
  value: string;
  className?: string;
  duration?: number;
}) {
  const ref = useRef<HTMLSpanElement>(null);
  const [display, setDisplay] = useState(value);
  const started = useRef(false);

  useEffect(() => {
    const el = ref.current;
    if (!el) return;

    const match = value.match(/[\d,.]+/);
    if (!match) return;
    const raw = match[0];
    const target = Number(raw.replace(/,/g, ''));
    if (!Number.isFinite(target) || target === 0) return;

    const grouped = raw.includes(',');
    const decimals = raw.includes('.') ? raw.split('.')[1].length : 0;
    const render = (n: number) => {
      const fixed = n.toFixed(decimals);
      const withGroups = grouped
        ? Number(fixed).toLocaleString('en-US', {
            minimumFractionDigits: decimals,
            maximumFractionDigits: decimals,
          })
        : fixed;
      return value.replace(raw, withGroups);
    };

    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;

    /* Deliberately NOT zeroed here. Zeroing on mount and waiting for
       intersection means any failure to observe — an unsupported browser, a
       capture that never scrolls, an element that never crosses the threshold —
       leaves the figure reading 0 forever. Wrong data beats no animation, so
       the real value stays on screen until the moment the count actually
       starts. */
    const io = new IntersectionObserver(
      (entries) => {
        if (!entries.some((e) => e.isIntersecting) || started.current) return;
        started.current = true;
        io.disconnect();

        const start = performance.now();
        const tick = (now: number) => {
          const t = Math.min(1, (now - start) / duration);
          /* Exponential ease-out: fast off the mark, long settle. The same
             curve the rest of the page's motion uses. */
          const eased = 1 - Math.pow(1 - t, 4);
          setDisplay(render(target * eased));
          if (t < 1) requestAnimationFrame(tick);
          else setDisplay(value);
        };
        requestAnimationFrame(tick);
      },
      { threshold: 0.4 },
    );

    io.observe(el);
    return () => io.disconnect();
  }, [value, duration]);

  return (
    <span ref={ref} className={className}>
      {/* Reserves the final width so counting never reflows the row. */}
      <span aria-hidden="true" className="invisible block h-0 overflow-hidden">
        {value}
      </span>
      <span aria-label={value}>{display}</span>
    </span>
  );
}
