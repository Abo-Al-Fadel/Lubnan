'use client';

import { useEffect, useState } from 'react';
import { PhotoField } from '@/components/ui/PhotoField';
import { Crosshair, Heart } from '@/components/ui/Subjects';
import { Reveal } from '@/components/ui/Reveal';
import { Counter } from '@/components/ui/Counter';
import { useAuth } from '@/lib/auth';
import { useCatalog } from '@/lib/catalog';
import { listPosts, toggleLike, type CommunityPost } from '@/lib/community';
import { ApiError } from '@/lib/api';
import { useSite } from '@/lib/site-state';
import secrets from '@/data/secrets.json';

type Slots = { showSlots: boolean };

/* ─────────────────────────────────────────────────────────────────────────
   Lede — a wide statement with two justified micro-columns as texture.
   mentora-golden-hour-bottom-anchor: short justified measures turn body copy
   into texture rather than into something read first.
   ────────────────────────────────────────────────────────────────────── */
export function Lede({
  statement,
  notes,
  showSlots,
}: {
  statement: string;
  notes: [string, string];
  showSlots: boolean;
}) {
  const { tc } = useSite();
  /* realspace-arc-photo-row: photographs of differing aspect ratios stepped
     along a shallow arc, alternating vertical offsets with the tallest at the
     centre, so the row reads as a composition rather than as a gallery strip.
     This section was pure text, which is what made it feel like a wall. */
  /* Every photograph is captioned. Five uncaptioned images in a row are
     decoration; the same five with a place, a date and one fact are the
     densest part of the page. */
  const arc = [
    {
      plate: 'G1',
      ratio: 'aspect-[3/4]',
      shift: 'md:translate-y-8',
      brief: 'Sidon sea castle on its causeway, portrait, late afternoon',
      place: 'Sidon',
      when: '1228',
      fact: 'Crusader castle on a causeway, built from Roman columns laid flat.',
    },
    {
      plate: 'G2',
      ratio: 'aspect-[4/5]',
      shift: 'md:translate-y-2',
      brief: 'Anjar Umayyad arcades, portrait, low sun through the arches',
      place: 'Anjar',
      when: '714',
      fact: 'The only Umayyad city built on a Roman grid, emptied within decades.',
    },
    {
      plate: 'G3',
      ratio: 'aspect-[4/5]',
      shift: 'md:-translate-y-6',
      brief: 'Beiteddine palace courtyard, portrait, arcaded facade and fountain',
      place: 'Beiteddine',
      when: '1818',
      fact: 'Thirty years of building, paid for by mountain silk.',
    },
    {
      plate: 'G4',
      ratio: 'aspect-[4/5]',
      shift: 'md:translate-y-2',
      brief: 'Tripoli Mamluk souk interior, portrait, light shafts through vaulting',
      place: 'Tripoli',
      when: '1289',
      fact: 'The largest Mamluk quarter outside Cairo, still trading.',
    },
    {
      plate: 'G5',
      ratio: 'aspect-[3/4]',
      shift: 'md:translate-y-8',
      brief: 'Bekaa vineyard rows toward the Anti-Lebanon range, portrait',
      place: 'Bekaa',
      when: '1857',
      fact: 'Commercial wine since Château Ksara. The Romans got there first.',
    },
  ];

  return (
    <section className="bg-ground px-5 py-20 md:px-10 md:py-28">
      <div className="grid gap-10 md:grid-cols-12 md:gap-14">
        <Reveal as="p" mode="words" className="max-w-[24ch] font-display text-[clamp(1.75rem,3.6vw,3rem)] font-medium uppercase leading-[1.04] tracking-[-0.02em] text-ink md:col-span-7">
          {tc('lede.statement', statement)}
        </Reveal>
        <div className="grid gap-6 sm:grid-cols-2 md:col-span-5">
          {notes.map((n, i) => (
            <p
              key={n.slice(0, 24)}
              className="text-[0.78rem] leading-[1.9] text-ink-dim [text-align:justify]"
            >
              {tc(`lede.note${i + 1}`, n)}
            </p>
          ))}
        </div>
      </div>

      <div className="mt-14 grid grid-cols-2 gap-3 md:mt-20 md:grid-cols-5 md:gap-5">
        {arc.map((a, i) => (
          <div
            key={a.plate}
            className={`group ${a.shift} ${i === 4 ? 'col-span-2 md:col-span-1' : ''}`}
          >
            <PhotoField
              brief={a.brief}
              showSlots={showSlots}
              plate={a.plate}
              className={`${a.ratio} w-full [&>img]:transition-transform [&>img]:duration-[900ms] [&>img]:ease-out group-hover:[&>img]:scale-[1.06]`}
              variant={i % 2 === 0 ? 'low' : 'high'}
            />
            <div className="mt-3 flex items-baseline justify-between gap-2 border-t border-ink-ghost pt-3">
              <span className="font-display text-sm uppercase tracking-wide text-ink">
                {a.place}
              </span>
              <span className="micro figures shrink-0 text-ink-dim">{a.when}</span>
            </div>
            <p className="mt-2 text-[0.72rem] leading-[1.6] text-ink-dim">{a.fact}</p>
          </div>
        ))}
      </div>
    </section>
  );
}

