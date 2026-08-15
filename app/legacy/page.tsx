'use client';

import { useState } from 'react';
import { Navbar, SiteFooter } from '@/components/sections/Chrome';
import { PhotoField } from '@/components/ui/PhotoField';
import { SiteDiagram, type DiagramKind } from '@/components/ui/SiteDiagram';
import { Counter } from '@/components/ui/Counter';
import { useSite } from '@/lib/site-state';

/**
 * Legacy is an archive, so it reads like one.
 *
 * Story travels sideways through full-bleed photographs. This does the
 * opposite on purpose: a dense vertical register, monospaced data columns,
 * and the drawn diagram as the primary image rather than a photograph. The
 * two pages now share a palette and nothing else — which is the point, since
 * they previously shared a skeleton and differed only in their nouns.
 */
const SITES: {
  id: string;
  name: string;
  arabic: string;
  year: string;
  criteria: string;
  area: string;
  coords: string;
  kind: DiagramKind;
  plate: string;
  claim: string;
  body: string;
  measures: [string, string][];
}[] = [
  {
    id: 'baalbek',
    plate: 'N1',
    name: 'Baalbek',
    arabic: 'بعلبك',
    year: '1984',
    criteria: 'i · iv',
    area: '4.9 ha',
    coords: '34.007 N · 36.204 E',
    kind: 'trilithon',
    claim: 'Three stones nobody can explain moving',
    body: 'The western podium wall contains three limestone blocks of roughly eight hundred tonnes each. A fourth, larger still, lies half-cut in the quarry eight hundred metres south — abandoned at what was evidently the limit of the method.',
    measures: [
      ['Standing columns', '6 of 54'],
      ['Column height', '22 m'],
      ['Trilithon block', '~800 t'],
      ['Quarry megalith', '~1,000 t'],
    ],
  },
  {
    id: 'anjar',
    plate: 'N2',
    name: 'Anjar',
    arabic: 'عنجر',
    year: '1984',
    criteria: 'iii · iv',
    area: '11.4 ha',
    coords: '33.726 N · 35.930 E',
    kind: 'grid',
    claim: 'One planned city, abandoned in decades',
    body: 'Two colonnaded streets crossing at right angles inside a walled rectangle with forty towers. The only Umayyad city of its kind, laid out on a Roman grid and left within a few decades of completion.',
    measures: [
      ['Wall towers', '40'],
      ['Enclosure', '385 × 350 m'],
      ['Occupied', 'c. 30 years'],
      ['Streets', '2, orthogonal'],
    ],
  },
  {
    id: 'byblos',
    plate: 'N3',
    name: 'Byblos',
    arabic: 'جبيل',
    year: '1984',
    criteria: 'iii · iv · vi',
    area: '10.7 ha',
    coords: '34.121 N · 35.645 E',
    kind: 'strata',
    claim: 'Seven thousand years, stacked',
    body: 'Nobody cleared the site. Each occupation built on the last, so the section reads as a continuous record from the Neolithic to the Crusades in a single vertical.',
    measures: [
      ['Continuous occupation', '~7,000 yr'],
      ['Distinct strata', '5 major'],
      ['Royal tombs', '9 shafts'],
      ['Still a harbour', 'Yes'],
    ],
  },
  {
    id: 'tyre',
    plate: 'N4',
    name: 'Tyre',
    arabic: 'صور',
    year: '1984',
    criteria: 'iii · vi',
    area: '15.4 ha',
    coords: '33.271 N · 35.196 E',
    kind: 'causeway',
    claim: 'An island that stopped being one',
    body: 'Alexander built a mole to take the island in 332 BC. The mole silted up and never stopped, permanently joining the city to the mainland — a siege work that became geography.',
    measures: [
      ['Mole built', '332 BC'],
      ['Hippodrome length', '480 m'],
      ['Spectators', '~20,000'],
      ['Now', 'Peninsula'],
    ],
  },
  {
    id: 'qadisha',
    plate: 'N5',
    name: 'Qadisha Valley',
    arabic: 'وادي قاديشا',
    year: '1998',
    criteria: 'iii · iv',
    area: '2,000 ha',
    coords: '34.250 N · 35.950 E',
    kind: 'profile',
    claim: 'A kilometre from rim to river',
    body: 'Monks cut cells and chapels directly into the cliff faces at heights that are still a scramble to reach, and stayed for centuries. Listed jointly with the grove above it.',
    measures: [
      ['Rim altitude', '~1,500 m'],
      ['Valley floor', '~500 m'],
      ['Rock-cut monasteries', '5 major'],
      ['Walkable end to end', '1 long day'],
    ],
  },
  {
    id: 'cedars',
    plate: 'N6',
    name: 'Horsh Arz el-Rab',
    arabic: 'أرز الربّ',
    year: '1998',
    criteria: 'iii · iv',
    area: '102 ha',
    coords: '34.245 N · 36.049 E',
    kind: 'grove',
    claim: 'The tree on the flag, in decline',
    body: 'Fences went up in the nineteenth century because goats were eating every seedling. What is left is a few hundred trunks at two thousand metres, some of them over a thousand years old.',
    measures: [
      ['Altitude', '~2,000 m'],
      ['Oldest trunks', '1,000+ yr'],
      ['Surviving groves', '17 nationally'],
      ['Protected since', '1876'],
    ],
  },
];

