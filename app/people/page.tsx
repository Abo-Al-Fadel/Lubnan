'use client';

import { useMemo, useState } from 'react';
import { Navbar, SiteFooter } from '@/components/sections/Chrome';
import { PhotoField } from '@/components/ui/PhotoField';
import { useSite } from '@/lib/site-state';

/**
 * The people, without their faces.
 *
 * These are real, named, mostly living individuals, so the page is built
 * around their *work* rather than around generated portraits — a plausible
 * AI face captioned "Fairuz" is a fabricated likeness of a real person, and it
 * would be indistinguishable from a photograph to anyone reading the page. The
 * object plates say more anyway: a record sleeve, a manuscript page, a gown,
 * a drafting table.
 *
 * If you want real portraits here, they need to be licensed images — the
 * layout keeps a portrait slot ready in each entry for that.
 *
 * The index is typographic and the name is the largest thing on the page,
 * which is the correct hierarchy for a page whose subject is people.
 */
type Person = {
  id: string;
  name: string;
  arabic: string;
  years: string;
  field: string;
  from: string;
  claim: string;
  body: string;
  work: string;
  plate: string;
  brief: string;
};

const PEOPLE: Person[] = [
  {
    id: 'gibran',
    name: 'Khalil Gibran',
    arabic: 'جبران خليل جبران',
    years: '1883 – 1931',
    field: 'Letters',
    from: 'Bsharri',
    claim: 'Wrote one of the best-selling books in any language',
    body: 'Left the Qadisha valley for Boston at twelve, and wrote The Prophet in English in 1923. It has never gone out of print, has been translated into more than a hundred languages, and has sold in the tens of millions. He also painted — several hundred works, mostly ignored next to the book.',
    work: 'The Prophet, 1923',
    plate: 'S1',
    brief: 'An open hardback book of prose poetry on a warm wooden table, golden afternoon light across the pages, ink illustration visible',
  },
  {
    id: 'malik',
    name: 'Charles Malik',
    arabic: 'شارل مالك',
    years: '1906 – 1987',
    field: 'Philosophy · diplomacy',
    from: 'Btourram',
    claim: 'Principal drafter of the Universal Declaration of Human Rights',
    body: 'A philosopher who studied under Heidegger and Whitehead, then chaired the UN commission that produced the 1948 Declaration. He argued, successfully, that the document had to protect individuals rather than states — which is the reason it reads the way it does.',
    work: 'Universal Declaration of Human Rights, 1948',
    plate: 'S2',
    brief: 'A typed 1940s document on a desk beside a fountain pen, warm lamplight, aged paper, shallow depth of field',
  },
  {
    id: 'fairuz',
    name: 'Fairuz',
    arabic: 'فيروز',
    years: 'b. 1934',
    field: 'Music',
    from: 'Beirut',
    claim: 'The voice the whole Arab world wakes up to',
    body: 'Nouhad Haddad has recorded for seven decades and is played across the Arab world every morning by unspoken convention. She kept performing through the civil war and refused to take a side in it, which is part of why she is one of the few things Lebanese of every position still agree on.',
    work: 'Baalbek International Festival, from 1957',
    plate: 'S3',
    brief: 'A vinyl record sleeve and turntable in warm evening light, rich colour, shallow depth of field, no faces',
  },
  {
    id: 'saab',
    name: 'Elie Saab',
    arabic: 'إيلي صعب',
    years: 'b. 1964',
    field: 'Couture',
    from: 'Damour',
    claim: 'First non-European invited into Paris haute couture',
    body: 'Started making dresses at nine and opened a house at eighteen. In 2003 he became the first designer from outside Europe admitted to the Chambre Syndicale de la Haute Couture, and his work has been on most red carpets since.',
    work: 'Paris haute couture, from 2003',
    plate: 'S4',
    brief: 'A beaded couture gown on a dress form in a warm-lit atelier, rich embroidery detail catching golden light',
  },
  {
    id: 'maalouf',
    name: 'Amin Maalouf',
    arabic: 'أمين معلوف',
    years: 'b. 1949',
    field: 'Letters',
    from: 'Beirut',
    claim: 'Elected to the Académie française',
    body: 'Left Beirut as a journalist when the war started and writes in French. Took seat 29 at the Académie française in 2011 — the seat previously held by Claude Lévi-Strauss — and became its Perpetual Secretary in 2023.',
    work: 'Léon l’Africain, 1986 · Le Rocher de Tanios, 1993',
    plate: 'S5',
    brief: 'A stack of French-language novels on a writing desk beside a window, warm morning light, worn spines',
  },
  {
    id: 'sabbah',
    name: 'Hassan Kamel Al-Sabbah',
    arabic: 'حسن كامل الصباح',
    years: '1894 – 1935',
    field: 'Engineering',
    from: 'Nabatieh',
    claim: 'Filed more than seventy patents in solar and electrical engineering',
    body: 'Worked at General Electric in Schenectady and patented widely across power conversion and solar energy, decades before either mattered commercially. He died in a car accident at forty and his notebooks were brought home to Nabatieh.',
    work: '76 US patents, 1927 – 1935',
    plate: 'S6',
    brief: 'A 1930s engineering drafting table with technical drawings, brass instruments, warm lamplight, rich detail',
  },
  {
    id: 'taleb',
    name: 'Nassim Nicholas Taleb',
    arabic: 'نسيم نيقولا طالب',
    years: 'b. 1960',
    field: 'Probability',
    from: 'Amioun',
    claim: 'Named the black swan',
    body: 'A trader turned essayist whose 2007 book gave the language a term for the rare, unpredictable event that changes everything — and then watched the financial crisis demonstrate it the following year. His argument about fragility comes, by his own account, from growing up in a country that kept collapsing.',
    work: 'The Black Swan, 2007',
    plate: 'S7',
    brief: 'A dark-covered hardback book on a desk with scattered handwritten notes and a coffee cup, warm side light',
  },
  {
    id: 'mika',
    name: 'Mika',
    arabic: 'ميكا',
    years: 'b. 1983',
    field: 'Music',
    from: 'Beirut',
    claim: 'Sold ten million records out of a Beirut childhood',
    body: 'Born in Beirut and evacuated as an infant when the war reached the family. Trained at the Royal College of Music, and his 2007 debut went to number one across most of Europe. He organised the televised aid concert for Beirut after the 2020 port explosion.',
    work: 'Life in Cartoon Motion, 2007',
    plate: 'S8',
    brief: 'A grand piano in warm stage light with colourful lighting rig behind, vivid saturated colour, no people',
  },
];

