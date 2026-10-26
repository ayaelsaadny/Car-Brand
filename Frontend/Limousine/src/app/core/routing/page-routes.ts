import { UrlMatchResult, UrlSegment } from '@angular/router';
import type { SupportedLanguage } from '../services/language.service';

export type ViewName =
  | 'home' | 'transfers' | 'experiences' | 'montreal' | 'airport' | 'cityToCity' | 'vip'
  | 'bizOverview' | 'bizCorporations' | 'bizAgencies' | 'bizPartners';

export type PageView = Exclude<ViewName, 'home'>;

/** اسم الصفحة في الرابط لكل لغة */
export const PAGE_SLUGS: Record<PageView, Record<SupportedLanguage, string>> = {
  transfers:       { en: 'airport-city-transfers',   fr: 'transferts-aeroport-ville' },
  experiences:     { en: 'private-city-experiences', fr: 'experiences-privees-en-ville' },
  airport:         { en: 'airport-transfers',        fr: 'transferts-aeroport' },
  cityToCity:      { en: 'city-to-city',             fr: 'transferts-entre-villes' },
  vip:             { en: 'vip-chauffeur',            fr: 'chauffeur-vip' },
  montreal:        { en: 'montreal',                 fr: 'montreal' },
  bizOverview:     { en: 'business/overview',        fr: 'entreprises/apercu' },
  bizCorporations: { en: 'business/corporations',    fr: 'entreprises/societes' },
  bizAgencies:     { en: 'business/agencies',        fr: 'entreprises/agences' },
  bizPartners:     { en: 'business/partners',        fr: 'entreprises/partenaires' }
};

export function slugFor(view: ViewName, lang: SupportedLanguage): string {
  return view === 'home' ? '' : PAGE_SLUGS[view][lang];
}

/** ['/', 'fr', 'chauffeur-vip'] جاهزة لـ routerLink و router.navigate */
export function linkFor(view: ViewName, lang: SupportedLanguage): string[] {
  const slug = slugFor(view, lang);
  return slug ? ['/', lang, ...slug.split('/')] : ['/', lang];
}

/** بيعرف الصفحة من الـ slug سواء إنجليزي أو فرنسي */
export function viewForPath(path: string): ViewName | null {
  if (!path) return 'home';
  for (const [view, s] of Object.entries(PAGE_SLUGS)) {
    if (s.en === path || s.fr === path) return view as PageView;
  }
  return null;
}

/** Matcher: بيطابق الهوم وكل صفحاته الفرعية في route واحد */
export function homeMatcher(segments: UrlSegment[]): UrlMatchResult | null {
  const path = segments.map(s => s.path).join('/');
  return viewForPath(path) ? { consumed: segments } : null;
}