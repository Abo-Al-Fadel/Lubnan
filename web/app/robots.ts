import type { MetadataRoute } from 'next';

/**
 * Generated, not a static file in /public.
 *
 * The sitemap URL has to be absolute, and the origin differs between a preview
 * deployment and production. A hand-written robots.txt would point every
 * preview at the production sitemap, which is how a staging build ends up
 * asking Google to index it.
 */
export default function robots(): MetadataRoute.Robots {
  const origin = process.env.WEB_ORIGIN ?? 'http://localhost:3000';

  // Preview deployments must never be indexed. Two builds of the same commit
  // served on two hostnames are duplicate content, and the preview usually
  // wins because it is newer.
  const isProduction = process.env.VERCEL_ENV
    ? process.env.VERCEL_ENV === 'production'
    : process.env.NODE_ENV === 'production';

  if (!isProduction) {
    return { rules: [{ userAgent: '*', disallow: '/' }] };
  }

  return {
    rules: [
      {
        userAgent: '*',
        allow: '/',

        // Nothing here is useful to a crawler and some of it is per-reader.
        // /api is the proxy, /profile needs a session, and a confirmation
        // link fetched by a crawler would spend a single-use token.
        disallow: ['/api/', '/profile', '/confirm-email', '/reset-password'],
      },
    ],
    sitemap: `${origin}/sitemap.xml`,
  };
}