const FIELDS = ['All', 'Letters', 'Music', 'Philosophy · diplomacy', 'Couture', 'Engineering', 'Probability'];

export default function PeoplePage() {
  const { tr } = useSite();
  const [field, setField] = useState('All');
  const [open, setOpen] = useState<string>('gibran');

  const shown = useMemo(
    () => (field === 'All' ? PEOPLE : PEOPLE.filter((p) => p.field === field)),
    [field],
  );

  const person = PEOPLE.find((p) => p.id === open) ?? PEOPLE[0];

  return (
    <div data-palette="cedar" className="bg-ground">
      <Navbar />

      <main id="main">
        <section className="px-5 pb-10 pt-36 md:px-10 md:pb-14 md:pt-44">
          <p className="micro text-ink-dim">{tr('people.eyebrow')}</p>
          <h1 className="mt-6 max-w-[12ch] font-display text-[clamp(2.5rem,8vw,6rem)] font-bold uppercase leading-[0.88] tracking-[-0.025em] text-ink">
            {tr('people.title')}
          </h1>
          <p className="mt-8 max-w-[56ch] border-t border-ink-ghost pt-8 text-[0.95rem] leading-[1.85] text-ink-dim">
            {tr('people.lede')}
          </p>
        </section>

        {/* Field filter */}
        <div className="rail flex gap-2 overflow-x-auto px-5 pb-8 md:px-10">
          {FIELDS.map((f) => (
            <button
              key={f}
              type="button"
              onClick={() => setField(f)}
              aria-pressed={field === f}
              className={`micro shrink-0 rounded-full border px-3.5 py-2 transition-colors ${
                field === f
                  ? 'border-accent bg-accent text-[color:var(--accent-ink)]'
                  : 'border-ink-ghost text-ink-dim hover:text-ink'
              }`}
            >
              {f}
            </button>
          ))}
        </div>

        {/* ── Index left, detail right ──────────────────────────────────
            The name is the largest thing on the page. Selecting one swaps the
            detail panel rather than navigating, so you can move down the list
            quickly and compare. */}
        <section className="grid gap-10 px-5 pb-20 md:grid-cols-12 md:gap-12 md:px-10 md:pb-28">
          <div className="md:col-span-6 lg:col-span-7">
            {shown.map((p) => {
              const isOpen = open === p.id;
              return (
                <button
                  key={p.id}
                  type="button"
                  onClick={() => setOpen(p.id)}
                  aria-pressed={isOpen}
                  className="group block w-full border-t border-ink-ghost py-6 text-left last:border-b"
                >
                  <div className="flex items-baseline justify-between gap-4">
                    <span
                      className="font-display text-[clamp(1.5rem,4.2vw,3rem)] font-bold uppercase leading-[0.98] tracking-[-0.02em] transition-colors duration-300"
                      style={{ color: isOpen ? 'var(--accent)' : 'var(--ink)' }}
                    >
                      {p.name}
                    </span>
                    <span className="figures micro shrink-0 text-ink-dim">{p.years}</span>
                  </div>
                  <div className="mt-3 flex flex-wrap items-baseline gap-x-4 gap-y-1">
                    <span className="micro text-ink-dim">{p.field}</span>
                    <span aria-hidden="true" className="text-ink-dim opacity-40">·</span>
                    <span className="micro text-ink-dim">{p.from}</span>
                    <span className="font-arabic ms-auto text-sm text-ink-dim">{p.arabic}</span>
                  </div>
                  <p className="mt-3 max-w-[52ch] text-sm leading-relaxed text-ink-dim">
                    {p.claim}
                  </p>
                </button>
              );
            })}
          </div>

          <aside className="md:col-span-6 lg:col-span-5">
            <div className="md:sticky md:top-28">
              {/* The plate shows the work, not the person. */}
              <PhotoField
                brief={person.brief}
                showSlots={false}
                plate={person.plate}
                className="aspect-[4/5] w-full"
                variant="mid"
              />
              <div className="mt-6 border-t border-ink-ghost pt-6">
                <p className="micro text-ink-dim">{tr('people.work')}</p>
                <p className="mt-3 font-display text-lg uppercase tracking-wide text-ink">
                  {person.work}
                </p>
                <p className="mt-5 max-w-[46ch] text-[0.92rem] leading-[1.85] text-ink-dim">
                  {person.body}
                </p>
              </div>
            </div>
          </aside>
        </section>

        <section className="border-t border-ink-ghost bg-band px-5 py-12 md:px-10 md:py-16">
          <p className="max-w-[70ch] text-sm leading-[1.85] text-ink-dim">
            {tr('people.portraitNote')}
          </p>
        </section>
      </main>

      <SiteFooter />
    </div>
  );
}
