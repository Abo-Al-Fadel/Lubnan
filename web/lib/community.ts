import { api } from '@/lib/api';
import { apiRegion, displayRegion } from '@/lib/regions';

export type CommunityAuthor = {
  id: string;
  displayName: string;
};

export type CommunityComment = {
  id: string;
  author: CommunityAuthor;
  body: string;
  createdAt: string;
  mine: boolean;
};

export type CommunityPost = {
  id: string;
  author: CommunityAuthor;
  body: string;
  placeSlug: string | null;
  placeName: string | null;
  region: string | null;
  plate: string | null;
  createdAt: string;
  likeCount: number;
  likedByMe: boolean;
  comments: CommunityComment[];
};

export type LikeState = {
  liked: boolean;
  likeCount: number;
};

function decorate(post: CommunityPost): CommunityPost {
  return { ...post, region: post.region ? displayRegion(post.region) : post.region };
}

export async function listPosts(region?: string | null) {
  const name = apiRegion(region);
  const query = name ? `?region=${encodeURIComponent(name)}` : '';
  const rows = await api<CommunityPost[]>(`/api/v1/community/posts${query}`);
  return rows.map(decorate);
}

export async function createPost(body: string, placeSlug?: string) {
  const post = await api<CommunityPost>('/api/v1/community/posts', {
    method: 'POST',
    body: JSON.stringify({ body, placeSlug: placeSlug || null }),
  });
  return decorate(post);
}

export function toggleLike(postId: string) {
  return api<LikeState>(`/api/v1/community/posts/${postId}/like`, { method: 'POST' });
}

export function addComment(postId: string, body: string) {
  return api<CommunityComment>(`/api/v1/community/posts/${postId}/comments`, {
    method: 'POST',
    body: JSON.stringify({ body }),
  });
}

/**
 * Delete a comment.
 *
 * Whether this is allowed is the server's decision, not the button's: the
 * endpoint answers 403 for somebody else's comment and 404 for one already
 * gone. The interface hides the control from people who cannot use it, which
 * is courtesy - it is not the check.
 */
export function removeComment(postId: string, commentId: string) {
  return api<void>(`/api/v1/community/posts/${postId}/comments/${commentId}`, {
    method: 'DELETE',
  });
}

export function relativeTime(iso: string, now = Date.now()): string {
  const then = new Date(iso).getTime();
  if (Number.isNaN(then)) return '';
  const minutes = Math.max(0, Math.round((now - then) / 60_000));
  if (minutes < 1) return 'now';
  if (minutes < 60) return `${minutes}m`;
  const hours = Math.round(minutes / 60);
  if (hours < 24) return `${hours}h`;
  const days = Math.round(hours / 24);
  return `${days}d`;
}
