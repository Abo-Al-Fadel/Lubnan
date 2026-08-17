'use client';

import { useEffect, useState } from 'react';
import { variations } from '@/data/variations';
import { LandingPage } from '@/components/LandingPage';

/**
 * The landing page. One direction: Cedar.
 *
 * Raouche is kept in data/variations.ts rather than deleted: its palette and
 * sea-stack subject are the right treatment for the account area, where a
 * second colour world reads as a room inside this site rather than as a
 * competing homepage.
 */
export default function Home() {
  const variation = variations[0];

  /**
   * Plate-ID overlay, for filling image slots.
   *
   * Now keyboard-only. The floating button was useful while every slot was a
   * grey rectangle and is clutter now that they all hold photographs, but the
   * overlay itself still earns its keep whenever a new plate needs placing.
   * Press I.
   */
  const [showSlots, setShowSlots] = useState(false);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      const el = document.activeElement;
      if (el instanceof HTMLInputElement || el instanceof HTMLTextAreaElement) return;
      if (e.key === 'Escape') {
        setShowSlots(false);
        return;
      }
      if (e.key.toLowerCase() === 'i') setShowSlots((s) => !s);
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, []);

  return (
    <>
      <main id="main">
        <LandingPage variation={variation} showSlots={showSlots} />
      </main>
    </>
  );
}
