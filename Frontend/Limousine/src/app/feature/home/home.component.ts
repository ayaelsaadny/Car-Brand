import { Component, OnInit, OnDestroy, AfterViewInit, HostListener, inject, signal, computed, effect, PLATFORM_ID } from '@angular/core';
import { CommonModule, DecimalPipe, isPlatformBrowser } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { PublicService } from '../../core/services/public.service';
import { AuthService } from '../../core/services/auth.service';
import { LanguageService } from '../../core/services/language.service';
import { SeoService } from '../../core/services/seo.service';
import { ViewName, linkFor, viewForPath } from '../../core/routing/page-routes';
import { RevealDirective } from './reveal.directive';
import { Vehicle, ServiceItem, Category, Review, CreateReviewRequest } from '../../core/interfaces/api.models';

interface MenuItem { view: ViewName; titleKey: string; descKey: string; pageDescKey?: string; image: string; btnKey?: string; }
type GroupKey = 'airport' | 'experiences' | 'cityToCity' | 'vip';
interface MenuGroup { key: GroupKey; titleKey: string; items: MenuItem[]; }
interface ChooseCard { view: ViewName; titleKey: string; descKey: string; btnKey: string; image: string; }

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    DecimalPipe,
    TranslocoPipe,
    RevealDirective
  ],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css', './journey.css', './service-page.css', './nav-menu.css', './choose-panels.css']
})
export class HomeComponent implements OnInit, AfterViewInit, OnDestroy {
  private readonly publicService = inject(PublicService);
  readonly authService = inject(AuthService);
  readonly langService = inject(LanguageService);
  private readonly transloco = inject(TranslocoService);
  private readonly seoService = inject(SeoService);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  // ===== Navbar: transparent over the hero, turns white after scrolling =====
  scrolled = signal(false);
  showTop = signal(false);
  scrollTop(): void { if (isPlatformBrowser(this.platformId)) window.scrollTo({ top: 0, behavior: 'smooth' }); }

  @HostListener('window:scroll')
  onWindowScroll(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    this.scrolled.set(window.scrollY > 40);
    this.showTop.set(window.scrollY > 700);
    this.updateJourney();
  }

  // ===== Services dropdown (small, opens on click; Montréal only) =====
  megaOpen = signal(false);
  activeGroup = signal<GroupKey | null>(null);

  // ===== Page views: now taken from the URL (e.g. /en/vip-chauffeur, /fr/chauffeur-vip) =====
  // The URL → view mapping lives in core/routing/page-routes.ts
  private readonly urlSegments = toSignal(this.route.url, { initialValue: this.route.snapshot.url });
  readonly view = computed<ViewName>(
    () => viewForPath(this.urlSegments().map(s => s.path).join('/')) ?? 'home'
  );

  /** Router link (array) for a page in the current language, e.g. link('vip') */
  link(v: ViewName): string[] { return linkFor(v, this.langService.currentLang()); }

  // The 4 services under the hero ("Luxury in Every Journey"). To add one: add an item here + its texts (choose.cNTitle / cNDesc / cNBtn) in en.json and fr.json.
  // Photos: public/assets/menu/
  readonly chooseCards: ChooseCard[] = [
    { view: 'airport',     titleKey: 'choose.c1Title', descKey: 'choose.c1Desc', btnKey: 'choose.c1Btn', image: 'assets/menu/airport.jpg' },
    { view: 'experiences', titleKey: 'choose.c2Title', descKey: 'choose.c2Desc', btnKey: 'choose.c2Btn', image: 'assets/menu/city.jpg' },
    { view: 'cityToCity',  titleKey: 'choose.c3Title', descKey: 'choose.c3Desc', btnKey: 'choose.c3Btn', image: 'assets/menu/city-to-city.jpg' },
    { view: 'vip',         titleKey: 'choose.c4Title', descKey: 'choose.c4Desc', btnKey: 'choose.c4Btn', image: 'assets/menu/private-chauffeur.jpg' }
  ];

