'use client';

import { useEffect, useState } from 'react';
import { Navbar, SiteFooter } from '@/components/sections/Chrome';
import { PhotoField } from '@/components/ui/PhotoField';
import { LebanonMap } from '@/components/ui/LebanonMap';
import { Counter } from '@/components/ui/Counter';
import { useSite } from '@/lib/site-state';
import { removeAvatar, setAvatar, useAuth } from '@/lib/auth';
import { ApiError } from '@/lib/api';
import { useCatalog } from '@/lib/catalog';
import { listPosts, relativeTime, type CommunityPost } from '@/lib/community';
import { listSaved, unpinSaved } from '@/lib/saved';

/**
 * Saved places, in Raouche.
 *
 * This is the payoff for keeping the second palette: "your Lebanon" reads as a
 * distinct room inside the site rather than as another page of the same one,
 * and the drawn map returns here with only the saved places pinned — so the
 * map becomes a record of where the visitor has actually been.
 */

export default function ProfilePage() {
  const { tr } = useSite();
  const { me, ready } = useAuth();
  const { places } = useCatalog();
  const [saved, setSaved] = useState<string[]>([]);
  const [mine, setMine] = useState<CommunityPost[]>([]);
  const [tab, setTab] = useState<'places' | 'trips' | 'posts'>('places');

  /**
   * Avatar state.
   *
   * `avatarVersion` doubles as "has one": null means initials. It is seeded
   * with a HEAD probe rather than carried on /me, so the account payload stays
   * about the account.
   */
  const [avatarVersion, setAvatarVersion] = useState<string | null>(null);
  const [avatarBusy, setAvatarBusy] = useState(false);
  const [avatarError, setAvatarError] = useState<string | null>(null);

  // Seeded from /me, which already knows. No probe, so a profile with no
  // picture no longer logs a 404 on every visit.
  useEffect(() => {
    setAvatarVersion(me?.avatarVersion ?? null);
  }, [me]);

  const onPickAvatar = async (file: File) => {
    setAvatarBusy(true);
    setAvatarError(null);
    try {
      setAvatarVersion(await setAvatar(file));
    } catch (err) {
      // The server's sentence, not a generic one: it says whether the file was
      // too large, the wrong kind, or too many pixels, and each has a different
      // thing to do about it.
      setAvatarError(err instanceof ApiError ? err.message : tr('profile.avatarFailed'));
    } finally {
      setAvatarBusy(false);
    }
  };

  const onRemoveAvatar = async () => {
    setAvatarBusy(true);
    setAvatarError(null);
    try {
      await removeAvatar();
      setAvatarVersion(null);
    } catch {
      setAvatarError(tr('profile.avatarFailed'));
    } finally {
      setAvatarBusy(false);
    }
  };

  useEffect(() => {
    if (!me) {
      setSaved([]);
      setMine([]);
      return;
    }
    let cancelled = false;
    void listSaved()
      .then((rows) => {
        if (!cancelled) setSaved(rows.map((row) => row.slug));
      })
      .catch(() => {
        if (!cancelled) setSaved([]);
      });
    void listPosts()
      .then((posts) => {
        if (!cancelled) setMine(posts.filter((post) => post.author.id === me.id));
      })
      .catch(() => {
        if (!cancelled) setMine([]);
      });
    return () => {
      cancelled = true;
    };
  }, [me]);

  const remove = async (slug: string) => {
    setSaved((cur) => cur.filter((id) => id !== slug));
    try {
      await unpinSaved(slug);
    } catch {
      setSaved((cur) => (cur.includes(slug) ? cur : [...cur, slug]));
    }
  };

  const savedPlaces = places.filter((p) => saved.includes(p.id));

  const TABS: [typeof tab, string, number][] = [
    ['places', tr('profile.tabPlaces'), savedPlaces.length],
    ['trips', tr('profile.tabTrips'), 0],
    ['posts', tr('profile.tabPosts'), mine.length],
  ];

  return (
    <div data-palette="raouche" className="bg-ground">
      <Navbar />

      <main id="main" className="px-5 pb-24 pt-32 md:px-10 md:pt-40">
        {!ready ? (
          <p className="micro text-ink-dim" role="status">
            {tr('login.pending')}
          </p>
        ) : !me ? (
          <header className="max-w-xl border-b border-ink-ghost pb-12">
            <p className="micro text-ink-dim">{tr('profile.eyebrow')}</p>
            <h1 className="mt-4 font-display text-[clamp(2.25rem,6vw,4rem)] font-bold uppercase leading-[0.92] tracking-[-0.02em] text-ink">
              {tr('profile.guestTitle')}
            </h1>
            <p className="mt-5 max-w-[42ch] text-sm leading-relaxed text-ink-dim">
              {tr('profile.guestBody')}
            </p>
            <a
              href="/login"
              className="btn-solid mt-8 inline-flex rounded-full px-8 py-4 text-sm font-semibold"
            >
              {tr('profile.signInCta')}
            </a>
          </header>
        ) : (
          <>
            <header className="grid gap-8 border-b border-ink-ghost pb-12 md:grid-cols-12 md:gap-12">
              <div className="md:col-span-7">
                <p className="micro text-ink-dim">{tr('profile.eyebrow')}</p>

                <div className="mt-5 flex items-center gap-4">
                  {/* The version in the query string is what makes a new
                      picture appear. The image is served immutable for a year,
                      so without it the browser would keep showing the old one
                      until the cache expired. */}
                  <span className="relative block h-20 w-20 shrink-0 overflow-hidden rounded-full bg-band">
                    {avatarVersion ? (
                      <img
                        src={`/api/v1/users/${me.id}/avatar?v=${avatarVersion}`}
                        alt=""
                        className="h-full w-full object-cover"
                      />
                    ) : (
                      <span className="flex h-full w-full items-center justify-center font-display text-2xl uppercase text-ink-dim">
                        {me.displayName.trim().charAt(0)}
                      </span>
                    )}
                  </span>

                  <div className="flex flex-col gap-1">
                    <label className="tap micro cursor-pointer text-ink underline decoration-ink-ghost underline-offset-4 hover:decoration-ink">
                      {avatarBusy ? tr('profile.avatarWorking') : tr('profile.avatarChange')}
                      <input
                        type="file"
                        // A hint for the file picker, not a check. The server
                        // decodes and re-encodes whatever arrives, which is
                        // what actually decides.
                        accept="image/png,image/jpeg,image/webp"
                        className="sr-only"
                        disabled={avatarBusy}
                        onChange={(e) => {
                          const file = e.target.files?.[0];
                          e.target.value = '';
                          if (file) void onPickAvatar(file);
                        }}
                      />
                    </label>

                    {avatarVersion ? (
                      <button
                        type="button"
                        onClick={() => void onRemoveAvatar()}
                        disabled={avatarBusy}
                        className="tap micro self-start text-ink-dim hover:text-ink disabled:opacity-40"
                      >
                        {tr('profile.avatarRemove')}
                      </button>
                    ) : null}

                    {avatarError ? (
                      <p role="alert" className="micro max-w-[28ch] leading-[1.6] text-status-warn">
                        {avatarError}
                      </p>
                    ) : null}
                  </div>
                </div>

                <h1 className="mt-4 font-display text-[clamp(2.25rem,6vw,4rem)] font-bold uppercase leading-[0.92] tracking-[-0.02em] text-ink">
                  {me.displayName}
                </h1>
                <p className="mt-5 max-w-[42ch] text-sm leading-relaxed text-ink-dim">
                  {tr('profile.signedInAs')} {me.email}
                </p>
              </div>

              <dl className="grid grid-cols-3 gap-6 md:col-span-5 md:self-end">
                {[
                  [String(savedPlaces.length), tr('profile.statSaved')],
                  ['0', tr('profile.statTrips')],
                  [String(mine.length), tr('profile.statPosts')],
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
                            className="aspect-[4/5] w-full [&_img]:transition-transform [&_img]:duration-[900ms] [&_img]:ease-out group-hover:[&_img]:scale-[1.05]"
                            variant="mid"
                          />
                          <p className="mt-3 font-display text-base uppercase tracking-wide text-ink">
                            {p.name}
                          </p>
                          <p className="micro mt-1.5 text-ink-dim">{p.region}</p>
                        </a>
                        <button
                          type="button"
                          onClick={() => void remove(p.id)}
                          className="micro absolute right-2 top-2 rounded-full bg-black/55 px-3 py-1.5 text-[color:var(--hero-ink)] opacity-100 backdrop-blur-sm transition-opacity duration-200 md:opacity-0 md:group-hover:opacity-100 md:focus-visible:opacity-100"
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

            {tab === 'trips' ? (
              <p className="mt-12 max-w-[46ch] text-sm text-ink-dim">
                {tr('profile.emptyTrips')}{' '}
                <a href="/plan" className="tap border-b border-ink pb-0.5 text-ink">
                  {tr('nav.plan')}
                </a>
              </p>
            ) : null}

            {tab === 'posts' ? (
              mine.length === 0 ? (
                <p className="mt-12 max-w-[46ch] text-sm text-ink-dim">
                  {tr('profile.emptyPosts')}{' '}
                  <a href="/community" className="tap border-b border-ink pb-0.5 text-ink">
                    {tr('nav.community')}
                  </a>
                </p>
              ) : (
                <ul className="mt-12 max-w-xl">
                  {mine.map((post) => (
                    <li key={post.id} className="border-t border-ink-ghost py-5 last:border-b">
                      <a href={`/community#${post.id}`} className="block">
                        <p className="text-sm leading-relaxed text-ink">{post.body}</p>
                        <p className="micro mt-2 text-ink-dim">
                          {post.placeName ?? tr('community.anyPlace')} · {relativeTime(post.createdAt)}
                        </p>
                      </a>
                    </li>
                  ))}
                </ul>
              )
            ) : null}
          </>
        )}
      </main>

      <SiteFooter />
    </div>
  );
}
