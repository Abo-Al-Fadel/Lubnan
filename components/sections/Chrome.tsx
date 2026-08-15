'use client';

import { useEffect, useState } from 'react';
import { Wordmark } from '@/components/ui/Wordmark';
import { SearchOverlay } from '@/components/ui/Search';
import { useSite } from '@/lib/site-state';
import { LOCALES } from '@/data/translations';
import {
  Copyright,
  Facebook,
  Instagram,
  Moon,
  SearchIcon,
  Sun,
  X,
  YouTube,
} from '@/components/ui/Icons';

const NAV: [key: string, href: string][] = [
  ['nav.explore', '/explore'],
  ['nav.story', '/story'],
  ['nav.people', '/people'],
  ['nav.achievements', '/achievements'],
  ['nav.legacy', '/legacy'],
  ['nav.plan', '/plan'],
  ['nav.community', '/community'],
];

/** Segmented language control. Buttons, not decoration — they switch the site. */
function LanguageSwitch({ tone = 'hero' }: { tone?: 'hero' | 'page' }) {
  const { locale, setLocale } = useSite();
  const dim = tone === 'hero' ? 'text-hero-ink-dim' : 'text-ink-dim';
  const ring = tone === 'hero' ? 'border-hero-ink-ghost' : 'border-ink-ghost';
  const hover = tone === 'hero' ? 'hover:text-hero-ink' : 'hover:text-ink';

  return (
    <div
      className={`flex items-center rounded-full border ${ring} p-0.5`}
      role="group"
      aria-label="Language"
    >
      {LOCALES.map((l) => {
        const active = l.code === locale;
        return (
          <button
            key={l.code}
            type="button"
            onClick={() => setLocale(l.code)}
            aria-pressed={active}
            title={l.native}
            className={`micro rounded-full px-2.5 py-1.5 transition-colors duration-200 ease-out ${
              active ? 'bg-accent text-[color:var(--accent-ink)]' : `${dim} ${hover}`
            }`}
          >
            {l.label}
          </button>
        );
      })}
    </div>
  );
}

function ThemeToggle({ tone = 'hero' }: { tone?: 'hero' | 'page' }) {
  const { theme, toggleTheme, tr } = useSite();
  const ring = tone === 'hero' ? 'border-hero-ink-ghost' : 'border-ink-ghost';
  const ink = tone === 'hero' ? 'text-hero-ink' : 'text-ink';

  return (
    <button
      type="button"
      onClick={toggleTheme}
      title={tr('nav.theme')}
      aria-label={tr('nav.theme')}
      className={`flex h-9 w-9 items-center justify-center rounded-full border ${ring} ${ink} transition-colors duration-200 ease-out hover:border-accent hover:bg-accent hover:text-[color:var(--accent-ink)]`}
    >
      {theme === 'light' ? <Moon /> : <Sun />}
    </button>
  );
}