/* ─────────────────────────────────────────────────────────────────────────
   DestinationRail — a horizontal rail cut by the viewport edge.
   africa-ghosted-vertical-drum: cutting the rail at the edge is a deliberate
   refusal to fit everything neatly inside the container.
   ────────────────────────────────────────────────────────────────────── */
export function DestinationRail({ showSlots }: Slots) {
  const { tr } = useSite();
  const { places } = useCatalog();
  return (
    <section className="bg-band py-20 md:py-28">
      <div className="mb-10 flex items-end justify-between px-5 md:mb-12 md:px-10">
        <Reveal as="p" mode="words" className="max-w-[18ch] font-display text-[clamp(1.5rem,3vw,2.5rem)] font-medium uppercase leading-[1.06] text-ink">
          {tr('rail.title')}
        </Reveal>
        <p className="micro figures shrink-0 text-ink-dim">
          01. {String(places.length).padStart(2, '0')}
        </p>
      </div>

      <div className="rail flex snap-x snap-mandatory gap-4 overflow-x-auto px-5 pb-2 md:gap-6 md:px-10">
        {places.map((d) => (
          <a
            key={d.id}
            href={`/explore/${d.id}`}
            className="group w-[78vw] shrink-0 snap-start sm:w-[52vw] md:w-[34vw] lg:w-[26vw]"
          >
            <PhotoField
              brief={`${d.name}, ${d.region}. Establishing shot that reads at card size, natural light, no people in frame`}
              showSlots={showSlots}
              plate={d.plateRail}
              className="aspect-[4/5] w-full [&>img]:transition-transform [&>img]:duration-[900ms] [&>img]:ease-out group-hover:[&>img]:scale-[1.06]"
              variant={Number(d.index) % 2 === 0 ? 'low' : 'mid'}
            />
            <div className="mt-4 flex items-baseline justify-between gap-3">
              <p className="font-display text-lg uppercase tracking-wide text-ink transition-colors duration-300 ease-out group-hover:text-accent">
                {d.name}
              </p>
              <p className="micro figures shrink-0 text-ink-dim">{d.index}</p>
            </div>
            <p className="micro mt-2 text-ink-dim">
              {d.region} · <span className="font-arabic normal-case">{d.arabic}</span>
            </p>
            <p className="mt-3 max-w-[38ch] text-[0.82rem] leading-relaxed text-ink-dim">
              {d.note}
            </p>
          </a>
        ))}
      </div>
    </section>
  );
}

/* ─────────────────────────────────────────────────────────────────────────
   DestinationMosaic — asymmetric grid, unequal spans, one tile bleeding wide.
   pga-resort-full-scroll: almost no two blocks share a column structure,
   which is what stops a page reading as templated.
   ────────────────────────────────────────────────────────────────────── */
