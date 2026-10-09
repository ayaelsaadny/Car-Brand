import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { LanguageService } from '../services/language.service';

export const adminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const langService = inject(LanguageService);

  if (authService.isLoggedIn() && authService.isAdmin()) {
    return true;
  }

  const lang = langService.currentLang();
  // If not admin, redirect to dashboard or home
  return router.createUrlTree([`/${lang}/dashboard`]);
};
