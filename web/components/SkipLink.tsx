'use client';

import { useSite } from '@/lib/site-state';

export function SkipLink() {
  const { tr } = useSite();
  return (
    <a
      href="#main"
      className="sr-only focus:not-sr-only focus:absolute focus:left-4 focus:top-4 focus:z-[80] focus:rounded-full focus:bg-[color:var(--btn-solid)] focus:px-4 focus:py-2 focus:text-[color:var(--btn-solid-ink)]"
    >
      {tr('nav.skip')}
    </a>
  );
}
