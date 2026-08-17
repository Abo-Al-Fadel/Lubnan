import { api } from '@/lib/api';

export type SavedPlace = {
  slug: string;
  savedAt: string;
};

export function listSaved() {
  return api<SavedPlace[]>('/api/v1/me/saved');
}

export function pinSaved(slug: string) {
  return api<SavedPlace>('/api/v1/me/saved', {
    method: 'POST',
    body: JSON.stringify({ slug }),
  });
}

export function unpinSaved(slug: string) {
  return api(`/api/v1/me/saved/${encodeURIComponent(slug)}`, { method: 'DELETE' });
}
