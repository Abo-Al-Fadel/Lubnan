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

export async function api<T = void>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers);
  const method = (init.method ?? 'GET').toUpperCase();

  if (method !== 'GET' && method !== 'HEAD' && method !== 'OPTIONS') {
    const csrf = csrfToken();
    if (csrf) headers.set('X-CSRF-Token', csrf);
  }

  if (init.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }
  if (!headers.has('Accept')) headers.set('Accept', 'application/json');

  let res: Response;
  try {
    res = await fetch(path, { ...init, method, headers, credentials: 'same-origin' });
  } catch {
    throw new ApiError(0, 'network', 'network');
  }

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
