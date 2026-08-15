'use client';

import { Navbar, SiteFooter } from '@/components/sections/Chrome';
import { PhotoField } from '@/components/ui/PhotoField';
import type { PaletteKey } from '@/data/variations';

/**
 * Everything every route below the landing page shares.
 *
 * The landing page keeps its own bespoke hero — it is the one page allowed to
 * be a poster. These pages get a shorter banner built on the same grammar:
 * plate, scrim, condensed caps, and the feathered seam on the way out, so the
 * boundary into the page body is never a hard line.
 */
export function PageShell({
  palette = 'cedar',
  children,
}: {
  palette?: PaletteKey | 'gentle';
  children: React.ReactNode;
}) {
  return (
    <div data-palette={palette} className="bg-ground">
      <Navbar />
      <main id="main">{children}</main>
      <SiteFooter />
    </div>
  );
}

/**
 * The standard page banner: about half a viewport, so it establishes the page
 * without making the visitor scroll past a second poster to reach content.
 */
export function PageBanner({
  eyebrow,
  title,
  standfirst,
  plate,
  brief,
  showSlots = false,
  children,
}: {
  eyebrow: string;
  title: string;
  standfirst?: string;
  plate: string;
  brief: string;
  showSlots?: boolean;
  children?: React.ReactNode;
}) {
  return (
    <section className="relative flex flex-col overflow-hidden bg-band">
      <PhotoField
        brief={brief}
        showSlots={showSlots}
        plate={plate}
        priority
        className="anim-plate absolute inset-0"
        variant="high"
      />

      {/* scrim-deep, because a banner carries a standfirst and not just a
          title. On Gentle this bleaches the plate toward the page ground
          rather than darkening it — that palette's hero ink is dark — and the
          plain scrim left /plan's standfirst at 3.92:1 once a real photograph
          replaced the placeholder. */}
      <div className="scrim scrim-deep relative flex min-h-[62svh] flex-col justify-end px-5 pb-12 pt-32 text-hero-ink md:px-10 md:pb-16 md:pt-40">
        <p className="micro anim-lift text-hero-ink-dim">{eyebrow}</p>
        <h1 className="anim-word mt-4 max-w-[16ch] font-display text-[clamp(2.5rem,8.5vw,6.5rem)] font-bold uppercase leading-[0.86] tracking-[-0.01em] text-hero-ink">
          {title}
        </h1>
        {standfirst ? (
          <p
            className="anim-lift mt-6 max-w-[52ch] text-sm leading-relaxed text-hero-ink md:text-base"
            style={{ animationDelay: '260ms' }}
          >
            {standfirst}
          </p>
        ) : null}
        {children}
      </div>

      {/* Sentinel: the navbar swaps to page ink when this scrolls out, so the
          swap tracks the actual banner height instead of assuming every page
          opens on a full viewport. */}
      <div id="nav-sentinel" aria-hidden="true" className="h-px w-full" />

      <div className="seam h-24 w-full md:h-36">
        <div aria-hidden="true" className="seam-blur seam-blur-1" />
        <div aria-hidden="true" className="seam-blur seam-blur-2" />
        <div aria-hidden="true" className="seam-blur seam-blur-3" />
        <div aria-hidden="true" className="seam-bleed" />
      </div>
    </section>
  );
}

/** Section heading used across the inner pages. */
export function SectionHead({
  title,
  note,
  aside,
}: {
  title: string;
  note?: string;
  aside?: React.ReactNode;
}) {
  return (
    <div className="mb-10 flex flex-wrap items-end justify-between gap-4 md:mb-12">
      <div>
        <p className="max-w-[22ch] font-display text-[clamp(1.5rem,3vw,2.5rem)] font-medium uppercase leading-[1.06] text-ink">
          {title}
        </p>
        {note ? <p className="mt-3 max-w-[46ch] text-sm text-ink-dim">{note}</p> : null}
      </div>
      {aside}
    </div>
  );
}
