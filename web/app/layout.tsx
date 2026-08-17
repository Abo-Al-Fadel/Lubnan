import type { Metadata } from 'next';
import { Archivo, Oswald, Fraunces, Jost, Noto_Sans_Arabic } from 'next/font/google';
import './globals.css';
import { SiteProvider } from '@/lib/site-state';
import { AuthProvider } from '@/lib/auth';
import { SkipLink } from '@/components/SkipLink';
import { TitleSync } from '@/components/TitleSync';
import { ViewportHeight } from '@/components/ViewportHeight';

/* Four display faces, one per direction, plus a shared utility grotesque and
   an Arabic companion for the bilingual wordmark. No direction sees more than
   two families. Inter is deliberately absent — fonts.md treats it as a choice
   requiring justification, not a neutral default. */

const archivo = Archivo({
  subsets: ['latin', 'latin-ext'],
  variable: '--font-archivo',
  display: 'swap',
});

const oswald = Oswald({
  subsets: ['latin', 'latin-ext'],
  variable: '--font-oswald',
  display: 'swap',
});

const fraunces = Fraunces({
  subsets: ['latin', 'latin-ext'],
  variable: '--font-fraunces',
  display: 'swap',
  axes: ['SOFT', 'WONK', 'opsz'],
});

const jost = Jost({
  subsets: ['latin', 'latin-ext'],
  variable: '--font-jost',
  display: 'swap',
});

const notoArabic = Noto_Sans_Arabic({
  subsets: ['arabic'],
  variable: '--font-arabic',
  display: 'swap',
});

export const metadata: Metadata = {
  title: {
    default: 'Lubnān',
    template: '%s · Lubnān',
  },
  description:
    'A guide to Lebanon: the cedar groves above Bsharri, the sea stacks off Beirut, and the places that never make the map.',
  metadataBase: new URL(process.env.WEB_ORIGIN ?? 'http://localhost:3000'),
  robots: { index: true, follow: true },
};

const BOOTSTRAP = `(function(){try{var t=localStorage.getItem('lubnan.theme');if(t==='light'||t==='dark')document.documentElement.setAttribute('data-theme',t);else if(window.matchMedia('(prefers-color-scheme: dark)').matches)document.documentElement.setAttribute('data-theme','dark');var l=localStorage.getItem('lubnan.locale');if(l==='en'||l==='fr'||l==='ar'){document.documentElement.lang=l;document.documentElement.dir=l==='ar'?'rtl':'ltr';}}catch(e){}})();`;

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html
      lang="en"
      suppressHydrationWarning
      className={`${archivo.variable} ${oswald.variable} ${fraunces.variable} ${jost.variable} ${notoArabic.variable}`}
    >
      <head>
        <script dangerouslySetInnerHTML={{ __html: BOOTSTRAP }} />
      </head>
      <body>
        <SiteProvider>
          <AuthProvider>
            <ViewportHeight />
            <SkipLink />
            <TitleSync />
            {children}
          </AuthProvider>
        </SiteProvider>
      </body>
    </html>
  );
}
