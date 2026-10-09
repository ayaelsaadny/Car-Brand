import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { LanguageService } from '../services/language.service';

export const languageInterceptor: HttpInterceptorFn = (req, next) => {
  const languageService = inject(LanguageService);
  const currentLang = languageService.currentLang();

  const modifiedReq = req.clone({
    setHeaders: {
      'Accept-Language': currentLang
    }
  });

  return next(modifiedReq);
};
