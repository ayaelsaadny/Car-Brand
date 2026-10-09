import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { LanguageService } from '../services/language.service';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const langService = inject(LanguageService);

  if (authService.isLoggedIn()) {
    return true;
  }

  const lang = langService.currentLang();
  return router.createUrlTree([`/${lang}/auth/login`], {
    queryParams: { returnUrl: state.url }
  });
};
