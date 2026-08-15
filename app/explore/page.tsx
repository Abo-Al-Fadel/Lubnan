'use client';

import { useMemo, useState } from 'react';
import { SectionHead } from '@/components/PageShell';
import { PhotoField } from '@/components/ui/PhotoField';
import { LebanonMap } from '@/components/ui/LebanonMap';
import { Navbar, SiteFooter } from '@/components/sections/Chrome';
import { useSite } from '@/lib/site-state';
import { places, REGIONS } from '@/data/places';

/* Regions get their own photograph rather than a colour chip.
   forest-sage-colourway-sheet: each swatch is shot in the environment it
   names, so the picker is informative instead of five identical tiles. */
const REGION_PLATES: Record<string, { plate: string; brief: string }> = {
  Coast: { plate: 'H1', brief: 'Lebanese coastline from the air at golden hour, turquoise shallows over pale limestone' },
  'Mount Lebanon': { plate: 'H2', brief: 'Terraced slopes of Mount Lebanon in late afternoon light, stone villages on the ridges' },
  North: { plate: 'H3', brief: 'The northern highlands above Bsharri under warm alpenglow, snow on the peaks' },
  South: { plate: 'H4', brief: 'Southern hills running down to the sea near Tyre, olive groves in low sun' },
  Bekaa: { plate: 'H5', brief: 'The Bekaa valley at sunrise, vineyards and the Anti-Lebanon range beyond' },
};

