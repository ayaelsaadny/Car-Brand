import { UrlMatchResult, UrlSegment } from '@angular/router';
import type { SupportedLanguage } from '../services/language.service';

export type ViewName =
  | 'home' | 'airport' | 'experiences' | 'cityToCity' | 'vip'
  | 'airportPickup' | 'airportDropoff' |'montreal' | 'ottawa'
  | 'quebec' | 'toronto' | 'tremblant'| 'cityOther'|  'weddings' | 'graduations' | 'events'
  | 'shopping' | 'hourly' | 'bizOverview' | 'bizCorporations' | 'bizAgencies'
  | 'bizPartners' 

export type PageView = Exclude<ViewName, 'home'>;

/** The page name in the URL, for each language */
export const PAGE_SLUGS: Record<PageView, Record<SupportedLanguage, string>> = {
  airport:         { en: "airport-transfers", fr: "transferts-aeroport" },
  experiences:     { en: "private-city-experiences", fr: "experiences-privees-en-ville" },
  cityToCity:      { en: "city-to-city", fr: "transferts-entre-villes" },
  vip:             { en: "private-chauffeur", fr: "chauffeur-prive" },
  airportPickup:   { en: "airport-transfers/airport-pickup", fr: "transferts-aeroport/prise-en-charge-aeroport" },
  airportDropoff:  { en: "airport-transfers/airport-drop-off", fr: "transferts-aeroport/depose-aeroport" },

  montreal:        { en: "private-city-experiences/montreal-city", fr: "experiences-privees-en-ville/montreal" },
  ottawa:          { en: "city-to-city/montreal-to-ottawa", fr: "transferts-entre-villes/montreal-ottawa" },
  quebec:          { en: "city-to-city/montreal-to-quebec-city", fr: "transferts-entre-villes/montreal-quebec" },
  toronto:         { en: "city-to-city/montreal-to-toronto", fr: "transferts-entre-villes/montreal-toronto" },
  tremblant: { en: "city-to-city/montreal-to-mont-tremblant", fr: "transferts-entre-villes/montreal-mont-tremblant" },
  cityOther: { en: "city-to-city/other-destinations", fr: "transferts-entre-villes/autres-destinations" },
  weddings:        { en: "private-chauffeur/weddings", fr: "chauffeur-prive/mariages" },
  graduations:     { en: "private-chauffeur/graduations", fr: "chauffeur-prive/remises-de-diplomes" },
  events:          { en: "private-chauffeur/events-parties", fr: "chauffeur-prive/evenements-fetes" },
  shopping:        { en: "private-chauffeur/shopping-errands", fr: "chauffeur-prive/magasinage-courses" },
  hourly:          { en: "private-chauffeur/business-by-the-hour", fr: "chauffeur-prive/affaires-a-l-heure" },
  bizOverview:     { en: "business/overview", fr: "entreprises/apercu" },
  bizCorporations: { en: "business/corporations", fr: "entreprises/societes" },
  bizAgencies:     { en: "business/agencies", fr: "entreprises/agences" },
  bizPartners:     { en: "business/partners", fr: "entreprises/partenaires" }
};

/** Old links (bookmarks, Google) that still work: they redirect to the new address */
const LEGACY_SLUGS: Record<string, PageView> = {
  'airport-city-transfers': 'airport',
  'transferts-aeroport-ville': 'airport',
  'vip-chauffeur': 'vip',
  'chauffeur-vip': 'vip',
  'montreal': 'montreal'
};

export function slugFor(view: ViewName, lang: SupportedLanguage): string {
  return view === 'home' ? '' : PAGE_SLUGS[view][lang];
}

/** ['/', 'fr', 'chauffeur-prive'] ready for routerLink and router.navigate */
export function linkFor(view: ViewName, lang: SupportedLanguage): string[] {
  const slug = slugFor(view, lang);
  return slug ? ['/', lang, ...slug.split('/')] : ['/', lang];
}

/** Finds the page from its slug (English or French, or an old slug) */
export function viewForPath(path: string): ViewName | null {
  if (!path) return 'home';
  for (const [view, s] of Object.entries(PAGE_SLUGS)) {
    if (s.en === path || s.fr === path) return view as PageView;
  }
  return LEGACY_SLUGS[path] ?? null;
}

/** Matcher: one route for the home page and all its sub pages */
export function homeMatcher(segments: UrlSegment[]): UrlMatchResult | null {
  const path = segments.map(s => s.path).join('/');
  return viewForPath(path) ? { consumed: segments } : null;
}