export default function LegacyPage() {
  const { tr } = useSite();
  const [open, setOpen] = useState<string | null>('baalbek');

  return (
    <div data-palette="gentle" className="bg-ground">
      <Navbar />

      <main id="main">
        {/* ── Archive header: data, not a poster ─────────────────────── */}
        <section className="border-b border-ink-ghost px-5 pb-10 pt-36 md:px-10 md:pb-14 md:pt-44">
          <div className="grid gap-8 md:grid-cols-12">
            <div className="md:col-span-7">
              <p className="micro text-ink-dim">{tr('legacy.eyebrow')}</p>
              <h1 className="mt-5 max-w-[15ch] font-display text-[clamp(2.25rem,6.5vw,4.5rem)] font-bold uppercase leading-[0.9] tracking-[-0.025em] text-ink">
                {tr('legacy.title')}
              </h1>
            </div>
            <div className="md:col-span-5 md:self-end">
              <p className="max-w-[44ch] text-[0.95rem] leading-[1.85] text-ink-dim">
                {tr('legacy.lede')}
              </p>
            </div>
          </div>
        </section>

        {/* ── The register ───────────────────────────────────────────── */}
        <section className="px-5 py-10 md:px-10 md:py-14">
          <p className="micro mb-6 text-ink-dim">{tr('legacy.register')}</p>
          <div className="overflow-x-auto">
            <table className="w-full min-w-[46rem] border-collapse text-left">
              <thead>
                <tr className="border-b border-ink">
                  {[
                    tr('legacy.colSite'),
                    tr('legacy.colYear'),
                    tr('legacy.colCriteria'),
                    tr('legacy.colArea'),
                    tr('legacy.colCoords'),
                    tr('legacy.colWhy'),
                  ].map((h) => (
                    <th key={h} className="micro pb-3 pe-6 font-normal text-ink-dim">
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {SITES.map((s) => (
                  <tr
                    key={s.id}
                    className="cursor-pointer border-b border-ink-ghost transition-colors last:border-b-0 hover:bg-[color:var(--band)]"
                    onClick={() => {
                      setOpen(s.id);
                      document.getElementById(`site-${s.id}`)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
                    }}
                  >
                    <td className="py-3.5 pe-6">
                      <span className="font-display text-base uppercase tracking-wide text-ink">
                        {s.name}
                      </span>
                      <span className="font-arabic ms-3 text-sm text-ink-dim">{s.arabic}</span>
                    </td>
                    <td className="figures py-3.5 pe-6 text-sm text-ink-dim">{s.year}</td>
                    <td className="figures py-3.5 pe-6 text-sm text-ink-dim">{s.criteria}</td>
                    <td className="figures py-3.5 pe-6 text-sm text-ink-dim">{s.area}</td>
                    <td className="figures py-3.5 pe-6 text-xs text-ink-dim">{s.coords}</td>
                    <td className="py-3.5 text-sm text-ink-dim">{s.claim}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>

        {/* ── Entries: diagram left, data right, no photographs ───────── */}
        <section className="border-t border-ink-ghost bg-band">
          <p className="micro px-5 pt-10 text-ink-dim md:px-10 md:pt-14">{tr('legacy.drawn')}</p>

          {SITES.map((s) => {
            const isOpen = open === s.id;
            return (
              <article
                key={s.id}
                id={`site-${s.id}`}
                className="border-t border-ink-ghost first:border-t-0"
              >
                <button
                  type="button"
                  onClick={() => setOpen(isOpen ? null : s.id)}
                  aria-expanded={isOpen}
                  className="flex w-full items-baseline gap-5 px-5 py-6 text-left transition-opacity hover:opacity-70 md:px-10"
                >
                  <span className="figures micro shrink-0 pt-1 text-ink-dim">{s.year}</span>
                  <span
                    className={`flex-1 font-display text-lg uppercase leading-tight tracking-wide md:text-2xl ${isOpen ? 'text-ink' : 'text-ink-dim'}`}
                  >
                    {s.name}
                  </span>
                  <span className="micro hidden shrink-0 pt-1 text-ink-dim sm:block">
                    {s.claim}
                  </span>
                  <span aria-hidden="true" className="micro shrink-0 pt-1 text-ink-dim">
                    {isOpen ? '−' : '+'}
                  </span>
                </button>

                {isOpen ? (
                  <div className="grid gap-8 px-5 pb-12 md:grid-cols-12 md:gap-12 md:px-10">
                    <div className="md:col-span-7">
                      {/* A strip, not a hero. The photograph is evidence that
                          the diagram is of a real place; keeping it letterboxed
                          stops this page turning back into Story. */}
                      <PhotoField
                        brief={`${s.name} — whole site legible in one frame, warm golden hour light`}
                        showSlots={false}
                        plate={s.plate}
                        className="aspect-[21/9] w-full"
                        variant="mid"
                      />
                      <div className="mt-3 border border-ink-ghost bg-ground p-4 md:p-6">
                        <SiteDiagram kind={s.kind} />
                      </div>
                    </div>

                    <div className="md:col-span-5">
                      <p className="max-w-[46ch] text-[0.92rem] leading-[1.85] text-ink-dim">
                        {s.body}
                      </p>

                      <dl className="mt-7">
                        {s.measures.map(([k, v]) => (
                          <div
                            key={k}
                            className="flex items-baseline justify-between gap-4 border-t border-ink-ghost py-3 last:border-b"
                          >
                            <dt className="micro text-ink-dim">{k}</dt>
                            <dd className="figures text-sm text-ink">{v}</dd>
                          </div>
                        ))}
                      </dl>

                      <p className="micro mt-6 text-ink-dim">
                        {tr('legacy.criteria')} {s.criteria} · {s.area} · {s.coords}
                      </p>
                    </div>
                  </div>
                ) : null}
              </article>
            );
          })}
        </section>

        {/* ── At risk ─────────────────────────────────────────────────── */}
        <section className="border-t border-ink-ghost px-5 py-16 md:px-10 md:py-20">
          <div className="grid gap-10 md:grid-cols-12 md:gap-14">
            <div className="md:col-span-5">
              <p className="micro text-accent">{tr('legacy.riskEyebrow')}</p>
              <p className="mt-4 max-w-[18ch] font-display text-[clamp(1.5rem,3.4vw,2.5rem)] font-medium uppercase leading-[1.06] text-ink">
                {tr('legacy.riskTitle')}
              </p>
            </div>
            <div className="grid gap-8 sm:grid-cols-3 md:col-span-7">
              {[
                { figure: '6', label: tr('legacy.riskInscribed') },
                { figure: '17', label: tr('legacy.riskGroves') },
                { figure: '1', label: tr('legacy.riskTentative') },
              ].map((r) => (
                <div key={r.label}>
                  <Counter
                    value={r.figure}
                    className="figures block font-display text-[clamp(2rem,4.4vw,3rem)] font-medium leading-none text-ink"
                  />
                  <p className="micro mt-3 max-w-[20ch] leading-[1.7] text-ink-dim">{r.label}</p>
                </div>
              ))}
            </div>
          </div>
        </section>
      </main>

      <SiteFooter />
    </div>
  );
}
