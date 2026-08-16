import type { Config } from 'tailwindcss';

const config: Config = {
  content: ['./app/**/*.{ts,tsx}', './components/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        ground: 'var(--ground)',
        band: 'var(--band)',
        ink: 'var(--ink)',
        'ink-dim': 'var(--ink-dim)',
        'ink-ghost': 'var(--ink-ghost)',
        /* Hero type sits on a photograph, not on the page ground, so it needs
           its own ink — otherwise a light-ground palette makes the hero
           headline dark on a dark plate. */
        'hero-ink': 'var(--hero-ink)',
        'hero-ink-dim': 'var(--hero-ink-dim)',
        'hero-ink-ghost': 'var(--hero-ink-ghost)',
        subject: 'var(--subject)',
        accent: 'var(--accent)',
        'display-ink': 'var(--display-ink)',
        cta: 'var(--cta)',
        'photo-a': 'var(--photo-a)',
        'photo-b': 'var(--photo-b)',
        'photo-c': 'var(--photo-c)',
      },
      fontFamily: {
        display: ['var(--font-display)', 'serif'],
        body: ['var(--font-body)', 'sans-serif'],
        arabic: ['var(--font-arabic)', 'sans-serif'],
      },
      transitionTimingFunction: {
        out: 'var(--ease-out)',
        'in-out': 'var(--ease-in-out)',
      },
    },
  },
  plugins: [],
};

export default config;