  // Put the photos in public/assets/menu/ (airport.jpg, city.jpg, vip.jpg, montreal.jpg)
  readonly groups: MenuGroup[] = [
    {
      key: 'airport',
      titleKey: 'megaMenu.airport',
      items: [
        { view: 'airportPickup',  titleKey: 'sections.airportPickup',  descKey: 'sections.airportPickupDesc',  image: 'assets/menu/airport-pickup.jpg' },
        { view: 'airportDropoff', titleKey: 'sections.airportDropoff', descKey: 'sections.airportDropoffDesc', image: 'assets/menu/airport-dropoff.jpg' }
      ]
    },
    {
      key: 'experiences',
      titleKey: 'megaMenu.experiences',
      items: [
        { view: 'montreal', titleKey: 'sections.montrealCity', descKey: 'sections.montrealCityDesc', image: 'assets/menu/montreal.jpg', btnKey: 'megaMenu.exploreMontreal' }
      ]
    },
    {
      key: 'cityToCity',
      titleKey: 'megaMenu.cityToCity',
      items: [
        { view: 'ottawa',  titleKey: 'sections.ottawa',  descKey: 'sections.ottawaDesc',  image: 'assets/menu/intercity-ride.jpg' },
        { view: 'quebec',  titleKey: 'sections.quebec',  descKey: 'sections.quebecDesc',  image: 'assets/menu/quebec-city.jpg' },
        { view: 'toronto', titleKey: 'sections.toronto', descKey: 'sections.torontoDesc', image: 'assets/menu/toronto.jpg' },
        { view: 'tremblant', titleKey: 'sections.tremblant', descKey: 'sections.tremblantDesc', image: 'assets/menu/mont-tremblant.jpg' },
        { view: 'cityOther', titleKey: 'sections.cityOther', descKey: 'sections.cityOtherDesc', image: 'assets/menu/other-city.jpg', btnKey: 'common.bookNow' }
      ]
    },
    {
      key: 'vip',
      titleKey: 'megaMenu.vip',
      items: [
        { view: 'weddings',    titleKey: 'sections.weddings',    descKey: 'sections.weddingsDesc',    image: 'assets/menu/wedding.jpg' },
        { view: 'graduations', titleKey: 'sections.graduations', descKey: 'sections.graduationsDesc', image: 'assets/menu/graduations.jpg' },
        { view: 'events',      titleKey: 'sections.events',      descKey: 'sections.eventsDesc',      image: 'assets/menu/events-parties.jpg' },
        { view: 'shopping',    titleKey: 'sections.shopping',    descKey: 'sections.shoppingDesc',    image: 'assets/menu/shopping-errands.jpg' },
        { view: 'hourly',      titleKey: 'sections.hourly',      descKey: 'sections.hourlyDesc',      image: 'assets/menu/business-hourly.jpg' }
      ]
    }
  ];

  // "For Business" dropdown (4 pages). Photos: public/assets/menu/business.jpg, corporations.jpg, agencies.jpg, partnerships.jpg
  readonly businessItems: MenuItem[] = [
    { view: 'bizOverview',     titleKey: 'business.overview',     descKey: 'business.overviewDesc',     image: 'assets/menu/business.jpg' },
    { view: 'bizCorporations', titleKey: 'business.corporations', descKey: 'business.corporationsDesc', image: 'assets/menu/corporations.jpg' },
    { view: 'bizAgencies',     titleKey: 'business.agencies',     descKey: 'business.agenciesDesc',     image: 'assets/menu/agencies.jpg' },
    { view: 'bizPartners',     titleKey: 'business.partnerships', descKey: 'business.partnershipsDesc', image: 'assets/menu/partnerships.jpg' }
  ];
  readonly isBizView = computed(() => this.view().startsWith('biz'));
  bizOpen = signal(false);

  // flat list for the mobile menu
  readonly mobileServiceItems: MenuItem[] = this.groups.flatMap((g: MenuGroup) => g.items);
  mobileSection = signal<'services' | 'business' | null>(null);
  toggleMobileSection(k: 'services' | 'business'): void {
    this.mobileSection.update(cur => (cur === k ? null : k));
  }

  currentItem = computed(() => [...this.groups.flatMap((g: MenuGroup) => g.items), ...this.businessItems].find(i => i.view === this.view()) ?? null);

  /** The service a section page belongs to (null on service pages, business pages and home) */
  readonly parentGroup = computed<MenuGroup | null>(() => this.groups.find(g => g.items.some(i => i.view === this.view())) ?? null);

  /** Title of the page (section title, or the service title on a service page) */
  readonly pageTitleKey = computed(() => this.currentItem()?.titleKey ?? this.groups.find(g => g.key === this.view())?.titleKey ?? 'megaMenu.airport');

