'use client';

import { useEffect, useState } from 'react';

/**
 * Subscribe to a media query so a resize — DevTools device mode, rotation,
 * closing inspect — actually updates React state.
 *
 * A one-shot `matchMedia().matches` on mount is why the hero used to freeze
 * on the phone crop after inspect was cancelled: the query was read once and
 * never again.
 */
export function useMediaQuery(query: string): boolean {
  const [matches, setMatches] = useState(false);

  useEffect(() => {
    const mql = window.matchMedia(query);
    const update = () => setMatches(mql.matches);
    update();
    mql.addEventListener('change', update);
    return () => mql.removeEventListener('change', update);
  }, [query]);

  return matches;
}
