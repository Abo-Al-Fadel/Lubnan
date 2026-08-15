'use client';

import { HeroOcclusion } from '@/components/sections/Hero';
import {
  ClosingCTA,
  CommunityStrip,
  DestinationMosaic,
  DestinationRail,
  Lede,
  NumbersBar,
  SecretsList,
} from '@/components/sections/Blocks';
import { SiteFooter } from '@/components/sections/Chrome';
import type { PaletteKey, Variation } from '@/data/variations';

type Content = {
  heroBrief: string;
  standfirst: string;
  stats: { figure: string; label: string }[];
  ledeStatement: string;
  ledeNotes: [string, string];
  closingLine: string;
};

/**
 * Per-palette content. The subject of each page's photography follows its
 * palette, so the colour world and the place are one decision rather than a
 * tint applied over generic imagery.
 */
const content: Record<PaletteKey, Content> = {
  cedar: {
    heroBrief:
      'Cedar grove above Bsharri under flat overcast light, mid shot, cold desaturated slate and grey-green, snow patches between the trunks, winter',
    standfirst:
      'Two ranges run the length of the country and hold their snow into spring — which is how somewhere this small keeps a coastline and a treeline within an hour of each other.',
    stats: [
      { figure: '1,000+', label: 'years on the oldest trunks' },
      { figure: '2,000m', label: 'altitude of the grove' },
      { figure: '17', label: 'surviving groves' },
    ],
    ledeStatement: 'The tree on the flag is down to a few hundred trunks.',
    ledeNotes: [
      'Fences went up around Horsh Arz el-Rab in the nineteenth century because goats were eating every seedling before it could root.',
      'Snow closes the road above Bsharri for part of the winter. The trees have handled about forty of those winters per human lifetime.',
    ],
    closingLine: 'Walk in under the old ones',
  },
  raouche: {
    heroBrief:
      'The Raouche sea stacks off Beirut at midday from the clifftop Corniche, wide shot, bright cool Mediterranean blue against bleached limestone, hard overhead light',
    standfirst:
      'Two limestone stacks sit a hundred metres off the Corniche. Half of Beirut walks past them every evening and still stops to look.',
    stats: [
      { figure: '225', label: 'km of coastline' },
      { figure: '60m', label: 'the taller stack' },
      { figure: '4,500', label: 'years of harbour' },
    ],
    ledeStatement: 'The sea got here first and never left.',
    ledeNotes: [
      'Phoenician crews launched from Tyre, Sidon and Byblos and mapped the Mediterranean before anyone drew it. The harbours they used are still harbours.',
      'The Corniche runs about five kilometres. Walk it at dusk and you pass fishermen, cyclists, families and at least one man selling roasted corn.',
    ],
    closingLine: 'Come see what the water built',
  },
};

export function LandingPage({
  variation,
  showSlots,
}: {
  variation: Variation;
  showSlots: boolean;
}) {
  const c = content[variation.key];

  /* The rhythm string in variations.ts drives section order, so no two pages
     share a shape. hallmark: structural variety, not just visual variety. */
  const blocks: Record<string, JSX.Element> = {
    hero: (
      <HeroOcclusion
        variation={variation}
        showSlots={showSlots}
        imageBrief={c.heroBrief}
        standfirst={c.standfirst}
        stats={c.stats}
      />
    ),
    lede: <Lede statement={c.ledeStatement} notes={c.ledeNotes} showSlots={showSlots} />,
    rail: <DestinationRail showSlots={showSlots} />,
    mosaic: <DestinationMosaic showSlots={showSlots} />,
    numbers: <NumbersBar />,
    secrets: <SecretsList showSlots={showSlots} />,
    community: <CommunityStrip showSlots={showSlots} />,
    close: <ClosingCTA showSlots={showSlots} />,
  };

  const order = variation.rhythm.split('→').map((s) => s.trim());

  return (
    <div data-palette={variation.key} className="bg-ground">
      {/* The hero/page seam used to live here, between the sections, where it
          had no photograph behind it to work on. It now sits inside the hero
          itself — see the `.seam` strip in Hero.tsx. */}
      {order.map((key) => (
        <div key={key}>{blocks[key]}</div>
      ))}
      <SiteFooter />
    </div>
  );
}
