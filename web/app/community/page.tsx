'use client';

import { useMemo, useState } from 'react';
import { Navbar, SiteFooter } from '@/components/sections/Chrome';
import { PhotoField } from '@/components/ui/PhotoField';
import { Heart } from '@/components/ui/Subjects';
import { useSite } from '@/lib/site-state';
import { useAuth } from '@/lib/auth';
import { REGIONS, places } from '@/data/places';
import posts from '@/data/posts.json';

/**
 * A feed, not a gallery.
 *
 * The previous version was a masonry grid of images — which is a *portfolio*
 * layout, and reading it required opening a modal to see anything. A social
 * feed is a single narrow column you scroll: author, image, actions, caption,
 * replies, all inline, one post at a time. Nothing is hidden behind a click.
 *
 * The column is capped at 34rem because a feed's job is to be read at a fixed
 * comfortable width no matter how wide the window gets — the same reason
 * Instagram and LinkedIn don't reflow their posts to fill a desktop monitor.
 */

const AUTHORS = [
  { name: 'Rania K.', handle: 'raniak', bio: 'Qadisha, mostly' },
  { name: 'Marc H.', handle: 'marc.h', bio: 'Batroun · photographs food' },
  { name: 'Yara S.', handle: 'yaras', bio: 'Tyre → Beirut' },
  { name: 'Elias N.', handle: 'eliasn', bio: 'Corniche every evening' },
];

const CAPTIONS = [
  'Took the long path down from Bsharri. Three hours, one monastery cut into the cliff, and a man who insisted I take his thermos.',
  'The lemonade stand everyone tells you about is real, and it is worth the queue.',
  'Hippodrome at seven in the morning, completely empty. Two thousand years of chariot racing and one stray cat.',
  'Sunset from the Corniche. Half the city is out walking and someone is always selling corn.',
  'Snow on the road above Bsharri closed it for two days. Worth waiting for.',
  'Mezze that arrived in nine plates when we ordered four. Standard.',
  'Baalbek at golden hour. The columns are twenty-two metres and photographs do not carry it.',
  'Diving off the rocks at Batroun. Water was colder than it looks here.',
  'Vineyard lunch in the Bekaa. Long table, dappled light, nobody left before dark.',
  'First light at Tyre harbour. The boats go out before the tourists arrive.',
  'Jeita from the boat. No cameras allowed inside so this is the entrance.',
  'Byblos harbour, same size it has been for three thousand years.',
];

const PLATES = ['D1', 'D2', 'D3', 'D4', 'Q5', 'Q6', 'Q7', 'Q8', 'Q9', 'Q10', 'Q11', 'Q12'];

const FEED = Array.from({ length: 12 }, (_, i) => {
  const author = AUTHORS[i % AUTHORS.length];
  const place = places[i % places.length];
  return {
    id: `p${i + 1}`,
    author,
    place: place.name,
    region: place.region,
    placeId: place.id,
    plate: PLATES[i],
    caption: CAPTIONS[i],
    likes: 38 + ((i * 47) % 260),
    comments: 2 + ((i * 7) % 21),
    when: `${1 + ((i * 3) % 20)}h`,
    tall: i % 4 === 1,
  };
});

