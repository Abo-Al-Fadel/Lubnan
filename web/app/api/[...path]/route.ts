/**
 * Cookie-safe reverse proxy onto the API.
 *
 * next.config rewrites drop or merge multiple Set-Cookie headers, which would
 * silently break a session that issues three cookies at once. This handler
 * forwards every one, and is the only path the browser uses for /api/*.
 */

export const dynamic = 'force-dynamic';
export const runtime = 'nodejs';

const API = process.env.API_ORIGIN ?? 'http://localhost:5080';

const HOP = new Set([
  'connection',
  'keep-alive',
  'proxy-authenticate',
  'proxy-authorization',
  'te',
  'trailers',
  'transfer-encoding',
  'upgrade',
  'host',
  'content-length',
]);

/**
 * Headers a client is not allowed to dictate.
 *
 * X-Forwarded-* decides, on the other side, what address the API believes a
 * request came from — which is what rate limiting partitions on and what the
 * audit log records. Forwarding the client's own copy would let anyone spoof
 * their address to escape a limit or to sign a hostile action with somebody
 * else's IP. They are stripped here and set from what this hop actually
 * observed.
 */
const CLIENT_MUST_NOT_SET = new Set([
  'x-forwarded-for',
  'x-forwarded-host',
  'x-forwarded-proto',
  'x-forwarded-port',
  'x-real-ip',
  'forwarded',
]);

/** A hung upstream must not hold a Node connection open indefinitely. */
const UPSTREAM_TIMEOUT_MS = 15_000;

async function proxy(req: Request, path: string[]) {
  const incoming = new URL(req.url);

  /*
   * Refuse anything that could climb out of /api/.
   *
   * `path` arrives decoded, so a segment can literally be `..` — and `fetch`
   * normalises the result, meaning `/api/%2e%2e/%2e%2e/health` would resolve
   * against the API origin's root and reach endpoints this proxy is not meant
   * to expose. An allow-list of URL-safe characters is the cheap, total fix;
   * every real route here is lowercase words, digits, hyphens and GUIDs.
   */
  if (!path.every((seg) => /^[A-Za-z0-9._~-]+$/.test(seg) && seg !== '.' && seg !== '..')) {
    return Response.json(
      {
        type: 'https://lubnan.app/errors/notFound',
        title: 'No such endpoint.',
        status: 404,
        code: 'route.invalid',
      },
      { status: 404 },
    );
  }

  const target = `${API}/api/${path.join('/')}${incoming.search}`;

  const headers = new Headers();
  req.headers.forEach((value, key) => {
    const lower = key.toLowerCase();
    if (HOP.has(lower) || CLIENT_MUST_NOT_SET.has(lower)) return;
    headers.set(key, value);
  });

  // Set from what this hop saw, after stripping whatever the client claimed.
  const clientIp =
    req.headers.get('x-vercel-forwarded-for') ??
    req.headers.get('cf-connecting-ip') ??
    null;
  if (clientIp) headers.set('x-forwarded-for', clientIp);
  headers.set('x-forwarded-proto', incoming.protocol.replace(':', ''));
  headers.set('x-forwarded-host', incoming.host);

  const method = req.method.toUpperCase();
  const body = method === 'GET' || method === 'HEAD' ? undefined : await req.arrayBuffer();

  let upstream: Response;
  try {
    upstream = await fetch(target, {
      method,
      headers,
      body,
      redirect: 'manual',
      signal: AbortSignal.timeout(UPSTREAM_TIMEOUT_MS),
    });
  } catch {
    return Response.json(
      {
        type: 'https://lubnan.app/errors/failure',
        title: 'The API is not reachable.',
        status: 502,
        code: 'network',
      },
      { status: 502 },
    );
  }

  /*
   * Content-Encoding must not survive the hop, and this one is subtle enough
   * to be worth the paragraph.
   *
   * Render compresses with Brotli. Node's fetch transparently *decompresses*
   * what it receives, so `upstream.body` here is plain JSON — but
   * `upstream.headers` still says `content-encoding: br`. Copy that across and
   * the browser is handed uncompressed bytes labelled compressed: it tries to
   * Brotli-decode plain text, fails, and renders nothing.
   *
   * The failure is nastier than a 500 because every visible signal says
   * success — 200, `application/json`, no error anywhere — and only the body
   * is empty. `content-length` is dropped for the same reason: it describes
   * the compressed size, which no longer matches anything.
   *
   * Vercel re-compresses on the way out, so nothing is lost by stripping it.
   */
  const DECODED_BY_FETCH = new Set(['content-encoding', 'content-length']);

  const out = new Headers();
  upstream.headers.forEach((value, key) => {
    const lower = key.toLowerCase();
    if (HOP.has(lower) || DECODED_BY_FETCH.has(lower) || lower === 'set-cookie') return;
    out.append(key, value);
  });

  const cookies =
    typeof upstream.headers.getSetCookie === 'function' ? upstream.headers.getSetCookie() : [];
  for (const cookie of cookies) out.append('set-cookie', cookie);

  return new Response(upstream.body, { status: upstream.status, headers: out });
}

type Ctx = { params: { path: string[] } };

export const GET = (req: Request, ctx: Ctx) => proxy(req, ctx.params.path);
export const POST = (req: Request, ctx: Ctx) => proxy(req, ctx.params.path);
export const PUT = (req: Request, ctx: Ctx) => proxy(req, ctx.params.path);
export const PATCH = (req: Request, ctx: Ctx) => proxy(req, ctx.params.path);
export const DELETE = (req: Request, ctx: Ctx) => proxy(req, ctx.params.path);
export const OPTIONS = (req: Request, ctx: Ctx) => proxy(req, ctx.params.path);
