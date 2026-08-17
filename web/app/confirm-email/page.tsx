'use client';

import { Suspense, useEffect, useState } from 'react';
import { useSearchParams } from 'next/navigation';
import { Navbar, SiteFooter } from '@/components/sections/Chrome';
import { useSite } from '@/lib/site-state';
import { api, ApiError } from '@/lib/api';

export default function ConfirmEmailPage() {
  return (
    <Suspense fallback={<ConfirmShell state="working" />}>
      <ConfirmEmailInner />
    </Suspense>
  );
}

function ConfirmEmailInner() {
  const search = useSearchParams();
  const [state, setState] = useState<'working' | 'ok' | 'bad'>('working');

  useEffect(() => {
    const token = search.get('token');
    if (!token) {
      setState('bad');
      return;
    }

    let cancelled = false;
    api('/api/v1/auth/confirm-email', {
      method: 'POST',
      body: JSON.stringify({ token }),
    })
      .then(() => {
        if (!cancelled) setState('ok');
      })
      .catch((err: unknown) => {
        if (!cancelled) setState(err instanceof ApiError ? 'bad' : 'bad');
      });

    return () => {
      cancelled = true;
    };
  }, [search]);

  return <ConfirmShell state={state} />;
}

function ConfirmShell({ state }: { state: 'working' | 'ok' | 'bad' }) {
  const { tr } = useSite();
  return (
    <div data-palette="raouche" className="bg-ground">
      <Navbar />
      <main id="main" className="mx-auto max-w-lg px-5 py-32 md:px-10">
        <p className="micro text-ink-dim">{tr('login.eyebrowIn')}</p>
        <h1 className="mt-4 font-display text-[clamp(2rem,5vw,3rem)] font-bold uppercase leading-[0.94] tracking-[-0.02em] text-ink">
          {tr('confirm.title')}
        </h1>
        <p className="mt-6 text-sm leading-relaxed text-ink-dim" role="status">
          {state === 'working' ? tr('confirm.working') : state === 'ok' ? tr('confirm.ok') : tr('confirm.bad')}
        </p>
        {state !== 'working' ? (
          <a
            href="/login"
            className="btn-solid mt-10 inline-flex rounded-full px-8 py-4 text-sm font-semibold"
          >
            {tr('confirm.signIn')}
          </a>
        ) : null}
      </main>
      <SiteFooter />
    </div>
  );
}
