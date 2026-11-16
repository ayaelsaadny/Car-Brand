import { Injectable, inject, signal, computed, PLATFORM_ID, effect } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { AdminBookingsService } from './admin-bookings.service';
import { AdminReviewsService } from './admin-reviews.service';
import { UsersService } from './users.service';
import { AuthService } from './auth.service';
import { BookingRequestItem, Review, UserItem } from '../interfaces/api.models';

@Injectable({
  providedIn: 'root'
})
export class AdminBadgeService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly adminBookingsService = inject(AdminBookingsService);
  private readonly adminReviewsService = inject(AdminReviewsService);
  private readonly usersService = inject(UsersService);

  readonly bookingsBadge = signal<number>(0);
  readonly reviewsBadge = signal<number>(0);
  readonly usersBadge = signal<number>(0);

  readonly totalBadge = computed(() => this.bookingsBadge() + this.reviewsBadge() + this.usersBadge());

  private pollInterval: ReturnType<typeof setInterval> | null = null;
  private latestBookings: BookingRequestItem[] = [];
  private latestReviews: Review[] = [];
  private latestUsers: UserItem[] = [];

  constructor() {
    if (isPlatformBrowser(this.platformId)) {
      // Listen to navigation events to immediately clear badge when entering a section
      this.router.events.pipe(
        filter(event => event instanceof NavigationEnd)
      ).subscribe((event: any) => {
        const url = (event as NavigationEnd).urlAfterRedirects || (event as NavigationEnd).url || '';
        this.handleRouteSeen(url);
      });

      // Initial route check on page boot
      setTimeout(() => {
        this.handleRouteSeen(this.router.url);
      }, 100);

      // Reactively start polling when staff or admin is logged in
      effect(() => {
        const isStaff = this.authService.isStaff();
        if (isStaff) {
          this.refresh();
          if (!this.pollInterval) {
            this.pollInterval = setInterval(() => this.refresh(), 20000);
          }
        } else {
          if (this.pollInterval) {
            clearInterval(this.pollInterval);
            this.pollInterval = null;
          }
          this.bookingsBadge.set(0);
          this.reviewsBadge.set(0);
          this.usersBadge.set(0);
        }
      });
    }
  }

  private handleRouteSeen(url: string): void {
    if (!url) return;
    if (url.includes('/bookings')) {
      this.markBookingsAsSeen();
    }
    if (url.includes('/reviews')) {
      this.markReviewsAsSeen();
    }
    if (url.includes('/users')) {
      this.markUsersAsSeen();
    }
  }

  refresh(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    if (!this.authService.isStaff()) return;

    const currentUrl = this.router.url;

    // 1. Check Bookings
    this.adminBookingsService.getAllBookings().subscribe({
      next: (res) => {
        this.latestBookings = res.data || [];
        this.updateBookingsBadge(currentUrl);
      },
      error: () => {}
    });

    // 2. Check Reviews (Admin only)
    if (this.authService.isAdmin()) {
      this.adminReviewsService.getAllReviews().subscribe({
        next: (res) => {
          this.latestReviews = res.data || [];
          this.updateReviewsBadge(currentUrl);
        },
        error: () => {}
      });

      // 3. Check Users (Admin only)
      this.usersService.getUsers().subscribe({
        next: (res) => {
          this.latestUsers = res.data || [];
          this.updateUsersBadge(currentUrl);
        },
        error: () => {}
      });
    }
  }

  private updateBookingsBadge(url?: string): void {
    if (!isPlatformBrowser(this.platformId)) return;
    const currentUrl = url || this.router.url;

    if (currentUrl.includes('/bookings')) {
      this.markBookingsAsSeen();
      return;
    }

    const seenTimeStr = localStorage.getItem('limousine_admin_seen_bookings_time');
    const seenMaxId = Number(localStorage.getItem('limousine_admin_seen_bookings_max_id') || 0);

    const pending = this.latestBookings.filter(b => {
      const st = String(b.statusName || b.status || '').toLowerCase();
      return st === 'pending' || st === '1';
    });

    if (!seenTimeStr) {
      this.bookingsBadge.set(pending.length);
    } else {
      const seenTime = new Date(seenTimeStr).getTime();
      const unseen = pending.filter(b => {
        const itemTime = b.createdAt ? new Date(b.createdAt).getTime() : 0;
        return (b.id && b.id > seenMaxId) || itemTime > seenTime;
      });
      this.bookingsBadge.set(unseen.length);
    }
  }

  markBookingsAsSeen(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    const maxId = this.latestBookings.reduce((max, b) => Math.max(max, b.id || 0), 0);
    localStorage.setItem('limousine_admin_seen_bookings_time', new Date().toISOString());
    if (maxId > 0) {
      localStorage.setItem('limousine_admin_seen_bookings_max_id', maxId.toString());
    }
    this.bookingsBadge.set(0);
  }

  private updateReviewsBadge(url?: string): void {
    if (!isPlatformBrowser(this.platformId)) return;
    const currentUrl = url || this.router.url;

    if (currentUrl.includes('/reviews')) {
      this.markReviewsAsSeen();
      return;
    }

    const seenTimeStr = localStorage.getItem('limousine_admin_seen_reviews_time');
    const seenMaxId = Number(localStorage.getItem('limousine_admin_seen_reviews_max_id') || 0);

    const pending = this.latestReviews.filter(r => !r.isApproved && !r.reviewedAt);

    if (!seenTimeStr) {
      this.reviewsBadge.set(pending.length);
    } else {
      const seenTime = new Date(seenTimeStr).getTime();
      const unseen = pending.filter(r => {
        const itemTime = r.createdAt ? new Date(r.createdAt).getTime() : 0;
        return (r.id && r.id > seenMaxId) || itemTime > seenTime;
      });
      this.reviewsBadge.set(unseen.length);
    }
  }

  markReviewsAsSeen(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    const maxId = this.latestReviews.reduce((max, r) => Math.max(max, r.id || 0), 0);
    localStorage.setItem('limousine_admin_seen_reviews_time', new Date().toISOString());
    if (maxId > 0) {
      localStorage.setItem('limousine_admin_seen_reviews_max_id', maxId.toString());
    }
    this.reviewsBadge.set(0);
  }

  private updateUsersBadge(url?: string): void {
    if (!isPlatformBrowser(this.platformId)) return;
    const currentUrl = url || this.router.url;

    if (currentUrl.includes('/users')) {
      this.markUsersAsSeen();
      return;
    }

    const seenTimeStr = localStorage.getItem('limousine_admin_seen_users_time');
    const seenMaxId = Number(localStorage.getItem('limousine_admin_seen_users_max_id') || 0);

    if (seenTimeStr) {
      const seenTime = new Date(seenTimeStr).getTime();
      const unseen = this.latestUsers.filter(u => {
        const itemTime = u.createdAt ? new Date(u.createdAt).getTime() : 0;
        return (u.id && u.id > seenMaxId) || itemTime > seenTime;
      });
      this.usersBadge.set(unseen.length);
    } else {
      this.usersBadge.set(0);
    }
  }

  markUsersAsSeen(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    const maxId = this.latestUsers.reduce((max, u) => Math.max(max, u.id || 0), 0);
    localStorage.setItem('limousine_admin_seen_users_time', new Date().toISOString());
    if (maxId > 0) {
      localStorage.setItem('limousine_admin_seen_users_max_id', maxId.toString());
    }
    this.usersBadge.set(0);
  }
}
