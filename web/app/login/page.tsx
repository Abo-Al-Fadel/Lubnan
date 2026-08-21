'use client';

import { useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { PhotoField } from '@/components/ui/PhotoField';
import { Wordmark } from '@/components/ui/Wordmark';
import { useSite } from '@/lib/site-state';
import { useAuth } from '@/lib/auth';
import { api, ApiError } from '@/lib/api';
import { safeReturnTo } from '@/lib/return-to';

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
  const { refresh } = useAuth();
  const router = useRouter();
  const [mode, setMode] = useState<'in' | 'up'>('in');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [registered, setRegistered] = useState(false);

  return (
    <main id="main" data-palette="raouche" className="min-h-svh bg-ground">
      <div className="grid min-h-svh grid-cols-1 lg:grid-cols-2">
        <div className="order-2 flex flex-col justify-between px-5 py-10 md:px-14 md:py-14 lg:order-1">
          <Link href="/" className="text-ink" aria-label={tr('nav.home')}>
            <Wordmark size="md" />
          </Link>

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

            {registered ? (
              <p className="mt-10 text-sm leading-relaxed text-ink" role="status">
                {tr('login.registered')}
              </p>
            ) : (
              <form
                className="mt-10 flex flex-col gap-7"
                onSubmit={async (e) => {
                  e.preventDefault();
                  if (busy) return;
                  setError(null);
                  setBusy(true);
                  const form = e.currentTarget;
                  const data = new FormData(form);
                  const email = String(data.get('email') ?? '');
                  const password = String(data.get('password') ?? '');
                  const displayName = String(data.get('name') ?? '');
                  try {
                    if (mode === 'up') {
                      await api('/api/v1/auth/register', {
                        method: 'POST',
                        body: JSON.stringify({ email, password, displayName }),
                      });
                      setRegistered(true);
                    } else {
                      await api('/api/v1/auth/login', {
                        method: 'POST',
                        body: JSON.stringify({ email, password }),
                      });
                      await refresh();

                      // Read here rather than with useSearchParams: that hook
                      // forces this page under a Suspense boundary at build
                      // time, and the value is only ever needed at this one
                      // moment. safeReturnTo refuses anything off-origin.
                      router.push(safeReturnTo(window.location.search));
                    }
                  } catch (err) {
                    if (err instanceof ApiError && err.code === 'network') {
                      setError(tr('login.errorNetwork'));
                    } else if (err instanceof ApiError && err.status === 401) {
                      setError(tr('login.errorCredentials'));
                    } else if (err instanceof ApiError && err.status === 400) {
                      setError(err.message || tr('login.errorGeneric'));
                    } else {
                      setError(tr('login.errorGeneric'));
                    }
                  } finally {
                    setBusy(false);
                  }
                }}
              >
                {mode === 'up' ? (
                  <Field
                    id="name"
                    label={tr('login.name')}
                    type="text"
                    autoComplete="name"
                    minLength={2}
                    maxLength={40}
                    disabled={busy}
                  />
                ) : null}
                <Field
                  id="email"
                  label={tr('login.email')}
                  type="email"
                  autoComplete="email"
                  maxLength={254}
                  disabled={busy}
                />
                <Field
                  id="password"
                  label={tr('login.password')}
                  type="password"
                  autoComplete={mode === 'in' ? 'current-password' : 'new-password'}
                  minLength={mode === 'up' ? 12 : 1}
                  maxLength={256}
                  hint={mode === 'up' ? tr('login.passwordHint') : undefined}
                  disabled={busy}
                />

                <button
                  type="submit"
                  disabled={busy}
                  className="btn-solid mt-2 w-full rounded-full px-8 py-4 text-sm font-semibold transition-all duration-300 hover:-translate-y-0.5 disabled:pointer-events-none disabled:opacity-60"
                >
                  {busy
                    ? tr('login.pending')
                    : mode === 'in'
                      ? tr('login.submitIn')
                      : tr('login.submitUp')}
                </button>

                {error ? (
                  <p className="micro text-[color:var(--status-warn)]" role="alert">
                    {error}
                  </p>
                ) : null}
              </form>
            )}

            <p className="mt-9 text-sm text-ink-dim">
              {mode === 'in' ? tr('login.swapToUp') : tr('login.swapToIn')}{' '}
              <button
                type="button"
                disabled={busy}
                onClick={() => {
                  setMode(mode === 'in' ? 'up' : 'in');
                  setError(null);
                  setRegistered(false);
                }}
                className="tap border-b border-ink pb-0.5 text-ink transition-opacity hover:opacity-60"
              >
                {mode === 'in' ? tr('login.titleUp') : tr('login.titleIn')}
              </button>
            </p>
          </div>

          <p className="micro text-ink-dim">{tr('login.foot')}</p>
        </div>

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
    </main>
  );
}

function Field({
  id,
  label,
  type,
  autoComplete,
  minLength,
  maxLength,
  hint,
  disabled,
}: {
  id: string;
  label: string;
  type: string;
  autoComplete: string;
  minLength?: number;
  maxLength?: number;
  hint?: string;
  disabled?: boolean;
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
        minLength={minLength}
        maxLength={maxLength}
        disabled={disabled}
        aria-describedby={hint ? `${id}-hint` : undefined}
        className="w-full border-0 border-b border-ink-ghost bg-transparent px-0 py-2.5 text-base text-ink outline-none transition-colors duration-200 placeholder:text-ink-dim focus:border-accent disabled:opacity-60"
      />
      {hint ? (
        <p id={`${id}-hint`} className="micro text-ink-dim">
          {hint}
        </p>
      ) : null}
    </div>
  );
}
