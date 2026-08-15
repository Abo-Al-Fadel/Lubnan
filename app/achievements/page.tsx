'use client';

import { useState } from 'react';
import { Navbar, SiteFooter } from '@/components/sections/Chrome';
import { PhotoField } from '@/components/ui/PhotoField';
import { Counter } from '@/components/ui/Counter';
import { useSite } from '@/lib/site-state';

/**
 * What the country gave the world.
 *
 * Shape: a ledger of claims, each with the date it happened and the evidence
 * that supports it. The date is the structural device and it is real
 * information — the entries are chronological and the span from 1500 BC to
 * 1948 is the argument. No photograph competes with the figures; the imagery
 * sits in a separate band so the ledger stays a ledger.
 *
 * Every claim below is one a museum label would make. Where a claim is
 * contested — the circumnavigation of Africa rests on a single line in
 * Herodotus — the entry says so rather than flattening it into a boast.
 */
const CLAIMS = [
  {
    when: 'c. 1500 BC',
    field: 'Writing',
    title: 'The alphabet',
    body: 'Twenty-two consonant signs, developed on the Phoenician coast and carried out of Byblos by traders. Greek added vowels to it, Latin took it from Greek, and almost every alphabet in use west of India is descended from it — including the one you are reading.',
    evidence: 'Ahiram sarcophagus, Byblos · earliest long alphabetic inscription',
    certainty: 'established',
  },
  {
    when: 'c. 1500 BC',
    field: 'Colour',
    title: 'Tyrian purple',
    body: 'A dye extracted from murex sea snails at Tyre and Sidon, and the only colourfast purple in the ancient world. It took roughly ten thousand shells to dye a single robe, which is why purple came to mean royal and still does.',
    evidence: 'Murex shell middens still visible on the Tyre and Sidon shore',
    certainty: 'established',
  },
  {
    when: 'c. 814 BC',
    field: 'Cities',
    title: 'Carthage, founded from Tyre',
    body: 'Settlers from Tyre founded Carthage on the North African coast. It grew into the power that fought Rome for the western Mediterranean across three wars — an argument begun by a colony of a Lebanese port city.',
    evidence: 'Classical sources; Carthaginian material culture traces directly to Tyre',
    certainty: 'established',
  },
  {
    when: 'c. 600 BC',
    field: 'Navigation',
    title: 'Around Africa, possibly',
    body: 'Herodotus reports that Phoenician crews sailed from the Red Sea, around Africa, and back through the Pillars of Hercules in three years. He disbelieved the detail that decided it for modern readers — that the sun stood on their right hand — which is exactly what would happen south of the equator.',
    evidence: 'Herodotus, Histories IV.42 · single source, widely debated',
    certainty: 'contested',
  },
  {
    when: 'c. 50 BC',
    field: 'Craft',
    title: 'Glassblowing',
    body: 'Glass had been made for centuries, but blowing it — inflating a gather on the end of a hollow rod — was worked out on the Syro-Lebanese coast around Sidon. It turned glass from a luxury for the few into an everyday container, almost overnight.',
    evidence: 'Earliest blown glass fragments, Sidon workshops',
    certainty: 'established',
  },
  {
    when: 'c. 200 AD',
    field: 'Law',
    title: 'The first school of law',
    body: 'Beirut held the most important school of Roman law in the empire, and its jurists wrote a substantial part of what became the Justinian Code — the foundation of most continental European legal systems. An earthquake took the city and the school in 551.',
    evidence: 'Justinian named Beirut one of three official law schools of the empire',
    certainty: 'established',
  },
  {
    when: '1866',
    field: 'Education',
    title: 'The American University of Beirut',
    body: 'Founded as the Syrian Protestant College and still one of the region’s leading universities. It admitted women from 1922, decades ahead of most of its peers anywhere.',
    evidence: 'Continuous operation since 1866',
    certainty: 'established',
  },
  {
    when: '1923',
    field: 'Letters',
    title: 'The Prophet',
    body: 'Khalil Gibran, from Bsharri above the Qadisha valley, wrote a slim book of prose poetry in English that has never gone out of print in a century and has sold in the tens of millions. It is among the best-selling books ever written.',
    evidence: 'Continuously in print since 1923 · translated into 100+ languages',
    certainty: 'established',
  },
  {
    when: '1948',
    field: 'Rights',
    title: 'The Universal Declaration',
    body: 'Charles Malik, a philosopher from Btourram, was one of the principal drafters of the Universal Declaration of Human Rights and chaired the commission that carried it. He argued successfully that the document had to apply to individuals rather than to states.',
    evidence: 'UN Commission on Human Rights drafting record, 1947–48',
    certainty: 'established',
  },
];

const FIGURES = [
  { figure: '22', label: 'letters that became every Western alphabet' },
  { figure: '10,000', label: 'murex shells per dyed robe' },
  { figure: '3', label: 'years to sail around Africa, if Herodotus is right' },
  { figure: '100+', label: 'languages The Prophet has been translated into' },
];

