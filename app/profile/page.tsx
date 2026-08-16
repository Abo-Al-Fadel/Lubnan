'use client';

import { useState } from 'react';
import { Navbar, SiteFooter } from '@/components/sections/Chrome';
import { PhotoField } from '@/components/ui/PhotoField';
import { LebanonMap } from '@/components/ui/LebanonMap';
import { Counter } from '@/components/ui/Counter';
import { useSite } from '@/lib/site-state';
import { places } from '@/data/places';
import posts from '@/data/posts.json';

/**
 * Saved places, in Raouche.
 *
 * This is the payoff for keeping the second palette: "your Lebanon" reads as a
 * distinct room inside the site rather than as another page of the same one,
 * and the drawn map returns here with only the saved places pinned — so the
 * map becomes a record of where the visitor has actually been.
 */
const SAVED = ['byblos', 'cedars', 'baalbek', 'batroun', 'qadisha'];

export default function ProfilePage() {
  const { tr } = useSite();
  const [saved, setSaved] = useState<string[]>(SAVED);
  const [tab, setTab] = useState<'places' | 'trips' | 'posts'>('places');

  const savedPlaces = places.filter((p) => saved.includes(p.id));

  const TABS: [typeof tab, string, number][] = [
    ['places', tr('profile.tabPlaces'), savedPlaces.length],
    ['trips', tr('profile.tabTrips'), 2],
    ['posts', tr('profile.tabPosts'), posts.length],
  ];

  return (
    <div data-palette="raouche" className="bg-ground">
      <Navbar />

      <main id="main" className="px-5 pb-24 pt-32 md:px-10 md:pt-40">
        {/* ── Identity ────────────────────────────────────────────────── */}
        <header className="grid gap-8 border-b border-ink-ghost pb-12 md:grid-cols-12 md:gap-12">
          <div className="md:col-span-7">
            <p className="micro text-ink-dim">{tr('profile.eyebrow')}</p>
            <h1 className="mt-4 font-display text-[clamp(2.25rem,6vw,4rem)] font-bold uppercase leading-[0.92] tracking-[-0.02em] text-ink">
              Rania K.
            </h1>
            <p className="mt-5 max-w-[42ch] text-sm leading-relaxed text-ink-dim">
              {tr('profile.bio')}
            </p>
          </div>

          <dl className="grid grid-cols-3 gap-6 md:col-span-5 md:self-end">
            {[
              [String(savedPlaces.length), tr('profile.statSaved')],
              ['2', tr('profile.statTrips')],
              ['4', tr('profile.statPosts')],
            ].map(([figure, label]) => (
              <div key={label}>
                <dt className="sr-only">{label}</dt>
                <dd>
                  <Counter
                    value={figure}
                    className="figures block font-display text-[clamp(1.75rem,3.4vw,2.5rem)] font-medium leading-none text-ink"
                  />
                  <span className="micro mt-3 block max-w-[14ch] leading-[1.7] text-ink-dim">
                    {label}
                  </span>
                </dd>
              </div>
            ))}
          </dl>
        </header>

        {/* ── Tabs ────────────────────────────────────────────────────── */}
        <nav className="mt-10 flex flex-wrap gap-2" aria-label={tr('profile.eyebrow')}>
          {TABS.map(([id, label, count]) => (
            <button
              key={id}
              type="button"
              onClick={() => setTab(id)}
              aria-pressed={tab === id}
              className={`micro flex items-center gap-2 rounded-full border px-4 py-2.5 transition-colors ${
                tab === id
                  ? 'border-accent bg-accent text-[color:var(--accent-ink)]'
                  : 'border-ink-ghost text-ink-dim hover:text-ink'
              }`}
            >
              {label}
              <span className="figures opacity-60">{count}</span>
            </button>
          ))}
        </nav>

        {/* ── Saved places ────────────────────────────────────────────── */}
        {tab === 'places' ? (
          <div className="mt-12 grid gap-10 md:grid-cols-12 md:gap-14">
            <div className="md:col-span-8">
              <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
                {savedPlaces.map((p) => (
                  <article key={p.id} className="group relative">
                    <a href={`/explore/${p.id}`} className="block">
                      <PhotoField
                        brief={`${p.name}. Establishing shot, warm natural light`}
                        showSlots={false}
                        plate={p.plateMosaic ?? p.plateRail}
                        className="aspect-[4/5] w-full [&>img]:transition-transform [&>img]:duration-[900ms] [&>img]:ease-out group-hover:[&>img]:scale-[1.05]"
                        variant="mid"
                      />
                      <p className="mt-3 font-display text-base uppercase tracking-wide text-ink">
                        {p.name}
                      </p>
                      <p className="micro mt-1.5 text-ink-dim">{p.region}</p>
                    </a>
                    <button
                      type="button"
                      onClick={() => setSaved((s) => s.filter((x) => x !== p.id))}
                      className="micro absolute right-2 top-2 rounded-full bg-black/55 px-3 py-1.5 text-[color:var(--hero-ink)] opacity-0 backdrop-blur-sm transition-opacity duration-200 group-hover:opacity-100 focus-visible:opacity-100"
                    >
                      {tr('profile.remove')}
                    </button>
                  </article>
                ))}
              </div>

              {savedPlaces.length === 0 ? (
                <p className="max-w-[40ch] text-sm text-ink-dim">
                  {tr('profile.empty')}{' '}
                  <a href="/explore" className="tap border-b border-ink pb-0.5 text-ink">
                    {tr('nav.explore')}
                  </a>
                </p>
              ) : null}
            </div>

            <aside className="md:col-span-4">
              <div className="border border-ink-ghost p-5 md:sticky md:top-28">
                <p className="micro mb-5 text-ink-dim">{tr('profile.mapTitle')}</p>
                <LebanonMap
                  waypoints={savedPlaces.map((p) => ({
                    id: p.id,
                    name: p.name,
                    lon: p.lon,
                    lat: p.lat,
                    side: p.lon < 35.7 ? 'left' : 'right',
                  }))}
                  className="mx-auto h-[46svh] w-auto text-ink"
                />
              </div>
            </aside>
          </div>
        ) : null}

        {/* ── Trips ───────────────────────────────────────────────────── */}
        {tab === 'trips' ? (
          <div className="mt-12 flex flex-col">
            {[
              { name: 'North in five days', days: 5, stops: ['Byblos', 'Batroun', 'Qadisha', 'The Cedars of God', 'Tripoli'] },
              { name: 'Ruins run', days: 3, stops: ['Baalbek', 'Anjar', 'Tyre'] },
            ].map((t) => (
              <article key={t.name} className="border-t border-ink-ghost py-7 last:border-b">
                <div className="flex flex-wrap items-baseline justify-between gap-4">
                  <h2 className="font-display text-xl uppercase tracking-wide text-ink">{t.name}</h2>
                  <span className="micro figures text-ink-dim">
                    {t.days} {tr('plan.days')} · {t.stops.length} {tr('profile.stops')}
                  </span>
                </div>
                <ol className="mt-4 flex flex-wrap gap-x-2 gap-y-2">
                  {t.stops.map((s, i) => (
                    <li key={s} className="micro flex items-center gap-2 text-ink-dim">
                      {i > 0 ? <span aria-hidden="true" className="opacity-40">→</span> : null}
                      {s}
                    </li>
                  ))}
                </ol>
                <a
                  href="/plan"
                  className="micro mt-5 inline-block border-b border-ink pb-1 text-ink transition-opacity hover:opacity-60"
                >
                  {tr('profile.resume')}
                </a>
              </article>
            ))}
          </div>
        ) : null}

        {/* ── Posts ───────────────────────────────────────────────────── */}
        {tab === 'posts' ? (
          <div className="mt-12 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            {posts.map((p) => (
              <article key={p.id}>
                <PhotoField
                  brief={p.imageBrief}
                  showSlots={false}
                  plate={p.plate}
                  className="aspect-square w-full"
                  variant="mid"
                />
                <p className="micro mt-3 text-ink-dim">{p.place}</p>
                <p className="mt-2 text-[0.82rem] leading-relaxed text-ink-dim">{p.caption}</p>
              </article>
            ))}
          </div>
        ) : null}
      </main>

      <SiteFooter />
    </div>
  );
}