export function DestinationMosaic({ showSlots }: Slots) {
  const { tr, tc } = useSite();
  const { places } = useCatalog();
  const picks = places.slice(0, 5);
  /* Fixed auto-row height: with only absolutely positioned children the tiles
     have no intrinsic height, so a content-sized row collapses them.
     Two bands of 12 — 7+5, then 5+4+3. Every tile spans two rows, which is
     what keeps the ratio range survivable: the previous single-row tiles
     measured 3.94:1 at 1920, a slit no photograph reads in. Now the spread is
     1.1:1 to 2.7:1, and the bands stay irregular so it still reads as a
     mosaic rather than as a card grid. */
  const spans = [
    'aspect-[4/3] md:aspect-auto md:col-span-7 md:row-span-2',
    'aspect-[16/10] md:aspect-auto md:col-span-5 md:row-span-2',
    'aspect-[16/10] md:aspect-auto md:col-span-5 md:row-span-2',
    'aspect-[4/3] md:aspect-auto md:col-span-4 md:row-span-2',
    'aspect-[4/5] md:aspect-auto md:col-span-3 md:row-span-2',
  ];

  return (
    <section className="bg-ground px-5 py-20 md:px-10 md:py-28">
      <div className="mb-10 flex items-end justify-between md:mb-12">
        <Reveal as="p" mode="words" className="max-w-[20ch] font-display text-[clamp(1.5rem,3vw,2.5rem)] font-medium uppercase leading-[1.06] text-ink">
          {tr('mosaic.title')}
        </Reveal>
        <a
          href="/explore"
          className="micro tap shrink-0 border-b border-ink pb-1 text-ink transition-opacity duration-200 ease-out hover:opacity-60"
        >
          {tr('mosaic.all')}
        </a>
      </div>

      <div className="grid grid-cols-1 gap-3 md:auto-rows-[12rem] md:grid-cols-12 md:gap-4">
        {picks.map((d, i) => (
          <a key={d.id} href={`/explore/${d.id}`} className={`group relative ${spans[i]}`}>
            <PhotoField
              brief={`${d.name}, ${d.region}. ${i === 0 ? 'wide establishing shot with room for an overlaid caption' : 'mid shot, single clear subject'}, natural light`}
              showSlots={showSlots}
              plate={d.plateMosaic}
              className="absolute inset-0 h-full w-full [&>img]:transition-transform [&>img]:duration-[900ms] [&>img]:ease-out group-hover:[&>img]:scale-[1.06]"
              variant={i % 3 === 0 ? 'low' : i % 3 === 1 ? 'mid' : 'high'}
            />
            <div className="scrim absolute inset-0" aria-hidden="true" />
            <div className="absolute bottom-0 left-0 p-4 transition-transform duration-500 ease-out group-hover:-translate-y-1 md:p-6">
              <p className="font-display text-xl uppercase tracking-wide text-hero-ink md:text-2xl">
                {d.name}
              </p>
              <p className="micro mt-2 text-hero-ink-dim">
                {d.index} · {tc(`region.${d.region}`, d.region)}
              </p>
            </div>
          </a>
        ))}
      </div>
    </section>
  );
}

/* ─────────────────────────────────────────────────────────────────────────
   Nicknames. What the country has been called, and by whom.

   These are the names people actually reach for when they talk about Lebanon,
   and the site had none of them anywhere. Each is given with the reason it was
   coined and, where it matters, the tense it belongs in: "Switzerland of the
   Middle East" is a pre-1975 phrase, and presenting it as current would be a
   tourism brochure lying to a reader who can check.
   ────────────────────────────────────────────────────────────────────── */
