const REGION_LABELS: Record<string, string> = {
  coast: 'Coast',
  mountlebanon: 'Mount Lebanon',
  north: 'North',
  south: 'South',
  bekaa: 'Bekaa',
};

export function displayRegion(region: string): string {
  const compact = region.replace(/[\s_-]/g, '').toLowerCase();
  return REGION_LABELS[compact] ?? region;
}

export function apiRegion(region: string | null | undefined): string | null {
  if (!region) return null;
  return region.replace(/[\s_-]/g, '');
}
