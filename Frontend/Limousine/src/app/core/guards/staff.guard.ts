import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { LanguageService } from '../services/language.service';

export const staffGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const langService = inject(LanguageService);

  if (authService.isLoggedIn() && authService.isStaff()) {
    return true;
  }

  const lang = langService.currentLang();
  // If customer or unauthenticated, redirect to public home or login
  if (!authService.isLoggedIn()) {
    return router.createUrlTree([`/${lang}/auth/login`]);
  }
  return router.createUrlTree([`/${lang}/customer/bookings`]);
};
