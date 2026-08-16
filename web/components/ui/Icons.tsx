/**
 * Drawn icons, one consistent stroke weight, sized to the 9–11px micro layer.
 *
 * impeccable bans unicode glyphs and emoji standing in for an icon system, so
 * these are authored SVG rather than ✕ or ☾. Social marks are their own
 * logotypes and must stay solid; UI marks are stroked at 1.5.
 */

type P = { className?: string; size?: number };

const stroke = {
  fill: 'none',
  stroke: 'currentColor',
  strokeWidth: 1.5,
  strokeLinecap: 'round' as const,
  strokeLinejoin: 'round' as const,
};

export function Sun({ className = '', size = 16 }: P) {
  return (
    <svg viewBox="0 0 24 24" width={size} height={size} className={className} {...stroke} aria-hidden="true">
      <circle cx="12" cy="12" r="4" />
      <path d="M12 2v2M12 20v2M2 12h2M20 12h2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M19.1 4.9l-1.4 1.4M6.3 17.7l-1.4 1.4" />
    </svg>
  );
}

export function Moon({ className = '', size = 16 }: P) {
  return (
    <svg viewBox="0 0 24 24" width={size} height={size} className={className} {...stroke} aria-hidden="true">
      <path d="M20 14.5A8.5 8.5 0 0 1 9.5 4a8.5 8.5 0 1 0 10.5 10.5Z" />
    </svg>
  );
}

export function SearchIcon({ className = '', size = 15 }: P) {
  return (
    <svg viewBox="0 0 24 24" width={size} height={size} className={className} {...stroke} aria-hidden="true">
      <circle cx="11" cy="11" r="6.5" />
      <path d="M16 16l4.5 4.5" />
    </svg>
  );
}

export function Instagram({ className = '', size = 16 }: P) {
  return (
    <svg viewBox="0 0 24 24" width={size} height={size} className={className} {...stroke} aria-hidden="true">
      <rect x="3" y="3" width="18" height="18" rx="5" />
      <circle cx="12" cy="12" r="4" />
      <circle cx="17.2" cy="6.8" r="1.1" fill="currentColor" stroke="none" />
    </svg>
  );
}

export function YouTube({ className = '', size = 16 }: P) {
  return (
    <svg viewBox="0 0 24 24" width={size} height={size} className={className} {...stroke} aria-hidden="true">
      <rect x="2.5" y="5.5" width="19" height="13" rx="4" />
      <path d="M10.5 9.5l5 2.5-5 2.5z" fill="currentColor" stroke="none" />
    </svg>
  );
}

export function Facebook({ className = '', size = 16 }: P) {
  return (
    <svg viewBox="0 0 24 24" width={size} height={size} className={className} aria-hidden="true">
      <path
        fill="currentColor"
        d="M13.5 21v-8h2.7l.4-3.1h-3.1V7.9c0-.9.25-1.5 1.55-1.5H16.7V3.6A21 21 0 0 0 14.3 3.5c-2.4 0-4 1.45-4 4.12V9.9H7.6V13h2.7v8z"
      />
    </svg>
  );
}

/** X, formerly Twitter. */
export function X({ className = '', size = 15 }: P) {
  return (
    <svg viewBox="0 0 24 24" width={size} height={size} className={className} aria-hidden="true">
      <path
        fill="currentColor"
        d="M17.2 3h3.3l-7.2 8.3L21.8 21h-6.6l-4.3-5.6L5.9 21H2.6l7.7-8.8L2.5 3h6.8l3.9 5.2Zm-1.2 16h1.8L8.1 4.9H6.1Z"
      />
    </svg>
  );
}

export function Copyright({ className = '', size = 14 }: P) {
  return (
    <svg viewBox="0 0 24 24" width={size} height={size} className={className} {...stroke} aria-hidden="true">
      <circle cx="12" cy="12" r="9" />
      <path d="M15 9.4a4 4 0 1 0 0 5.2" />
    </svg>
  );
}
