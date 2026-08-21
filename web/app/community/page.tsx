'use client';

import { useCallback, useEffect, useMemo, useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { Navbar, SiteFooter } from '@/components/sections/Chrome';
import { PhotoField } from '@/components/ui/PhotoField';
import { Heart } from '@/components/ui/Subjects';
import { useSite } from '@/lib/site-state';
import { useAuth } from '@/lib/auth';
import { ApiError } from '@/lib/api';
import { loginHref } from '@/lib/return-to';
import {
  addComment,
  createPost,
  listPosts,
  relativeTime,
  removeComment,
  toggleLike,
  type CommunityPost,
} from '@/lib/community';
import { REGIONS, useCatalog } from '@/lib/catalog';

/**
 * A feed backed by the API.
 *
 * Likes and comments are authorised on the server. The session cookie names
 * the author — a request body cannot post or like as someone else. Guests
 * can read; writing without a session is 401.
 */

export default function CommunityPage() {
  const { tr } = useSite();
  const { me, ready, problem } = useAuth();
  const { places } = useCatalog();
  const router = useRouter();
  const [posts, setPosts] = useState<CommunityPost[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [region, setRegion] = useState<string | null>(null);
  const [draft, setDraft] = useState('');
  const [placeSlug, setPlaceSlug] = useState('');
  const [publishing, setPublishing] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);
  const [expanded, setExpanded] = useState<Record<string, boolean>>({});
  const [removing, setRemoving] = useState<string | null>(null);
  const [openComments, setOpenComments] = useState<Record<string, boolean>>({});
  const [reply, setReply] = useState<Record<string, string>>({});
  const [busy, setBusy] = useState<Record<string, boolean>>({});

  const load = useCallback(async (nextRegion: string | null) => {
    setLoading(true);
    setError(null);
    try {
      setPosts(await listPosts(nextRegion));
    } catch (err) {
      setPosts([]);
      setError(err instanceof ApiError && err.status === 0
        ? tr('community.errorNetwork')
        : tr('community.errorLoad'));
    } finally {
      setLoading(false);
    }
  }, [tr]);

  useEffect(() => {
    if (!ready) return;
    void load(region);
  }, [ready, region, load]);

  const contributors = useMemo(() => {
    const seen = new Map<string, string>();
    for (const post of posts) {
      if (!seen.has(post.author.id)) seen.set(post.author.id, post.author.displayName);
    }
    return Array.from(seen.entries()).slice(0, 6);
  }, [posts]);

  const trending = useMemo(() => {
    const counts = new Map<string, { name: string; slug: string; n: number }>();
    for (const post of posts) {
      if (!post.placeSlug) continue;
      const cur = counts.get(post.placeSlug);
      counts.set(post.placeSlug, {
        slug: post.placeSlug,
        name: post.placeName ?? post.placeSlug,
        n: (cur?.n ?? 0) + 1,
      });
    }
    return Array.from(counts.values()).sort((a, b) => b.n - a.n).slice(0, 5);
  }, [posts]);

  const needSignIn = () => {
    setNotice(tr('community.signInToAct'));
  };

  /**
   * Send a guest to sign in, and bring them back to the post they were on.
   *
   * Used for liking and not for posting or commenting, and the difference is
   * the draft. A like is one tap with nothing to lose, so a notice is easy to
   * miss and navigating costs nothing. A half-written comment is work, and
   * navigating away throws it in the bin - so those keep the notice.
   */
  const signInFor = (postId: string) => {
    router.push(loginHref(`/community#${postId}`));
  };

  const onPublish = async () => {
    if (!me) {
      needSignIn();
      return;
    }
    const body = draft.trim();
    if (!body || publishing) return;
    setPublishing(true);
    setNotice(null);
    try {
      const post = await createPost(body, placeSlug || undefined);
      setPosts((list) => [post, ...list]);
      setDraft('');
      setPlaceSlug('');
    } catch (err) {
      setNotice(err instanceof ApiError && err.status === 401
        ? tr('community.signInToAct')
        : tr('community.errorPost'));
    } finally {
      setPublishing(false);
    }
  };

  const onLike = async (post: CommunityPost) => {
    if (!me) {
      // Only when we actually know they are signed out. If /me failed rather
      // than answered, sending them to a sign-in form they may not need - and
      // which is served by the same API that just failed - is a dead end
      // dressed up as an instruction.
      if (problem === 'unavailable') {
        setNotice(tr('community.errorLoad'));
        return;
      }
      signInFor(post.id);
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
      setPosts((list) =>
        list.map((p) => (p.id === post.id ? post : p)),
      );

      // A 401 that survived the silent refresh means the session really is
      // over, whatever this page still believes about `me`.
      if (err instanceof ApiError && err.status === 401) {
        signInFor(post.id);
      } else {
        setNotice(tr('community.errorLike'));
      }
    } finally {
      setBusy((s) => ({ ...s, [post.id]: false }));
    }
  };

  const onComment = async (postId: string) => {
    if (!me) {
      needSignIn();
      return;
    }
    const body = (reply[postId] ?? '').trim();
    if (!body || busy[`c-${postId}`]) return;
    setBusy((s) => ({ ...s, [`c-${postId}`]: true }));
    try {
      const comment = await addComment(postId, body);
      setPosts((list) =>
        list.map((p) =>
          p.id === postId ? { ...p, comments: [...p.comments, comment] } : p,
        ),
      );
      setReply((s) => ({ ...s, [postId]: '' }));
    } catch (err) {
      setNotice(err instanceof ApiError && err.status === 401
        ? tr('community.signInToAct')
        : tr('community.errorComment'));
    } finally {
      setBusy((s) => ({ ...s, [`c-${postId}`]: false }));
    }
  };

  /**
   * Remove a comment, then take it off the page.
   *
   * The list is rebuilt from the server's answer rather than optimistically:
   * a delete that failed - somebody else's comment, or one already gone -
   * would otherwise disappear from the page and come back on the next load,
   * which reads as the site losing track of itself.
   */
  const onRemoveComment = async (postId: string, commentId: string) => {
    setRemoving(commentId);
    try {
      await removeComment(postId, commentId);
      setPosts((current) =>
        current.map((p) =>
          p.id === postId ? { ...p, comments: p.comments.filter((c) => c.id !== commentId) } : p,
        ),
      );
    } catch {
      /* The comment stays where it is, which is the truth. */
    } finally {
      setRemoving(null);
    }
  };

  const onShare = async (id: string) => {
    const url = `${window.location.origin}/community#${id}`;
    try {
      await navigator.clipboard.writeText(url);
      setNotice(tr('community.copied'));
    } catch {
      setNotice(url);
    }
  };

  return (
    <div data-palette="cedar" className="bg-band">
      <Navbar />

      <main id="main" className="px-4 pb-24 pt-28 md:px-8 md:pt-32">
        <h1 className="sr-only">{tr('community.pageTitle')}</h1>
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
          <div className="flex flex-col gap-5">
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
                    maxLength={2000}
                    className="w-full resize-none border-0 bg-transparent p-0 text-sm leading-relaxed text-ink outline-none placeholder:text-ink-dim disabled:opacity-70"
                  />
                </div>
              </div>
              {draft && me ? (
                <div className="mt-3 flex flex-wrap items-center justify-between gap-3 border-t border-ink-ghost pt-3">
                  <label className="micro text-ink-dim">
                    <span className="sr-only">{tr('community.place')}</span>
                    <select
                      value={placeSlug}
                      onChange={(e) => setPlaceSlug(e.target.value)}
                      className="bg-transparent text-ink outline-none"
                    >
                      <option value="">{tr('community.anyPlace')}</option>
                      {places.map((p) => (
                        <option key={p.id} value={p.id}>
                          {p.name}
                        </option>
                      ))}
                    </select>
                  </label>
                  <button
                    type="button"
                    onClick={() => void onPublish()}
                    disabled={publishing}
                    className="micro rounded-full bg-accent px-4 py-2 text-[color:var(--accent-ink)] disabled:opacity-60"
                  >
                    {publishing ? tr('community.posting') : tr('community.post')}
                  </button>
                </div>
              ) : null}
              {notice ? (
                <p className="micro mt-3 text-ink-dim" role="status">
                  {notice}{' '}
                  {!me ? (
                    <Link href="/login" className="tap text-ink underline-offset-2 hover:underline">
                      {tr('nav.login')}
                    </Link>
                  ) : null}
                </p>
              ) : null}
            </div>

            {loading ? (
              <p className="py-8 text-sm text-ink-dim">{tr('community.loading')}</p>
            ) : null}
            {error ? (
              <p className="py-8 text-sm text-ink-dim" role="alert">
                {error}
              </p>
            ) : null}
            {!loading && !error && posts.length === 0 ? (
              <p className="py-8 text-sm text-ink-dim">{tr('community.empty')}</p>
            ) : null}

            {posts.map((p) => {
              const isOpen = Boolean(expanded[p.id]);
              const commentsOpen = Boolean(openComments[p.id]);
              return (
                <article
                  key={p.id}
                  id={p.id}
                  className="rounded-xl border border-ink-ghost bg-ground"
                >
                  <header className="flex items-center gap-3 p-4">
                    <Avatar name={p.author.displayName} />
                    <div className="min-w-0 flex-1">
                      <p className="truncate text-sm font-semibold text-ink">{p.author.displayName}</p>
                      <p className="micro mt-1 truncate text-ink-dim">
                        {p.placeSlug ? (
                          <Link href={`/explore/${p.placeSlug}`} className="tap hover:text-accent">
                            {p.placeName ?? p.placeSlug}
                          </Link>
                        ) : (
                          tr('community.anyPlace')
                        )}{' '}
                        · {relativeTime(p.createdAt)}
                      </p>
                    </div>
                  </header>

                  <p className="px-4 pb-3 text-sm leading-relaxed text-ink">
                    {isOpen || p.body.length < 92 ? (
                      p.body
                    ) : (
                      <>
                        {p.body.slice(0, 88)}…{' '}
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

                  {p.plate ? (
                    <PhotoField
                      brief={`${p.placeName ?? p.placeSlug ?? p.author.displayName}. ${p.body.slice(0, 60)}`}
                      showSlots={false}
                      plate={p.plate}
                      className="aspect-square w-full"
                      variant="mid"
                    />
                  ) : null}

                  <div className="flex items-center gap-4 px-4 pt-3">
                    <span className="micro figures text-ink-dim">
                      {p.likeCount} {tr('community.likes')}
                    </span>
                    <span className="micro figures text-ink-dim">
                      {p.comments.length} {tr('community.replies')}
                    </span>
                  </div>

                  <div className="mt-3 flex items-stretch border-t border-ink-ghost">
                    <Action
                      onClick={() => void onLike(p)}
                      active={p.likedByMe}
                      pressed={p.likedByMe}
                      disabled={Boolean(busy[p.id])}
                      label={p.likedByMe ? tr('community.liked') : tr('community.like2')}
                      icon={<Heart filled={p.likedByMe} />}
                    />
                    <Action
                      onClick={() => setOpenComments((s) => ({ ...s, [p.id]: !s[p.id] }))}
                      pressed={commentsOpen}
                      label={tr('community.comment')}
                      icon={<CommentIcon />}
                    />
                    {/* Share is not rendered.
                        It copied a permalink to the clipboard and said nothing
                        about having done so, which on a phone is
                        indistinguishable from a button that does nothing — and
                        a control that appears broken is worse than one that is
                        absent. Removed rather than fixed because a share worth
                        having is a share sheet on mobile and a confirmation on
                        desktop, and that is a piece of work rather than a
                        tweak. onShare and ShareIcon are kept below so bringing
                        it back is adding the feedback, not rebuilding it. */}
                  </div>

                  {commentsOpen ? (
                    <div className="border-t border-ink-ghost px-4 py-4">
                      <ul className="flex flex-col gap-3">
                        {p.comments.map((c) => (
                          <li key={c.id} className="group/comment flex gap-3">
                            <Avatar name={c.author.displayName} />
                            <div className="min-w-0 flex-1">
                              <p className="text-sm text-ink">
                                <span className="font-semibold">{c.author.displayName}</span>{' '}
                                <span className="micro text-ink-dim">{relativeTime(c.createdAt)}</span>
                              </p>
                              <p className="mt-1 text-sm leading-relaxed text-ink">{c.body}</p>
                            </div>
                            {/* Shown only to somebody who can actually use it.
                                The server refuses anyone else regardless - this
                                is about not offering an action that would fail,
                                not about security. */}
                            {me && (me.id === c.author.id || me.isAdmin) ? (
                              <button
                                type="button"
                                onClick={() => void onRemoveComment(p.id, c.id)}
                                disabled={removing === c.id}
                                aria-label={tr('community.deleteComment')}
                                /* Always visible, quietly.
                                   It was opacity-0 until the comment was
                                   hovered, which is a pattern borrowed from
                                   desktop mail clients and wrong here: the
                                   control was invisible until you happened to
                                   pass the mouse over the right row, absent
                                   entirely on a touch screen, and unreachable
                                   for anyone who does not use a mouse.
                                   Playwright refused to click it without
                                   force, which is the same complaint stated
                                   precisely. Low contrast until hovered is
                                   enough deference for a destructive action
                                   that is already confirmed by the server. */
                                className="tap micro h-8 shrink-0 self-start px-2 text-ink-ghost transition-colors duration-200 hover:text-ink focus-visible:text-ink disabled:opacity-40"
                              >
                                {removing === c.id ? '…' : tr('community.delete')}
                              </button>
                            ) : null}
                          </li>
                        ))}
                      </ul>
                      {p.comments.length === 0 ? (
                        <p className="micro text-ink-dim">{tr('community.noComments')}</p>
                      ) : null}
                      <div className="mt-4 flex gap-2">
                        <label htmlFor={`reply-${p.id}`} className="sr-only">
                          {tr('community.comment')}
                        </label>
                        <input
                          id={`reply-${p.id}`}
                          value={reply[p.id] ?? ''}
                          onChange={(e) => setReply((s) => ({ ...s, [p.id]: e.target.value }))}
                          placeholder={me ? tr('community.commentPlaceholder') : tr('community.signInToAct')}
                          disabled={!me || Boolean(busy[`c-${p.id}`])}
                          maxLength={500}
                          className="min-w-0 flex-1 border-0 border-b border-ink-ghost bg-transparent py-2 text-sm text-ink outline-none focus:border-accent disabled:opacity-60"
                        />
                        <button
                          type="button"
                          onClick={() => void onComment(p.id)}
                          disabled={!me || Boolean(busy[`c-${p.id}`]) || !(reply[p.id] ?? '').trim()}
                          className="micro shrink-0 text-accent disabled:opacity-40"
                        >
                          {tr('community.reply')}
                        </button>
                      </div>
                    </div>
                  ) : null}
                </article>
              );
            })}

            <p className="micro py-8 text-center text-ink-dim">{tr('community.end')}</p>
          </div>

          <aside className="hidden lg:block">
            <div className="sticky top-32 flex flex-col gap-5">
              <div className="rounded-xl border border-ink-ghost bg-ground p-5">
                <p className="micro mb-4 text-ink-dim">{tr('community.contributors')}</p>
                <ul className="flex flex-col gap-4">
                  {contributors.map(([id, name]) => (
                    <li key={id} className="flex items-center gap-3">
                      <Avatar name={name} />
                      <p className="truncate text-sm font-medium text-ink">{name}</p>
                    </li>
                  ))}
                </ul>
              </div>

              <div className="rounded-xl border border-ink-ghost bg-ground p-5">
                <p className="micro mb-4 text-ink-dim">{tr('community.trending')}</p>
                <ul className="flex flex-col">
                  {trending.map((p) => (
                    <li key={p.slug} className="border-t border-ink-ghost first:border-t-0">
                      <Link
                        href={`/explore/${p.slug}`}
                        className="flex items-baseline justify-between gap-3 py-3 transition-colors hover:text-accent"
                      >
                        <span className="text-sm text-ink">{p.name}</span>
                        <span className="micro figures shrink-0 text-ink-dim">{p.n}</span>
                      </Link>
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
}: {
  label: string;
  icon: React.ReactNode;
  onClick?: () => void;
  active?: boolean;
  pressed?: boolean;
  disabled?: boolean;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-pressed={pressed}
      disabled={disabled}
      className="micro flex flex-1 items-center justify-center gap-2 py-3 transition-colors hover:bg-[color:var(--ink-ghost)] disabled:cursor-not-allowed disabled:opacity-50"
      style={{ color: active ? 'var(--accent)' : 'var(--ink-dim)' }}
    >
      {icon}
      <span className="hidden sm:inline">{label}</span>
    </button>
  );
}

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
