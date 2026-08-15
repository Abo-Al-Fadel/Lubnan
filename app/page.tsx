'use client';

import { useEffect, useState } from 'react';
import { variations } from '@/data/variations';
import { LandingPage } from '@/components/LandingPage';

/**
 * The landing page. One direction now — Cedar.
 *
 * Raouche is kept in data/variations.ts rather than deleted: its palette and
 * sea-stack subject are the right treatment for a "favourite places" feature
 * later, where a second colour world reads as a section of this site rather
 * than as a competing homepage.
 */
export default function Home() {
  const variation = variations[0];
  const [showSlots, setShowSlots] = useState(false);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      const el = document.activeElement;
      if (el instanceof HTMLInputElement || el instanceof HTMLTextAreaElement) return;
      if (e.key.toLowerCase() === 'i') setShowSlots((s) => !s);
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, []);

  return (
    <>
      <a
        href="#main"
        className="sr-only focus:not-sr-only focus:absolute focus:left-4 focus:top-4 focus:z-[60] focus:rounded-full focus:bg-white focus:px-4 focus:py-2 focus:text-black"
      >
        Skip to content
      </a>

      <main id="main">
        <LandingPage variation={variation} showSlots={showSlots} />
      </main>

      {/* Build-time helper only: prints each slot's plate ID over it. Small and
          in the corner so it never sits on the design being judged. */}
      <button
        type="button"
        onClick={() => setShowSlots((s) => !s)}
        aria-pressed={showSlots}
        title="Show image slots (I)"
        className={`fixed bottom-4 right-4 z-50 rounded-full border px-3 py-2 text-[0.625rem] uppercase tracking-[0.18em] backdrop-blur-md transition-colors duration-200 ${
          showSlots
            ? 'border-white bg-white text-black'
            : 'border-white/25 bg-black/50 text-white/70 hover:text-white'
        }`}
      >
        Slots
      </button>
    </>
  );
}
