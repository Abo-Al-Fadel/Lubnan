export type PaletteKey = 'cedar' | 'raouche';

export type Variation = {
  key: PaletteKey;
  index: string;
  name: string;
  cue: string;
  subject: 'arch' | 'cedar' | 'columns' | 'headland';
  /** Hero background plate ID, resolved under /img/. */
  heroPlate: string;
  /** Optional .mp4 of the same frame, played over the still. */
  heroVideo?: string;
  /** Hero cut-out ID, resolved under /img/cutouts/. Falls back to the drawn SVG. */
  cutoutPlate: string;
  heroWord: string;
  blurb: string;
  rhythm: string;
  swatches: string[];
};

/**
 * Down to two.
 *
 * Cedar and Raouche were both ranked first; Limestone was dropped at your
 * request. Both survivors are cool, so the warmth the set needs now comes from
 * the accent and the ambient light sweep rather than from a third palette.
 *
 * Gentle is held back rather than discarded: its pale ground is the right
 * surface for the long-text pages (Story, Plan Your Trip), where a dark
 * ground fights sustained reading. It returns as a reading palette once a
 * hero direction is settled, not as a competing hero.
 */
export const variations: Variation[] = [
  {
    key: 'cedar',
    cutoutPlate: 'B1',
    heroPlate: 'A1',
    heroVideo: 'A1',
    index: '01',
    name: 'Cedar',
    cue: 'cedar · cool · ranked first',
    subject: 'cedar',
    heroWord: 'Lubnān',
    blurb:
      'Chroma pulled out and temperature pushed cold. Winter slate rather than moss. The tree on the flag, in the weather it actually grows in.',
    rhythm: 'hero → secrets → lede → mosaic → names → numbers → community → close',
    swatches: ['#414F52', '#8B9A99', '#EFEBDD', '#1C2528'],
  },
  {
    key: 'raouche',
    cutoutPlate: 'B2',
    heroPlate: 'A2',
    index: '02',
    name: 'Raouche',
    cue: 'beach · Raouche Rock · cool · ranked first',
    subject: 'arch',
    heroWord: 'Lubnān',
    blurb:
      'Beirut’s sea stacks at midday. Bright cool water against bleached limestone, and the highest-key palette of the three.',
    rhythm: 'hero → lede → rail → numbers → secrets → community → close',
    swatches: ['#2E5A66', '#86A9AB', '#F0EDE1', '#16333B'],
  },
];
