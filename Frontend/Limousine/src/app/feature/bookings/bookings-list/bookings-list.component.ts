import { Component, ChangeDetectionStrategy, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { BookingsService } from '../../../core/services/bookings.service';
import { AuthService } from '../../../core/services/auth.service';
import { LanguageService } from '../../../core/services/language.service';
import { BookingRequestItem } from '../../../core/interfaces/api.models';
import { BookingDetailModalComponent } from '../booking-detail-modal/booking-detail-modal.component';
import { BookingActionModalComponent, BookingActionType } from '../booking-action-modal/booking-action-modal.component';
import { BookingCreateModalComponent } from '../booking-create-modal/booking-create-modal.component';

@Component({
  selector: 'app-bookings-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslocoPipe,
    BookingDetailModalComponent,
    BookingActionModalComponent,
    BookingCreateModalComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './bookings-list.component.html',
  styleUrls: ['./bookings-list.component.css']
})
export class BookingsListComponent implements OnInit {
  private readonly bookingsService = inject(BookingsService);
  readonly authService = inject(AuthService);
  private readonly langService = inject(LanguageService);
  private readonly transloco = inject(TranslocoService);
  private readonly route = inject(ActivatedRoute);

  readonly bookings = signal<BookingRequestItem[]>([]);
  readonly loading = signal<boolean>(true);
  readonly actionLoading = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  // Selected Booking & Action Modal State
  readonly isDetailsModalOpen = signal<boolean>(false);
  readonly isActionModalOpen = signal<boolean>(false);
  readonly isCreateModalOpen = signal<boolean>(false);
  readonly actionModalType = signal<BookingActionType | null>(null);
  readonly selectedBooking = signal<BookingRequestItem | null>(null);

  // Filter & Search Signals
  readonly searchQuery = signal<string>('');
  readonly statusFilter = signal<string>('all');

  // Role permissions
  readonly isStaff = computed(() => this.authService.isStaff());
  readonly isAdmin = computed(() => this.authService.isAdmin());
  readonly isCustomer = computed(() => this.authService.isCustomer());

  get currentLang(): string {
    return this.langService.currentLang();
  }

  readonly filteredBookings = computed(() => {
    const query = this.searchQuery().toLowerCase().trim();
    const filter = this.statusFilter().toLowerCase();

    return this.bookings().filter(b => {
      const matchesQuery = !query ||
        b.bookingReference?.toLowerCase().includes(query) ||
        b.customerName?.toLowerCase().includes(query) ||
        b.customerEmail?.toLowerCase().includes(query) ||
        b.customerPhone?.toLowerCase().includes(query) ||
        b.serviceName?.toLowerCase().includes(query);

      const statusStr = String(b.statusName || b.status || '').toLowerCase();
      const matchesStatus = filter === 'all' || statusStr === filter;

      return matchesQuery && matchesStatus;
    });
  });

  readonly stats = computed(() => {
    const list = this.bookings();
    return {
      total: list.length,
      pending: list.filter(b => String(b.statusName || b.status).toLowerCase() === 'pending').length,
      confirmed: list.filter(b => {
        const s = String(b.statusName || b.status).toLowerCase();
        return s === 'confirmed' || s === 'accepted';
      }).length,
      completed: list.filter(b => String(b.statusName || b.status).toLowerCase() === 'completed').length,
      cancelled: list.filter(b => {
        const s = String(b.statusName || b.status).toLowerCase();
        return s === 'cancelled' || s === 'rejected';
      }).length
    };
  });

  ngOnInit(): void {
    this.loadBookings();
    this.route.queryParams.subscribe(params => {
      if (params['create'] === 'true') {
        this.openCreateModal();
      }
    });
  }

  openCreateModal(): void {
    this.isCreateModalOpen.set(true);
  }

  closeCreateModal(): void {
    this.isCreateModalOpen.set(false);
  }

  onBookingCreated(item: BookingRequestItem): void {
    this.closeCreateModal();
    this.successMessage.set(this.transloco.translate('bookingsPage.createdSuccess') || 'Booking reservation created successfully.');
    setTimeout(() => this.successMessage.set(null), 4000);
    this.loadBookings();
  }

  loadBookings(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    // If customer, fetch user's own bookings; if staff/admin, fetch all fleet bookings
    const fetchObs = this.isCustomer()
      ? this.bookingsService.getMyBookings()
      : this.bookingsService.getAllBookings();

    fetchObs.subscribe({
      next: (res) => {
        this.loading.set(false);
        this.bookings.set(res.data || []);
      },
      error: (err) => {
        this.loading.set(false);
        const msg = err?.error?.message || this.transloco.translate('bookingsPage.errorLoading');
        this.errorMessage.set(msg);
      }
    });
  }

  openDetailsModal(booking: BookingRequestItem): void {
    this.selectedBooking.set(booking);
    this.isDetailsModalOpen.set(true);
  }

  closeDetailsModal(): void {
    this.isDetailsModalOpen.set(false);
    this.selectedBooking.set(null);
  }

  openActionModal(booking: BookingRequestItem, type: BookingActionType): void {
    this.selectedBooking.set(booking);
    this.actionModalType.set(type);
    this.isActionModalOpen.set(true);
  }

  closeActionModal(): void {
    this.isActionModalOpen.set(false);
    this.actionModalType.set(null);
    this.selectedBooking.set(null);
  }

  onActionConfirmed(event: { type: BookingActionType; reason?: string; notes?: string }): void {
    const booking = this.selectedBooking();
    if (!booking) return;

    this.actionLoading.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    if (event.type === 'accept') {
      this.bookingsService.acceptBooking(booking.id, { notes: event.notes }).subscribe({
        next: (res) => this.handleActionSuccess(res.data || { ...booking, status: 2, statusName: 'Confirmed' }, 'bookingsPage.acceptedSuccess'),
        error: (err) => this.handleActionError(err)
      });
    } else if (event.type === 'reject') {
      this.bookingsService.rejectBooking(booking.id, { rejectionReason: event.reason || 'Booking capacity full' }).subscribe({
        next: (res) => this.handleActionSuccess(res.data || { ...booking, status: 6, statusName: 'Rejected' }, 'bookingsPage.rejectedSuccess'),
        error: (err) => this.handleActionError(err)
      });
    } else if (event.type === 'complete') {
      this.bookingsService.completeBooking(booking.id, { notes: event.notes }).subscribe({
        next: (res) => this.handleActionSuccess(res.data || { ...booking, status: 4, statusName: 'Completed' }, 'bookingsPage.completedSuccess'),
        error: (err) => this.handleActionError(err)
      });
    } else if (event.type === 'cancel') {
      this.bookingsService.cancelBooking(booking.id, { cancellationReason: event.reason || 'Customer request' }, this.isAdmin()).subscribe({
        next: (res) => this.handleActionSuccess(res.data || { ...booking, status: 5, statusName: 'Cancelled' }, 'bookingsPage.cancelledSuccess'),
        error: (err) => this.handleActionError(err)
      });
    }
  }

  private handleActionSuccess(updated: BookingRequestItem, msgKey: string): void {
    this.actionLoading.set(false);
    this.closeActionModal();
    this.successMessage.set(this.transloco.translate(msgKey));
    setTimeout(() => this.successMessage.set(null), 4000);
    this.bookings.update(list => list.map(b => b.id === updated.id ? updated : b));
  }

  private handleActionError(err: any): void {
    this.actionLoading.set(false);
    const msg = err?.error?.message || this.transloco.translate('bookingsPage.errorAction');
    this.errorMessage.set(msg);
  }

  getStatusClass(status: number | string | undefined): string {
    const s = String(status || '').toLowerCase();
    if (s === '1' || s === 'pending') return 'status-pending';
    if (s === '2' || s === 'confirmed' || s === 'accepted') return 'status-confirmed';
    if (s === '3' || s === 'inprogress') return 'status-inprogress';
    if (s === '4' || s === 'completed') return 'status-completed';
    if (s === '5' || s === 'cancelled') return 'status-cancelled';
    if (s === '6' || s === 'rejected') return 'status-rejected';
    return 'status-pending';
  }
}
