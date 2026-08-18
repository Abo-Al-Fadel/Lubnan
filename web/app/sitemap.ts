import type { MetadataRoute } from 'next';
import destinations from '@/data/destinations.json';

/**
 * Every page a search engine should know about.
 *
 * Built from the same `destinations.json` the pages render from, so a ninth
 * destination appears here by existing rather than by somebody remembering to
 * add a line. A hand-maintained sitemap is wrong within two commits.
 */
export default function sitemap(): MetadataRoute.Sitemap {
  const origin = (process.env.WEB_ORIGIN ?? 'http://localhost:3000').replace(/\/$/, '');
  const now = new Date();

  // priority is a hint about relative importance within this site, not a
  // ranking lever. The homepage and the catalogue are the entry points;
  // everything with a session behind it is left out entirely.
  const pages: Array<[string, number, MetadataRoute.Sitemap[number]['changeFrequency']]> = [
    ['', 1.0, 'weekly'],
    ['/explore', 0.9, 'weekly'],
    ['/story', 0.7, 'monthly'],
    ['/people', 0.7, 'monthly'],
    ['/achievements', 0.7, 'monthly'],
    ['/legacy', 0.7, 'monthly'],
    ['/plan', 0.6, 'daily'],
    ['/community', 0.6, 'daily'],
  ];

  const places = (destinations as Array<{ id: string }>).map((place) => ({
    url: `${origin}/explore/${place.id}`,
    lastModified: now,
    changeFrequency: 'monthly' as const,
    priority: 0.8,
  }));

  return [
    ...pages.map(([path, priority, changeFrequency]) => ({
      url: `${origin}${path}`,
      lastModified: now,
      changeFrequency,
      priority,
    })),
    ...places,
  ];
}
