/**
 * Where to send somebody after they sign in.
 *
 * A guest who presses "like" is told to sign in; landing them on their profile
 * afterwards abandons the thing they were actually doing. The page they came
 * from travels in `?next=`.
 */

const DEFAULT = '/profile';

/** The sign-in URL that returns here afterwards. */
export function loginHref(returnTo: string): string {
  return `/login?next=${encodeURIComponent(returnTo)}`;
}

/**
 * Read `?next=` and refuse anything that is not a path on this site.
 *
 * `next` arrives from the URL bar, so it is attacker-controlled: a link to
 * `/login?next=https://lubnan-login.example` would show our real sign-in form
 * and then hand the visitor to a page of somebody else's choosing, at the exact
 * moment they have just proven they trust this site. That is an open redirect,
 * and it is the classic ingredient in a credential-phishing chain.
 *
 * A leading `/` is not enough on its own: `//evil.example` is a
 * protocol-relative URL, which the browser resolves to another host, and
 * `/\evil.example` is treated the same way by several of them. Both are
 * rejected, so what survives is a path on this origin and nothing else.
 */
export function safeReturnTo(search: string): string {
  const next = new URLSearchParams(search).get('next');

  if (!next || !next.startsWith('/') || next.startsWith('//') || next.startsWith('/\\')) {
    return DEFAULT;
  }

  return next;
}
