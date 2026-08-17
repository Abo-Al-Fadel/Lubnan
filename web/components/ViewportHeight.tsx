'use client';

import { useEffect } from 'react';

/**
 * Keep --app-vh / --app-vw honest after DevTools inspect, rotation, and
 * mobile browser chrome.
 *
 * Closing Chrome's device toolbar often leaves 100vw/100svh at the last
 * emulated size. visualViewport.resize is the event that still fires; we
 * write the real pixels onto the root so the hero can unlock.
 */
export function ViewportHeight() {
  useEffect(() => {
    const set = () => {
      const h = Math.round(window.visualViewport?.height ?? window.innerHeight);
      const w = Math.round(window.visualViewport?.width ?? window.innerWidth);
      const root = document.documentElement;
      root.style.setProperty('--app-vh', `${h}px`);
      root.style.setProperty('--app-vw', `${w}px`);
    };

    set();
    window.addEventListener('resize', set);
    window.addEventListener('orientationchange', set);
    window.visualViewport?.addEventListener('resize', set);
    window.visualViewport?.addEventListener('scroll', set);
    return () => {
      window.removeEventListener('resize', set);
      window.removeEventListener('orientationchange', set);
      window.visualViewport?.removeEventListener('resize', set);
      window.visualViewport?.removeEventListener('scroll', set);
    };
  }, []);

  return null;
}
