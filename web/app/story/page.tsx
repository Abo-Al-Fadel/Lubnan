'use client';

import { useEffect, useRef, useState } from 'react';
import { Navbar, SiteFooter } from '@/components/sections/Chrome';
import { PhotoField } from '@/components/ui/PhotoField';
import { Reveal } from '@/components/ui/Reveal';
import { useSite } from '@/lib/site-state';

/**
 * Story is a journey, so you travel through it sideways.
 *
 * This and Legacy had become the same page with different nouns — banner, then
 * a vertical stack of heading + body + photograph, seven times. The fix is not
 * a different accent; it is a different *shape*. Story is now a full-bleed
 * horizontal rail you scroll through century by century, with the photograph
 * behind the type rather than beside it. Legacy is a dense vertical archive
 * with almost no photography at all. Nothing about the two reads alike now.
 *
 * Native overflow-x with scroll-snap, not scroll-jacking: a touch swipe and a
 * trackpad both already do the right thing, and it degrades to an ordinary
 * scrollable region if anything fails.
 */
const ERAS = [
  {
    era: 'c. 3000 BC',
    tag: 'Phoenician',
    title: 'The alphabet leaves by boat',
    plate: 'M1',
    brief: 'Byblos harbour at sunrise, warm gold on still water, fishing boats, honey stone quay',
    body: 'Twenty-two consonants, no vowels, shipped out of Byblos and rearranged by everyone who received them. Greek took it and added vowels. Latin took it from Greek. Almost every alphabet west of India descends from what left that harbour, which is now roughly the size of a car park.',
    fact: 'The Ahiram sarcophagus carries the earliest long text in the alphabet.',
  },
  {
    era: '1200 – 333 BC',
    tag: 'Maritime',
    title: 'Tyre, Sidon, and the whole sea',
    plate: 'M2',
    brief: 'The Mediterranean off Tyre at golden hour, Roman columns in warm silhouette',
    body: 'Phoenician crews mapped the Mediterranean before anyone drew it, founded Carthage, and by Herodotus’ account rounded Africa. They were traders rather than conquerors, which is why they left harbours instead of monuments, and why the harbours are still harbours.',
    fact: 'Tyrian purple took ten thousand murex shells to dye a single robe.',
  },
  {
    era: '64 BC – 395 AD',
    tag: 'Roman',
    title: 'Rome builds too big',
    plate: 'M3',
    brief: 'Baalbek columns from below at late afternoon, warm gold stone against deep blue sky',
    body: 'Baalbek is the largest temple complex Rome ever attempted, and it went up in a provincial town at a thousand metres. The Temple of Jupiter had fifty-four columns; six still stand. The foundation holds three eight-hundred-tonne blocks, and a fourth still lies in the quarry, cut and never moved.',
    fact: 'The world’s first school of Roman law was in Beirut, from the third century.',
  },
  {
    era: '661 – 750',
    tag: 'Umayyad',
    title: 'A city on a grid, then abandoned',
    plate: 'M4',
    brief: 'Anjar Umayyad arcades in warm low sun, long shadows across the grid, green grass',
    body: 'The Umayyads laid out Anjar as a single planned city on a Roman grid. Two colonnaded streets crossing at right angles, a palace, baths, a mosque, and then walked away from it within a few decades. It is the only Umayyad city of its kind, and it is legible from the air in one look.',
    fact: 'Forty towers around a walled rectangle, built and emptied inside one lifetime.',
  },
  {
    era: '1250 – 1516',
    tag: 'Mamluk',
    title: 'The souks that never closed',
    plate: 'M5',
    brief: 'Tripoli Mamluk souk interior, warm light shafts through the vaulting, spice colour',
    body: 'Tripoli holds the largest concentration of Mamluk architecture outside Cairo: hammams, madrasas, khans, and a souk that has never stopped trading in seven hundred years. The vaulting throws light down in shafts, and the soap khan still sells soap.',
    fact: 'Khan al-Saboun has been making olive-oil soap on the same site since the 1500s.',
  },
  {
    era: '1516 – 1918',
    tag: 'Ottoman',
    title: 'Silk paid for the mountain',
    plate: 'M6',
    brief: 'Beiteddine palace courtyard in warm afternoon light, ochre arcades and fountain',
    body: 'Under Ottoman rule the mountain largely ran itself through local emirs, and silk paid for it. Beiteddine was built over thirty years and is the best surviving argument for what that money bought. Courtyards, arcades, and a hammam with a domed ceiling pierced for light.',
    fact: 'At its height the mountain sent raw silk to Lyon by the shipload.',
  },
  {
    era: '1943 →',
    tag: 'Now',
    title: 'Arguing with itself',
    plate: 'M7',
    brief: 'Beirut rooftops at sunset, warm pink and gold light, Mandate facades and modern towers',
    body: 'Independence, a boom, a war, a rebuild, and another rebuild. Beirut has been destroyed seven times and rebuilt seven times, and the result is a city where a Roman bath sits under an office block and nobody agrees on what any of it means. That disagreement is the most Lebanese thing about it.',
    fact: 'Charles Malik, from Btourram, co-drafted the Universal Declaration of Human Rights.',
  },
];

