import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { LanguageService } from '../services/language.service';

export const langGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);
  const languageService = inject(LanguageService);

  const lang = route.paramMap.get('lang')?.toLowerCase();

  if (lang === 'en' || lang === 'fr') {
    languageService.initLanguageFromRoute(lang);
    return true;
  }

  // If language is invalid or unsupported, redirect to /en with the rest of the url
  const segments = state.url.split('?')[0].split('/').filter(Boolean);
  const query = state.url.includes('?') ? `?${state.url.split('?')[1]}` : '';

  if (segments.length > 0 && segments[0].length === 2) {
    // Replace the 2-letter invalid lang with 'en'
    segments[0] = 'en';
  } else {
    // Prepend 'en'
    segments.unshift('en');
  }

  const redirectUrl = `/${segments.join('/')}${query}`;
  return router.parseUrl(redirectUrl);
};