export default function AchievementsPage() {
  const { tr } = useSite();
  const [open, setOpen] = useState<number | null>(0);

  return (
    <div data-palette="cedar" className="bg-ground">
      <Navbar />

      <main id="main">
        {/* ── Statement ────────────────────────────────────────────────── */}
        <section className="px-5 pb-12 pt-36 md:px-10 md:pb-16 md:pt-44">
          <p className="micro text-ink-dim">{tr('ach.eyebrow')}</p>
          <h1 className="mt-6 max-w-[14ch] font-display text-[clamp(2.5rem,8vw,6rem)] font-bold uppercase leading-[0.88] tracking-[-0.025em] text-ink">
            {tr('ach.title')}
          </h1>
          <div className="mt-10 grid gap-8 border-t border-ink-ghost pt-8 md:grid-cols-12">
            <p className="max-w-[54ch] text-[0.95rem] leading-[1.85] text-ink-dim md:col-span-7">
              {tr('ach.lede')}
            </p>
            <p className="micro max-w-[28ch] leading-[1.8] text-ink-dim md:col-span-5 md:text-right">
              {tr('ach.note')}
            </p>
          </div>
        </section>

        {/* ── Figures band ─────────────────────────────────────────────── */}
        <section className="border-y border-ink-ghost bg-band px-5 py-12 md:px-10 md:py-16">
          <div className="grid grid-cols-2 gap-8 md:grid-cols-4">
            {FIGURES.map((f) => (
              <div key={f.label}>
                <Counter
                  value={f.figure}
                  className="figures block font-display text-[clamp(1.75rem,4vw,3rem)] font-medium leading-none text-ink"
                />
                <p className="micro mt-3 max-w-[22ch] leading-[1.7] text-ink-dim">{f.label}</p>
              </div>
            ))}
          </div>
        </section>

        {/* ── The ledger ───────────────────────────────────────────────
            Dates run down the left as a spine. Chronology is the structure,
            and the two-thousand-year gap between the alphabet and the
            Declaration is the whole argument of the page. */}
        <section className="px-5 py-14 md:px-10 md:py-20">
          <p className="micro mb-8 text-ink-dim">{tr('ach.ledger')}</p>

          {CLAIMS.map((c, i) => {
            const isOpen = open === i;
            return (
              <article key={c.title} className="border-t border-ink-ghost last:border-b">
                <button
                  type="button"
                  onClick={() => setOpen(isOpen ? null : i)}
                  aria-expanded={isOpen}
                  className="grid w-full grid-cols-[5.5rem_1fr_auto] items-baseline gap-4 py-6 text-left transition-opacity hover:opacity-70 md:grid-cols-[8rem_7rem_1fr_auto] md:gap-6"
                >
                  <span
                    className="figures micro shrink-0 transition-colors"
                    style={{ color: isOpen ? 'var(--accent)' : 'var(--ink-dim)' }}
                  >
                    {c.when}
                  </span>
                  <span className="micro hidden shrink-0 text-ink-dim md:block">{c.field}</span>
                  <span
                    className={`font-display text-lg uppercase leading-tight tracking-wide md:text-2xl ${isOpen ? 'text-ink' : 'text-ink-dim'}`}
                  >
                    {c.title}
                  </span>
                  <span aria-hidden="true" className="micro shrink-0 text-ink-dim">
                    {isOpen ? '−' : '+'}
                  </span>
                </button>

                {isOpen ? (
                  <div className="grid gap-6 pb-9 md:grid-cols-[8rem_1fr] md:gap-6">
                    <div className="hidden md:block" />
                    <div>
                      <p className="max-w-[62ch] text-[0.95rem] leading-[1.85] text-ink-dim">
                        {c.body}
                      </p>
                      <div className="mt-6 flex flex-wrap items-center gap-x-4 gap-y-2 border-t border-ink-ghost pt-4">
                        <span
                          className="micro rounded-full px-3 py-1.5"
                          style={
                            c.certainty === 'contested'
                              ? { background: 'var(--accent)', color: 'var(--accent-ink)' }
                              : { border: '1px solid var(--ink-ghost)', color: 'var(--ink-dim)' }
                          }
                        >
                          {c.certainty === 'contested' ? tr('ach.contested') : tr('ach.established')}
                        </span>
                        <span className="micro max-w-[52ch] text-ink-dim">{c.evidence}</span>
                      </div>
                    </div>
                  </div>
                ) : null}
              </article>
            );
          })}
        </section>

        {/* ── One image, at the end ────────────────────────────────────── */}
        <section className="relative min-h-[52svh] overflow-hidden">
          <PhotoField
            brief="Close crop of carved Phoenician alphabetic letterforms in warm limestone, golden raking light throwing the carving into deep relief"
            showSlots={false}
            plate="M9"
            className="absolute inset-0"
            variant="high"
          />
          <div className="scrim scrim-deep relative flex min-h-[52svh] flex-col justify-end p-6 text-hero-ink md:p-12">
            <p className="max-w-[24ch] font-display text-[clamp(1.5rem,4vw,3rem)] font-bold uppercase leading-[0.98] tracking-[-0.015em] text-hero-ink">
              {tr('ach.closeLine')}
            </p>
            <p className="mt-5 max-w-[48ch] text-sm leading-relaxed text-hero-ink-dim">
              {tr('ach.closeNote')}
            </p>
          </div>
        </section>
      </main>

      <SiteFooter />
    </div>
  );
}
