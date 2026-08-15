import destinations from '@/data/destinations.json';

/**
 * Everything the place-detail template needs beyond the card data already in
 * destinations.json — coordinates for the drawn map, the annotated callouts,
 * and the practical strip.
 *
 * The callouts are the point of the page: each one names a real thing you can
 * find at a real position in the frame. That is what stops the hero being
 * decorative. Coordinates are fractions of the plate, so they survive any crop.
 */

export type Callout = {
  /** Position within the annotated plate, 0–1. */
  x: number;
  y: number;
  label: string;
  body: string;
};

export type Place = {
  id: string;
  name: string;
  localName: string;
  arabic: string;
  region: string;
  category: string;
  index: string;
  note: string;
  /** Card plates from destinations.json; not every place has both yet. */
  plateRail?: string;
  plateMosaic?: string;
  lon: number;
  lat: number;
  /** Cut-out subject for the occlusion hero. */
  subject: string;
  heroPlate: string;
  framePlate: string;
  standfirst: string;
  body: string;
  callouts: Callout[];
  practical: { label: string; value: string }[];
};

/** The fields this file adds on top of destinations.json. */
type PlaceExtra = Pick<
  Place,
  | 'id'
  | 'lon'
  | 'lat'
  | 'subject'
  | 'heroPlate'
  | 'framePlate'
  | 'standfirst'
  | 'body'
  | 'callouts'
  | 'practical'
>;

