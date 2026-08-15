'use client';

import { useState } from 'react';
import { notFound } from 'next/navigation';
import { PhotoField } from '@/components/ui/PhotoField';
import { HeroSubject } from '@/components/ui/HeroSubject';
import { LebanonMap } from '@/components/ui/LebanonMap';
import { Navbar, SiteFooter } from '@/components/sections/Chrome';
import { Reveal } from '@/components/ui/Reveal';
import { useSite } from '@/lib/site-state';
import { getPlace, places } from '@/data/places';
import type { Variation } from '@/data/variations';

export default function PlacePage({ params }: { params: { place: string } }) {
  const { tr } = useSite();
  const [open, setOpen] = useState<number | null>(null);
  const place = getPlace(params.place);
  if (!place) notFound();

  const nearby = places
    .filter((p) => p.id !== place.id)
    .map((p) => ({
      ...p,
      d: Math.hypot((p.lon - place.lon) * 92, (p.lat - place.lat) * 111),
    }))
    .sort((a, b) => a.d - b.d)
    .slice(0, 3);

  /* HeroSubject takes a Variation; the place supplies its own cut-out plate
     so each destination occludes its own name with its own subject. */
  const subjectVariation = {
    key: 'cedar',
    cutoutPlate: place.subject,
    subject: 'cedar',
  } as unknown as Variation;

  return (
    <div data-palette="cedar" className="bg-ground">
      <Navbar />

      <main id="main">
        {/* ── Occlusion hero, one per place ───────────────────────────── */}
        <section className="relative flex flex-col overflow-hidden bg-band">
          <PhotoField
            brief={`${place.name}, ${place.region} — wide establishing shot in warm low sun, rich natural colour`}
            showSlots={false}
            plate={place.heroPlate}
            priority
            className="anim-plate absolute inset-0"
            variant="high"
            /* Receives the card that was clicked on Explore. */
          />
          <div
            aria-hidden="true"
            className="absolute inset-0"
            style={{ viewTransitionName: `place-${place.id}` }}
          />

          <div className="scrim relative flex min-h-[86svh] flex-col justify-end px-5 pb-10 pt-32 text-hero-ink md:px-10 md:pb-14">
            <div className="relative flex-1">
              <span
                aria-hidden="true"
                className="anim-word pointer-events-none absolute bottom-0 left-0 z-10 font-display text-[clamp(2.75rem,13vw,11rem)] font-bold uppercase leading-[0.82] tracking-[-0.015em] text-hero-ink"
              >
                {place.name}
              </span>
              <div className="anim-subject absolute bottom-[-2%] right-[4%] z-20 hidden md:block">
                <HeroSubject
                  variation={subjectVariation}
                  className="h-[46svh] drop-shadow-[0_24px_50px_rgba(0,0,0,0.42)]"
                />
              </div>
            </div>

            <div className="relative z-30 mt-8 grid gap-6 border-t border-hero-ink-ghost pt-6 md:grid-cols-12">
              <p className="micro text-hero-ink-dim md:col-span-3">
                {place.index} · {place.region} ·{' '}
                <span className="font-arabic normal-case">{place.arabic}</span>
              </p>
              <p className="max-w-[58ch] text-sm leading-relaxed text-hero-ink md:col-span-9 md:text-base">
                {place.standfirst}
              </p>
            </div>
          </div>

          <div id="nav-sentinel" aria-hidden="true" className="h-px w-full" />

          <div className="seam h-24 w-full md:h-36">
            <div aria-hidden="true" className="seam-blur seam-blur-1" />
            <div aria-hidden="true" className="seam-blur seam-blur-2" />
            <div aria-hidden="true" className="seam-blur seam-blur-3" />
            <div aria-hidden="true" className="seam-bleed" />
          </div>
        </section>

        {/* ── Annotated frame ─────────────────────────────────────────────
            nexrv-annotated-hotspots: callouts joined by hairlines to real
            points in the photograph. Each one names something you can
            actually find, which is what makes the image explanatory rather
            than decorative. */}
        <section className="bg-ground px-5 py-16 md:px-10 md:py-24">
          <p className="micro mb-8 text-ink-dim">{tr('place.whatsHere')}</p>

          <div className="relative">
            <PhotoField
              brief={`${place.name} — wide annotated frame with clear separation between the named features, warm daylight`}
              showSlots={false}
              plate={place.framePlate}
              className="aspect-[16/10] w-full md:aspect-[21/9]"
              variant="mid"
            />

            <svg
              className="pointer-events-none absolute inset-0 h-full w-full"
              viewBox="0 0 100 100"
              preserveAspectRatio="none"
              aria-hidden="true"
            >
              {place.callouts.map((c, i) => (
                <line
                  key={c.label}
                  x1={c.x * 100}
                  y1={c.y * 100}
                  x2={c.x * 100}
                  y2={i % 2 === 0 ? 4 : 96}
                  stroke="var(--hero-ink)"
                  strokeWidth={0.18}
                  opacity={open === i ? 0.95 : 0.5}
                  vectorEffect="non-scaling-stroke"
                />
              ))}
            </svg>

            {place.callouts.map((c, i) => (
              <button
                key={c.label}
                type="button"
                onClick={() => setOpen(open === i ? null : i)}
                aria-expanded={open === i}
                className="absolute -translate-x-1/2 -translate-y-1/2"
                style={{ left: `${c.x * 100}%`, top: `${c.y * 100}%` }}
              >
                <span className="sr-only">{c.label}</span>
                <span
                  aria-hidden="true"
                  className={`block h-3.5 w-3.5 rounded-full border-2 border-[color:var(--hero-ink)] transition-all duration-300 ${
                    open === i ? 'scale-125 bg-accent' : 'bg-transparent'
                  }`}
                />
              </button>
            ))}

            {place.callouts.map((c, i) => (
              <div
                key={`${c.label}-card`}
                className={`absolute w-[min(20rem,62vw)] -translate-x-1/2 transition-opacity duration-300 ${
                  open === i ? 'opacity-100' : 'pointer-events-none opacity-0'
                }`}
                style={{
                  left: `clamp(11rem, ${c.x * 100}%, calc(100% - 11rem))`,
                  [i % 2 === 0 ? 'top' : 'bottom']: '4%',
                }}
              >
                <div className="border-l-2 border-accent bg-black/55 p-4 backdrop-blur-md">
                  <p className="micro text-hero-ink">{c.label}</p>
                  <p className="mt-2 text-[0.82rem] leading-relaxed text-hero-ink-dim">{c.body}</p>
                </div>
              </div>
            ))}
          </div>

          <div className="mt-6 flex flex-wrap gap-2">
            {place.callouts.map((c, i) => (
              <button
                key={`${c.label}-chip`}
                type="button"
                onClick={() => setOpen(open === i ? null : i)}
                className={`micro rounded-full border px-3.5 py-2 transition-colors duration-200 ${
                  open === i
                    ? 'border-accent bg-accent text-[color:var(--accent-ink)]'
                    : 'border-ink-ghost text-ink-dim hover:text-ink'
                }`}
              >
                {c.label}
              </button>
            ))}
          </div>
        </section>

        {/* ── The long fact ─────────────────────────────────────────────── */}
        <section className="bg-band px-5 py-16 md:px-10 md:py-24">
          <div className="grid gap-10 md:grid-cols-12 md:gap-14">
            <div className="md:col-span-7">
              <Reveal
                as="p"
                mode="words"
                className="max-w-[26ch] font-display text-[clamp(1.5rem,3.2vw,2.5rem)] font-medium uppercase leading-[1.06] text-ink"
              >
                {place.localName === place.name ? place.name : `${place.name} · ${place.localName}`}
              </Reveal>
              <p className="mt-6 max-w-[64ch] text-[0.95rem] leading-[1.85] text-ink-dim">
                {place.body}
              </p>
            </div>

            <div className="md:col-span-5">
              <p className="micro text-ink-dim">{tr('place.practical')}</p>
              <dl className="mt-5">
                {place.practical.map((row) => (
                  <div
                    key={row.label}
                    className="grid grid-cols-3 gap-4 border-t border-ink-ghost py-4 last:border-b"
                  >
                    <dt className="micro col-span-1 text-ink-dim">{row.label}</dt>
                    <dd className="col-span-2 text-sm leading-relaxed text-ink">{row.value}</dd>
                  </div>
                ))}
              </dl>

              <div className="mt-8 border border-ink-ghost p-5">
                <p className="micro mb-4 text-ink-dim">{tr('place.where')}</p>
                <LebanonMap
                  waypoints={[
                    {
                      id: place.id,
                      name: place.name,
                      lon: place.lon,
                      lat: place.lat,
                      side: place.lon < 35.7 ? 'left' : 'right',
                    },
                  ]}
                  activeId={place.id}
                  className="mx-auto h-[38svh] w-auto text-ink"
                />
              </div>
            </div>
          </div>
        </section>

        {/* ── Nearby ────────────────────────────────────────────────────── */}
        <section className="bg-ground py-16 md:py-24">
          <div className="mb-10 flex items-end justify-between px-5 md:px-10">
            <p className="font-display text-[clamp(1.5rem,3vw,2.25rem)] font-medium uppercase leading-[1.06] text-ink">
              {tr('place.nearby')}
            </p>
            <a
              href="/explore"
              className="micro shrink-0 border-b border-ink pb-1 text-ink transition-opacity hover:opacity-60"
            >
              {tr('mosaic.all')}
            </a>
          </div>

          <div className="rail flex snap-x snap-mandatory gap-4 overflow-x-auto px-5 pb-2 md:gap-6 md:px-10">
            {nearby.map((p) => (
              <a
                key={p.id}
                href={`/explore/${p.id}`}
                className="group w-[78vw] shrink-0 snap-start sm:w-[46vw] md:w-[30vw]"
              >
                <PhotoField
                  brief={`${p.name} — establishing shot, warm light`}
                  showSlots={false}
                  plate={p.plateMosaic ?? p.plateRail}
                  className="aspect-[4/3] w-full [&>img]:transition-transform [&>img]:duration-[900ms] [&>img]:ease-out group-hover:[&>img]:scale-[1.06]"
                  variant="mid"
                />
                <div className="mt-4 flex items-baseline justify-between gap-3">
                  <p className="font-display text-lg uppercase tracking-wide text-ink transition-colors group-hover:text-accent">
                    {p.name}
                  </p>
                  <p className="micro figures shrink-0 text-ink-dim">
                    {Math.round(p.d)} km
                  </p>
                </div>
                <p className="micro mt-2 text-ink-dim">{p.region}</p>
              </a>
            ))}
          </div>
        </section>
      </main>

      <SiteFooter />
    </div>
  );
}
