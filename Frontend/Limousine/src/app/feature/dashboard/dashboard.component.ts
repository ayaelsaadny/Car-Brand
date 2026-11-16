import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { LanguageService } from '../../core/services/language.service';
import { AdminBookingsService } from '../../core/services/admin-bookings.service';
import { PublicService } from '../../core/services/public.service';
import { BookingRequestItem, Vehicle } from '../../core/interfaces/api.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, DatePipe, RouterLink, TranslocoPipe],
  template: `
    <div class="page-container" dir="ltr">
      <div class="page-header">
        <div>
          <h1 class="page-title">{{ 'dashboard.title' | transloco }}</h1>
          <p class="page-subtitle">{{ 'dashboard.subtitle' | transloco }}</p>
        </div>
        <div class="header-actions">
          <a [routerLink]="['/' + langService.currentLang() + '/bookings']" [queryParams]="{ create: 'true' }" class="btn btn-primary">
            <span>{{ 'dashboard.newBooking' | transloco }}</span>
          </a>
        </div>
      </div>

      <!-- Quick Metrics -->
      <div class="stats-grid">
        <div class="stat-card">
          <div class="stat-icon gold">
            <svg class="icon-outline icon-outline-md" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M5 17h14M4 11l2.5-6h11L20 11M3 11h18v6H3z"/><circle cx="7" cy="17" r="2"/><circle cx="17" cy="17" r="2"/>
            </svg>
          </div>
          <div class="stat-info">
            <span class="stat-label">{{ 'dashboard.totalVehicles' | transloco }}</span>
            <strong class="stat-value">{{ totalVehiclesCount() }}</strong>
            <span class="stat-trend positive">{{ 'dashboard.newVehiclesTrend' | transloco }}</span>
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-icon success">
            <svg class="icon-outline icon-outline-md" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2"/>
              <rect x="8" y="2" width="8" height="4" rx="1" ry="1"/><path d="M9 12h6M9 16h6"/>
            </svg>
          </div>
          <div class="stat-info">
            <span class="stat-label">{{ 'dashboard.activeBookings' | transloco }}</span>
            <strong class="stat-value">{{ activeBookingsCount() }}</strong>
            <span class="stat-trend">{{ 'dashboard.today' | transloco }}</span>
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-icon info">
            <svg class="icon-outline icon-outline-md" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M6 3h12l4 6-10 12L2 9l4-6z"/>
              <path d="M2 9h20M10 3l-2 6 4 12 4-12-2-6"/>
            </svg>
          </div>
          <div class="stat-info">
            <span class="stat-label">{{ 'dashboard.availableServices' | transloco }}</span>
            <strong class="stat-value">6</strong>
            <span class="stat-trend">{{ 'dashboard.featuredServices' | transloco }}</span>
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-icon warning">
            <svg class="icon-outline icon-outline-md" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/>
            </svg>
          </div>
          <div class="stat-info">
            <span class="stat-label">{{ 'dashboard.rating' | transloco }}</span>
            <strong class="stat-value">4.9 / 5</strong>
            <span class="stat-trend positive">{{ 'dashboard.ratingTrend' | transloco }}</span>
          </div>
        </div>
      </div>

      <!-- Main Content Grid -->
      <div class="content-grid">
        <!-- Recent Bookings Table -->
        <div class="card">
          <div class="card-header">
            <h3>{{ 'dashboard.recentBookings' | transloco }}</h3>
            <a [routerLink]="['/' + langService.currentLang() + '/bookings']" class="card-link">{{ 'common.all' | transloco }} →</a>
          </div>
          <div class="table-responsive">
            <table class="data-table">
              <thead>
                <tr>
                  <th>{{ 'dashboard.bookingId' | transloco }}</th>
                  <th>{{ 'dashboard.customer' | transloco }}</th>
                  <th>{{ 'dashboard.vehicleType' | transloco }}</th>
                  <th>{{ 'dashboard.dateTime' | transloco }}</th>
                  <th>{{ 'dashboard.status' | transloco }}</th>
                </tr>
              </thead>
              <tbody>
                @if (recentBookings().length === 0) {
                  <tr>
                    <td colspan="5" style="text-align: center; color: #8c93a0; padding: 28px;">
                      {{ 'bookingsPage.noBookings' | transloco }}
                    </td>
                  </tr>
                } @else {
                  @for (b of recentBookings(); track b.id) {
                    <tr>
                      <td><strong>{{ b.bookingReference }}</strong></td>
                      <td>{{ b.customerName }}</td>
                      <td>{{ b.vehicleName || b.serviceName || 'Executive Chauffeur' }}</td>
                      <td>{{ b.pickupDate || (b.pickupDateTime | date:'shortDate') }} {{ b.pickupTime || '' }}</td>
                      <td>
                        <span class="badge" [ngClass]="getStatusBadgeClass(b.statusName || b.status)">
                          {{ b.statusName || b.status }}
                        </span>
                      </td>
                    </tr>
                  }
                }
              </tbody>
            </table>
          </div>
        </div>

        <!-- Fleet Overview List -->
        <div class="card">
          <div class="card-header">
            <h3>{{ 'dashboard.fleetOverview' | transloco }}</h3>
            <a [routerLink]="['/' + langService.currentLang() + '/vehicles']" class="card-link">{{ 'dashboard.manageFleet' | transloco }}</a>
          </div>
          <div class="fleet-list">
            <div class="fleet-item">
              <div class="fleet-avatar">
                <svg class="icon-outline icon-outline-lg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                  <path d="M5 17h14M4 11l2.5-6h11L20 11M3 11h18v6H3z"/><circle cx="7" cy="17" r="2"/><circle cx="17" cy="17" r="2"/>
                </svg>
              </div>
              <div class="fleet-details">
                <strong>Mercedes-Benz S-Class</strong>
                <small>{{ 'vehiclesPage.specsMercedes' | transloco }}</small>
              </div>
              <span class="badge badge-success">{{ 'common.available' | transloco }}</span>
            </div>

            <div class="fleet-item">
              <div class="fleet-avatar">
                <svg class="icon-outline icon-outline-lg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                  <rect x="3" y="8" width="18" height="9" rx="2"/><path d="M5 8l2-4h10l2 4"/><circle cx="7" cy="17" r="2"/><circle cx="17" cy="17" r="2"/>
                </svg>
              </div>
              <div class="fleet-details">
                <strong>Cadillac Escalade Platinum</strong>
                <small>{{ 'vehiclesPage.specsEscalade' | transloco }}</small>
              </div>
              <span class="badge badge-warning">{{ 'common.inTrip' | transloco }}</span>
            </div>

            <div class="fleet-item">
              <div class="fleet-avatar">
                <svg class="icon-outline icon-outline-lg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                  <path d="M4 18h16M5 15l2-8 5 4 5-4 2 8H5z"/>
                </svg>
              </div>
              <div class="fleet-details">
                <strong>Rolls-Royce Ghost</strong>
                <small>{{ 'vehiclesPage.specsGhost' | transloco }}</small>
              </div>
              <span class="badge badge-success">{{ 'common.available' | transloco }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .page-container {
      display: flex;
      flex-direction: column;
      gap: 24px;
    }
    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      flex-wrap: wrap;
      gap: 16px;
    }
    .page-title {
      font-family: var(--font-display, 'Orbitron', sans-serif);
      font-size: 1.75rem;
      font-weight: 700;
      color: #0a1f44;
      margin: 0 0 4px;
    }
    .page-subtitle {
      color: #5b6679;
      margin: 0;
      font-size: 0.95rem;
    }
    .btn {
      display: inline-flex;
      align-items: center;
      gap: 8px;
      padding: 10px 20px;
      border-radius: var(--radius-md);
      font-weight: 600;
      cursor: pointer;
      text-decoration: none;
      border: none;
      transition: all 0.2s ease;
    }
    .btn-primary {
      background: linear-gradient(135deg, #b3122a, #8f0e21);
      color: #ffffff;
      box-shadow: 0 4px 14px rgba(179, 18, 42, 0.25);
    }
    .btn-primary:hover {
      background: #8f0e21;
      box-shadow: 0 6px 18px rgba(179, 18, 42, 0.35);
    }
    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
      gap: 20px;
    }
    .stat-card {
      background: #ffffff;
      border: 1px solid #dfe6f1;
      border-radius: var(--radius-lg);
      padding: 20px;
      display: flex;
      align-items: center;
      gap: 16px;
      box-shadow: 0 4px 16px rgba(10, 31, 68, 0.05);
      transition: transform 0.2s ease, box-shadow 0.2s ease;
    }
    .stat-card:hover {
      transform: translateY(-2px);
      box-shadow: 0 8px 24px rgba(10, 31, 68, 0.08);
    }
    .stat-icon {
      width: 48px;
      height: 48px;
      border-radius: var(--radius-md);
      display: grid;
      place-items: center;
      font-size: 1.5rem;
    }
    .stat-icon.gold { background: rgba(179, 18, 42, 0.08); color: #b3122a; }
    .stat-icon.success { background: rgba(46, 157, 107, 0.12); color: #1e704a; }
    .stat-icon.info { background: rgba(27, 79, 156, 0.1); color: #1b4f9c; }
    .stat-icon.warning { background: rgba(224, 160, 48, 0.12); color: #b27b14; }

    .stat-info { display: flex; flex-direction: column; gap: 2px; }
    .stat-label { font-size: 0.85rem; color: #5b6679; font-weight: 500; }
    .stat-value { font-size: 1.5rem; font-weight: 700; color: #0a1f44; font-family: var(--font-display, 'Orbitron', sans-serif); }
    .stat-trend { font-size: 0.75rem; color: #5b6679; }
    .stat-trend.positive { color: #1e704a; font-weight: 600; }

    .content-grid {
      display: grid;
      grid-template-columns: 2fr 1fr;
      gap: 24px;
    }
    @media (max-width: 992px) {
      .content-grid { grid-template-columns: 1fr; }
    }
    .card {
      background: #ffffff;
      border: 1px solid #dfe6f1;
      border-radius: var(--radius-lg);
      padding: 24px;
      box-shadow: 0 4px 16px rgba(10, 31, 68, 0.05);
    }
    .card-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
    }
    .card-header h3 { margin: 0; font-size: 1.15rem; color: #0a1f44; font-family: var(--font-display, 'Orbitron', sans-serif); }
    .card-link { color: #1b4f9c; font-size: 0.85rem; font-weight: 700; text-decoration: none; }
    .card-link:hover { color: #143d7d; }
    .table-responsive { overflow-x: auto; }
    .data-table { width: 100%; border-collapse: collapse; text-align: start; }
    .data-table th { padding: 12px; font-size: 0.8rem; color: #0a1f44; border-bottom: 2px solid #dfe6f1; font-weight: 700; background: #f8fafc; }
    .data-table td { padding: 14px 12px; font-size: 0.9rem; color: #0a1f44; border-bottom: 1px solid #f1f5f9; }
    .badge { padding: 4px 10px; border-radius: var(--radius-pill); font-size: 0.75rem; font-weight: 600; display: inline-block; }
    .badge-success { background: rgba(46, 157, 107, 0.12); color: #1e704a; }
    .badge-warning { background: rgba(224, 160, 48, 0.15); color: #b27b14; }
    .badge-info { background: rgba(27, 79, 156, 0.12); color: #1b4f9c; }
    .badge-danger { background: rgba(179, 18, 42, 0.12); color: #b3122a; }

    .fleet-list { display: flex; flex-direction: column; gap: 16px; }
    .fleet-item { display: flex; align-items: center; justify-content: space-between; gap: 12px; padding-bottom: 12px; border-bottom: 1px solid var(--c-border); }
    .fleet-item:last-child { border-bottom: none; padding-bottom: 0; }
    .fleet-avatar { font-size: 1.75rem; }
    .fleet-details { display: flex; flex-direction: column; flex: 1; }
    .fleet-details strong { font-size: 0.9rem; }
    .fleet-details small { font-size: 0.75rem; color: var(--c-text-muted); }
  `]
})
export class DashboardComponent implements OnInit {
  readonly langService = inject(LanguageService);
  private readonly adminBookingsService = inject(AdminBookingsService);
  private readonly publicService = inject(PublicService);

