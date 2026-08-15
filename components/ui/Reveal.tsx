'use client';

import { useEffect, useRef, useState } from 'react';

/**
 * Reveals its children the first time they enter the viewport.
 *
 * A page-load animation is wrong for anything below the fold: by the time the
 * reader scrolls down it has already played, so they see nothing. Everything
 * below the hero therefore waits for intersection and fires once.
 *
 * `mode="words"` stages a line word by word — logistics-curtain-wipe-scroll
 * uses roughly a 60ms stagger so a headline assembles rather than appears, and
 * wealth-word-blur-and-scale resolves each word out of a blur, which is softer
 * than a fade and much softer than a slide. Reserved for headlines; on prose it
 * takes long enough that the page reads as unsettled.
 */
export function Reveal({
  children,
  as: Tag = 'div',
  mode = 'lift',
  delay = 0,
  className = '',
}: {
  children: React.ReactNode;
  as?: 'div' | 'p' | 'h1' | 'h2' | 'h3' | 'span';
  mode?: 'lift' | 'words';
  delay?: number;
  className?: string;
}) {
  const ref = useRef<HTMLElement>(null);
  const [shown, setShown] = useState(false);
  /* Content is visible until JS has confirmed it can actually run the reveal.
     Hiding first and waiting for intersection means a failed observer leaves
     the copy permanently invisible — the reveal is decoration, the words are
     the page. */
  const [armed, setArmed] = useState(false);

  useEffect(() => {
    const el = ref.current;
    if (!el) return;
    if (
      typeof IntersectionObserver === 'undefined' ||
      window.matchMedia('(prefers-reduced-motion: reduce)').matches
    ) {
      setShown(true);
      return;
    }

    setArmed(true);
    const io = new IntersectionObserver(
      (entries) => {
        if (entries.some((e) => e.isIntersecting)) {
          setShown(true);
          io.disconnect();
        }
      },
      { rootMargin: '0px 0px -12% 0px', threshold: 0.1 },
    );
    io.observe(el);

    /* Failsafe: whatever happens, the words appear. */
    const bail = window.setTimeout(() => setShown(true), 2600);
    return () => {
      io.disconnect();
      window.clearTimeout(bail);
    };
  }, []);

  const hidden = armed && !shown;

  const words =
    mode === 'words' && typeof children === 'string' ? children.split(' ') : null;

  return (
    <Tag
      ref={ref as React.Ref<never>}
      className={`${className} ${shown && mode === 'words' ? 'word-in' : ''}`}
      style={
        mode === 'lift'
          ? shown
            ? { animation: `lift 760ms var(--ease-out) ${delay}ms both` }
            : hidden
              ? { opacity: 0 }
              : undefined
          : undefined
      }
    >
      {words
        ? words.map((w, i) => (
            /* The space is a sibling text node, never inside the span.
               `.word-in > span` is inline-block, and trailing whitespace inside
               an inline-block collapses — which silently ran every headline
               together as THINGSNOBODYPUTSONTHEMAP. */
            <span key={`${w}-${i}`}>
              <span
                style={{ animationDelay: `${delay + i * 62}ms`, opacity: hidden ? 0 : undefined }}
              >
                {w}
              </span>
              {i < words.length - 1 ? ' ' : ''}
            </span>
          ))
        : children}
    </Tag>
  );
}