export default function CommunityPage() {
  const { tr } = useSite();
  const { me } = useAuth();
  const [liked, setLiked] = useState<Record<string, boolean>>({});
  const [expanded, setExpanded] = useState<Record<string, boolean>>({});
  const [region, setRegion] = useState<string | null>(null);
  const [draft, setDraft] = useState('');
  const [notice, setNotice] = useState<string | null>(null);

  const shown = useMemo(
    () => (region ? FEED.filter((p) => p.region === region) : FEED),
    [region],
  );

  return (
    <div data-palette="cedar" className="bg-band">
      <Navbar />

      <main id="main" className="px-4 pb-24 pt-28 md:px-8 md:pt-32">
        {/* The feed is chrome-light by design, but a page still needs a
            heading. Visually hidden rather than absent. */}
        <h1 className="sr-only">{tr('community.pageTitle')}</h1>
        {/* Filter bar sits above the feed and stays put — the only chrome. */}
        <div className="sticky top-[4.5rem] z-30 -mx-4 mb-6 border-b border-ink-ghost px-4 py-3 backdrop-blur-xl md:top-[5.5rem] md:-mx-8 md:px-8">
          <div
            className="absolute inset-0 -z-10"
            style={{ background: 'color-mix(in srgb, var(--band) 88%, transparent)' }}
          />
          <div className="rail mx-auto flex max-w-[74rem] gap-2 overflow-x-auto">
            <button
              type="button"
              onClick={() => setRegion(null)}
              aria-pressed={region === null}
              className={`micro shrink-0 rounded-full border px-3.5 py-2 transition-colors ${
                region === null
                  ? 'border-accent bg-accent text-[color:var(--accent-ink)]'
                  : 'border-ink-ghost text-ink-dim hover:text-ink'
              }`}
            >
              {tr('community.all')}
            </button>
            {REGIONS.map((r) => (
              <button
                key={r}
                type="button"
                onClick={() => setRegion(region === r ? null : r)}
                aria-pressed={region === r}
                className={`micro shrink-0 rounded-full border px-3.5 py-2 transition-colors ${
                  region === r
                    ? 'border-accent bg-accent text-[color:var(--accent-ink)]'
                    : 'border-ink-ghost text-ink-dim hover:text-ink'
                }`}
              >
                {r}
              </button>
            ))}
          </div>
        </div>

        <div className="mx-auto grid max-w-[74rem] grid-cols-1 gap-10 lg:grid-cols-[minmax(0,34rem)_minmax(0,1fr)] lg:justify-center">
          {/* ── The column ────────────────────────────────────────────── */}
          <div className="flex flex-col gap-5">
            {/* Compose */}
            <div className="rounded-xl border border-ink-ghost bg-ground p-4">
              <div className="flex gap-3">
                <Avatar name={me?.displayName ?? tr('community.you')} />
                <div className="flex-1">
                  <label htmlFor="compose" className="sr-only">
                    {tr('community.composeLabel')}
                  </label>
                  <textarea
                    id="compose"
                    rows={draft ? 3 : 1}
                    value={draft}
                    onChange={(e) => setDraft(e.target.value)}
                    placeholder={me ? tr('community.composePlaceholder') : tr('community.signInToPost')}
                    disabled={!me}
                    className="w-full resize-none border-0 bg-transparent p-0 text-sm leading-relaxed text-ink outline-none placeholder:text-ink-dim disabled:opacity-70"
                  />
                </div>
              </div>
              {draft && me ? (
                <div className="mt-3 flex items-center justify-between gap-3 border-t border-ink-ghost pt-3">
                  <p className="micro text-ink-dim">{tr('community.composeNote')}</p>
                  <button
                    type="button"
                    onClick={() => {
                      setDraft('');
                      setNotice(tr('community.composeNote'));
                    }}
                    className="micro rounded-full bg-accent px-4 py-2 text-[color:var(--accent-ink)]"
                  >
                    {tr('community.post')}
                  </button>
                </div>
              ) : null}
              {notice ? (
                <p className="micro mt-3 text-ink-dim" role="status">
                  {notice}
                </p>
              ) : null}
            </div>

            {shown.length === 0 ? (
              <p className="py-8 text-sm text-ink-dim">{tr('community.empty')}</p>
            ) : null}

            {shown.map((p) => {
              const isLiked = Boolean(liked[p.id]);
              const isOpen = Boolean(expanded[p.id]);
              return (
                <article key={p.id} className="rounded-xl border border-ink-ghost bg-ground">
                  {/* Author row */}
                  <header className="flex items-center gap-3 p-4">
                    <Avatar name={p.author.name} />
                    <div className="min-w-0 flex-1">
                      <p className="truncate text-sm font-semibold text-ink">{p.author.name}</p>
                      <p className="micro mt-1 truncate text-ink-dim">
                        <a href={`/explore/${p.placeId}`} className="tap hover:text-accent">
                          {p.place}
                        </a>{' '}
                        · {p.when}
                      </p>
                    </div>
                    <button
                      type="button"
                      aria-label={tr('community.more')}
                      disabled
                      title={tr('community.comingSoon')}
                      className="shrink-0 px-2 text-ink-dim opacity-40"
                    >
                      ···
                    </button>
                  </header>

                  {/* Caption above the image, LinkedIn-style — the words are
                      why you stop scrolling, not the photograph. */}
                  <p className="px-4 pb-3 text-sm leading-relaxed text-ink">
                    {isOpen || p.caption.length < 92 ? (
                      p.caption
                    ) : (
                      <>
                        {p.caption.slice(0, 88)}…{' '}
                        <button
                          type="button"
                          onClick={() => setExpanded((s) => ({ ...s, [p.id]: true }))}
                          className="tap text-ink-dim transition-colors hover:text-accent"
                        >
                          {tr('community.more')}
                        </button>
                      </>
                    )}
                  </p>

                  <PhotoField
                    brief={`${p.place}. ${p.caption.slice(0, 60)}`}
                    showSlots={false}
                    plate={p.plate}
                    className={`w-full ${p.tall ? 'aspect-[4/5]' : 'aspect-square'}`}
                    variant="mid"
                  />

                  {/* Counts, then actions — the order every feed uses. */}
                  <div className="flex items-center gap-4 px-4 pt-3">
                    <span className="micro figures text-ink-dim">
                      {p.likes + (isLiked ? 1 : 0)} {tr('community.likes')}
                    </span>
                    <span className="micro figures text-ink-dim">
                      {p.comments} {tr('community.replies')}
                    </span>
                  </div>

                  <div className="mt-3 flex items-stretch border-t border-ink-ghost">
                    <Action
                      onClick={() => setLiked((s) => ({ ...s, [p.id]: !s[p.id] }))}
                      active={isLiked}
                      pressed={isLiked}
                      label={isLiked ? tr('community.liked') : tr('community.like2')}
                      icon={<Heart filled={isLiked} />}
                    />
                    <Action label={tr('community.comment')} icon={<CommentIcon />} disabled title={tr('community.comingSoon')} />
                    <Action label={tr('community.share')} icon={<ShareIcon />} disabled title={tr('community.comingSoon')} />
                    <Action label={tr('community.save')} icon={<SaveIcon />} disabled title={tr('community.comingSoon')} />
                  </div>
                </article>
              );
            })}

            <p className="micro py-8 text-center text-ink-dim">{tr('community.end')}</p>
          </div>

          {/* ── Sidebar ───────────────────────────────────────────────── */}
          <aside className="hidden lg:block">
            <div className="sticky top-32 flex flex-col gap-5">
              <div className="rounded-xl border border-ink-ghost bg-ground p-5">
                <p className="micro mb-4 text-ink-dim">{tr('community.contributors')}</p>
                <ul className="flex flex-col gap-4">
                  {AUTHORS.map((a) => (
                    <li key={a.handle} className="flex items-center gap-3">
                      <Avatar name={a.name} />
                      <div className="min-w-0 flex-1">
                        <p className="truncate text-sm font-medium text-ink">{a.name}</p>
                        <p className="micro mt-1 truncate text-ink-dim">{a.bio}</p>
                      </div>
                      <button
                        type="button"
                        disabled
                        title={tr('community.comingSoon')}
                        className="micro shrink-0 rounded-full border border-ink-ghost px-3 py-1.5 text-ink opacity-50"
                      >
                        {tr('community.follow')}
                      </button>
                    </li>
                  ))}
                </ul>
              </div>

              <div className="rounded-xl border border-ink-ghost bg-ground p-5">
                <p className="micro mb-4 text-ink-dim">{tr('community.trending')}</p>
                <ul className="flex flex-col">
                  {places.slice(0, 5).map((p, i) => (
                    <li key={p.id} className="border-t border-ink-ghost first:border-t-0">
                      <a
                        href={`/explore/${p.id}`}
                        className="flex items-baseline justify-between gap-3 py-3 transition-colors hover:text-accent"
                      >
                        <span className="text-sm text-ink">{p.name}</span>
                        <span className="micro figures shrink-0 text-ink-dim">
                          {94 - i * 13}
                        </span>
                      </a>
                    </li>
                  ))}
                </ul>
              </div>
            </div>
          </aside>
        </div>
      </main>

      <SiteFooter />
    </div>
  );
}

