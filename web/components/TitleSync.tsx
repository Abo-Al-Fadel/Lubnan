'use client';

import { usePathname } from 'next/navigation';
import { useEffect } from 'react';
import { getPlace } from '@/data/places';
import { useSite } from '@/lib/site-state';

const KEYS: Record<string, string> = {
  '/': 'meta.home',
  '/explore': 'nav.explore',
  '/story': 'nav.story',
  '/people': 'nav.people',
  '/achievements': 'nav.achievements',
  '/legacy': 'nav.legacy',
  '/plan': 'nav.plan',
  '/community': 'nav.community',
  '/login': 'nav.login',
  '/profile': 'nav.profile',
  '/confirm-email': 'confirm.title',
};

export function TitleSync() {
  const pathname = usePathname();
  const { tr } = useSite();

  useEffect(() => {
    const placeMatch = pathname.match(/^\/explore\/([^/]+)$/);
    if (placeMatch) {
      const place = getPlace(placeMatch[1]);
      document.title = place ? `${place.name} · Lubnān` : tr('notfound.title');
      return;
    }
    const key = KEYS[pathname];
    document.title = key ? `${tr(key)} · Lubnān` : 'Lubnān';
    if (pathname === '/') document.title = 'Lubnān';
  }, [pathname, tr]);

  return null;
}