  // ===== Booking request form (Airport / City-to-City / VIP pages) =====
  // The request opens WhatsApp with the details already written (no server needed).
  // Sections of Airport / City-to-City / Private Chauffeur show the booking form. Private City Experiences does not.
  readonly isBookable = computed(() => { const g = this.parentGroup(); return !!g && g.key !== 'experiences'; });
  readonly minDate = new Date().toISOString().slice(0, 10);   // earliest date the calendar allows (today)
  booking = { name: '', email: '', phone: '', from: '', to: '', date: '', time: '', passengers: 1, message: '' };
  bookingSent = signal(false);

  scrollToBooking(ev?: Event): void {
    ev?.preventDefault();
    if (isPlatformBrowser(this.platformId)) document.getElementById('booking')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  submitBooking(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    const b = this.booking;
    const item = this.currentItem();
    const service = item ? this.transloco.translate(item.titleKey) : '';
    const text = [
      `New booking request: ${service}`,
      `Name: ${b.name}`,
      `Phone: ${b.phone}`,
      b.email && `Email: ${b.email}`,
      `From: ${b.from}`,
      `To: ${b.to}`,
      b.date && `Date: ${b.date}`,
      b.time && `Time: ${b.time}`,
      b.passengers && `Passengers: ${b.passengers}`,
      b.message && `Message: ${b.message}`
    ].filter(Boolean).join('\n');
    window.open(`${this.contact.whatsappUrl}?text=${encodeURIComponent(text)}`, '_blank', 'noopener');
    this.bookingSent.set(true);
  }

  // ===== Journey (road + pins + car) for the service pages =====
  // To add a new card: add an item to groups[x].items above. The road, the pins and the car grow by themselves.
  readonly isMobile = signal(false);   // true on phones / small tablets (the road is drawn differently)
  private readonly ROAD_SWAY = 7;   // how far the road leans left / right (in % of the width)

  /** The cards of the current page (empty on pages that are not a journey) */
  readonly journeyItems = computed<MenuItem[]>(() => {
    const v = this.view();
    return this.groups.find(g => g.key === v)?.items ?? [];
  });

  /** x position (in % of the width) of the pin of card i: left, right, left, right... */
  pinX(i: number): number {
    // phone: the road runs on the right of the 1st card, on the left of the 2nd, and so on
    if (this.isMobile()) return i % 2 === 0 ? 87 : 13;
    return 50 + (i % 2 === 0 ? this.ROAD_SWAY : -this.ROAD_SWAY);
  }

  /** x of the road at height y. One card = 100 units, the pin of card i sits at y = i * 100 + 50 */
  private roadX(y: number, n: number): number {
    if (y <= 50) return this.pinX(0);
    if (y >= n * 100 - 50) return this.pinX(n - 1);
    const t = (y - 50) / 100;
    const i = Math.floor(t);
    const u = t - i;
    // desktop: a gentle S-curve over the whole distance
    // phone: a soft, wide wave (starts bending early, ends late, so there are no sharp turns)
    const w = this.isMobile() ? Math.min(1, Math.max(0, (u - 0.12) / 0.76)) : u;
    const s = this.isMobile() ? (1 - Math.cos(Math.PI * w)) / 2 : w * w * (3 - 2 * w);
    return this.pinX(i) + (this.pinX(i + 1) - this.pinX(i)) * s;
  }

  /** The SVG path of the road (drawn in a 100 x (n*100) box) */
  readonly journeyPath = computed(() => {
    const n = this.journeyItems().length;
    if (!n) return '';
    let d = `M ${this.roadX(0, n)} 0`;
    for (let y = 2; y <= n * 100; y += 2) d += ` L ${this.roadX(y, n).toFixed(2)} ${y}`;
    return d;
  });

  /** Car state: x (% of width), p (0..1 along the road), rot (degrees) */
  readonly journeyCar = signal({ x: 50, p: 0, rot: 0 });
  private lastP = 0;
  private goingDown = true;

  private updateJourney(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    const el = document.getElementById('journey');
    const n = this.journeyItems().length;
    if (!el || !n) return;
    const r = el.getBoundingClientRect();
    if (!r.height) return;

    // the car sits where the middle of the screen is, along the road
    const p = Math.min(1, Math.max(0, (window.innerHeight * 0.55 - r.top) / r.height));
    if (Math.abs(p - this.lastP) > 0.0005) { this.goingDown = p >= this.lastP; this.lastP = p; }

    const y = p * n * 100;
    const x = this.roadX(y, n);
    // direction of the road in real pixels, so the car turns with the curve
    const dxPx = ((this.roadX(y + 1, n) - this.roadX(y - 1, n)) / 100) * r.width;
    const dyPx = (2 / (n * 100)) * r.height;
    const heading = (Math.atan2(dxPx, dyPx) * 180) / Math.PI;
    this.journeyCar.set({ x, p, rot: this.goingDown ? -heading : 180 - heading });
  }

  toggleBiz(): void {
    this.bizOpen.update(v => !v);
    this.megaOpen.set(false);
    this.activeGroup.set(null);
  }

  toggleMega(): void {
    this.bizOpen.set(false);
    this.megaOpen.update(v => !v);
    if (!this.megaOpen()) this.activeGroup.set(null);
  }
  closeMega(): void { this.megaOpen.set(false); this.bizOpen.set(false); this.activeGroup.set(null); }
  /** Click a group => its small sub-menu opens (click again to close it) */
  toggleGroup(k: GroupKey): void {
    this.activeGroup.update(cur => (cur === k ? null : k));
  }

  /** Click on a service: opens its page (the sections open on hover). On touch screens there is no hover, so the first tap opens the sections and the second tap opens the page. */
  onGroupClick(k: GroupKey, ev?: Event): void {
    const touch = isPlatformBrowser(this.platformId) && !!window.matchMedia?.('(hover: none)').matches;
    if (touch && this.activeGroup() !== k) {
      ev?.preventDefault();
      ev?.stopPropagation();
      this.activeGroup.set(k);
      return;
    }
    this.openView(k, ev);
  }

  /** Closes the dropdowns and the mobile menu (used after clicking a link) */
  closeNav(): void {
    this.closeMega();
    this.isMobileMenuOpen.set(false);
  }

  /** Click anywhere outside the dropdown => close it */
  @HostListener('document:click')
  onDocumentClick(): void { this.closeMega(); }

  /** Opens a page by navigating to its real URL (back button + sharing work) */
  openView(v: ViewName, ev?: Event): void {
    ev?.preventDefault();
    this.closeNav();
    this.router.navigate(this.link(v));
  }

  /** Nav anchors: go back to the home page, then scroll to the section */
  goTo(id: string, ev?: Event): void {
    ev?.preventDefault();
    this.closeNav();
    const scroll = () => {
      if (!isPlatformBrowser(this.platformId)) return;
      setTimeout(() => {
        if (id === 'top') window.scrollTo({ top: 0, behavior: 'smooth' });
        else document.getElementById(id)?.scrollIntoView({ behavior: 'smooth' });
      }, 30);
    };
    if (this.view() === 'home') scroll();
    else this.router.navigate(this.link('home')).then(scroll);
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.showWriteReviewModal()) {
      this.closeWriteReviewModal();
    } else if (this.showWelcomeModal()) {
      this.closeWelcomeModal();
    } else if (this.showAuthModal()) {
      this.closeAuthModal();
    } else {
      this.closeMega();
    }
  }

