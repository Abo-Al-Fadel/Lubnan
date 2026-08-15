'use client';

import { useState } from 'react';
import { PageShell, PageBanner } from '@/components/PageShell';
import { PhotoField } from '@/components/ui/PhotoField';
import { useSite } from '@/lib/site-state';
import { places } from '@/data/places';

/* Monthly climate. Real numbers, drawn — the band is data, not decoration. */
const MONTHS = [
  { m: 'J', hi: 17, rain: 191 },
  { m: 'F', hi: 17, rain: 133 },
  { m: 'M', hi: 20, rain: 96 },
  { m: 'A', hi: 23, rain: 43 },
  { m: 'M', hi: 26, rain: 12 },
  { m: 'J', hi: 29, rain: 2 },
  { m: 'J', hi: 31, rain: 0 },
  { m: 'A', hi: 32, rain: 1 },
  { m: 'S', hi: 30, rain: 8 },
  { m: 'O', hi: 27, rain: 57 },
  { m: 'N', hi: 23, rain: 118 },
  { m: 'D', hi: 19, rain: 175 },
];

/* A real sequence — you cannot pick a transfer before you have a flight — so
   the numbered accordion is honest structure rather than decoration.
   pga-resort-full-scroll: numbered list paired with an image. */
const STEPS = [
  {
    id: 'visa',
    title: 'Visa',
    plate: 'P1',
    brief: 'Beirut airport arrivals hall in warm morning light, bright and welcoming, travellers with luggage',
    body: 'Most Western passports get a free single-entry visa on arrival, valid one month, issued at the desk before immigration. Bring a printed onward ticket and an address. An Israeli stamp, or any evidence of an Israeli visit. Will refuse you entry, and that rule is enforced.',
  },
  {
    id: 'flights',
    title: 'Flights',
    plate: 'P2',
    brief: 'Aircraft wing over the Lebanese coast at sunrise, warm gold light on the Mediterranean and mountains',
    body: 'Beirut–Rafic Hariri (BEY) is the only international airport. Direct from most of Europe and the Gulf; from North America you will connect, usually Istanbul, Paris, or Doha. Fares climb hard in July and August when the diaspora comes home.',
  },
  {
    id: 'transfer',
    title: 'Airport transfer',
    plate: 'P3',
    brief: 'Coastal road into Beirut at golden hour, warm light, sea on one side and city on the other',
    body: 'The airport is 9 km from downtown, which is fifteen minutes at 3 am and an hour at 6 pm. Agree the fare before you get in. There is no meter culture. Ride-hailing works and is usually cheaper.',
  },
  {
    id: 'money',
    title: 'Money',
    plate: 'P4',
    brief: 'Beirut street market in warm afternoon light, colourful produce, lively and bright',
    body: 'Bring US dollars in cash, in good condition. The economy runs on them and card acceptance is patchy outside Beirut. Prices are frequently quoted in dollars even when paid in lira, and the rate moves. Ask before you commit.',
  },
  {
    id: 'around',
    title: 'Getting around',
    plate: 'P5',
    brief: 'Mountain road above the Qadisha valley in warm evening light, terraced slopes, rich colour',
    body: 'There is no passenger rail. Service (shared) taxis run fixed routes cheaply, private taxis go anywhere, and renting a car makes sense for the mountains. Nothing in the country is more than three hours from Beirut, which is the single best thing about travelling here.',
  },
  {
    id: 'before',
    title: 'Before you go',
    plate: 'P6',
    brief: 'Lebanese mezze table in warm daylight, colourful dishes, fresh and inviting',
    body: 'Type A, B, C and G sockets at 220 V. Bring an adapter and expect scheduled power cuts. Arabic is the language; French and English are widely spoken in Beirut. Modest dress for religious sites. Tap water is not for drinking.',
  },
];

const TRANSFERS = [
  { mode: 'Ride-hailing', cost: '$10 – 15', time: '15 – 45 min', night: 'Yes' },
  { mode: 'Airport taxi', cost: '$25 – 35', time: '15 – 45 min', night: 'Yes' },
  { mode: 'Hotel car', cost: '$35 – 50', time: '15 – 45 min', night: 'Yes, if booked' },
  { mode: 'Public bus', cost: 'Under $1', time: '60 – 90 min', night: 'No' },
];