export default function StoryPage() {
  const { tr } = useSite();
  const railRef = useRef<HTMLDivElement>(null);
  const [active, setActive] = useState(0);

  /* Track which panel is centred so the timeline ticks stay in step. */
  useEffect(() => {
    const rail = railRef.current;
    if (!rail) return;
    let frame = 0;
    const onScroll = () => {
      if (frame) return;
      frame = requestAnimationFrame(() => {
        const i = Math.round((rail.scrollLeft / rail.scrollWidth) * ERAS.length);
        setActive(Math.min(ERAS.length - 1, Math.max(0, i)));
        frame = 0;
      });
    };
    rail.addEventListener('scroll', onScroll, { passive: true });
    return () => {
      rail.removeEventListener('scroll', onScroll);
      if (frame) cancelAnimationFrame(frame);
    };
  }, []);

  const goTo = (i: number) => {
    const rail = railRef.current;
    if (!rail) return;
    rail.scrollTo({ left: (rail.scrollWidth / ERAS.length) * i, behavior: 'smooth' });
  };

  return (
    <div data-palette="cedar" className="bg-ground">
      <Navbar />

      <main id="main">
        {/* ── Opening statement, type only ───────────────────────────────
            No banner photograph: the imagery on this page lives inside the
            rail below, and opening on a poster would repeat the landing page
            for no reason. */}
        <section className="px-5 pb-14 pt-36 md:px-10 md:pb-20 md:pt-44">
          <p className="micro text-ink-dim">{tr('story.eyebrow')}</p>
          <Reveal
            as="h1"
            mode="words"
            className="mt-6 max-w-[13ch] font-display text-[clamp(2.75rem,9vw,7rem)] font-bold uppercase leading-[0.88] tracking-[-0.025em] text-ink"
          >
            {tr('story.title')}
          </Reveal>
          <div className="mt-10 grid gap-8 border-t border-ink-ghost pt-8 md:grid-cols-12">
            <p className="max-w-[52ch] text-[0.95rem] leading-[1.85] text-ink-dim md:col-span-7">
              {tr('story.lede')}
            </p>
            <p className="micro text-ink-dim md:col-span-5 md:text-right">
              {tr('story.swipe')}
            </p>
          </div>
        </section>

        {/* ── The timeline scrubber ───────────────────────────────────── */}
        <div className="sticky top-[4.5rem] z-30 px-5 md:top-[5.5rem] md:px-10">
          <div className="rail flex items-end gap-0 overflow-x-auto border-b border-ink-ghost">
            {ERAS.map((e, i) => (
              <button
                key={e.era}
                type="button"
                onClick={() => goTo(i)}
                aria-current={active === i}
                className="group shrink-0 pb-3 pe-6 pt-2 text-left"
              >
                <span
                  className="micro block transition-colors duration-300"
                  style={{ color: active === i ? 'var(--accent)' : 'var(--ink-dim)' }}
                >
                  {e.era}
                </span>
                <span
                  className="mt-2 block h-0.5 w-full transition-colors duration-300"
                  style={{ background: active === i ? 'var(--accent)' : 'transparent' }}
                />
              </button>
            ))}
          </div>
        </div>

        {/* ── Horizontal rail, one century per screen ─────────────────── */}
        <div
          id="spine"
          ref={railRef}
          className="rail flex snap-x snap-mandatory overflow-x-auto scroll-mt-28"
          aria-label={tr('story.spine')}
        >
          {ERAS.map((e, i) => (
            <section
              key={e.era}
              className="relative flex min-h-[78svh] w-[92vw] shrink-0 snap-start flex-col justify-end overflow-hidden md:min-h-[82svh] md:w-[72vw] lg:w-[58vw]"
            >
              <PhotoField
                brief={e.brief}
                showSlots={false}
                plate={e.plate}
                className="absolute inset-0"
                variant="high"
              />
              <div className="scrim scrim-deep relative flex flex-1 flex-col justify-end p-6 text-hero-ink md:p-10">
                <div className="flex items-baseline gap-4">
                  <span className="figures micro text-hero-ink-dim">
                    {String(i + 1).padStart(2, '0')} / {ERAS.length}
                  </span>
                  <span className="micro rounded-full border border-hero-ink-ghost px-3 py-1.5 text-hero-ink-dim">
                    {e.tag}
                  </span>
                </div>

                <p className="micro mt-6 text-hero-ink-dim">{e.era}</p>
                <h2 className="mt-3 max-w-[14ch] font-display text-[clamp(1.75rem,4.4vw,3.25rem)] font-bold uppercase leading-[0.96] tracking-[-0.015em] text-hero-ink">
                  {e.title}
                </h2>
                <p className="mt-5 max-w-[48ch] text-sm leading-[1.8] text-hero-ink md:text-[0.95rem]">
                  {e.body}
                </p>
                <p className="mt-6 max-w-[46ch] border-t border-hero-ink-ghost pt-4 text-[0.8rem] leading-relaxed text-hero-ink-dim">
                  {e.fact}
                </p>
              </div>
            </section>
          ))}
        </div>

        {/* ── Language ─────────────────────────────────────────────────── */}
        <section id="language" className="bg-band px-5 py-20 md:px-10 md:py-28">
          <div className="grid gap-10 md:grid-cols-12 md:gap-14">
            <div className="md:col-span-5">
              <Reveal
                as="p"
                mode="words"
                className="max-w-[16ch] font-display text-[clamp(1.5rem,3.4vw,2.75rem)] font-medium uppercase leading-[1.06] text-ink"
              >
                {tr('story.langTitle')}
              </Reveal>
            </div>
            <p className="max-w-[58ch] text-[0.95rem] leading-[1.85] text-ink-dim md:col-span-7">
              {tr('story.langBody')}
            </p>
          </div>

          <div className="mt-12 grid gap-px overflow-hidden border border-ink-ghost bg-[color:var(--ink-ghost)] md:grid-cols-3">
            {[
              { label: 'العربية', note: tr('story.langAr'), sample: 'كيفك؟', font: 'font-arabic' },
              { label: 'Français', note: tr('story.langFr'), sample: 'Ça va ?', font: '' },
              { label: 'English', note: tr('story.langEn'), sample: 'You good?', font: '' },
            ].map((l) => (
              <div key={l.label} className="bg-band p-6 md:p-8">
                <p className={`font-display text-lg uppercase tracking-wide text-ink ${l.font}`}>
                  {l.label}
                </p>
                <p className={`mt-5 text-3xl text-accent ${l.font}`}>{l.sample}</p>
                <p className="mt-5 text-sm leading-relaxed text-ink-dim">{l.note}</p>
              </div>
            ))}
          </div>
        </section>
      </main>

      <SiteFooter />
    </div>
  );
}