const extra: Record<string, PlaceExtra> = {
  byblos: {
    id: 'byblos',
    lon: 35.65,
    lat: 34.12,
    subject: 'K1',
    heroPlate: 'J1',
    framePlate: 'L1',
    standfirst:
      'Seven thousand years of people, stacked. The crusader keep sits on Roman, on Phoenician, on Bronze Age, and the harbour below it still works.',
    body: 'Byblos is the argument for staying put. Every civilisation that arrived built on what the last one left rather than clearing it, so the site reads as a section drawing rather than a ruin — you can stand at one corner and see four thousand years of wall in a single glance. The alphabet that became Greek and then Latin shipped out of the harbour at the bottom of the hill, which is now about the size of a car park and full of fishing boats.',
    callouts: [
      { x: 0.28, y: 0.42, label: 'Crusader keep', body: 'Twelfth century, built from Roman columns laid on their sides as bracing.' },
      { x: 0.62, y: 0.66, label: 'The harbour', body: 'Still working. Boats leave from the north wall most mornings.' },
      { x: 0.78, y: 0.34, label: 'Royal necropolis', body: 'Nine shaft tombs. The Ahiram sarcophagus carried the earliest long alphabetic text.' },
    ],
    practical: [
      { label: 'Getting there', value: '40 km north of Beirut · 45 min by road' },
      { label: 'Best season', value: 'April–June, September–November' },
      { label: 'Hours', value: '09:00 – 18:00 summer, 09:00 – 16:00 winter' },
      { label: 'Entry', value: 'Ticketed site; the old town and harbour are free' },
    ],
  },
  baalbek: {
    id: 'baalbek',
    lon: 36.2,
    lat: 34.01,
    subject: 'K2',
    heroPlate: 'J2',
    framePlate: 'L2',
    standfirst:
      'The largest Roman temple ever built, standing on three foundation stones that nobody has satisfactorily explained moving.',
    body: 'The Temple of Jupiter had fifty-four columns. Six are still standing, and they are twenty-two metres tall. Underneath them the podium contains three limestone blocks of roughly eight hundred tonnes each — the trilithon — and a fourth, larger still, remains in the quarry eight hundred metres to the south, half-cut and abandoned. Whatever method moved the first three was clearly at its limit. The site sits in the Bekaa at a thousand metres, which is why the light here is harder and clearer than on the coast.',
    callouts: [
      { x: 0.34, y: 0.55, label: 'The trilithon', body: 'Three blocks, roughly 800 tonnes each, in the western podium wall.' },
      { x: 0.66, y: 0.3, label: 'Six columns', body: 'All that stands of fifty-four. Twenty-two metres to the capital.' },
      { x: 0.5, y: 0.78, label: 'Temple of Bacchus', body: 'Better preserved than anything in Rome, and larger than the Parthenon.' },
    ],
    practical: [
      { label: 'Getting there', value: '85 km east of Beirut · 2 hr over the Dahr el-Baidar pass' },
      { label: 'Best season', value: 'May–October. The pass can close in winter' },
      { label: 'Hours', value: '08:30 – 18:00' },
      { label: 'Entry', value: 'Ticketed. The quarry megalith is outside the fence and free' },
    ],
  },
  jeita: {
    id: 'jeita',
    lon: 35.64,
    lat: 33.94,
    subject: 'K3',
    heroPlate: 'J3',
    framePlate: 'L3',
    standfirst:
      'Nine kilometres of surveyed limestone gallery on two levels. You walk the upper one and take a boat through the lower.',
    body: 'The Nahr al-Kalb cut these galleries out of the limestone over a few million years and is still doing it. The upper cavern holds one of the largest known stalactites — about eight metres — and the acoustics are good enough that concerts have been held inside. The lower level is a river, navigated by flat-bottomed boat for a few hundred metres before the guides turn you around. Photography is not allowed inside, which is unusual now and worth knowing before you carry a camera up.',
    callouts: [
      { x: 0.44, y: 0.38, label: 'Upper gallery', body: 'Walkable. The 8 m stalactite is roughly a third of the way in.' },
      { x: 0.7, y: 0.62, label: 'Boat turnaround', body: 'The lower river is navigable in winter only when the water is low enough.' },
      { x: 0.22, y: 0.7, label: 'Cable car', body: 'Links the entrance to the upper cavern. Worth taking up, walking down.' },
    ],
    practical: [
      { label: 'Getting there', value: '18 km north of Beirut · 30 min' },
      { label: 'Best season', value: 'Year round. The lower grotto closes when the river is high' },
      { label: 'Hours', value: '09:00 – 18:00, closed Mondays out of season' },
      { label: 'Note', value: 'No cameras inside. Lockers at the entrance' },
    ],
  },
  cedars: {
    id: 'cedars',
    lon: 36.05,
    lat: 34.24,
    subject: 'K4',
    heroPlate: 'J4',
    framePlate: 'L4',
    standfirst:
      'Horsh Arz el-Rab — the Forest of the Cedars of God. A few hundred trunks at two thousand metres, some of them a thousand years old.',
    body: 'This is the tree on the flag, and there is not much of it left. Fences went up in the nineteenth century because goats were eating every seedling before it could root, and the grove has been managed ever since. Snow closes the road above Bsharri for part of most winters — the trees have handled roughly forty of those winters per human lifetime. Stand under the oldest ones and the scale is not height, it is girth: some of the trunks take four people to reach around.',
    callouts: [
      { x: 0.3, y: 0.5, label: 'The old grove', body: 'A few hundred trees. The oldest are estimated above a thousand years.' },
      { x: 0.68, y: 0.44, label: 'Replanting', body: 'Seedlings behind the fence line, planted from the 1990s onward.' },
      { x: 0.52, y: 0.76, label: 'Snowline', body: 'The road from Bsharri closes here in heavy winters.' },
    ],
    practical: [
      { label: 'Getting there', value: '120 km from Beirut · 2 hr 30 via Bsharri' },
      { label: 'Best season', value: 'June–October for walking, January–March for snow' },
      { label: 'Altitude', value: '~2,000 m — bring a layer even in summer' },
      { label: 'Entry', value: 'Small fee at the grove gate' },
    ],
  },
  qadisha: {
    id: 'qadisha',
    lon: 35.95,
    lat: 34.25,
    subject: 'K5',
    heroPlate: 'J5',
    framePlate: 'L5',
    standfirst:
      'A gorge with monasteries cut into its walls. People moved in to be left alone and stayed for a thousand years.',
    body: 'Qadisha means holy, and the valley earned it by being difficult to reach. Monks and hermits cut cells and chapels directly into the cliff faces, some of them only reachable by a path that is still a scramble. The valley floor is walkable end to end in a long day, and the drop from the rim at Bsharri to the river is close to a kilometre. It is on the UNESCO list jointly with the cedar grove above it, which is the correct way to list them — one is the reason the other survived.',
    callouts: [
      { x: 0.26, y: 0.36, label: 'Deir Qannoubine', body: 'Cut into the rock face. Patriarchal seat for roughly five centuries.' },
      { x: 0.6, y: 0.58, label: 'Valley floor', body: 'The Qadisha river. Walkable end to end in a long day.' },
      { x: 0.8, y: 0.24, label: 'Bsharri rim', body: 'Roughly a kilometre above the river. The cedar grove is behind it.' },
    ],
    practical: [
      { label: 'Getting there', value: '110 km from Beirut · 2 hr 15' },
      { label: 'Best season', value: 'April–November. The paths ice over in winter' },
      { label: 'Walking', value: 'Rim to floor and back is a full day. Guides available in Bsharri' },
      { label: 'Entry', value: 'Free. Individual monasteries have their own hours' },
    ],
  },
  tyre: {
    id: 'tyre',
    lon: 35.2,
    lat: 33.27,
    subject: 'K6',
    heroPlate: 'J6',
    framePlate: 'L6',
    standfirst:
      'Alexander built a causeway to take this island. The sand never left, and Tyre has been a peninsula ever since.',
    body: 'Tyre was an island city and it was, for a long time, unconquerable. Alexander solved that in 332 BC by building a mole out to it from the mainland — and the mole silted up, permanently joining the two. The Roman hippodrome on the landward side held tens of thousands for chariot racing and is one of the largest ever found. The city below is still a working fishing port with a good beach at the end of it, which is a more useful combination than most archaeological sites manage.',
    callouts: [
      { x: 0.36, y: 0.62, label: 'Hippodrome', body: 'Roman, one of the largest known. The turning posts are still in place.' },
      { x: 0.7, y: 0.44, label: 'The causeway', body: "Alexander's mole, now permanently silted into an isthmus." },
      { x: 0.2, y: 0.3, label: 'Fishing harbour', body: 'Still working. The beach runs south from it.' },
    ],
    practical: [
      { label: 'Getting there', value: '83 km south of Beirut · 1 hr 30' },
      { label: 'Best season', value: 'May–October for the beach; year round for the site' },
      { label: 'Hours', value: '08:30 – 18:00' },
      { label: 'Entry', value: 'Two ticketed sites — Al-Bass and the marine city' },
    ],
  },
  beirut: {
    id: 'beirut',
    lon: 35.5,
    lat: 33.89,
    subject: 'K7',
    heroPlate: 'J7',
    framePlate: 'L7',
    standfirst:
      'Destroyed seven times and rebuilt seven times, which is why the city argues with itself in every direction at once.',
    body: 'Beirut does not resolve. A Roman bath sits under an office block, a French Mandate façade stands beside raw concrete, and the Corniche runs five kilometres along the sea past fishermen, cyclists, families, and at least one man selling roasted corn. The sea stacks at Raouche are a hundred metres offshore and half the city walks past them every evening and still stops to look. Come for the food and stay for the fact that nobody here agrees on what the city is.',
    callouts: [
      { x: 0.3, y: 0.58, label: 'The Corniche', body: 'Five kilometres of seafront promenade. Best at dusk.' },
      { x: 0.68, y: 0.4, label: 'Raouche', body: 'Two limestone stacks a hundred metres offshore.' },
      { x: 0.48, y: 0.74, label: 'Downtown', body: 'Roman baths, Ottoman souk lines, Mandate façades, all within a few streets.' },
    ],
    practical: [
      { label: 'Getting there', value: 'Beirut–Rafic Hariri airport is 9 km from downtown' },
      { label: 'Best season', value: 'April–June, September–November' },
      { label: 'Getting around', value: 'Taxis and service (shared) taxis. Walkable along the coast' },
      { label: 'Note', value: 'Cash economy in most places. Bring US dollars' },
    ],
  },
  batroun: {
    id: 'batroun',
    lon: 35.66,
    lat: 34.25,
    subject: 'K8',
    heroPlate: 'J8',
    /* L8 is the one annotated frame not yet generated. Pointing at the hero
       keeps a real photograph of the right place on the page — the callouts
       sit at fractions of the frame, so they land approximately rather than
       exactly until L8 arrives. */
    framePlate: 'J8',
    standfirst:
      'A Phoenician sea wall still takes the swell off the old harbour, and the town behind it has become the coast’s best evening.',
    body: 'The wall is the thing: a natural sandstone ridge that the Phoenicians extended and squared off, and it has been breaking the Mediterranean for the town for something like three thousand years. Batroun is small, walkable, and has quietly become where people from Beirut go for a weekend — old stone streets, a lot of good seafood, and lemonade that the town is genuinely known for. The vineyards start about twenty minutes inland.',
    callouts: [
      { x: 0.3, y: 0.66, label: 'The Phoenician wall', body: 'Natural ridge, squared and extended. Still doing its job.' },
      { x: 0.66, y: 0.46, label: 'Old town', body: 'Sandstone streets behind the harbour, walkable end to end.' },
      { x: 0.82, y: 0.7, label: 'Vineyards', body: 'Twenty minutes inland, on the limestone slopes.' },
    ],
    practical: [
      { label: 'Getting there', value: '54 km north of Beirut · 1 hr' },
      { label: 'Best season', value: 'May–October' },
      { label: 'Worth ordering', value: 'Lemonade, and whatever came in that morning' },
      { label: 'Entry', value: 'Free — it is a town, not a site' },
    ],
  },
};

export const places: Place[] = destinations.map((d) => ({
  ...d,
  ...extra[d.id],
})) as Place[];

export const getPlace = (id: string) => places.find((p) => p.id === id);

export const REGIONS = ['Coast', 'Mount Lebanon', 'North', 'South', 'Bekaa'] as const;
export const CATEGORIES = ['ruins', 'nature', 'mountains', 'coast', 'city'] as const;