export default function PlanPage() {
  const { tr } = useSite();
  const [open, setOpen] = useState(0);
  const [sheet, setSheet] = useState(false);
  const [days, setDays] = useState(5);
  const [picked, setPicked] = useState<string[]>(['byblos', 'baalbek', 'cedars']);

  const capacity = days <= 3 ? 3 : days <= 5 ? 5 : 8;

  const toggle = (id: string) =>
    setPicked((cur) =>
      cur.includes(id) ? cur.filter((p) => p !== id) : cur.length < capacity ? [...cur, id] : cur,
    );

  return (
    <PageShell palette="gentle">
      <PageBanner
        eyebrow={tr('plan.eyebrow')}
        title={tr('plan.title')}
        standfirst={tr('plan.lede')}
        plate="P7"
        brief="Wide view of the Lebanese coast from the mountains at golden hour, warm saturated light, sea and snow in one frame"
      />

      {/* ── Season band ───────────────────────────────────────────────── */}
      <section className="bg-ground px-5 py-14 md:px-10 md:py-20">
        <div className="mb-8 flex flex-wrap items-end justify-between gap-4">
          <p className="max-w-[20ch] font-display text-[clamp(1.5rem,3vw,2.25rem)] font-medium uppercase leading-[1.06] text-ink">
            {tr('plan.seasonTitle')}
          </p>
          <p className="micro text-ink-dim">{tr('plan.seasonNote')}</p>
        </div>

        <div className="grid grid-cols-12 gap-1 md:gap-2">
          {MONTHS.map((mo, i) => {
            const good = mo.rain < 60 && mo.hi < 31;
            return (
              <div key={i} className="flex flex-col items-center gap-2">
                <div className="flex h-32 w-full flex-col justify-end gap-px md:h-40">
                  <div
                    className="w-full bg-accent"
                    style={{ height: `${(mo.hi / 34) * 100}%`, opacity: good ? 1 : 0.4 }}
                    title={`${mo.hi}°C`}
                  />
                  <div
                    className="w-full bg-ink"
                    style={{ height: `${(mo.rain / 200) * 45}%`, opacity: 0.28 }}
                    title={`${mo.rain} mm`}
                  />
                </div>
                <span className="micro text-ink-dim">{mo.m}</span>
              </div>
            );
          })}
        </div>
        <div className="mt-5 flex flex-wrap gap-6">
          <span className="micro flex items-center gap-2 text-ink-dim">
            <span className="h-2.5 w-2.5 bg-accent" /> {tr('plan.legendTemp')}
          </span>
          <span className="micro flex items-center gap-2 text-ink-dim">
            <span className="h-2.5 w-2.5 bg-ink opacity-30" /> {tr('plan.legendRain')}
          </span>
        </div>
      </section>

      {/* ── Numbered accordion ────────────────────────────────────────── */}
      <section className="bg-band px-5 py-16 md:px-10 md:py-24">
        <div className="grid gap-10 md:grid-cols-12 md:gap-14">
          <div className="md:col-span-5">
            <div className="md:sticky md:top-28">
              <PhotoField
                brief={STEPS[open]?.brief ?? STEPS[0].brief}
                showSlots={false}
                plate={STEPS[open]?.plate ?? STEPS[0].plate}
                className="aspect-[4/5] w-full"
                variant="mid"
              />
            </div>
          </div>

          <div className="md:col-span-7">
            {STEPS.map((s, i) => {
              const isOpen = open === i;
              return (
                <div key={s.id} id={s.id} className="border-t border-ink-ghost last:border-b">
                  <h2>
                    <button
                      type="button"
                      onClick={() => setOpen(isOpen ? -1 : i)}
                      aria-expanded={isOpen}
                      className="flex w-full items-baseline gap-5 py-6 text-left transition-opacity duration-200 hover:opacity-70"
                    >
                      <span
                        className="micro figures shrink-0 pt-1 transition-colors duration-300"
                        style={{ color: isOpen ? 'var(--accent)' : 'var(--ink-dim)' }}
                      >
                        {String(i + 1).padStart(2, '0')}
                      </span>
                      <span
                        className={`flex-1 font-display text-lg uppercase leading-tight tracking-wide md:text-xl ${isOpen ? 'text-ink' : 'text-ink-dim'}`}
                      >
                        {s.title}
                      </span>
                      <span aria-hidden="true" className="micro shrink-0 pt-1 text-ink-dim">
                        {isOpen ? '−' : '+'}
                      </span>
                    </button>
                  </h2>
                  {isOpen ? (
                    <div className="pb-7 ps-[3.1rem]">
                      <p className="max-w-[58ch] text-sm leading-[1.85] text-ink-dim">{s.body}</p>
                      {s.id === 'transfer' ? (
                        <button
                          type="button"
                          onClick={() => setSheet(true)}
                          className="micro mt-5 rounded-full border border-ink px-4 py-2.5 text-ink transition-colors hover:border-accent hover:bg-accent hover:text-[color:var(--accent-ink)]"
                        >
                          {tr('plan.compare')}
                        </button>
                      ) : null}
                    </div>
                  ) : null}
                </div>
              );
            })}
          </div>
        </div>
      </section>

      {/* ── Itinerary builder ─────────────────────────────────────────── */}
      <section className="bg-ground px-5 py-16 md:px-10 md:py-24">
        <div className="mb-10 flex flex-wrap items-end justify-between gap-4">
          <div>
            <p className="max-w-[20ch] font-display text-[clamp(1.5rem,3vw,2.25rem)] font-medium uppercase leading-[1.06] text-ink">
              {tr('plan.buildTitle')}
            </p>
            <p className="mt-3 max-w-[46ch] text-sm text-ink-dim">{tr('plan.buildNote')}</p>
          </div>
          <div className="flex items-center rounded-full border border-ink-ghost p-0.5">
            {[3, 5, 7].map((d) => (
              <button
                key={d}
                type="button"
                onClick={() => setDays(d)}
                aria-pressed={days === d}
                className={`micro rounded-full px-4 py-2 transition-colors ${
                  days === d
                    ? 'bg-accent text-[color:var(--accent-ink)]'
                    : 'text-ink-dim hover:text-ink'
                }`}
              >
                {d} {tr('plan.days')}
              </button>
            ))}
          </div>
        </div>

        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          {places.map((p) => {
            const on = picked.includes(p.id);
            const full = !on && picked.length >= capacity;
            return (
              <button
                key={p.id}
                type="button"
                onClick={() => toggle(p.id)}
                aria-pressed={on}
                disabled={full}
                className={`group relative text-left transition-opacity ${full ? 'cursor-not-allowed opacity-35' : ''}`}
              >
                <PhotoField
                  brief={`${p.name}. Establishing shot, warm natural light`}
                  showSlots={false}
                  plate={p.plateMosaic ?? p.plateRail}
                  className="aspect-[3/2] w-full"
                  variant="mid"
                />
                <span
                  aria-hidden="true"
                  className={`pointer-events-none absolute inset-0 border-2 transition-colors ${on ? 'border-accent' : 'border-transparent'}`}
                />
                <div className="mt-3 flex items-baseline justify-between gap-2">
                  <span className="font-display text-sm uppercase tracking-wide text-ink">
                    {p.name}
                  </span>
                  <span className="micro shrink-0" style={{ color: on ? 'var(--accent)' : 'var(--ink-dim)' }}>
                    {on ? '✓' : '+'}
                  </span>
                </div>
                <p className="micro mt-1.5 text-ink-dim">{p.region}</p>
              </button>
            );
          })}
        </div>

        <p className="micro mt-6 text-ink-dim">
          {picked.length} / {capacity} {tr('plan.selected')}
        </p>
      </section>

      {/* ── Transfer sheet ────────────────────────────────────────────────
          forest-sage-colourway-sheet: a sheet slides up over the page rather
          than navigating away, so the accordion keeps its position. */}
      {sheet ? (
        <div
          className="fixed inset-0 z-50 flex items-end justify-center bg-black/55 p-0 backdrop-blur-sm md:p-6"
          role="dialog"
          aria-modal="true"
          aria-label={tr('plan.sheetTitle')}
          onClick={() => setSheet(false)}
        >
          <div
            className="anim-lift w-full max-w-3xl rounded-t-2xl bg-ground p-6 md:rounded-2xl md:p-10"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-start justify-between gap-6">
              <div>
                <p className="micro text-ink-dim">{tr('plan.sheetEyebrow')}</p>
                <p className="mt-3 font-display text-2xl uppercase tracking-wide text-ink md:text-3xl">
                  {tr('plan.sheetTitle')}
                </p>
              </div>
              <button
                type="button"
                onClick={() => setSheet(false)}
                aria-label={tr('nav.close')}
                className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full border border-ink-ghost text-ink transition-colors hover:border-accent hover:bg-accent hover:text-[color:var(--accent-ink)]"
              >
                ×
              </button>
            </div>

            <div className="mt-8 overflow-x-auto">
              <table className="w-full min-w-[30rem] border-collapse text-left">
                <thead>
                  <tr className="border-b border-ink-ghost">
                    {[tr('plan.colMode'), tr('plan.colCost'), tr('plan.colTime'), tr('plan.colNight')].map(
                      (h) => (
                        <th key={h} className="micro pb-3 pr-6 font-normal text-ink-dim">
                          {h}
                        </th>
                      ),
                    )}
                  </tr>
                </thead>
                <tbody>
                  {TRANSFERS.map((t) => (
                    <tr key={t.mode} className="border-b border-ink-ghost last:border-b-0">
                      <td className="py-3.5 pr-6 text-sm text-ink">{t.mode}</td>
                      <td className="figures py-3.5 pr-6 text-sm text-ink-dim">{t.cost}</td>
                      <td className="figures py-3.5 pr-6 text-sm text-ink-dim">{t.time}</td>
                      <td className="py-3.5 text-sm text-ink-dim">{t.night}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <p className="mt-6 max-w-[54ch] text-sm leading-relaxed text-ink-dim">
              {tr('plan.sheetNote')}
            </p>
          </div>
        </div>
      ) : null}
    </PageShell>
  );
}
