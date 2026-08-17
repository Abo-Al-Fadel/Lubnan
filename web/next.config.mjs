/**
 * The API is proxied, not called directly.
 *
 * /api is handled by app/api/[...path]/route.ts so multiple Set-Cookie headers
 * on a session survive the hop. Health probes stay as a rewrite.
 *
 * Same origin means no CORS, first-party cookies, and SameSite=Lax.
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
      // Health probes stay as a rewrite. /api is handled by app/api/[...path]
      // so multiple Set-Cookie headers on a session survive the hop.
      { source: '/health/:path*', destination: `${api}/health/:path*` },
    ];
  },

  async headers() {
    const dev = process.env.NODE_ENV !== 'production';
    return [
      {
        source: '/:path*',
        headers: [
          { key: 'X-Content-Type-Options', value: 'nosniff' },
          { key: 'Referrer-Policy', value: 'strict-origin-when-cross-origin' },
          { key: 'X-Frame-Options', value: 'DENY' },
          {
            key: 'Permissions-Policy',
            value: 'geolocation=(), camera=(), microphone=(), payment=(), usb=()',
          },
          {
            key: 'Content-Security-Policy',
            value: [
              "default-src 'self'",
              `script-src 'self' 'unsafe-inline'${dev ? " 'unsafe-eval'" : ''}`,
              "style-src 'self' 'unsafe-inline'",
              "img-src 'self' data: blob:",
              "media-src 'self' blob:",
              "font-src 'self'",
              "connect-src 'self'",
              "frame-ancestors 'none'",
              "base-uri 'self'",
              "form-action 'self'",
              "object-src 'none'",
            ].join('; '),
          },
        ],
      },
      {
        source: '/img/:path*',
        headers: [{ key: 'Cache-Control', value: 'public, max-age=31536000, immutable' }],
      },
      {
        source: '/brand/:path*',
        headers: [{ key: 'Cache-Control', value: 'public, max-age=31536000, immutable' }],
      },
      {
        source: '/vid/:path*',
        headers: [{ key: 'Cache-Control', value: 'public, max-age=31536000, immutable' }],
      },
    ];
  },
};

export default nextConfig;
