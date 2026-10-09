import { Injectable, inject, PLATFORM_ID } from '@angular/core';
import { Title, Meta } from '@angular/platform-browser';
import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { SupportedLanguage } from './language.service';

@Injectable({
  providedIn: 'root'
})
export class SeoService {
  private readonly titleService = inject(Title);
  private readonly metaService = inject(Meta);
  private readonly document = inject(DOCUMENT);
  private readonly platformId = inject(PLATFORM_ID);

  private readonly metaContent: Record<SupportedLanguage, { title: string; description: string }> = {
    en: {
      title: 'Prestige Limousine | Exclusive VIP Chauffeur & Executive Travel',
      description: 'Experience extraordinary journeys with our luxury fleet, executive limousines, and 24/7 dedicated chauffeurs.'
    },
    fr: {
      title: 'Limousine Prestige | Service Chauffeur VIP & Transport Exécutif',
      description: "Découvrez des voyages d'exception avec notre flotte de luxe, limousines exécutives et chauffeurs dédiés 24h/24 et 7j/7."
    }
  };

  updateSeoForLanguage(lang: SupportedLanguage, currentPath: string = ''): void {
    const meta = this.metaContent[lang] || this.metaContent.en;

    // 1. Update Title
    this.titleService.setTitle(meta.title);

    // 2. Update Meta Description
    this.metaService.updateTag({ name: 'description', content: meta.description });
    this.metaService.updateTag({ property: 'og:title', content: meta.title });
    this.metaService.updateTag({ property: 'og:description', content: meta.description });
    this.metaService.updateTag({ property: 'og:locale', content: lang === 'fr' ? 'fr_FR' : 'en_US' });

    // 3. Update canonical and hreflang links
    this.updateLinkTags(lang, currentPath);
  }

  private updateLinkTags(currentLang: SupportedLanguage, path: string): void {
    if (!this.document) return;

    let baseUrl = 'https://limousine.example.com';
    if (isPlatformBrowser(this.platformId) && window.location.origin) {
      baseUrl = window.location.origin;
    }

    // Clean relative path without lang prefix
    const cleanPath = path.replace(/^\/(en|fr)/, '');

    // Canonical link
    const canonicalUrl = `${baseUrl}/${currentLang}${cleanPath}`;
    this.setOrUpdateLinkTag('canonical', canonicalUrl);

    // Hreflang alternates
    this.setOrUpdateLinkTag('alternate', `${baseUrl}/en${cleanPath}`, 'en');
    this.setOrUpdateLinkTag('alternate', `${baseUrl}/fr${cleanPath}`, 'fr');
    this.setOrUpdateLinkTag('alternate', `${baseUrl}/en${cleanPath}`, 'x-default');
  }

  private setOrUpdateLinkTag(rel: string, href: string, hreflang?: string): void {
    const head = this.document.head;
    if (!head) return;

    let selector = `link[rel="${rel}"]`;
    if (hreflang) {
      selector += `[hreflang="${hreflang}"]`;
    }

    let link = head.querySelector<HTMLLinkElement>(selector);
    if (!link) {
      link = this.document.createElement('link');
      link.rel = rel;
      if (hreflang) link.hreflang = hreflang;
      head.appendChild(link);
    }
    link.href = href;
  }
}