  readonly bookings = signal<BookingRequestItem[]>([]);
  readonly vehicles = signal<Vehicle[]>([]);

  readonly activeBookingsCount = computed(() => {
    return this.bookings().filter(b => {
      const s = String(b.statusName || b.status || '').toLowerCase();
      return s === 'pending' || s === 'confirmed' || s === 'accepted' || s === '1' || s === '2';
    }).length;
  });

  readonly totalVehiclesCount = computed(() => this.vehicles().length || 1);

  readonly recentBookings = computed(() => this.bookings().slice(0, 5));

  ngOnInit(): void {
    this.adminBookingsService.getAllBookings().subscribe({
      next: (res) => this.bookings.set(res.data || []),
      error: () => {}
    });

    this.publicService.getVehicles().subscribe({
      next: (res) => this.vehicles.set(res || []),
      error: () => {}
    });
  }

  getStatusBadgeClass(status: string | number | undefined): string {
    const s = String(status || '').toLowerCase();
    if (s === 'confirmed' || s === 'accepted' || s === 'completed' || s === '2' || s === '3') return 'badge-success';
    if (s === 'pending' || s === '1') return 'badge-warning';
    if (s === 'cancelled' || s === 'rejected' || s === '4' || s === '5') return 'badge-danger';
    return 'badge-info';
  }
}