export function Navbar() {
  const [open, setOpen] = useState(false);
  const [searchOpen, setSearchOpen] = useState(false);
  const [stuck, setStuck] = useState(false);
  const { tr } = useSite();

  /* The bar travels with the page instead of scrolling away with the hero.
     Past the plate it can no longer borrow hero ink — that ink is chosen to sit
     on a photograph, and on the snow ground it would be white on white — so the
     whole bar swaps to page tokens and picks up a surface behind it. */
  useEffect(() => {
    /* `#nav-sentinel` sits at the foot of a page's photographic hero and marks
       where hero ink stops being safe. Every page that opens on a plate
       renders one; pages that open on the page ground — Story, Legacy, People,
       Achievements, Community, Profile — deliberately do not.

       So no sentinel means no plate to sit on, and the bar has to start in
       page ink immediately. It previously fell back to "72% of the viewport",
       which left those pages showing eggshell type on an off-white ground: the
       wordmark and the whole nav were invisible until you scrolled. */
    const sentinel = document.getElementById('nav-sentinel');
    if (!sentinel) {
      setStuck(true);
      return;
    }

    const io = new IntersectionObserver(
      ([entry]) => setStuck(!entry.isIntersecting && entry.boundingClientRect.top < 0),
      { threshold: 0 },
    );
    io.observe(sentinel);
    return () => io.disconnect();
  }, []);

  const tone = stuck ? 'page' : 'hero';
  const ink = stuck ? 'text-ink' : 'text-hero-ink';
  const dim = stuck ? 'text-ink-dim' : 'text-hero-ink-dim';
  const ring = stuck ? 'border-ink-ghost' : 'border-hero-ink-ghost';
  const hoverInk = stuck ? 'hover:text-ink' : 'hover:text-hero-ink';

  /* The stuck background is an inline style, not a Tailwind class.
     `bg-[color:var(--band)]/92` silently compiles to nothing — Tailwind cannot
     inject an alpha channel into a var(), so the opacity modifier is dropped
     and the bar stayed fully transparent over the page content. color-mix does
     the same job and actually resolves. */
  return (
    <header
      className="fixed inset-x-0 top-0 z-40 transition-[background-color,box-shadow] duration-500 ease-out"
      /* Unstuck, the bar sits directly on a photograph and borrows hero ink,
         so it needs its own gradient — the hero's scrim is painted inside the
         hero, and this header is a fixed sibling above it. Without this the
         wordmark measured 2.4–2.9:1 wherever a plate happened to be bright at
         the top, which is most of them at golden hour. */
      style={{
        background: stuck
          ? 'color-mix(in srgb, var(--band) 92%, transparent)'
          : 'linear-gradient(to bottom, color-mix(in srgb, var(--scrim) 92%, transparent) 0%, color-mix(in srgb, var(--scrim) 55%, transparent) 55%, transparent 100%)',
        backdropFilter: stuck ? 'blur(18px)' : 'none',
        boxShadow: stuck ? '0 1px 0 0 var(--ink-ghost)' : 'none',
      }}
    >
      <SearchOverlay open={searchOpen} onClose={() => setSearchOpen(false)} />

      <div className="flex items-center justify-between gap-4 px-5 py-5 md:px-10 md:py-7">
        <a href="/" className={`vt-wordmark ${ink}`} aria-label={tr('nav.home')}>
          <Wordmark size="md" />
        </a>

        {/* Seven items now, so the inline nav needs more room than xl and the
            gap tightens. Below that the drawer carries them. */}
        <nav aria-label="Primary" className="hidden 2xl:block">
          <ul className="flex gap-6">
            {NAV.map(([key, href]) => (
              <li key={key}>
                <a
                  href={href}
                  className={`micro ${dim} transition-colors duration-300 ease-out ${hoverInk}`}
                >
                  {tr(key)}
                </a>
              </li>
            ))}
          </ul>
        </nav>

        <div className="hidden items-center gap-3 xl:flex">
          <button
            type="button"
            onClick={() => setSearchOpen(true)}
            className={`flex w-44 items-center gap-2.5 rounded-full border ${ring} px-4 py-2.5 text-left ${dim} transition-colors duration-300 ease-out ${hoverInk}`}
          >
            <SearchIcon />
            <span className="micro truncate">{tr('nav.search')}</span>
          </button>
          <LanguageSwitch tone={tone} />
          <ThemeToggle tone={tone} />
          {/* Filled with the accent on hover, not with ink. An ink fill made the
              label inherit hero ink for both background and text, so "Log in"
              vanished into its own button. */}
          <a
            href="/login"
            className={`micro rounded-full border ${ring} px-4 py-2.5 ${ink} transition-colors duration-300 ease-out hover:border-accent hover:bg-accent hover:text-[color:var(--accent-ink)]`}
          >
            {tr('nav.login')}
          </a>
        </div>

        <div className="flex items-center gap-2 xl:hidden">
          <button
            type="button"
            onClick={() => setSearchOpen(true)}
            aria-label={tr('nav.searchShort')}
            className={`flex h-9 w-9 items-center justify-center rounded-full border ${ring} ${ink}`}
          >
            <SearchIcon />
          </button>
          <button
            type="button"
            onClick={() => setOpen((v) => !v)}
            aria-expanded={open}
            aria-controls="mobile-drawer"
            className={`micro rounded-full border ${ring} px-4 py-2.5 ${ink}`}
          >
            {open ? tr('nav.close') : tr('nav.menu')}
          </button>
        </div>
      </div>

      {open ? (
        <div
          id="mobile-drawer"
          className={`border-y ${ring} px-5 py-6 backdrop-blur-xl xl:hidden`}
          style={{
            background: stuck
              ? 'color-mix(in srgb, var(--band) 95%, transparent)'
              : 'rgba(0,0,0,0.85)',
          }}
        >
          <ul className="space-y-4">
            {NAV.map(([key, href]) => (
              <li key={key}>
                <a href={href} className={`font-display text-xl uppercase tracking-wide ${ink}`}>
                  {tr(key)}
                </a>
              </li>
            ))}
          </ul>
          <div className={`mt-7 flex flex-wrap items-center justify-between gap-3 border-t ${ring} pt-5`}>
            <LanguageSwitch tone={tone} />
            <div className="flex items-center gap-2">
              <ThemeToggle tone={tone} />
              <a
                href="/login"
                className={`micro rounded-full border ${ring} px-4 py-2.5 ${ink} transition-colors duration-300 ease-out hover:border-accent hover:bg-accent hover:text-[color:var(--accent-ink)]`}
              >
                {tr('nav.login')}
              </a>
            </div>
          </div>
        </div>
      ) : null}
    </header>
  );
}

