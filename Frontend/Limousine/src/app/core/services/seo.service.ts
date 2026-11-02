import { Injectable, inject, PLATFORM_ID } from '@angular/core';
import { Title, Meta } from '@angular/platform-browser';
import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { SupportedLanguage } from './language.service';
import { ViewName, slugFor } from '../routing/page-routes';

// Change it to the real site domain (used on the server when window is not available)
const SITE_URL = 'https://limousine.example.com';

type PageMeta = Record<SupportedLanguage, { title: string; description: string }>;

const PAGE_META: Record<ViewName, PageMeta> = {
  home: {
    en: { title: "Oudy | Exclusive VIP Chauffeur & Executive Travel", description: "Experience extraordinary journeys with our luxury fleet, executive limousines, and 24/7 dedicated chauffeurs." },
    fr: { title: "Oudy | Service Chauffeur VIP & Transport Exécutif", description: "Découvrez des voyages d'exception avec notre flotte de luxe, limousines exécutives et chauffeurs dédiés 24h/24 et 7j/7." }
  },
  airport: {
    en: { title: "Airport Transfers | Private Chauffeur Pickup | Oudy", description: "Private airport pickup and drop-off with a professional chauffeur, flight tracking and 24/7 service." },
    fr: { title: "Transferts aéroport | Chauffeur privé | Oudy", description: "Prise en charge et dépose privées à l'aéroport avec chauffeur professionnel, suivi de vol et service 24h/24." }
  },
  experiences: {
    en: { title: "Private City Experiences in Montréal | Oudy", description: "Discover Montréal in comfort with a private chauffeur and a tailored city experience." },
    fr: { title: "Expériences privées en ville à Montréal | Oudy", description: "Découvrez Montréal avec confort grâce à un chauffeur privé et une expérience sur mesure." }
  },
  cityToCity: {
    en: { title: "City-to-City Transfers from Montréal | Oudy", description: "Comfortable private transfers from Montréal to other cities with an experienced chauffeur and a premium vehicle." },
    fr: { title: "Transferts entre villes depuis Montréal | Oudy", description: "Transferts privés confortables de Montréal vers d'autres villes avec chauffeur expérimenté et véhicule haut de gamme." }
  },
  vip: {
    en: { title: "Private Chauffeur Service in Montréal | Oudy", description: "A private chauffeur for weddings, graduations, events, shopping and long days, with you from start to finish." },
    fr: { title: "Service de chauffeur privé à Montréal | Oudy", description: "Un chauffeur privé pour mariages, remises de diplômes, événements, magasinage et longues journées, avec vous du début à la fin." }
  },
  airportPickup: {
    en: { title: "Airport Pickup in Montréal | Oudy", description: "Your chauffeur waits at arrivals and drives you from the airport to your destination." },
    fr: { title: "Prise en charge à l'aéroport à Montréal | Oudy", description: "Votre chauffeur vous attend aux arrivées et vous conduit de l'aéroport à votre destination." }
  },
  airportDropoff: {
    en: { title: "Airport Drop-off in Montréal | Oudy", description: "A private ride from your door to the airport terminal, on time and without stress." },
    fr: { title: "Dépose à l'aéroport à Montréal | Oudy", description: "Un trajet privé de votre porte jusqu'au terminal de l'aéroport, à l'heure et sans stress." }
  },
 
montreal: {
  en: { title: "Montréal City Tour | Private Chauffeur Tours | Oudy", description: "Book a private Montréal city tour with a professional chauffeur." },
  fr: { title: "Visite de la ville de Montréal | Chauffeur privé | Oudy", description: "Réservez une visite privée de Montréal avec un chauffeur professionnel." }
},
  ottawa: {
    en: { title: "Montréal to Ottawa Private Transfer | Oudy", description: "Private chauffeur transfer from Montréal to Ottawa, door to door." },
    fr: { title: "Transfert privé Montréal – Ottawa | Oudy", description: "Transfert privé avec chauffeur de Montréal à Ottawa, de porte à porte." }
  },
  quebec: {
    en: { title: "Montréal to Québec City Private Transfer | Oudy", description: "Private chauffeur transfer from Montréal to Québec City, door to door." },
    fr: { title: "Transfert privé Montréal – Québec | Oudy", description: "Transfert privé avec chauffeur de Montréal à Québec, de porte à porte." }
  },
  toronto: {
    en: { title: "Montréal to Toronto Private Transfer | Oudy", description: "Private chauffeur transfer from Montréal to Toronto, door to door." },
    fr: { title: "Transfert privé Montréal – Toronto | Oudy", description: "Transfert privé avec chauffeur de Montréal à Toronto, de porte à porte." }
  },
  tremblant: {
  en: { title: "Montréal to Mont-Tremblant Private Transfer | Oudy", description: "Private chauffeur transfer from Montréal to Mont-Tremblant, door to door." },
  fr: { title: "Transfert privé Montréal – Mont-Tremblant | Oudy", description: "Transfert privé avec chauffeur de Montréal à Mont-Tremblant, de porte à porte." }
},
cityOther: {
  en: { title: "Private Transfer to Any City from Montréal | Oudy", description: "Traveling to another city? Tell us your destination and we will arrange your private chauffeur ride." },
  fr: { title: "Transfert privé vers toute ville depuis Montréal | Oudy", description: "Vous allez dans une autre ville ? Indiquez-nous votre destination et nous organisons votre trajet privé avec chauffeur." }
},
  weddings: {
    en: { title: "Wedding Chauffeur in Montréal | Oudy", description: "Arrive in style on your wedding day with a private, elegant chauffeur." },
    fr: { title: "Chauffeur de mariage à Montréal | Oudy", description: "Arrivez avec style le jour de votre mariage avec un chauffeur privé et élégant." }
  },
  graduations: {
    en: { title: "Graduation Chauffeur in Montréal | Oudy", description: "A private chauffeur for your graduation day and the celebration that follows." },
    fr: { title: "Chauffeur pour remise de diplômes à Montréal | Oudy", description: "Un chauffeur privé pour votre remise de diplômes et la fête qui suit." }
  },
  events: {
    en: { title: "Event & Party Chauffeur in Montréal | Oudy", description: "A private chauffeur for events and parties, so you can enjoy the night." },
    fr: { title: "Chauffeur pour événements et fêtes à Montréal | Oudy", description: "Un chauffeur privé pour vos événements et fêtes, pour profiter de la soirée." }
  },
  shopping: {
    en: { title: "Shopping & Personal Errands Chauffeur | Oudy", description: "Your chauffeur stays with you for shopping and errands, waits and carries the bags." },
    fr: { title: "Chauffeur magasinage et courses personnelles | Oudy", description: "Votre chauffeur reste avec vous pour le magasinage et les courses, attend et porte les sacs." }
  },
  hourly: {
    en: { title: "Chauffeur by the Hour for Business | Oudy", description: "A private chauffeur at your disposal by the hour for meetings and full business days." },
    fr: { title: "Chauffeur à l'heure pour affaires | Oudy", description: "Un chauffeur privé à votre disposition à l'heure pour réunions et journées d'affaires complètes." }
  },
  bizOverview: {
    en: { title: "Chauffeur Services for Business | Oudy", description: "Premium chauffeur solutions for corporations, agencies and partners." },
    fr: { title: "Services de chauffeur pour entreprises | Oudy", description: "Solutions de chauffeur haut de gamme pour sociétés, agences et partenaires." }
  },
  bizCorporations: {
    en: { title: "Corporate Chauffeur Services | Oudy", description: "Reliable executive transportation for corporate travel, clients and teams." },
    fr: { title: "Chauffeur pour sociétés | Oudy", description: "Transport exécutif fiable pour les déplacements professionnels, clients et équipes." }
  },
  bizAgencies: {
    en: { title: "Chauffeur Services for Travel Agencies | Oudy", description: "Dependable ground transportation for travel and event agencies and their guests." },
    fr: { title: "Chauffeur pour agences de voyage | Oudy", description: "Transport terrestre fiable pour les agences de voyage et d'événements et leurs invités." }
  },
  bizPartners: {
    en: { title: "Partnerships | Oudy", description: "Partner with Oudy to offer premium chauffeur services to your customers." },
    fr: { title: "Partenariats | Oudy", description: "Devenez partenaire d'Oudy pour offrir des services de chauffeur haut de gamme à vos clients." }
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

  /** Title + description + canonical + hreflang for one page in one language */
  updateSeo(lang: SupportedLanguage, view: ViewName): void {
    const meta = (PAGE_META[view] ?? PAGE_META.home)[lang];

    this.titleService.setTitle(meta.title);
    this.metaService.updateTag({ name: 'description', content: meta.description });
    this.metaService.updateTag({ property: 'og:title', content: meta.title });
    this.metaService.updateTag({ property: 'og:description', content: meta.description });
    this.metaService.updateTag({ property: 'og:locale', content: lang === 'fr' ? 'fr_CA' : 'en_CA' });

    this.updateLinkTags(lang, view);
  }

  /** For any other component that still calls the old function (it is treated as the home page) */
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