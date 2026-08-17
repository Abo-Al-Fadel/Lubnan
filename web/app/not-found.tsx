'use client';

import { Navbar, SiteFooter } from '@/components/sections/Chrome';
import { useSite } from '@/lib/site-state';

export default function NotFound() {
  const { tr } = useSite();
  return (
    <div data-palette="cedar" className="bg-ground">
      <Navbar />
      <main id="main" className="mx-auto max-w-2xl px-5 py-32 md:px-10">
        <p className="micro text-ink-dim">404</p>
        <h1 className="mt-4 font-display text-[clamp(2.25rem,6vw,4.5rem)] font-bold uppercase leading-[0.9] tracking-[-0.02em] text-ink">
          {tr('notfound.title')}
        </h1>
        <p className="mt-6 max-w-[46ch] text-sm leading-relaxed text-ink-dim">{tr('notfound.body')}</p>
        <a href="/" className="btn-solid mt-10 inline-flex rounded-full px-8 py-4 text-sm font-semibold">
          {tr('notfound.home')}
        </a>
      </main>
      <SiteFooter />
    </div>
  );
}