  // ===== Language: ONE button. Shows current language (EN first), click => switches to the other =====
  readonly otherLang = computed(() => (this.langService.currentLang() === 'en' ? 'fr' : 'en'));

  toggleLanguage(): void {
    // Stays on the same page and uses the translated URL (vip-chauffeur → chauffeur-vip)
    this.router.navigate(linkFor(this.view(), this.otherLang()), { queryParamsHandling: 'preserve' });
  }

  // ===== Hero slider =====
  // Put your photos in public/assets/hero/ named hero-1.jpg, hero-2.jpg, hero-3.jpg
  // (until you add them, the matching .svg illustration is shown instead)
  readonly heroSlides = [
    { titleKey: 'hero.s1T', subKey: 'hero.s1S', targetId: 'choose', image: 'assets/hero/hero-1.jpg' },
    { titleKey: 'hero.s2T', subKey: 'hero.s2S', targetId: 'choose', image: 'assets/hero/hero-2.jpg' },
    { titleKey: 'hero.s3T', subKey: 'hero.s3S', targetId: 'choose', image: 'assets/hero/hero-3.jpg' },
    { titleKey: 'hero.s4T', subKey: 'hero.s4S', targetId: 'choose', image: 'assets/hero/hero-4.jpg' },
    { titleKey: 'hero.s5T', subKey: 'hero.s5S', targetId: 'choose', image: 'assets/hero/hero-5.jpg' },
    { titleKey: 'hero.s6T', subKey: 'hero.s6S', targetId: 'choose', image: 'assets/hero/hero-6.jpg' }
  ];
  // false until the page is on screen, so the first slide also plays its zoom-in
  heroReady = signal(false);
  currentSlide = signal(0);
  private sliderTimer: ReturnType<typeof setInterval> | null = null;