export function SiteFooter() {
  const { tr } = useSite();

  /* Each column head is a real route and each link deep-links into a section
     of it, so the footer is navigation rather than a list of words. */
  const cols: { head: string; href: string; links: [string, string][] }[] = [
    {
      head: 'footer.explore',
      href: '/explore',
      links: [
        ['footer.coast', '/explore?region=Coast'],
        ['footer.mountains', '/explore?region=Mount+Lebanon'],
        ['footer.ruins', '/explore?category=ruins'],
        ['footer.food', '/explore?category=city'],
        ['footer.wine', '/explore?region=Bekaa'],
      ],
    },
    {
      head: 'footer.story',
      href: '/story',
      links: [
        ['footer.timeline', '/story#spine'],
        ['footer.secrets', '/#secrets'],
        ['footer.culture', '/legacy'],
        ['footer.language', '/story#language'],
      ],
    },
    {
      head: 'footer.plan',
      href: '/plan',
      links: [
        ['footer.flights', '/plan#flights'],
        ['footer.airport', '/plan#transfer'],
        ['footer.visa', '/plan#visa'],
        ['footer.before', '/plan#before'],
      ],
    },
  ];

  const social = [
    { name: 'Instagram', Icon: Instagram },
    { name: 'YouTube', Icon: YouTube },
    { name: 'Facebook', Icon: Facebook },
    { name: 'X', Icon: X },
  ];

  return (
    <footer className="border-t border-ink-ghost bg-band px-5 py-14 md:px-10">
      <div className="grid gap-10 md:grid-cols-12">
        <div className="md:col-span-4">
          <Wordmark size="lg" className="text-ink" />
          <p className="mt-5 max-w-[42ch] text-sm leading-relaxed text-ink-dim">
            {tr('footer.about')}
          </p>
          <div className="mt-6 lg:hidden">
            <LanguageSwitch tone="page" />
          </div>
        </div>

        {cols.map((c) => (
          <div key={c.head} className="md:col-span-2">
            <a href={c.href} className="micro text-ink-dim transition-colors hover:text-accent">
              {tr(c.head)}
            </a>
            <ul className="mt-4 space-y-2.5">
              {c.links.map(([key, href]) => (
                <li key={key}>
                  <a
                    href={href}
                    className="text-sm text-ink transition-colors duration-200 ease-out hover:text-accent"
                  >
                    {tr(key)}
                  </a>
                </li>
              ))}
            </ul>
          </div>
        ))}

        <div className="md:col-span-2">
          <p className="micro text-ink-dim">{tr('footer.elsewhere')}</p>
          <ul className="mt-4 space-y-2.5">
            {social.map(({ name, Icon }) => (
              <li key={name}>
                <a
                  href="#"
                  className="group flex items-center gap-2.5 text-sm text-ink transition-colors duration-200 ease-out hover:text-accent"
                >
                  <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full border border-ink-ghost transition-colors duration-200 ease-out group-hover:border-accent">
                    <Icon />
                  </span>
                  {name}
                </a>
              </li>
            ))}
          </ul>
        </div>
      </div>

      <div className="mt-14 flex flex-col gap-3 border-t border-ink-ghost pt-6 md:flex-row md:items-center md:justify-between">
        <p className="micro flex items-center gap-2 text-ink-dim">
          <Copyright className="shrink-0" />
          <span className="figures">2026 Lubnān</span>
          <span aria-hidden="true" className="opacity-40">
            ·
          </span>
          {tr('footer.rights')}
        </p>
        <p className="micro text-ink-dim">{tr('footer.note')}</p>
      </div>
    </footer>
  );
}