export function Nicknames() {
  const { tr } = useSite();

  const names = [
    { key: 'swiss', era: 'c. 1950 – 1975' },
    { key: 'paris', era: 'c. 1950 – 1975' },
    { key: 'pearl', era: 'Still used' },
    { key: 'cedar', era: 'On the flag since 1943' },
  ];

  return (
    <section className="border-y border-ink-ghost bg-band px-5 py-16 md:px-10 md:py-24">
      <div className="grid gap-10 md:grid-cols-12 md:gap-14">
        <div className="md:col-span-4">
          <Reveal
            as="p"
            mode="words"
            className="max-w-[15ch] font-display text-[clamp(1.5rem,3vw,2.5rem)] font-medium uppercase leading-[1.06] text-ink"
          >
            {tr('names.title')}
          </Reveal>
          <p className="mt-5 max-w-[38ch] text-sm leading-relaxed text-ink-dim">
            {tr('names.lede')}
          </p>
        </div>

        <div className="md:col-span-8">
          {names.map((n) => (
            <div
              key={n.key}
              className="grid gap-2 border-t border-ink-ghost py-6 last:border-b md:grid-cols-12 md:gap-6"
            >
              <p className="font-display text-lg uppercase tracking-wide text-ink md:col-span-5 md:text-xl">
                {tr(`names.${n.key}.name`)}
              </p>
              <p className="micro text-ink-dim md:col-span-2">{n.era}</p>
              <p className="max-w-[46ch] text-sm leading-relaxed text-ink-dim md:col-span-5">
                {tr(`names.${n.key}.why`)}
              </p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}

/* ─────────────────────────────────────────────────────────────────────────
   NumbersBar — hairline-divided figures, no cards.
   realspace-arc-photo-row: a stat row separated by thin vertical rules
   instead of four shadowed cards.
   ────────────────────────────────────────────────────────────────────── */
export function NumbersBar() {
  const { tr } = useSite();
  const rows = [
    { figure: '10,452', unit: 'km²', label: tr('numbers.area') },
    { figure: '3,088', unit: 'm', label: tr('numbers.peak') },
    { figure: '225', unit: 'km', label: tr('numbers.coast') },
    { figure: '6', unit: 'sites', label: tr('numbers.unesco') },
  ];

  return (
    <section className="border-y border-ink-ghost bg-band px-5 py-14 md:px-10 md:py-16">
      <p className="micro mb-9 text-ink-dim">{tr('numbers.title')}</p>
      <div className="grid grid-cols-2 gap-y-10 md:grid-cols-4 md:gap-y-0 md:divide-x md:divide-ink-ghost">
        {rows.map((r, i) => (
          <div key={r.label} className={i > 0 ? 'md:pl-8' : ''}>
            <p className="font-display text-[clamp(2rem,4.4vw,3.25rem)] font-medium leading-none text-ink">
              <Counter value={r.figure} className="figures" />
              <span className="ml-1.5 text-[0.4em] font-normal text-ink-dim">{r.unit}</span>
            </p>
            <p className="micro mt-3 max-w-[20ch] leading-[1.7] text-ink-dim">{r.label}</p>
          </div>
        ))}
      </div>
    </section>
  );
}

/* ─────────────────────────────────────────────────────────────────────────
   SecretsList — a numbered accordion with hairline dividers.
   pga-resort-full-scroll: this does the job of a three-icon feature grid
   with no icons, no cards and no shadows. The numbers are ordinal, which is
   the only condition under which the craft floor allows them.
   ────────────────────────────────────────────────────────────────────── */
export function SecretsList({ showSlots }: Slots) {
  const { tr, tc } = useSite();
  const [open, setOpen] = useState(0);

  /* scroll-mt clears the fixed navbar. Without it an anchor lands with its
     heading underneath the bar. */
  return (
    <section id="secrets" className="scroll-mt-28 bg-ground px-5 py-20 md:px-10 md:py-28">
      {/* Two columns, not three. As 3 + 4 + 5 the middle column held only a
          heading and two lines, so everything below them was a tall empty
          rectangle sitting between the photograph and the list. Heading, lede
          and photograph now stack in one column that fills its own height, and
          the list takes the other. */}
      <div className="grid gap-10 md:grid-cols-12 md:gap-14">
        <div className="md:col-span-5">
          <Reveal as="p" mode="words" className="max-w-[16ch] font-display text-[clamp(1.5rem,3vw,2.5rem)] font-medium uppercase leading-[1.06] text-ink">
            {tr('secrets.title')}
          </Reveal>
          <p className="mt-5 max-w-[38ch] text-sm leading-relaxed text-ink-dim">
            {tr('secrets.lede')}
          </p>
          {/* pga-resort-full-scroll pairs its numbered accordion with an image
              rather than letting the list stand alone on the page. */}
          <PhotoField
            brief="The unfinished megalith in the Baalbek quarry, a person at its base for scale, warm low sun"
            showSlots={showSlots}
            plate="F1"
            className="mt-8 aspect-[4/3] w-full md:mt-10 md:aspect-[3/2]"
            variant="high"
          />
          <p className="micro mt-4 max-w-[34ch] leading-[1.7] text-ink-dim">
            {tr('secrets.caption')}
          </p>
        </div>

        <div className="md:col-span-7">
          {secrets.map((s, i) => {
            const isOpen = open === i;
            return (
              <div key={s.index} className="border-t border-ink-ghost last:border-b">
                <h3>
                  <button
                    type="button"
                    onClick={() => setOpen(isOpen ? -1 : i)}
                    aria-expanded={isOpen}
                    className="flex w-full items-baseline gap-5 py-6 text-left transition-opacity duration-200 ease-out hover:opacity-70"
                  >
                    <span
                      className="micro figures shrink-0 pt-1 transition-colors duration-300 ease-out"
                      style={{ color: isOpen ? 'var(--accent)' : 'var(--ink-dim)' }}
                    >
                      {s.index}
                    </span>
                    <span
                      className={`flex-1 font-display text-lg uppercase leading-tight tracking-wide md:text-xl ${isOpen ? 'text-ink' : 'text-ink-dim'}`}
                    >
                      {tc(`secret.${s.index}.title`, s.title)}
                    </span>
                    <span aria-hidden="true" className="micro shrink-0 pt-1 text-ink-dim">
                      {isOpen ? '−' : '+'}
                    </span>
                  </button>
                </h3>
                {isOpen ? (
                  <p className="max-w-[62ch] pb-7 pl-[3.1rem] text-sm leading-relaxed text-ink-dim">
                    {tc(`secret.${s.index}.body`, s.body)}
                  </p>
                ) : null}
              </div>
            );
          })}
        </div>
      </div>
    </section>
  );
}

/* ─────────────────────────────────────────────────────────────────────────
   CommunityStrip — the live feed, four cards.
   ────────────────────────────────────────────────────────────────────── */
export function CommunityStrip({ showSlots }: Slots) {
  const { tr } = useSite();
  const { me } = useAuth();
  const [posts, setPosts] = useState<CommunityPost[]>([]);
  const [ready, setReady] = useState(false);
  const [busy, setBusy] = useState<Record<string, boolean>>({});

  useEffect(() => {
    let cancelled = false;
    void listPosts()
      .then((rows) => {
        if (!cancelled) setPosts(rows.slice(0, 4));
      })
      .catch(() => {
        if (!cancelled) setPosts([]);
      })
      .finally(() => {
        if (!cancelled) setReady(true);
      });
    return () => {
      cancelled = true;
    };
  }, [me]);

  const onLike = async (post: CommunityPost) => {
    if (!me) {
      window.location.href = '/login';
      return;
    }
    if (busy[post.id]) return;
    setBusy((s) => ({ ...s, [post.id]: true }));
    setPosts((list) =>
      list.map((p) =>
        p.id === post.id
          ? {
              ...p,
              likedByMe: !p.likedByMe,
              likeCount: p.likeCount + (p.likedByMe ? -1 : 1),
            }
          : p,
      ),
    );
    try {
      const state = await toggleLike(post.id);
      setPosts((list) =>
        list.map((p) =>
          p.id === post.id ? { ...p, likedByMe: state.liked, likeCount: state.likeCount } : p,
        ),
      );
    } catch (err) {
      setPosts((list) => list.map((p) => (p.id === post.id ? post : p)));
      if (err instanceof ApiError && err.status === 401) {
        window.location.href = '/login';
      }
    } finally {
      setBusy((s) => ({ ...s, [post.id]: false }));
    }
  };

  return (
    <section className="bg-band px-5 py-20 md:px-10 md:py-28">
      <div className="mb-10 flex flex-wrap items-end justify-between gap-4 md:mb-12">
        <Reveal as="p" mode="words" className="max-w-[20ch] font-display text-[clamp(1.5rem,3vw,2.5rem)] font-medium uppercase leading-[1.06] text-ink">
          {tr('community.title')}
        </Reveal>
        <a
          href="/community"
          className="micro tap shrink-0 border-b border-ink pb-1 text-ink transition-opacity duration-200 ease-out hover:opacity-60"
        >
          {tr('community.open')}
        </a>
      </div>

      {ready && posts.length === 0 ? (
        <p className="max-w-[46ch] text-sm text-ink-dim">{tr('community.errorLoad')}</p>
      ) : (
        <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
          {posts.map((p) => (
            <article key={p.id} className="group flex flex-col">
              <a href={`/community#${p.id}`}>
                <PhotoField
                  brief={p.body}
                  showSlots={showSlots}
                  plate={p.plate ?? undefined}
                  className="aspect-square w-full [&>img]:transition-transform [&>img]:duration-[900ms] [&>img]:ease-out group-hover:[&>img]:scale-[1.05]"
                  variant="mid"
                />
              </a>
              <div className="mt-4 flex items-baseline justify-between gap-2">
                <p className="text-sm font-medium text-ink">{p.author.displayName}</p>
                <p className="micro shrink-0 text-ink-dim">{p.placeName ?? p.region ?? ''}</p>
              </div>
              <p className="mt-2.5 max-w-[40ch] flex-1 text-[0.82rem] leading-relaxed text-ink-dim">
                {p.body}
              </p>
              <div className="mt-4 flex items-center gap-5 border-t border-ink-ghost pt-3.5">
                <button
                  type="button"
                  onClick={() => void onLike(p)}
                  aria-pressed={p.likedByMe}
                  className="micro figures tap flex min-h-[32px] items-center gap-2 transition-opacity duration-200 ease-out hover:opacity-70"
                  style={{ color: p.likedByMe ? 'var(--accent)' : 'var(--ink-dim)' }}
                >
                  <Heart filled={p.likedByMe} />
                  {p.likeCount}
                  <span className="sr-only">{p.likedByMe ? tr('community.unlike') : tr('community.like')}</span>
                </button>
                <span className="micro figures text-ink-dim">
                  {p.comments.length} {tr('community.replies')}
                </span>
              </div>
            </article>
          ))}
        </div>
      )}
    </section>
  );
}

/* ─────────────────────────────────────────────────────────────────────────
   ClosingCTA — the occlusion move echoed at the foot of the page, so the
   page opens and closes on the same idea.
   ────────────────────────────────────────────────────────────────────── */
export function ClosingCTA({ showSlots }: Slots) {
  const { tr, tc } = useSite();
  return (
    <section className="relative min-h-[72svh] overflow-hidden bg-ground">
      <PhotoField
        brief="Window seat on approach to Beirut, wing over the Mediterranean with the coastline and mountains beyond, early morning"
        showSlots={showSlots}
        plate="E1"
        className="absolute inset-0"
        variant="low"
      />
      <div className="scrim scrim-deep relative flex min-h-[72svh] flex-col justify-end p-5 md:p-10">
        <Crosshair className="mb-6 text-hero-ink-dim" />
        <Reveal as="p" mode="words" className="max-w-[15ch] font-display text-[clamp(2.25rem,6.4vw,4.75rem)] font-bold uppercase leading-[0.94] tracking-[-0.025em] text-hero-ink">
          {tc('cta.line')}
        </Reveal>
        <div className="mt-9 flex flex-wrap items-center gap-4">
          <a
            href="/plan"
            /* Primary action on a photograph, so it has to hold its own against
               whatever is behind it. Resting state is a solid eggshell pill
               with a near-black label — 16.6:1, reads on any frame. Hover
               inverts to the accent teal with eggshell type, 6.5:1, so the
               state change is carried by fill *and* label rather than by a
               1px lift you have to be looking for. Both are tokens, so the
               button follows the palette instead of pinning its own colours. */
            className="btn-solid group inline-flex items-center gap-2.5 rounded-full px-8 py-4 text-sm font-semibold shadow-[0_8px_28px_-8px_rgba(0,0,0,0.65)] ring-1 ring-black/10 transition-all duration-300 ease-out hover:-translate-y-0.5 hover:shadow-[0_14px_34px_-10px_rgba(0,0,0,0.75)]"
          >
            {tr('cta.plan')}
            <span
              aria-hidden="true"
              className="transition-transform duration-300 ease-out group-hover:translate-x-1"
            >
              →
            </span>
          </a>
          <a
            href="/plan#transfer"
            /* A bare underlined link sat on the bright half of the plate and
               went white-on-water. Given a ring and its own faint ground it
               reads at any crop, and it still stays clearly secondary to the
               solid pill beside it. `border-hero-ink/50` was also a no-op —
               Tailwind cannot put an alpha on a var() colour. */
            className="micro rounded-full border border-[color:var(--hero-ink-ghost)] bg-black/25 px-5 py-3.5 text-hero-ink backdrop-blur-sm transition-colors duration-300 ease-out hover:border-[color:var(--hero-ink)] hover:bg-black/40"
          >
            {tr('cta.airport')}
          </a>
        </div>
      </div>
    </section>
  );
}