  // ===== Reviews (Loaded dynamically from database) =====
  readonly reviews = signal<Review[]>([]);
  currentReview = signal(0);
  private reviewTimer: ReturnType<typeof setInterval> | null = null;

  // How many review cards are visible at once (3 desktop / 2 tablet / 1 mobile)
  perView = signal(3);
  private maxReviewIndex = computed(() => Math.max(0, this.reviews().length - this.perView()));
  reviewDots = computed(() => Array.from({ length: this.maxReviewIndex() + 1 }, (_, i) => i));

  // Write Review Modal State
  showWriteReviewModal = signal(false);
  reviewRating = signal(5);
  reviewHoverRating = signal(0);
  reviewAuthorName = signal('');
  reviewCity = signal('');
  reviewComment = signal('');
  submittingReview = signal(false);
  reviewSuccessMessage = signal<string | null>(null);
  reviewErrorMessage = signal<string | null>(null);

  setRating(stars: number): void {
    this.reviewRating.set(stars);
  }

  setHoverRating(stars: number): void {
    this.reviewHoverRating.set(stars);
  }

  openWriteReviewModal(): void {
    this.reviewRating.set(5);
    this.reviewHoverRating.set(0);
    this.reviewComment.set('');
    this.reviewSuccessMessage.set(null);
    this.reviewErrorMessage.set(null);

    const user = this.authService.currentUser();
    if (user) {
      this.reviewAuthorName.set(user.fullName);
    } else {
      this.reviewAuthorName.set('');
      this.reviewCity.set('');
    }

    this.showWriteReviewModal.set(true);
  }

  closeWriteReviewModal(): void {
    this.showWriteReviewModal.set(false);
    this.reviewSuccessMessage.set(null);
    this.reviewErrorMessage.set(null);
  }

  onSubmitReview(): void {
    const user = this.authService.currentUser();
    let name = this.reviewAuthorName().trim();
    if (!user && !name) {
      this.reviewErrorMessage.set(this.transloco.translate('reviews.nameRequiredError'));
      return;
    }
    if (user && !name) {
      name = user.fullName;
    }

    const rating = this.reviewRating();
    if (rating < 1 || rating > 5) {
      this.reviewErrorMessage.set(this.transloco.translate('reviews.selectRatingError'));
      return;
    }

    this.submittingReview.set(true);
    this.reviewErrorMessage.set(null);

    const payload: CreateReviewRequest = {
      rating,
      comment: this.reviewComment().trim() || undefined,
      authorName: name,
      city: this.reviewCity().trim() || undefined
    };

    this.publicService.submitReview(payload).subscribe({
      next: () => {
        this.submittingReview.set(false);
        this.reviewSuccessMessage.set(this.transloco.translate('reviews.successMsg'));
        setTimeout(() => {
          this.closeWriteReviewModal();
        }, 3200);
      },
      error: (err) => {
        this.submittingReview.set(false);
        this.reviewErrorMessage.set(err?.error?.message || 'Error submitting review. Please try again.');
      }
    });
  }

  @HostListener('window:resize')
  onResize(): void { this.updatePerView(); this.updateJourney(); }

  private updatePerView(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    const w = window.innerWidth;
    this.isMobile.set(w <= 860);
    this.perView.set(w <= 640 ? 1 : w <= 992 ? 2 : 3);
    if (this.currentReview() > this.maxReviewIndex()) this.currentReview.set(this.maxReviewIndex());
  }

  nextReview(): void {
    this.currentReview.update(i => (i >= this.maxReviewIndex() ? 0 : i + 1));
    this.restartReviews();
  }
  prevReview(): void {
    this.currentReview.update(i => (i <= 0 ? this.maxReviewIndex() : i - 1));
    this.restartReviews();
  }
  goToReview(i: number): void {
    this.currentReview.set(i);
    this.restartReviews();
  }
  pauseReviews(): void { if (this.reviewTimer) { clearInterval(this.reviewTimer); this.reviewTimer = null; } }
  resumeReviews(): void { this.restartReviews(); }
  private restartReviews(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    if (this.reviewTimer) clearInterval(this.reviewTimer);
    this.reviewTimer = setInterval(() => this.currentReview.update(i => (i >= this.maxReviewIndex() ? 0 : i + 1)), 6000);
  }

