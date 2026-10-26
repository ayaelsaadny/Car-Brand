import { Injectable, inject, PLATFORM_ID } from '@angular/core';
import { Title, Meta } from '@angular/platform-browser';
import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { SupportedLanguage } from './language.service';
import { ViewName, slugFor } from '../routing/page-routes';

// غيّريه لدومين الموقع الحقيقي (بيتستخدم على السيرفر لما window مش موجود)
const SITE_URL = 'https://limousine.example.com';

type PageMeta = Record<SupportedLanguage, { title: string; description: string }>;

const PAGE_META: Record<ViewName, PageMeta> = {
  home: {
    en: { title: 'Oudy | Exclusive VIP Chauffeur & Executive Travel', description: 'Experience extraordinary journeys with our luxury fleet, executive limousines, and 24/7 dedicated chauffeurs.' },
    fr: { title: 'Oudy | Service Chauffeur VIP & Transport Exécutif', description: "Découvrez des voyages d'exception avec notre flotte de luxe, limousines exécutives et chauffeurs dédiés 24h/24 et 7j/7." }
  },
  transfers: {
    en: { title: 'Airport & City Transfers in Montréal | Oudy', description: 'Reliable private airport and city-to-city transfers with professional chauffeurs, available 24/7.' },
    fr: { title: 'Transferts aéroport et ville à Montréal | Oudy', description: "Transferts privés fiables entre l'aéroport et les villes avec chauffeurs professionnels, disponibles 24h/24 et 7j/7." }
  },
  experiences: {
    en: { title: 'Private City Experiences in Montréal | Oudy', description: 'Discover Montréal in comfort with a private chauffeur and a tailored city experience.' },
    fr: { title: 'Expériences privées en ville à Montréal | Oudy', description: 'Découvrez Montréal avec confort grâce à un chauffeur privé et une expérience sur mesure.' }
  },
  montreal: {
    en: { title: 'Montréal Private Tours & Chauffeur Services | Oudy', description: 'Book private tours and chauffeur-driven services in Montréal with Oudy.' },
    fr: { title: 'Visites privées et service de chauffeur à Montréal | Oudy', description: 'Réservez des visites privées et des services avec chauffeur à Montréal avec Oudy.' }
  },
  airport: {
    en: { title: 'Airport Transfers | Private Chauffeur Pickup | Oudy', description: 'Private airport pickup and drop-off with a professional chauffeur, flight tracking and 24/7 service.' },
    fr: { title: 'Transferts aéroport | Chauffeur privé | Oudy', description: "Prise en charge et dépose privées à l'aéroport avec chauffeur professionnel, suivi de vol et service 24h/24." }
  },
  cityToCity: {
    en: { title: 'City-to-City Transfers | Oudy', description: 'Comfortable private transfers between cities with an experienced chauffeur and a premium vehicle.' },
    fr: { title: 'Transferts entre villes | Oudy', description: 'Transferts privés confortables entre villes avec chauffeur expérimenté et véhicule haut de gamme.' }
  },
  vip: {
    en: { title: 'VIP Chauffeur Service in Montréal | Oudy', description: 'Executive VIP chauffeur service for business travel, events and special occasions.' },
    fr: { title: 'Service de chauffeur VIP à Montréal | Oudy', description: 'Service de chauffeur VIP pour voyages d’affaires, événements et occasions spéciales.' }
  },
  bizOverview: {
    en: { title: 'Chauffeur Services for Business | Oudy', description: 'Premium chauffeur solutions for corporations, agencies and partners.' },
    fr: { title: 'Services de chauffeur pour entreprises | Oudy', description: 'Solutions de chauffeur haut de gamme pour sociétés, agences et partenaires.' }
  },
  bizCorporations: {
    en: { title: 'Corporate Chauffeur Services | Oudy', description: 'Reliable executive transportation for corporate travel, clients and teams.' },
    fr: { title: 'Chauffeur pour sociétés | Oudy', description: 'Transport exécutif fiable pour les déplacements professionnels, clients et équipes.' }
  },
  bizAgencies: {
    en: { title: 'Chauffeur Services for Travel Agencies | Oudy', description: 'Dependable ground transportation for travel and event agencies and their guests.' },
    fr: { title: 'Chauffeur pour agences de voyage | Oudy', description: 'Transport terrestre fiable pour les agences de voyage et d’événements et leurs invités.' }
  },
  bizPartners: {
    en: { title: 'Partnerships | Oudy', description: 'Partner with Oudy to offer premium chauffeur services to your customers.' },
    fr: { title: 'Partenariats | Oudy', description: 'Devenez partenaire d’Oudy pour offrir des services de chauffeur haut de gamme à vos clients.' }
  }
};

@Injectable({
  providedIn: 'root'
})
export class SeoService {
  private readonly titleService = inject(Title);
  private readonly metaService = inject(Meta);
  private readonly document = inject(DOCUMENT);
  private readonly platformId = inject(PLATFORM_ID);

  /** Title + description + canonical + hreflang لصفحة معينة بلغة معينة */
  updateSeo(lang: SupportedLanguage, view: ViewName): void {
    const meta = (PAGE_META[view] ?? PAGE_META.home)[lang];

    this.titleService.setTitle(meta.title);
    this.metaService.updateTag({ name: 'description', content: meta.description });
    this.metaService.updateTag({ property: 'og:title', content: meta.title });
    this.metaService.updateTag({ property: 'og:description', content: meta.description });
    this.metaService.updateTag({ property: 'og:locale', content: lang === 'fr' ? 'fr_CA' : 'en_CA' });

    this.updateLinkTags(lang, view);
  }

  /** للتوافق مع أي كومبوننت تاني لسه بينادي الدالة القديمة (بتعتبرها صفحة الهوم) */
  updateSeoForLanguage(lang: SupportedLanguage, _currentPath: string = ''): void {
    this.updateSeo(lang, 'home');
  }

  private updateLinkTags(lang: SupportedLanguage, view: ViewName): void {
    if (!this.document) return;

    const base = isPlatformBrowser(this.platformId) && window.location.origin ? window.location.origin : SITE_URL;
    const url = (l: SupportedLanguage) => {
      const slug = slugFor(view, l);
      return `${base}/${l}${slug ? '/' + slug : ''}`;
    };

    this.setOrUpdateLinkTag('canonical', url(lang));
    this.setOrUpdateLinkTag('alternate', url('en'), 'en');
    this.setOrUpdateLinkTag('alternate', url('fr'), 'fr');
    this.setOrUpdateLinkTag('alternate', url('en'), 'x-default');
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