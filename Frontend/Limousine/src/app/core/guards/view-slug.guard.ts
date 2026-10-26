import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { linkFor, slugFor, viewForPath } from '../routing/page-routes'; 
import { SupportedLanguage } from '../services/language.service';

export const viewSlugGuard: CanActivateFn = (route) => {
  const router = inject(Router);
  const lang = (route.parent?.paramMap.get('lang')?.toLowerCase() === 'fr' ? 'fr' : 'en') as SupportedLanguage;
  const path = route.url.map(s => s.path).join('/');
  const view = viewForPath(path);
  if (!view || slugFor(view, lang) === path) return true;
  return router.createUrlTree(linkFor(view, lang));
};