  // ===== Stats (count-up animation when the section scrolls into view) =====
  // PLACEHOLDER numbers: replace with Oudy's real figures.
  readonly stats = [
    { target: 500, suffix: '+', labelKey: 'stats.rides',     decimals: false },
    { target: 120, suffix: '+', labelKey: 'stats.crossCity', decimals: false },
    { target: 99,  suffix: '%', labelKey: 'stats.onTime',    decimals: false },
    { target: 200, suffix: '+', labelKey: 'stats.tours',     decimals: false }
  ];

  statValues = signal<number[]>(this.stats.map(() => 0));
  private statsPlayed = false;
  private statsRaf = 0;
  private observer: IntersectionObserver | null = null;

  ngAfterViewInit(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    const el = document.getElementById('stats');
    const reduce = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
    if (!el || reduce || !('IntersectionObserver' in window)) {
      this.statValues.set(this.stats.map(s => s.target));
      return;
    }
    // Counts once, when the section first appears, then stops on the final numbers.
    this.observer = new IntersectionObserver(entries => {
      if (entries.some(e => e.isIntersecting) && !this.statsPlayed) {
        this.statsPlayed = true;
        this.playStats();
        this.observer?.disconnect();
      }
    }, { threshold: 0.3 });
    this.observer.observe(el);
  }

  private playStats(): void {
    const duration = 9000;   // total count time in ms. Bigger number = slower.
    const start = performance.now();
    const tick = (now: number) => {
      const p = Math.min((now - start) / duration, 1);
      const eased = 1 - Math.pow(1 - p, 2);   // gentle slow-down at the end
      this.statValues.set(this.stats.map(s => s.decimals ? Math.round(s.target * eased * 10) / 10 : Math.round(s.target * eased)));
      if (p < 1) this.statsRaf = requestAnimationFrame(tick);
    };
    this.statsRaf = requestAnimationFrame(tick);
  }

  // ===== Contact (edit here) =====
  readonly contact = {
    email: 'Mo.waheed80@gmail.com',
    whatsappUser: 'mo.waheed',
    // wa.me needs a phone number (country code + number, digits only), not a username
    whatsappUrl: 'https://wa.me/14389891189'
  };

  // ===== Our Car: ONE vehicle (the first one returned by the API) =====
  readonly carImage = 'assets/car/car.jpg';   // put your car photo here
  car = computed(() => this.vehicles()[0] ?? null);

  nextSlide(): void {
    this.currentSlide.update(i => (i + 1) % this.heroSlides.length);
    this.restartSlider();
  }
  prevSlide(): void {
    this.currentSlide.update(i => (i - 1 + this.heroSlides.length) % this.heroSlides.length);
    this.restartSlider();
  }
  goToSlide(i: number): void {
    this.currentSlide.set(i);
    this.restartSlider();
  }
  private restartSlider(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    if (this.sliderTimer) clearInterval(this.sliderTimer);
    this.sliderTimer = setInterval(() => this.currentSlide.update(i => (i + 1) % this.heroSlides.length), 7500);
  }
  ngOnDestroy(): void {
    if (this.sliderTimer) clearInterval(this.sliderTimer);
    if (this.reviewTimer) clearInterval(this.reviewTimer);
    this.observer?.disconnect();
    if (this.statsRaf) cancelAnimationFrame(this.statsRaf);
  }

  // Data from Public APIs
  services = signal<ServiceItem[]>([]);
  vehicles = signal<Vehicle[]>([]);
  categories = signal<Category[]>([]);
  settings = signal<Record<string, string>>({});

  loadingServices = signal(true);
  loadingVehicles = signal(true);
  loadingCategories = signal(true);

  // Auth Modal State
  showAuthModal = signal(false);
  authTab = signal<'login' | 'register'>('login');
  authError = signal<string | null>(null);
  authSuccess = signal<string | null>(null);
  authLoading = signal(false);

  // Welcome Promo Modal State
  showWelcomeModal = signal(false);

  closeWelcomeModal(): void {
    this.showWelcomeModal.set(false);
  }

  openRegisterFromWelcome(): void {
    this.closeWelcomeModal();
    this.openAuthModal('register');
  }