/** Initials on the accent. No stock avatar photography, no placeholder faces. */
function Avatar({ name }: { name: string }) {
  const initials = name
    .split(' ')
    .map((w) => w[0])
    .join('')
    .slice(0, 2)
    .toUpperCase();
  return (
    <span
      aria-hidden="true"
      className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full text-xs font-semibold"
      style={{ background: 'var(--accent)', color: 'var(--accent-ink)' }}
    >
      {initials}
    </span>
  );
}

function Action({
  label,
  icon,
  onClick,
  active,
  pressed,
  disabled,
  title,
}: {
  label: string;
  icon: React.ReactNode;
  onClick?: () => void;
  active?: boolean;
  pressed?: boolean;
  disabled?: boolean;
  title?: string;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-pressed={pressed}
      disabled={disabled}
      title={title}
      className="micro flex flex-1 items-center justify-center gap-2 py-3 transition-colors hover:bg-[color:var(--ink-ghost)] disabled:cursor-not-allowed disabled:opacity-50"
      style={{ color: active ? 'var(--accent)' : 'var(--ink-dim)' }}
    >
      {icon}
      <span className="hidden sm:inline">{label}</span>
    </button>
  );
}

/* Drawn to the same 1.4px stroke as the rest of the icon set. */
const S = {
  fill: 'none',
  stroke: 'currentColor',
  strokeWidth: 1.4,
  strokeLinecap: 'round' as const,
  strokeLinejoin: 'round' as const,
};

function CommentIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 16 16" aria-hidden="true">
      <path d="M14 7.5a5.5 5.5 0 0 1-8.1 4.85L2 13.5l1.2-3.5A5.5 5.5 0 1 1 14 7.5Z" {...S} />
    </svg>
  );
}

function ShareIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 16 16" aria-hidden="true">
      <path d="M14.5 1.5 7.2 8.8M14.5 1.5l-4.7 13-2.6-5.7-5.7-2.6 13-4.7Z" {...S} />
    </svg>
  );
}

function SaveIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 16 16" aria-hidden="true">
      <path d="M3.5 1.8h9v12.4L8 11l-4.5 3.2V1.8Z" {...S} />
    </svg>
  );
}
