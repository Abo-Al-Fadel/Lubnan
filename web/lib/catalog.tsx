'use client';

import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { api, ApiError } from '@/lib/api';
import { displayRegion } from '@/lib/regions';
import { useSite } from '@/lib/site-state';
import {
  getPlace as getStaticPlace,
  places as staticPlaces,
  type Callout,
  type Place,
} from '@/data/places';

export type { Callout, Place };
export { CATEGORIES, REGIONS, getPlace as getStaticPlace, places as staticPlaces } from '@/data/places';
export { apiRegion, displayRegion } from '@/lib/regions';

type ApiPlates = {
  hero?: string | null;
  frame?: string | null;
  subject?: string | null;
  rail?: string | null;
  mosaic?: string | null;
};

type ApiPlaceSummary = {
  slug: string;
  name: string;
  localName?: string | null;
  note: string;
  region: string;
  category: string;
  index: string;
  latitude: number;
  longitude: number;
  plates: ApiPlates;
};

type ApiPlaceDetail = ApiPlaceSummary & {
  locale: string;
  standfirst: string;
  body: string;
  callouts: Callout[];
  practical: { label: string; value: string }[];
};

function mergeStatic(slug: string, mapped: Place): Place {
  const extra = getStaticPlace(slug);
  if (!extra) return mapped;
  return {
    ...extra,
    ...mapped,
    arabic: extra.arabic || mapped.arabic,
    plateRail: mapped.plateRail ?? extra.plateRail,
    plateMosaic: mapped.plateMosaic ?? extra.plateMosaic,
    subject: mapped.subject || extra.subject,
    heroPlate: mapped.heroPlate || extra.heroPlate,
    framePlate: mapped.framePlate || extra.framePlate,
    standfirst: mapped.standfirst || extra.standfirst,
    body: mapped.body || extra.body,
    callouts: mapped.callouts.length > 0 ? mapped.callouts : extra.callouts,
    practical: mapped.practical.length > 0 ? mapped.practical : extra.practical,
  };
}

function fromSummary(row: ApiPlaceSummary): Place {
  return mergeStatic(row.slug, {
    id: row.slug,
    name: row.name,
    localName: row.localName ?? row.name,
    arabic: '',
    region: displayRegion(row.region),
    category: row.category.toLowerCase(),
    index: row.index,
    note: row.note,
    plateRail: row.plates.rail ?? undefined,
    plateMosaic: row.plates.mosaic ?? undefined,
    lon: row.longitude,
    lat: row.latitude,
    subject: row.plates.subject ?? '',
    heroPlate: row.plates.hero ?? '',
    framePlate: row.plates.frame ?? '',
    standfirst: row.note,
    body: '',
    callouts: [],
    practical: [],
  });
}

function fromDetail(row: ApiPlaceDetail): Place {
  return mergeStatic(row.slug, {
    ...fromSummary(row),
    standfirst: row.standfirst,
    body: row.body,
    callouts: row.callouts ?? [],
    practical: row.practical ?? [],
  });
}

export async function fetchPlaces(locale: string): Promise<{ places: Place[]; fromApi: boolean }> {
  try {
    const rows = await api<ApiPlaceSummary[]>(`/api/v1/places?locale=${encodeURIComponent(locale)}`);
    if (!Array.isArray(rows) || rows.length === 0) {
      return { places: staticPlaces, fromApi: false };
    }
    return { places: rows.map(fromSummary), fromApi: true };
  } catch {
    return { places: staticPlaces, fromApi: false };
  }
}

export async function fetchPlace(slug: string, locale: string): Promise<Place | null> {
  try {
    const row = await api<ApiPlaceDetail>(
      `/api/v1/places/${encodeURIComponent(slug)}?locale=${encodeURIComponent(locale)}`,
    );
    return fromDetail(row);
  } catch (err) {
    if (err instanceof ApiError && err.status === 404) return null;
    return getStaticPlace(slug) ?? null;
  }
}

type CatalogCtx = {
  places: Place[];
  ready: boolean;
  fromApi: boolean;
  getPlace: (id: string) => Place | undefined;
  refresh: () => Promise<void>;
};

const CatalogContext = createContext<CatalogCtx | null>(null);

export function CatalogProvider({ children }: { children: React.ReactNode }) {
  const { locale } = useSite();
  const [places, setPlaces] = useState<Place[]>(staticPlaces);
  const [fromApi, setFromApi] = useState(false);
  const [ready, setReady] = useState(false);

  const refresh = useCallback(async () => {
    const next = await fetchPlaces(locale);
    setPlaces(next.places);
    setFromApi(next.fromApi);
    setReady(true);
  }, [locale]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  const getPlace = useCallback((id: string) => places.find((p) => p.id === id), [places]);

  const value = useMemo(
    () => ({ places, ready, fromApi, getPlace, refresh }),
    [places, ready, fromApi, getPlace, refresh],
  );

  return <CatalogContext.Provider value={value}>{children}</CatalogContext.Provider>;
}

export function useCatalog() {
  const ctx = useContext(CatalogContext);
  if (!ctx) throw new Error('useCatalog must be used inside CatalogProvider');
  return ctx;
}