export default function ExplorePage() {
  const { tr } = useSite();
  const [region, setRegion] = useState<string | null>(null);
  const [hover, setHover] = useState<string | null>(null);

  const filtered = useMemo(
    () => (region ? places.filter((p) => p.region === region) : places),
    [region],
  );

  const waypoints = useMemo(
    () =>
      filtered.map((p) => ({
        id: p.id,
        name: p.name,
        lon: p.lon,
        lat: p.lat,
        /* Coastal places hang their label seaward so it never crosses the
           border line; inland places hang inland. */
        side: (p.lon < 35.7 ? 'left' : 'right') as 'left' | 'right',
      })),
    [filtered],
  );

  /* Mosaic spans: unequal on purpose. A uniform grid is the one thing that
     would make this page read as a template. */
  const spans = [
    'md:col-span-7 md:row-span-2',
    'md:col-span-5 md:row-span-2',
    'md:col-span-4 md:row-span-2',
    'md:col-span-4 md:row-span-2',
    'md:col-span-4 md:row-span-2',
    'md:col-span-5 md:row-span-2',
    'md:col-span-7 md:row-span-2',
    'md:col-span-12 md:row-span-2',
  ];

  return (
    <div data-palette="cedar" className="bg-ground">
      <Navbar />

      <main id="main">
        {/* ── The map is the hero ─────────────────────────────────────────
            yosemite-route-line-overlay: the one graphic on the page encodes
            the actual geography rather than decorating the frame. */}
        <section className="relative flex flex-col overflow-hidden bg-band">
          <PhotoField
            brief="Aerial of the Lebanese coast meeting the mountains at sunrise, warm light on snow, deep blue Mediterranean"
            showSlots={false}
            plate="H7"
            priority
            className="anim-plate absolute inset-0"
            variant="high"
          />

          <div className="scrim scrim-left relative grid min-h-[92svh] grid-cols-1 items-center gap-8 px-5 pb-16 pt-32 text-hero-ink md:grid-cols-12 md:px-10 md:pb-20 md:pt-36">
            <div className="md:col-span-6 lg:col-span-5">
              <p className="micro anim-lift text-hero-ink-dim">{tr('explore.eyebrow')}</p>
              <h1 className="anim-word mt-4 font-display text-[clamp(2.75rem,7vw,5.5rem)] font-bold uppercase leading-[0.86] tracking-[-0.01em] text-hero-ink">
                {tr('explore.title')}
              </h1>
              <p
                className="anim-lift mt-6 max-w-[46ch] text-sm leading-relaxed text-hero-ink md:text-base"
                style={{ animationDelay: '240ms' }}
              >
                {tr('explore.lede')}
              </p>

              <dl className="mt-10 grid max-w-md grid-cols-3 gap-4 border-t border-hero-ink-ghost pt-6">
                {[
                  ['225', tr('explore.statCoast')],
                  ['3,088', tr('explore.statPeak')],
                  ['8', tr('explore.statPlaces')],
                ].map(([figure, label]) => (
                  <div key={label}>
                    <dt className="figures font-display text-2xl font-medium leading-none text-hero-ink">
                      {figure}
                    </dt>
                    <dd className="micro mt-2 text-hero-ink-dim">{label}</dd>
                  </div>
                ))}
              </dl>
            </div>

            <div className="flex justify-center md:col-span-6 md:justify-end lg:col-span-7">
              <LebanonMap
                waypoints={waypoints}
                activeId={hover}
                onSelect={(id) => {
                  window.location.href = `/explore/${id}`;
                }}
                className="h-[52svh] w-auto md:h-[72svh]"
              />
            </div>
          </div>

          <div className="seam h-24 w-full md:h-36">
            {/* See Hero: in flow this was a 1px band of un-scrimmed plate. */}
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

        {/* ── Region rail, not a checkbox column ───────────────────────── */}
        <section className="bg-ground py-16 md:py-20">
          <div className="px-5 md:px-10">
            <SectionHead
              title={tr('explore.regionTitle')}
              note={tr('explore.regionNote')}
              aside={
                region ? (
                  <button
                    type="button"
                    onClick={() => setRegion(null)}
                    className="micro border-b border-ink pb-1 text-ink transition-opacity hover:opacity-60"
                  >
                    {tr('explore.clear')}
                  </button>
                ) : null
              }
            />
          </div>

          <div className="rail flex snap-x snap-mandatory gap-3 overflow-x-auto px-5 pb-3 md:gap-5 md:px-10">
            {REGIONS.map((r) => {
              const active = region === r;
              const count = places.filter((p) => p.region === r).length;
              return (
                <button
                  key={r}
                  type="button"
                  onClick={() => setRegion(active ? null : r)}
                  aria-pressed={active}
                  className="group w-[62vw] shrink-0 snap-start text-left sm:w-[40vw] md:w-[22vw] lg:w-[17vw]"
                >
                  <div className="relative">
                    <PhotoField
                      brief={REGION_PLATES[r].brief}
                      showSlots={false}
                      plate={REGION_PLATES[r].plate}
                      className={`aspect-[3/4] w-full transition-opacity duration-300 [&>img]:transition-transform [&>img]:duration-[900ms] [&>img]:ease-out group-hover:[&>img]:scale-[1.06] ${
                        region && !active ? 'opacity-45' : 'opacity-100'
                      }`}
                      variant="mid"
                    />
                    {active ? (
                      <span
                        aria-hidden="true"
                        className="pointer-events-none absolute inset-0 border-2 border-accent"
                      />
                    ) : null}
                  </div>
                  <div className="mt-3 flex items-baseline justify-between gap-2">
                    <span className="font-display text-sm uppercase tracking-wide text-ink">
                      {r}
                    </span>
                    <span className="micro figures text-ink-dim">{count}</span>
                  </div>
                </button>
              );
            })}
          </div>

          {/* Two-tone progress rule instead of a scrollbar. */}
          <div className="mt-5 px-5 md:px-10">
            <div className="h-px w-full bg-ink-ghost">
              <div
                className="h-px bg-accent transition-[width] duration-500 ease-out"
                style={{ width: region ? '100%' : '32%' }}
              />
            </div>
          </div>
        </section>

        {/* ── Results ───────────────────────────────────────────────────── */}
        <section className="bg-band px-5 py-16 md:px-10 md:py-24">
          <SectionHead
            title={region ? `${region}` : tr('explore.allTitle')}
            note={`${filtered.length} ${tr('explore.results')}`}
          />

          <div className="grid grid-cols-1 gap-3 md:auto-rows-[11rem] md:grid-cols-12 md:gap-4">
            {filtered.map((p, i) => (
              <a
                key={p.id}
                href={`/explore/${p.id}`}
                onMouseEnter={() => setHover(p.id)}
                onMouseLeave={() => setHover(null)}
                /* Names the card as the same element as the place hero, so
                   the View Transition expands this tile into that page rather
                   than cross-fading two unrelated screens. */
                style={{ viewTransitionName: `place-${p.id}` }}
                className={`group relative block aspect-[4/3] md:aspect-auto ${spans[i % spans.length]}`}
              >
                <PhotoField
                  brief={`${p.name}, ${p.region}. Establishing shot, warm natural light`}
                  showSlots={false}
                  plate={p.plateMosaic ?? p.plateRail}
                  className="absolute inset-0 h-full w-full [&>img]:transition-transform [&>img]:duration-[900ms] [&>img]:ease-out group-hover:[&>img]:scale-[1.06]"
                  variant={i % 3 === 0 ? 'low' : i % 3 === 1 ? 'mid' : 'high'}
                />
                <div className="scrim absolute inset-0" aria-hidden="true" />
                <div className="absolute inset-x-0 bottom-0 p-4 transition-transform duration-500 ease-out group-hover:-translate-y-1 md:p-6">
                  <p className="font-display text-xl uppercase tracking-wide text-hero-ink md:text-2xl">
                    {p.name}
                  </p>
                  <p className="micro mt-2 text-hero-ink-dim">
                    {p.index} · {p.region}
                  </p>
                  <p className="mt-3 max-w-[44ch] text-[0.82rem] leading-relaxed text-hero-ink-dim opacity-0 transition-opacity duration-500 group-hover:opacity-100">
                    {p.note}
                  </p>
                </div>
              </a>
            ))}
          </div>
        </section>
      </main>

      <SiteFooter />
    </div>
  );
}
