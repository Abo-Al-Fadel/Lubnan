import type { Locale } from '@/data/translations';

/**
 * Editorial copy, localised.
 *
 * Separate from translations.ts because these are article strings rather than
 * interface labels — a real CMS would serve them per locale, and this file is
 * the shape that API will fill. Keys mirror the JSON ids so a swap is 1:1.
 */
type Dict = Record<string, string>;

export const content: Record<Locale, Dict> = {
  en: {
    'hero.standfirst':
      'Two ranges run the length of the country and hold their snow into spring, which is how somewhere this small keeps a coastline and a treeline within an hour of each other.',
    'lede.statement': 'The tree on the flag is down to a few hundred trunks.',
    'lede.note1':
      'Fences went up around Horsh Arz el-Rab in the nineteenth century because goats were eating every seedling before it could root.',
    'lede.note2':
      'Snow closes the road above Bsharri for part of the winter. The trees have handled about forty of those winters per human lifetime.',
    'cta.line': 'Walk in under the old ones',

    'region.Coast': 'Coast',
    'region.Bekaa': 'Bekaa',
    'region.Mount Lebanon': 'Mount Lebanon',
    'region.North': 'North',
    'region.South': 'South',

    'secret.01.title': 'The alphabet left from a harbour you can still swim in',
    'secret.01.body':
      'Twenty-two Phoenician letters shipped out of Byblos and came back as Greek, then as Latin. The harbour is now about the size of a car park, and fishermen tie up in it every morning.',
    'secret.02.title': 'Baalbek rests on three stones nobody can explain',
    'secret.02.body':
      'The Trilithon blocks under the Temple of Jupiter weigh roughly eight hundred tonnes each. A fourth was left half-cut in the quarry down the road, and it is larger than the three above it.',
    'secret.03.title': 'You can ski in the morning and swim after lunch',
    'secret.03.body':
      'The claim gets repeated so often that visitors assume it is marketing. Faraya to the Beirut seafront is about forty kilometres, and in March the timing works.',
    'secret.04.title': 'The cedars on the flag are down to a few groves',
    'secret.04.body':
      'Horsh Arz el-Rab above Bsharri holds a few hundred trees, some of them past a thousand years old. Fences went up in the nineteenth century because goats were eating the seedlings.',
    'secret.05.title': 'More Lebanese live outside the country than inside it',
    'secret.05.body':
      'The diaspora reaches Brazil, West Africa, Australia and the Gulf, and it has been leaving in waves since the 1860s. Figure to confirm before publication.',
  },

  fr: {
    'hero.standfirst':
      'Deux chaînes parcourent le pays sur toute sa longueur et gardent leur neige jusqu’au printemps. C’est ainsi qu’un territoire si petit garde un littoral et une limite forestière à une heure l’un de l’autre.',
    'lede.statement': 'L’arbre du drapeau se réduit à quelques centaines de troncs.',
    'lede.note1':
      'Des clôtures ont été posées autour de Horsh Arz el-Rab au dix-neuvième siècle, car les chèvres mangeaient chaque jeune pousse avant qu’elle ne s’enracine.',
    'lede.note2':
      'La neige ferme la route au-dessus de Bcharré une partie de l’hiver. Les arbres en ont traversé une quarantaine par vie humaine.',
    'cta.line': 'Marchez sous les anciens',

    'region.Coast': 'Littoral',
    'region.Bekaa': 'Bekaa',
    'region.Mount Lebanon': 'Mont-Liban',
    'region.North': 'Nord',
    'region.South': 'Sud',

    'secret.01.title': 'L’alphabet est parti d’un port où l’on se baigne encore',
    'secret.01.body':
      'Vingt-deux lettres phéniciennes ont quitté Byblos et sont revenues en grec, puis en latin. Le port fait aujourd’hui la taille d’un parking, et des pêcheurs y amarrent chaque matin.',
    'secret.02.title': 'Baalbek repose sur trois pierres que personne n’explique',
    'secret.02.body':
      'Les blocs du Trilithon sous le temple de Jupiter pèsent environ huit cents tonnes chacun. Un quatrième est resté à moitié taillé dans la carrière voisine, et il est plus grand que les trois autres.',
    'secret.03.title': 'Skier le matin et nager après le déjeuner',
    'secret.03.body':
      'On le répète si souvent que les visiteurs y voient un argument publicitaire. De Faraya au bord de mer de Beyrouth, il y a une quarantaine de kilomètres, et en mars le calcul tient.',
    'secret.04.title': 'Les cèdres du drapeau tiennent en quelques bosquets',
    'secret.04.body':
      'Horsh Arz el-Rab, au-dessus de Bcharré, compte quelques centaines d’arbres, certains millénaires. Les clôtures datent du dix-neuvième siècle, à cause des chèvres.',
    'secret.05.title': 'Plus de Libanais vivent hors du pays qu’à l’intérieur',
    'secret.05.body':
      'La diaspora atteint le Brésil, l’Afrique de l’Ouest, l’Australie et le Golfe, et part par vagues depuis les années 1860. Chiffre à confirmer avant publication.',
  },

  ar: {
    'hero.standfirst':
      'سلسلتان جبليتان تمتدّان على طول البلاد وتحتفظان بثلجهما حتى الربيع. وهكذا يجمع بلد بهذا الصغر بين ساحله وحدّ أشجاره في ساعة واحدة.',
    'lede.statement': 'شجرة العلم لم يبقَ منها سوى بضع مئات من الجذوع.',
    'lede.note1':
      'أُقيمت الأسوار حول حرش أرز الربّ في القرن التاسع عشر لأن الماعز كان يأكل كل شتلة قبل أن تتجذّر.',
    'lede.note2':
      'يقطع الثلج الطريق فوق بشرّي جزءاً من الشتاء. احتملت الأشجار نحو أربعين شتاءً كهذا في عمر إنسان واحد.',
    'cta.line': 'امشِ تحت الأشجار العتيقة',

    'region.Coast': 'الساحل',
    'region.Bekaa': 'البقاع',
    'region.Mount Lebanon': 'جبل لبنان',
    'region.North': 'الشمال',
    'region.South': 'الجنوب',

    'secret.01.title': 'الأبجدية خرجت من مرفأ ما زال يُسبح فيه',
    'secret.01.body':
      'اثنتان وعشرون حرفاً فينيقياً أبحرت من جبيل وعادت يونانية ثم لاتينية. المرفأ اليوم بحجم موقف سيارات، ويربط فيه الصيادون قواربهم كل صباح.',
    'secret.02.title': 'بعلبك تقوم على ثلاثة حجارة لا تفسير لها',
    'secret.02.body':
      'تزن كتل الثالوث تحت معبد جوبيتر نحو ثمانمئة طن للواحدة. وتُرك حجر رابع نصف منحوت في المقلع القريب، وهو أكبر من الثلاثة فوقه.',
    'secret.03.title': 'تتزلّج صباحاً وتسبح بعد الغداء',
    'secret.03.body':
      'تتكرّر هذه العبارة كثيراً حتى يظنّها الزائر دعاية. من فاريا إلى واجهة بيروت البحرية نحو أربعين كيلومتراً، وفي آذار يصحّ التوقيت.',
    'secret.04.title': 'أرز العلم لم يبقَ منه إلا بضعة أحراج',
    'secret.04.body':
      'يضمّ حرش أرز الربّ فوق بشرّي بضع مئات من الأشجار، بعضها تجاوز الألف عام. أُقيمت الأسوار في القرن التاسع عشر لأن الماعز كان يأكل الشتلات.',
    'secret.05.title': 'عدد اللبنانيين خارج البلاد أكبر من عددهم فيها',
    'secret.05.body':
      'يمتدّ الانتشار إلى البرازيل وغرب أفريقيا وأستراليا والخليج، وبدأ بموجات منذ ستينيات القرن التاسع عشر. الرقم بانتظار التأكيد.',
  },
};
