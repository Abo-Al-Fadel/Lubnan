export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly code: string,
    message: string,
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

/** Readable CSRF cookie. The session cookies are httpOnly and never appear here. */
export function csrfToken(): string | null {
  if (typeof document === 'undefined') return null;
  for (const part of document.cookie.split(';')) {
    const [name, ...rest] = part.trim().split('=');
    if (name === 'lubnan_csrf') return decodeURIComponent(rest.join('='));
  }
  return null;
}

const REFRESH_PATH = '/api/v1/auth/refresh';

/**
 * Endpoints that must never trigger a silent refresh.
 *
 * Refreshing after a failed sign-in would answer the wrong question - the 401
 * there means "wrong password", not "expired token" - and refreshing after a
 * refresh is how you write an infinite loop.
 */
const NEVER_REFRESH = new Set([
  '/api/v1/auth/login',
  '/api/v1/auth/register',
  '/api/v1/auth/logout',
  REFRESH_PATH,
]);

async function send(path: string, init: RequestInit, method: string): Promise<Response> {
  const headers = new Headers(init.headers);

  if (method !== 'GET' && method !== 'HEAD' && method !== 'OPTIONS') {
    // Read inside `send`, not once per call. A refresh rotates the CSRF token
    // along with the session, so a retry that reused the token captured before
    // the refresh would be rejected by the double-submit check.
    const csrf = csrfToken();
    if (csrf) headers.set('X-CSRF-Token', csrf);
  }

  // Not for FormData, and this one is easy to get wrong.
  //
  // A multipart body needs a Content-Type carrying the boundary the browser
  // generated - `multipart/form-data; boundary=----WebKitFormBoundary...` - and
  // only the browser knows that string. Setting the header here at all means it
  // is sent without a boundary, and the server cannot parse a body it has been
  // told is multipart but given no delimiter for. Leaving it unset is what lets
  // fetch fill it in correctly.
  if (init.body && !(init.body instanceof FormData) && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }
  if (!headers.has('Accept')) headers.set('Accept', 'application/json');

  try {
    return await fetch(path, { ...init, method, headers, credentials: 'same-origin' });
  } catch {
    throw new ApiError(0, 'network', 'network');
  }
}

/**
 * One refresh at a time, shared by every caller that is waiting on it.
 *
 * A page can easily have four requests in flight when an access token expires -
 * the feed, the account, a like, a saved list - and each would otherwise start
 * its own refresh. Refresh tokens rotate and reuse is treated as theft, so four
 * concurrent refreshes with the same token would look exactly like a stolen one
 * and the server would kill the whole session. The single-flight promise is not
 * an optimisation here; it is what stops the client attacking itself.
 */
let inFlight: Promise<boolean> | null = null;

function refreshSession(): Promise<boolean> {
  inFlight ??= (async () => {
    try {
      const res = await send(REFRESH_PATH, { method: 'POST' }, 'POST');
      return res.ok;
    } catch {
      return false;
    } finally {
      // Callers already hold this promise, so clearing the slot only affects
      // the next 401 - which should get a fresh attempt, not this answer.
      inFlight = null;
    }
  })();

  return inFlight;
}

async function read<T>(res: Response): Promise<T> {
  if (res.status === 204 || res.status === 205) {
    return undefined as T;
  }

  const text = await res.text();
  let data: { code?: string; title?: string; detail?: string } | null = null;
  if (text) {
    try {
      data = JSON.parse(text) as { code?: string; title?: string; detail?: string };
    } catch {
      data = null;
    }
  }

  if (!res.ok) {
    throw new ApiError(
      res.status,
      data?.code ?? 'error',
      data?.title ?? data?.detail ?? res.statusText,
    );
  }

  return data as T;
}

/**
 * Call the API, renewing the session once if the access token has expired.
 *
 * The access token lives fifteen minutes; the refresh token lives thirty days.
 * Nothing on the client used to spend the second one, so every session ended
 * fifteen minutes after sign-in - silently, and looking exactly like being
 * signed out. The whole rotating-refresh design existed on the server and was
 * never reached from the browser.
 *
 * One retry, never two: if a fresh access token still gets 401, the answer is
 * that this person genuinely may not do this, and repeating the request will
 * not change that.
 */
export async function api<T = void>(path: string, init: RequestInit = {}): Promise<T> {
  const method = (init.method ?? 'GET').toUpperCase();

  let res = await send(path, init, method);

  if (res.status === 401 && !NEVER_REFRESH.has(path.split('?')[0]) && (await refreshSession())) {
    // Safe to replay: every body this app sends is a string or a FormData, both
    // of which fetch re-serialises per call. A ReadableStream body would already
    // be consumed here, and is deliberately not used anywhere.
    res = await send(path, init, method);
  }

  return read<T>(res);
}
