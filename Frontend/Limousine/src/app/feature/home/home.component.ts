import { Component, OnInit, inject, signal, effect } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { PublicService } from '../../core/services/public.service';
import { AuthService } from '../../core/services/auth.service';
import { LanguageService } from '../../core/services/language.service';
import { SeoService } from '../../core/services/seo.service';
import { LanguageSwitcherComponent } from '../../shared/components/language-switcher/language-switcher.component';
import { Vehicle, ServiceItem, Category } from '../../core/interfaces/api.models';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    DecimalPipe,
    TranslocoPipe,
    LanguageSwitcherComponent
  ],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {
  private readonly publicService = inject(PublicService);
  readonly authService = inject(AuthService);
  readonly langService = inject(LanguageService);
  private readonly transloco = inject(TranslocoService);
  private readonly seoService = inject(SeoService);

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
    // Whenever current language changes, refresh SEO meta and reload localized public data
    effect(() => {
      const currentLang = this.langService.currentLang();
      this.seoService.updateSeoForLanguage(currentLang, `/${currentLang}`);
      this.loadPublicData();
    });
  }

  ngOnInit(): void {
    // Initial data load handled by effect
  }

  loadPublicData(): void {
    this.loadingServices.set(true);
    this.loadingVehicles.set(true);
    this.loadingCategories.set(true);

    // 1. Load Services (returns localized text according to current Accept-Language)
    this.publicService.getServices().subscribe({
      next: (data) => {
        this.services.set(data);
        this.loadingServices.set(false);
      },
      error: () => this.loadingServices.set(false)
    });

    // 2. Load Vehicles
    this.publicService.getVehicles().subscribe({
      next: (data) => {
        this.vehicles.set(data);
        this.loadingVehicles.set(false);
      },
      error: () => this.loadingVehicles.set(false)
    });

    // 3. Load Categories (returns localized text according to current Accept-Language)
    this.publicService.getCategories().subscribe({
      next: (data) => {
        this.categories.set(data);
        this.loadingCategories.set(false);
      },
      error: () => this.loadingCategories.set(false)
    });

    // 4. Load Website Settings
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

  fillAdminDemo(): void {
    this.loginData.email = 'admin@admin.com';
    this.loginData.password = 'admin123';
    this.onLogin();
  }

  logout(): void {
    this.authService.logout();
  }
}
