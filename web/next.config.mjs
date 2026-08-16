/**
 * The API is proxied, not called directly.
 *
 * Everything under /api/ is rewritten server-side to whichever host the backend
 * is deployed on, so the browser only ever talks to this origin. That is worth
 * a paragraph, because it decides the entire authentication design:
 *
 *   - Same origin means no CORS. No preflight on every mutation, no allow-list
 *     to keep in step with a deployment.
 *   - Same origin means the session cookies are first-party. SameSite=Lax works,
 *     and none of it depends on third-party cookies, which browsers are steadily
 *     turning off.
 *   - The API host never appears in a URL a reader can see, so it can sit behind
 *     the platform's private networking rather than on the public internet.
 *
 * The alternative — the browser calling api.example.com directly — needs CORS
 * with credentials, and either a shared registrable domain or SameSite=None
 * cookies that are on a deprecation path. This costs one line of config.
 *
 * Set API_ORIGIN in the deployment. Locally it defaults to the port
 * `dotnet run` uses.
 *
 * @type {import('next').NextConfig}
 */
const nextConfig = {
  reactStrictMode: true,

  async rewrites() {
    const api = process.env.API_ORIGIN ?? 'http://localhost:5080';

    return [
      { source: '/api/:path*', destination: `${api}/api/:path*` },

      // Health probes are proxied too, so an uptime check can watch the API
      // through the same front door a reader uses. A check that talks straight
      // to the origin is a check that stays green while the path everyone
      // actually takes is broken.
      { source: '/health/:path*', destination: `${api}/health/:path*` },
    ];
  },

  async headers() {
    return [
      {
        source: '/:path*',
        headers: [
          { key: 'X-Content-Type-Options', value: 'nosniff' },
          { key: 'Referrer-Policy', value: 'strict-origin-when-cross-origin' },
          { key: 'X-Frame-Options', value: 'DENY' },
          {
            key: 'Permissions-Policy',
            value: 'geolocation=(), camera=(), microphone=(), payment=()',
          },
        ],
      },
    ];
  },
};

export default nextConfig;
