'use client';

import { PhotoField } from '@/components/ui/PhotoField';
import { HeroSubject } from '@/components/ui/HeroSubject';
import { Counter } from '@/components/ui/Counter';
import { Navbar } from '@/components/sections/Chrome';
import { useSite } from '@/lib/site-state';
import type { Variation } from '@/data/variations';

/**
 * The cedar cut-out over the wordmark is parked, not deleted. Everything it
 * needs is still wired — B1.png, HeroSubject, the sizing and the 41% centre
 * that lands it on the B/N junction — so flipping this back to `true` restores
 * the occlusion exactly as it was.
 */
const SHOW_HERO_SUBJECT = false;

/**
 * The hero is static. The wordmark and the subject are locked to the plate —
 * no parallax, no drift — so the three planes hold their exact composition
 * while the page scrolls past them. The occlusion only reads if the letter
 * junction the subject cuts stays where it was placed.
 *
 * Type sits on the photograph, so it uses --hero-ink rather than --ink. On the
 * snow palette those differ: the page below is near-black on off-white, while
 * the hero stays light-on-plate.
 */
export function HeroOcclusion({
  variation,
  showSlots,
  imageBrief,
  standfirst,
  stats,
}: {
  variation: Variation;
  showSlots: boolean;
  imageBrief: string;
  standfirst: string;
  stats: { figure: string; label: string }[];
}) {
  const { tr, tc } = useSite();

  return (
    /* The 100svh now lives on the scrim, not on the section. With it on the
       section, the seam was a flex sibling of the composition and took its
       176px out of the same 100svh — it cropped the frame instead of
       extending it. Now the composition owns a full viewport and the seam is
       height added *below* it, so the section is 100svh + seam and the plate
       keeps running behind the whole of it. */
    <section className="relative flex flex-col overflow-hidden bg-band">
      <PhotoField
        brief={imageBrief}
        showSlots={showSlots}
        plate={variation.heroPlate}
        video={variation.heroVideo}
        priority
        className="anim-plate vt-hero absolute inset-0"
        variant="high"
      />

      <Navbar />

      <div className="scrim relative flex min-h-[100svh] flex-col text-hero-ink">
        <div className="relative flex flex-1 flex-col">
          <h1 className="sr-only">
            Lubnān. A guide to Lebanon&rsquo;s coast, mountains and ruins
          </h1>

          {/* A corner wash under the standfirst. The head scrim is tuned for an
              overcast still and the video's sky is far brighter than that —
              eggshell on it measured about 2.5:1. Darkening the whole head
              gradient to fix one paragraph would flatten the sky across the
              entire frame, so the weight goes only where the type is. */}
          {/* The gradient has to reach transparent *inside* the element, not
              at its edge. At 135%/120% it was still ~11% opaque where the box
              stopped, so the wash ended on a straight vertical line down the
              middle of the frame — a dark rectangle with clean edges rather
              than a wash. Bigger box, gradient fully out by 68%. */}
          <div
            aria-hidden="true"
            className="pointer-events-none absolute right-0 top-0 z-[5] hidden h-[74%] w-[82%] md:block"
            style={{
              background:
                'radial-gradient(100% 100% at 100% 0%, var(--scrim-strong) 0%, var(--scrim-strong) 22%, color-mix(in srgb, var(--scrim-strong) 74%, transparent) 38%, color-mix(in srgb, var(--scrim-strong) 38%, transparent) 52%, color-mix(in srgb, var(--scrim-strong) 12%, transparent) 62%, transparent 68%)',
            }}
          />

          {/* Phones stack the standfirst full-width under the nav rather than
              tucking it into a corner, so it needs a band, not a radial. */}
          <div
            aria-hidden="true"
            className="pointer-events-none absolute inset-x-0 top-0 z-[5] h-[42%] md:hidden"
            style={{
              background:
                'linear-gradient(to bottom, var(--scrim-strong) 0%, var(--scrim-strong) 42%, color-mix(in srgb, var(--scrim-strong) 72%, transparent) 66%, color-mix(in srgb, var(--scrim-strong) 34%, transparent) 84%, transparent 100%)',
            }}
          />

          <p
            className="anim-lift relative z-30 mt-24 max-w-[34ch] px-5 text-sm leading-relaxed text-hero-ink md:absolute md:right-10 md:top-28 md:mt-0 md:px-0 md:text-right"
            style={{ animationDelay: '320ms' }}
          >
            {tc('hero.standfirst', standfirst)}
          </p>

          {/* Uppercase, with the macron. Oswald is condensed — its caps run
              about 0.55em wide — so 26vw across six glyphs plus tracking lands
              the name at ~88% of the frame at every width. It was set to 19vw
              on phones on the theory that the name would otherwise overflow;
              measured, 19vw only spanned 63vw and the word floated in the
              middle of the plate looking like a caption. One size, one
              composition, tracking easing open as the frame widens. */}
          <span
            aria-hidden="true"
            className="anim-word pointer-events-none absolute bottom-[4%] left-1/2 z-10 w-[116vw] -translate-x-1/2 text-center font-display text-[24vw] font-bold uppercase leading-[0.8] tracking-[0.015em] text-hero-ink md:bottom-[7%] md:text-[23vw] md:tracking-[0.04em]"
          >
            {variation.heroWord}
          </span>

          {/* Centre at 41% of the frame: the crown rises above the cap line and
              the trunk runs past the baseline to the stat rule, cutting the
              B/N junction so both "LU" and "NĀN" stay readable. Same fraction
              at every width, because the wordmark is now the same fraction too.

              The centering transform lives here and the rise animation lives on
              the child — see .anim-subject. Sized as a share of the wordmark
              rather than of the viewport: about a third of the word's width,
              which is what makes the cut read as occlusion instead of as a
              sticker dropped on the type. */}
          {SHOW_HERO_SUBJECT ? (
            <div className="absolute bottom-[4%] left-[41%] z-20 -translate-x-1/2 md:bottom-[-3%]">
              <div className="anim-subject">
                <HeroSubject
                  variation={variation}
                  className="h-[16svh] drop-shadow-[0_24px_50px_rgba(0,0,0,0.42)] sm:h-[34svh] md:h-[60svh]"
                />
              </div>
            </div>
          ) : null}
        </div>

        {/* Was a frosted panel — a blurred rectangle laid over the photograph.
            perplexity-lumen-atlas does this better: a full-width hairline opens
            a real text zone at the foot of the frame instead of floating a card
            on it. The scrim already carries legibility down here, so the panel
            was doing nothing except softening the photograph it sat on. */}
        {/* Four cells, so phones get a clean 2×2 instead of a full-width CTA
            forcing the three figures into a 2 + 1 with a hole in the corner.
            That layout was 280px tall on a 915px phone — nearly a third of the
            frame spent on a stat bar, which is what squeezed the composition
            above it. */}
        <div className="relative z-30 border-t border-hero-ink-ghost">
          <div className="grid grid-cols-2 md:grid-cols-4 md:divide-x md:divide-[color:var(--hero-ink-ghost)]">
            <div className="border-b border-r border-hero-ink-ghost px-5 py-6 md:border-b-0 md:border-r-0 md:px-8 md:py-8">
              <p className="micro text-hero-ink-dim">{tr('hero.start')}</p>
              <a href="#" className="group mt-3 inline-flex items-center gap-2 text-sm text-hero-ink">
                <span className="border-b border-hero-ink pb-1 transition-colors duration-300 ease-out group-hover:border-accent group-hover:text-accent">
                  {tr('hero.plan')}
                </span>
                <span
                  aria-hidden="true"
                  className="transition-transform duration-300 ease-out group-hover:translate-x-1"
                >
                  →
                </span>
              </a>
            </div>
            {stats.map((s, i) => (
              <div
                key={s.label}
                className={`px-5 py-6 md:px-8 md:py-8 ${i === 0 ? 'border-b border-hero-ink-ghost md:border-b-0' : ''} ${i === 1 ? 'border-r border-hero-ink-ghost md:border-r-0' : ''}`}
              >
                <Counter
                  value={s.figure}
                  className="figures block font-display text-2xl font-medium leading-none text-hero-ink md:text-4xl"
                />
                <p className="micro mt-2.5 text-hero-ink-dim">{s.label}</p>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* The feathered seam.

          It lives *inside* the hero section, below the stat bar, which is the
          whole point: the plate is `absolute inset-0`, so it continues behind
          this strip and the photograph itself is what dissolves. Put between
          the sections instead — as it was — there is nothing behind it to
          blur, and all you get is a gradient rectangle sitting on a hard
          edge. */}
      <div className="seam h-28 w-full md:h-44">
        {/* Marks where hero ink stops being safe. See Navbar. Positioned
            rather than in flow: as a 1px block it put a band of un-scrimmed
            plate between the scrim and the seam, and that read as a hard
            line. */}
        <div
          id="nav-sentinel"
          aria-hidden="true"
          className="pointer-events-none absolute inset-x-0 top-0 h-px"
        />
        <div aria-hidden="true" className="seam-blur seam-blur-1" />
        <div aria-hidden="true" className="seam-blur seam-blur-2" />
        <div aria-hidden="true" className="seam-blur seam-blur-3" />
        <div aria-hidden="true" className="seam-bleed" />
      </div>
    </section>
  );
}
