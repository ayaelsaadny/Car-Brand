import { Component, HostListener, computed, inject, input, output, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslocoService } from '@jsverse/transloco';
import { LanguageService } from '../../../core/services/language.service';
import { AuthService } from '../../../core/services/auth.service';
import { LanguageSwitcherComponent } from '../language-switcher/language-switcher.component';

export interface NavItem {
  label: string;
  link: string;
  icon?: string;
  iconClass?: string;
  iconKey?: 'globe' | 'dashboard' | 'vehicles' | 'bookings' | 'services' | 'categories' | 'users' | 'settings' | 'profile';
  badge?: string | number;
}

export interface LayoutUser {
  name: string;
  subtitle?: string;
  avatarUrl?: string;
}

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    LanguageSwitcherComponent
  ],
  templateUrl: './main-layout.html',
  styleUrls: ['./main-layout.css'],
})
export class MainLayoutComponent {
  private readonly languageService = inject(LanguageService);
  private readonly transloco = inject(TranslocoService);
  readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  appName = input<string>('Limousine');
  logoUrl = input<string | null>(null);
  navItems = input<NavItem[] | null>(null);
  user = input<LayoutUser | null>(null);
  dir = input<'rtl' | 'ltr'>('ltr');
  logoutLabel = input<string | null>(null);

  logout = output<void>();

  // Current active user for header display
  readonly currentUserDisplay = computed<LayoutUser>(() => {
    const custom = this.user();
    if (custom) return custom;

    const u = this.authService.currentUser();
    if (u) {
      return {
        name: u.fullName,
        subtitle: u.role
      };
    }
    return {
      name: 'Guest User',
      subtitle: 'Limousine Executive'
    };
  });

  // Reactive computed nav items that update dynamically based on user role and language
  readonly computedNavItems = computed<NavItem[]>(() => {
    const custom = this.navItems();
    if (custom && custom.length > 0) return custom;

    const lang = this.languageService.currentLang();
    const isAdmin = this.authService.isAdmin();
    const isEmployee = this.authService.isEmployee();
    const isCustomer = this.authService.isCustomer();

    const items: NavItem[] = [
      { label: this.transloco.translate('nav.publicSite'), link: `/${lang}`, iconKey: 'globe' }
    ];

    // Admin sees all operational pages + Users Management + Settings + Profile
    if (isAdmin) {
      items.push(
        { label: this.transloco.translate('nav.dashboard'), link: `/${lang}/dashboard`, iconKey: 'dashboard' },
        { label: this.transloco.translate('nav.vehicles'), link: `/${lang}/vehicles`, iconKey: 'vehicles' },
        { label: this.transloco.translate('nav.bookings'), link: `/${lang}/bookings`, iconKey: 'bookings' },
        { label: this.transloco.translate('nav.services'), link: `/${lang}/services`, iconKey: 'services' },
        { label: this.transloco.translate('nav.categories'), link: `/${lang}/categories`, iconKey: 'categories' },
        { label: this.transloco.translate('nav.users'), link: `/${lang}/users`, iconKey: 'users' },
        { label: this.transloco.translate('nav.settings'), link: `/${lang}/settings`, iconKey: 'settings' },
        { label: this.transloco.translate('nav.profile'), link: `/${lang}/account/profile`, iconKey: 'profile' }
      );
    } else if (isEmployee) {
      // Employee sees fleet operations but NOT Users management or Settings
      items.push(
        { label: this.transloco.translate('nav.dashboard'), link: `/${lang}/dashboard`, iconKey: 'dashboard' },
        { label: this.transloco.translate('nav.vehicles'), link: `/${lang}/vehicles`, iconKey: 'vehicles' },
        { label: this.transloco.translate('nav.bookings'), link: `/${lang}/bookings`, iconKey: 'bookings' },
        { label: this.transloco.translate('nav.services'), link: `/${lang}/services`, iconKey: 'services' },
        { label: this.transloco.translate('nav.categories'), link: `/${lang}/categories`, iconKey: 'categories' },
        { label: this.transloco.translate('nav.profile'), link: `/${lang}/account/profile`, iconKey: 'profile' }
      );
    } else if (isCustomer) {
      // Customer sees their bookings & customer profile
      items.push(
        { label: this.transloco.translate('nav.myBookings'), link: `/${lang}/customer/bookings`, iconKey: 'bookings' },
        { label: this.transloco.translate('nav.profile'), link: `/${lang}/customer/profile`, iconKey: 'profile' }
      );
    } else {
      // Unauthenticated fallback
      items.push(
        { label: this.transloco.translate('nav.login'), link: `/${lang}/auth/login`, iconKey: 'profile' }
      );
    }

    return items;
  });

  readonly computedLogoutLabel = computed<string>(() => {
    const custom = this.logoutLabel();
    if (custom) return custom;
    // Dependency on currentLang signal to recompute on language switch
    const _ = this.languageService.currentLang();
    return this.transloco.translate('nav.logout') || 'Logout';
  });

  sidebarOpen = signal(false);
  toggle() { this.sidebarOpen.update(v => !v); }
  close()  { this.sidebarOpen.set(false); }

  onLogout(): void {
    this.logout.emit();
    this.authService.logout();
    const lang = this.languageService.currentLang();
    this.router.navigateByUrl(`/${lang}`);
  }

  @HostListener('window:resize')
  onResize() { if (window.innerWidth > 992) this.close(); }

  @HostListener('document:keydown.escape')
  onEsc() { this.close(); }
}