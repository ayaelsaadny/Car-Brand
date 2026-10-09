import { Injectable, inject, signal, computed, PLATFORM_ID } from '@angular/core';
import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { Router } from '@angular/router';
import { TranslocoService } from '@jsverse/transloco';

export type SupportedLanguage = 'en' | 'fr';
export const SUPPORTED_LANGUAGES: readonly SupportedLanguage[] = ['en', 'fr'] as const;
export const DEFAULT_LANGUAGE: SupportedLanguage = 'en';

@Injectable({
  providedIn: 'root'
})
export class LanguageService {
  private readonly router = inject(Router);
  private readonly translocoService = inject(TranslocoService);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly document = inject(DOCUMENT);

  readonly currentLang = signal<SupportedLanguage>(this.detectInitialLanguage());
  readonly dir = computed<'ltr'>(() => 'ltr');

  constructor() {
    // Synchronize document attributes and Transloco active language on start
    this.applyLanguageToDom(this.currentLang());
    this.translocoService.setActiveLang(this.currentLang());
  }

  /**
   * Called by LangGuard when route parameter :lang is resolved.
   */
  initLanguageFromRoute(lang: string): void {
    const validated = this.validateLanguage(lang);
    if (this.currentLang() !== validated) {
      this.currentLang.set(validated);
      this.translocoService.setActiveLang(validated);
      this.applyLanguageToDom(validated);
      if (isPlatformBrowser(this.platformId)) {
        try {
          localStorage.setItem('preferred_lang', validated);
        } catch {
          // ignore storage errors
        }
      }
    }
  }

  /**
   * Switches language, updating Transloco, DOM, LocalStorage and URL prefix.
   */
  setLanguage(lang: SupportedLanguage): void {
    const validated = this.validateLanguage(lang);
    this.currentLang.set(validated);
    this.translocoService.setActiveLang(validated);
    this.applyLanguageToDom(validated);

    if (isPlatformBrowser(this.platformId)) {
      try {
        localStorage.setItem('preferred_lang', validated);
      } catch {
        // ignore storage errors
      }

      // Update URL prefix preserving the rest of the current path and query params
      const currentUrl = this.router.url;
      const urlSegments = currentUrl.split('?');
      const pathname = urlSegments[0];
      const query = urlSegments[1] ? `?${urlSegments[1]}` : '';

      const parts = pathname.split('/').filter(Boolean);
      if (parts.length > 0 && (parts[0] === 'en' || parts[0] === 'fr')) {
        parts[0] = validated;
      } else {
        parts.unshift(validated);
      }

      const newUrl = `/${parts.join('/')}${query}`;
      if (newUrl !== currentUrl) {
        this.router.navigateByUrl(newUrl);
      }
    }
  }

  /**
   * Validate language; strictly allows only 'en' or 'fr', falling back to 'en'.
   */
  validateLanguage(lang: string | null | undefined): SupportedLanguage {
    if (lang === 'fr') return 'fr';
    return 'en';
  }

  private detectInitialLanguage(): SupportedLanguage {
    if (!isPlatformBrowser(this.platformId)) {
      return DEFAULT_LANGUAGE;
    }

    // 1. Check URL path prefix
    try {
      const pathParts = window.location.pathname.split('/').filter(Boolean);
      if (pathParts.length > 0) {
        const first = pathParts[0].toLowerCase();
        if (first === 'en' || first === 'fr') {
          return first;
        }
      }
    } catch {
      // fallback
    }

    // 2. Check localStorage
    try {
      const saved = localStorage.getItem('preferred_lang');
      if (saved === 'en' || saved === 'fr') {
        return saved;
      }
    } catch {
      // fallback
    }

    // 3. Check browser navigator language
    try {
      const browserLang = navigator.language?.toLowerCase();
      if (browserLang && browserLang.startsWith('fr')) {
        return 'fr';
      }
    } catch {
      // fallback
    }

    return DEFAULT_LANGUAGE;
  }

  private applyLanguageToDom(lang: SupportedLanguage): void {
    if (this.document?.documentElement) {
      this.document.documentElement.lang = lang;
      this.document.documentElement.dir = 'ltr';
    }
  }
}