  openLoginFromWelcome(): void {
    this.closeWelcomeModal();
    this.openAuthModal('login');
  }

  // Mobile Dropdown Menu State
  readonly isMobileMenuOpen = signal<boolean>(false);

  toggleMobileMenu(): void {
    this.isMobileMenuOpen.update(open => !open);
  }

  closeMobileMenu(): void {
    this.isMobileMenuOpen.set(false);
  }

  // Form Models
  loginData = {
    email: '',
    password: ''
  };

  registerData = {
    fullName: '',
    email: '',
    phoneNumber: '',
    password: ''
  };

  constructor() {
    // Re-position the car when the page (view) changes
    effect(() => {
      this.view();
      if (isPlatformBrowser(this.platformId)) setTimeout(() => this.updateJourney(), 80);
    });

    // SEO (title, description, canonical, hreflang) follows the page AND the language
    effect(() => {
      this.seoService.updateSeo(this.langService.currentLang(), this.view());
    });

    // Public data is reloaded only when the language changes (not on every page change)
    effect(() => {
      this.langService.currentLang();
      this.loadPublicData();
    });
  }

  ngOnInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      setTimeout(() => this.heroReady.set(true), 60);
      if (!this.authService.isLoggedIn()) {
        setTimeout(() => this.showWelcomeModal.set(true), 4000);
      }
    }
    this.updatePerView();
    this.restartSlider();
    this.restartReviews();
  }

  loadPublicData(): void {
    this.loadingServices.set(true);
    this.loadingVehicles.set(true);
    this.loadingCategories.set(true);

    this.publicService.getServices().subscribe({
      next: (data) => {
        this.services.set(data);
        this.loadingServices.set(false);
      },
      error: () => this.loadingServices.set(false)
    });

    this.publicService.getVehicles().subscribe({
      next: (data) => {
        this.vehicles.set(data);
        this.loadingVehicles.set(false);
      },
      error: () => this.loadingVehicles.set(false)
    });

    this.publicService.getCategories().subscribe({
      next: (data) => {
        this.categories.set(data);
        this.loadingCategories.set(false);
      },
      error: () => this.loadingCategories.set(false)
    });

    this.publicService.getReviews().subscribe({
      next: (data) => {
        this.reviews.set(data);
        if (this.currentReview() > this.maxReviewIndex()) {
          this.currentReview.set(0);
        }
        this.restartReviews();
      }
    });

    this.publicService.getSettings().subscribe({
      next: (data) => {
        const dict: Record<string, string> = {};
        data.forEach(s => {
          if (s.key) dict[s.key] = s.value;
        });
        this.settings.set(dict);
      }
    });
  }

  openAuthModal(tab: 'login' | 'register' = 'login'): void {
    this.authTab.set(tab);
    this.authError.set(null);
    this.authSuccess.set(null);
    this.showAuthModal.set(true);
  }

  closeAuthModal(): void {
    this.showAuthModal.set(false);
    this.authError.set(null);
    this.authSuccess.set(null);
  }

  onLogin(): void {
    if (!this.loginData.email || !this.loginData.password) {
      this.authError.set(this.transloco.translate('auth.validationError'));
      return;
    }

    this.authLoading.set(true);
    this.authError.set(null);

    this.authService.login(this.loginData).subscribe({
      next: () => {
        this.authLoading.set(false);
        this.authSuccess.set(this.transloco.translate('auth.loginSuccess'));
        setTimeout(() => {
          this.closeAuthModal();
        }, 1200);
      },
      error: (err) => {
        this.authLoading.set(false);
        const msg = err?.error?.message || this.transloco.translate('auth.invalidCredentials');
        this.authError.set(msg);
      }
    });
  }

  onRegister(): void {
    if (!this.registerData.fullName || !this.registerData.email || !this.registerData.password) {
      this.authError.set(this.transloco.translate('auth.validationError'));
      return;
    }

    this.authLoading.set(true);
    this.authError.set(null);

    this.authService.register(this.registerData).subscribe({
      next: () => {
        this.authLoading.set(false);
        this.authSuccess.set(this.transloco.translate('auth.registerSuccess'));
        setTimeout(() => {
          this.closeAuthModal();
        }, 1200);
      },
      error: (err) => {
        this.authLoading.set(false);
        const msg = err?.error?.message || this.transloco.translate('auth.registerError');
        this.authError.set(msg);
      }
    });
  }

  logout(): void {
    this.authService.logout();
  }
}