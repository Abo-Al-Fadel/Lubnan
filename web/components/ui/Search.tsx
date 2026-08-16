'use client';

import { useEffect, useMemo, useRef, useState } from 'react';
import destinations from '@/data/destinations.json';
import secrets from '@/data/secrets.json';

type Result = {
  id: string;
  kind: 'Destination' | 'Story';
  title: string;
  meta: string;
  body: string;
  /** Every result goes somewhere. They were all href="#". */
  href: string;
};

const INDEX: Result[] = [
  ...destinations.map((d) => ({
    id: d.id,
    kind: 'Destination' as const,
    title: d.name,
    meta: `${d.region} · ${d.localName} · ${d.arabic}`,
    body: d.note,
    href: `/explore/${d.id}`,
  })),
  ...secrets.map((s) => ({
    id: `secret-${s.index}`,
    kind: 'Story' as const,
    title: s.title,
    meta: `Secrets · ${s.index}`,
    body: s.body,
    href: '/#secrets',
  })),
];

/**
 * Search over the local mock CMS. Filtering is real — it reads the same JSON
 * files a live API will replace 1:1, so swapping in a backend changes the
 * data source and nothing about this component.
 */
export function SearchOverlay({ open, onClose }: { open: boolean; onClose: () => void }) {
  const [q, setQ] = useState('');
  const inputRef = useRef<HTMLInputElement>(null);
  const panelRef = useRef<HTMLDivElement>(null);

  const results = useMemo(() => {
    const term = q.trim().toLowerCase();
    if (!term) return [];
    return INDEX.filter((r) =>
      `${r.title} ${r.meta} ${r.body}`.toLowerCase().includes(term),
    ).slice(0, 8);
  }, [q]);

  useEffect(() => {
    if (!open) return;
    setQ('');
    const t = window.setTimeout(() => inputRef.current?.focus(), 40);

    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        onClose();
        return;
      }
      /* Keep focus inside the dialog while it is open. */
      if (e.key === 'Tab' && panelRef.current) {
        const nodes = panelRef.current.querySelectorAll<HTMLElement>(
          'input, button, a[href]',
        );
        if (nodes.length === 0) return;
        const first = nodes[0];
        const last = nodes[nodes.length - 1];
        if (e.shiftKey && document.activeElement === first) {
          e.preventDefault();
          last.focus();
        } else if (!e.shiftKey && document.activeElement === last) {
          e.preventDefault();
          first.focus();
        }
      }
    };

    document.addEventListener('keydown', onKey);
    const prevOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    return () => {
      window.clearTimeout(t);
      document.removeEventListener('keydown', onKey);
      document.body.style.overflow = prevOverflow;
    };
  }, [open, onClose]);

  if (!open) return null;

  return (
    <div
      className="fixed inset-0 z-[70] flex items-start justify-center bg-black/65 p-4 pt-[12vh] backdrop-blur-sm"
      onClick={(e) => {
        if (e.target === e.currentTarget) onClose();
      }}
    >
      <div
        ref={panelRef}
        role="dialog"
        aria-modal="true"
        aria-label="Search Lubnān"
        className="w-full max-w-2xl overflow-hidden rounded-lg border border-ink-ghost bg-band"
      >
        <div className="flex items-center gap-3 border-b border-ink-ghost px-5">
          <span className="micro shrink-0 text-ink-dim">Search</span>
          <input
            ref={inputRef}
            value={q}
            onChange={(e) => setQ(e.target.value)}
            type="search"
            placeholder="Byblos, cedars, the alphabet…"
            className="w-full bg-transparent py-5 text-base text-ink outline-none placeholder:text-ink-dim"
          />
          <button
            type="button"
            onClick={onClose}
            className="micro shrink-0 rounded-full border border-ink-ghost px-3 py-1.5 text-ink-dim transition-colors duration-200 ease-out hover:text-ink"
          >
            Esc
          </button>
        </div>

        <div className="max-h-[52vh] overflow-y-auto">
          {q.trim() === '' ? (
            <div className="px-5 py-6">
              <p className="micro mb-4 text-ink-dim">Try</p>
              <div className="flex flex-wrap gap-2">
                {['Baalbek', 'cedars', 'Qadisha', 'alphabet', 'ski'].map((s) => (
                  <button
                    key={s}
                    type="button"
                    onClick={() => setQ(s)}
                    className="micro rounded-full border border-ink-ghost px-3.5 py-2 text-ink transition-colors duration-200 ease-out hover:bg-ink hover:text-ground"
                  >
                    {s}
                  </button>
                ))}
              </div>
            </div>
          ) : results.length === 0 ? (
            <p className="px-5 py-8 text-sm text-ink-dim">
              Nothing matches “{q}”. The index covers destinations and story
              entries so far.
            </p>
          ) : (
            <ul>
              {results.map((r) => (
                <li key={r.id} className="border-b border-ink-ghost last:border-b-0">
                  <a
                    href={r.href}
                    onClick={onClose}
                    className="flex flex-col gap-1.5 px-5 py-4 transition-colors duration-200 ease-out hover:bg-ground"
                  >
                    <span className="flex items-baseline justify-between gap-3">
                      <span className="font-display text-base uppercase tracking-wide text-ink">
                        {r.title}
                      </span>
                      <span className="micro shrink-0 text-ink-dim">{r.kind}</span>
                    </span>
                    <span className="micro text-ink-dim">{r.meta}</span>
                    <span className="line-clamp-2 max-w-[62ch] text-[0.82rem] leading-relaxed text-ink-dim">
                      {r.body}
                    </span>
                  </a>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </div>
  );
}
