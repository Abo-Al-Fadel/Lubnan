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

async function proxy(req: Request, path: string[]) {
  const incoming = new URL(req.url);
  const target = `${API}/api/${path.join('/')}${incoming.search}`;

  const headers = new Headers();
  req.headers.forEach((value, key) => {
    if (!HOP.has(key.toLowerCase())) headers.set(key, value);
  });

  const method = req.method.toUpperCase();
  const body = method === 'GET' || method === 'HEAD' ? undefined : await req.arrayBuffer();

  let upstream: Response;
  try {
    upstream = await fetch(target, {
      method,
      headers,
      body,
      redirect: 'manual',
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

  const out = new Headers();
  upstream.headers.forEach((value, key) => {
    const lower = key.toLowerCase();
    if (HOP.has(lower) || lower === 'set-cookie') return;
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
