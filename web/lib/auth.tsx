'use client';

import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { api, ApiError } from '@/lib/api';

export type Me = {
  id: string;
  email: string;
  displayName: string;
  emailConfirmed: boolean;
  isAdmin: boolean;
  state: string;
  createdAt: string;
  pendingDeletionUntil: string | null;
  activeSessions: number;
  avatarVersion: string | null;
};

/**
 * Why "unavailable" is not the same as signed out.
 *
 * Every failure used to collapse to `me: null`, so a 500 from /me rendered the
 * signed-out page to somebody holding a valid session. That is exactly what a
 * missing migration produced: the account endpoint threw, the profile showed
 * the guest wall, and the bug read as "login is broken" when login had worked
 * perfectly. A server that is unreachable or broken has told us nothing about
 * who is signed in, and the interface should say so rather than guess.
 */
export type AuthProblem = 'none' | 'unavailable';

type AuthCtx = {
  me: Me | null;
  ready: boolean;
  problem: AuthProblem;
  refresh: () => Promise<void>;
  logout: () => Promise<void>;
};

const AuthContext = createContext<AuthCtx | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [me, setMe] = useState<Me | null>(null);
  const [ready, setReady] = useState(false);
  const [problem, setProblem] = useState<AuthProblem>('none');

  const refresh = useCallback(async () => {
    /*
     * Skip the probe when there is visibly no session.
     *
     * This ran unconditionally on every page, so every anonymous visitor - who
     * is nearly every visitor - paid a round trip through the proxy to the API
     * to be told 401, and logged a console error doing it. The browser logs
     * that itself; no amount of catching here silences it, so the only fix is
     * not to make the request.
     *
     * The CSRF cookie is the marker: it is issued with the session, cleared
     * with it, and is the one cookie deliberately readable by script. Its
     * absence is not proof of being signed out - the cookie could be dropped
     * while the httpOnly pair survives - but in that case the next action
     * fails, refreshes, and recovers. Its *presence* is what matters, and a
     * forged one costs an attacker nothing but a 401 of their own.
     */
    if (!document.cookie.split('; ').some((c) => c.startsWith('lubnan_csrf='))) {
      setMe(null);
      setProblem('none');
      setReady(true);
      return;
    }

    try {
      setMe(await api<Me>('/api/v1/me'));
      setProblem('none');
    } catch (err) {
      setMe(null);

      // 401 is an answer: the session is over, and the signed-out page is
      // correct. Anything else - no network, a 5xx, a proxy timeout - is the
      // absence of an answer, and must not be dressed up as one.
      const answered = err instanceof ApiError && err.status === 401;
      setProblem(answered ? 'none' : 'unavailable');
    } finally {
      setReady(true);
    }
  }, []);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  const logout = useCallback(async () => {
    try {
      await api('/api/v1/auth/logout', { method: 'POST' });
    } catch {
      /* Clearing local state is the outcome the person asked for. */
    }
    setMe(null);
    setProblem('none');
  }, []);

  const value = useMemo(
    () => ({ me, ready, problem, refresh, logout }),
    [me, ready, problem, refresh, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

/**
 * Upload a profile picture.
 *
 * multipart/form-data, and the Content-Type header is deliberately not set:
 * the browser has to write it itself so that it can append the boundary it
 * generated. Setting it by hand produces a header with no boundary and a body
 * the server cannot parse.
 *
 * Returns the new version string, which goes in the image URL so the browser
 * fetches the new picture instead of the cached old one.
 */
export async function setAvatar(file: File): Promise<string> {
  const form = new FormData();
  form.append('file', file);

  const result = await api<{ version: string }>('/api/v1/me/avatar', {
    method: 'POST',
    body: form,
  });

  return result.version;
}

export function removeAvatar() {
  return api<void>('/api/v1/me/avatar', { method: 'DELETE' });
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider');
  return ctx;
}
