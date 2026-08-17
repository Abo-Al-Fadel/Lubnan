'use client';

import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { LOCALES, t, type Locale } from '@/data/translations';
import { content } from '@/data/content-translations';

type Theme = 'light' | 'dark';

type Ctx = {
  locale: Locale;
  setLocale: (l: Locale) => void;
  dir: 'ltr' | 'rtl';
  /** Translate a UI key. Falls back to English, then to the key itself. */
  tr: (key: string) => string;
  /** Translate editorial copy. Falls back to the supplied English original. */
  tc: (key: string, fallback?: string) => string;
  theme: Theme;
  toggleTheme: () => void;
};

const SiteContext = createContext<Ctx | null>(null);

export function SiteProvider({ children }: { children: React.ReactNode }) {
  const [locale, setLocaleState] = useState<Locale>('en');
  const [theme, setTheme] = useState<Theme>('light');
  const [hydrated, setHydrated] = useState(false);

  /* Restore choices after mount. A blocking script in <head> already applied
     them to <html>, so this effect must not write light/en over that before
     it has read storage — that was the flash. */
  useEffect(() => {
    const savedLocale = localStorage.getItem('lubnan.locale') as Locale | null;
    const savedTheme = localStorage.getItem('lubnan.theme') as Theme | null;
    if (savedLocale && LOCALES.some((l) => l.code === savedLocale)) setLocaleState(savedLocale);
    if (savedTheme === 'light' || savedTheme === 'dark') setTheme(savedTheme);
    else if (window.matchMedia('(prefers-color-scheme: dark)').matches) setTheme('dark');
    setHydrated(true);
  }, []);

  const dir = LOCALES.find((l) => l.code === locale)?.dir ?? 'ltr';

  useEffect(() => {
    if (!hydrated) return;
    document.documentElement.lang = locale;
    document.documentElement.dir = dir;
    document.documentElement.dataset.theme = theme;
  }, [hydrated, locale, dir, theme]);

  const setLocale = useCallback((l: Locale) => {
    setLocaleState(l);
    localStorage.setItem('lubnan.locale', l);
  }, []);

  const toggleTheme = useCallback(() => {
    setTheme((prev) => {
      const next = prev === 'light' ? 'dark' : 'light';
      localStorage.setItem('lubnan.theme', next);
      return next;
    });
  }, []);

  const tr = useCallback((key: string) => t[locale][key] ?? t.en[key] ?? key, [locale]);

  const tc = useCallback(
    (key: string, fallback?: string) =>
      content[locale][key] ?? fallback ?? content.en[key] ?? key,
    [locale],
  );

  const value = useMemo(
    () => ({ locale, setLocale, dir, tr, tc, theme, toggleTheme }),
    [locale, setLocale, dir, tr, tc, theme, toggleTheme],
  );

  return <SiteContext.Provider value={value}>{children}</SiteContext.Provider>;
}

export function useSite() {
  const ctx = useContext(SiteContext);
  if (!ctx) throw new Error('useSite must be used inside SiteProvider');
  return ctx;
}
