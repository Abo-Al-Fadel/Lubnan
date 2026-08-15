'use client';

import { useState } from 'react';
import { PhotoField } from '@/components/ui/PhotoField';
import { Wordmark } from '@/components/ui/Wordmark';
import { useSite } from '@/lib/site-state';

/**
 * ridge-quiet-half-composition: the form takes one half against a flat ground,
 * a single full-bleed photograph holds the other. No card floating on a
 * gradient, no centred box.
 *
 * Runs on Raouche — kept in variations.ts rather than deleted precisely for
 * this. The account area being a different colour world makes it read as a
 * distinct room inside the site rather than another page of the same one.
 */
export default function LoginPage() {
  const { tr } = useSite();
  const [mode, setMode] = useState<'in' | 'up'>('in');
  const [sent, setSent] = useState(false);

  return (
    <div data-palette="raouche" className="min-h-svh bg-ground">
      <div className="grid min-h-svh grid-cols-1 lg:grid-cols-2">
        {/* Quiet half */}
        <div className="order-2 flex flex-col justify-between px-5 py-10 md:px-14 md:py-14 lg:order-1">
          <a href="/" className="text-ink" aria-label={tr('nav.home')}>
            <Wordmark size="md" />
          </a>

          <div className="mx-auto w-full max-w-[26rem] py-14">
            <p className="micro text-ink-dim">
              {mode === 'in' ? tr('login.eyebrowIn') : tr('login.eyebrowUp')}
            </p>
            <h1 className="mt-4 font-display text-[clamp(2rem,5vw,3rem)] font-bold uppercase leading-[0.94] tracking-[-0.02em] text-ink">
              {mode === 'in' ? tr('login.titleIn') : tr('login.titleUp')}
            </h1>
            <p className="mt-5 max-w-[38ch] text-sm leading-relaxed text-ink-dim">
              {tr('login.note')}
            </p>

            <form
              className="mt-10 flex flex-col gap-7"
              onSubmit={(e) => {
                e.preventDefault();
                setSent(true);
              }}
            >
              {mode === 'up' ? (
                <Field id="name" label={tr('login.name')} type="text" autoComplete="name" />
              ) : null}
              <Field id="email" label={tr('login.email')} type="email" autoComplete="email" />
              <Field
                id="password"
                label={tr('login.password')}
                type="password"
                autoComplete={mode === 'in' ? 'current-password' : 'new-password'}
              />

              <button
                type="submit"
                className="btn-solid mt-2 w-full rounded-full px-8 py-4 text-sm font-semibold transition-all duration-300 hover:-translate-y-0.5"
              >
                {mode === 'in' ? tr('login.submitIn') : tr('login.submitUp')}
              </button>

              {sent ? (
                <p className="micro text-accent" role="status">
                  {tr('login.demo')}
                </p>
              ) : null}
            </form>

            <p className="mt-9 text-sm text-ink-dim">
              {mode === 'in' ? tr('login.swapToUp') : tr('login.swapToIn')}{' '}
              <button
                type="button"
                onClick={() => {
                  setMode(mode === 'in' ? 'up' : 'in');
                  setSent(false);
                }}
                className="border-b border-ink pb-0.5 text-ink transition-opacity hover:opacity-60"
              >
                {mode === 'in' ? tr('login.titleUp') : tr('login.titleIn')}
              </button>
            </p>
          </div>

          <p className="micro text-ink-dim">{tr('login.foot')}</p>
        </div>

        {/* Photographic half */}
        <div className="relative order-1 min-h-[38svh] lg:order-2 lg:min-h-svh">
          <PhotoField
            brief="Raouche sea stacks off Beirut at sunset, warm gold and deep blue water, dramatic saturated colour"
            showSlots={false}
            plate="R1"
            priority
            className="absolute inset-0"
            variant="low"
          />
          <div className="scrim absolute inset-0" aria-hidden="true" />
          <p className="absolute bottom-6 left-6 right-6 max-w-[34ch] text-sm leading-relaxed text-hero-ink md:bottom-10 md:left-10">
            {tr('login.caption')}
          </p>
        </div>
      </div>
    </div>
  );
}

/** Hairline-underlined input. No boxes — it matches the rules used site-wide. */
function Field({
  id,
  label,
  type,
  autoComplete,
}: {
  id: string;
  label: string;
  type: string;
  autoComplete: string;
}) {
  return (
    <div className="flex flex-col gap-2">
      <label htmlFor={id} className="micro text-ink-dim">
        {label}
      </label>
      <input
        id={id}
        name={id}
        type={type}
        autoComplete={autoComplete}
        required
        className="w-full border-0 border-b border-ink-ghost bg-transparent px-0 py-2.5 text-base text-ink outline-none transition-colors duration-200 placeholder:text-ink-dim focus:border-accent"
      />
    </div>
  );
}
