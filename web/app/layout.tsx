import type { Metadata } from 'next';
import { Archivo, Oswald, Fraunces, Jost, Noto_Sans_Arabic } from 'next/font/google';
import './globals.css';
import { SiteProvider } from '@/lib/site-state';

/* Four display faces, one per direction, plus a shared utility grotesque and
   an Arabic companion for the bilingual wordmark. No direction sees more than
   two families. Inter is deliberately absent — fonts.md treats it as a choice
   requiring justification, not a neutral default. */

const archivo = Archivo({
  /* latin-ext is not optional here: the wordmark is LUBNĀN, and Ā is U+0100 —
     outside the `latin` subset. Without it the glyph silently fell out of
     Oswald and the macron vanished from the hero entirely. */
  subsets: ['latin', 'latin-ext'],
  variable: '--font-archivo',
  display: 'swap',
});

const oswald = Oswald({
  /* latin-ext is not optional here: the wordmark is LUBNĀN, and Ā is U+0100 —
     outside the `latin` subset. Without it the glyph silently fell out of
     Oswald and the macron vanished from the hero entirely. */
  subsets: ['latin', 'latin-ext'],
  variable: '--font-oswald',
  display: 'swap',
});

const fraunces = Fraunces({
  /* latin-ext is not optional here: the wordmark is LUBNĀN, and Ā is U+0100 —
     outside the `latin` subset. Without it the glyph silently fell out of
     Oswald and the macron vanished from the hero entirely. */
  subsets: ['latin', 'latin-ext'],
  variable: '--font-fraunces',
  display: 'swap',
  axes: ['SOFT', 'WONK', 'opsz'],
});

const jost = Jost({
  /* latin-ext is not optional here: the wordmark is LUBNĀN, and Ā is U+0100 —
     outside the `latin` subset. Without it the glyph silently fell out of
     Oswald and the macron vanished from the hero entirely. */
  subsets: ['latin', 'latin-ext'],
  variable: '--font-jost',
  display: 'swap',
});

const notoArabic = Noto_Sans_Arabic({
  subsets: ['arabic'],
  variable: '--font-arabic',
  display: 'swap',
});

/* No `icons` block: app/icon.png and app/apple-icon.png are picked up by the
   file convention and hashed automatically. Those two are the supplied cedar
   cropped to its ink — it filled only 71% × 65% of the 1254px tile it arrived
   in, and that transparent margin was scaled down along with the tree, which
   is what made the mark look small in the tab. */
export const metadata: Metadata = {
  title: 'Lubnān',
  description:
    'A guide to Lebanon: the cedar groves above Bsharri, the sea stacks off Beirut, and the places that never make the map.',
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html
      lang="en"
      className={`${archivo.variable} ${oswald.variable} ${fraunces.variable} ${jost.variable} ${notoArabic.variable}`}
    >
      <body>
        <SiteProvider>{children}</SiteProvider>
      </body>
    </html>
  );
}
