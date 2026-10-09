import { Component, ChangeDetectionStrategy, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { CustomerService } from '../../../core/services/customer.service';
import { LanguageService } from '../../../core/services/language.service';
import { BookingRequestItem } from '../../../core/interfaces/api.models';

@Component({
  selector: 'app-customer-bookings',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterLink,
    TranslocoPipe
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './customer-bookings.component.html',
  styleUrls: ['./customer-bookings.component.css']
})
export class CustomerBookingsComponent implements OnInit {
  private readonly customerService = inject(CustomerService);
  private readonly langService = inject(LanguageService);
  private readonly fb = inject(FormBuilder);
  private readonly transloco = inject(TranslocoService);

  readonly bookings = signal<BookingRequestItem[]>([]);
  readonly loading = signal<boolean>(true);
  readonly actionLoading = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  // Modals state
  readonly selectedBooking = signal<BookingRequestItem | null>(null);
  readonly showDetailsModal = signal<boolean>(false);
  readonly showCancelModal = signal<boolean>(false);

  // Filters
  readonly statusFilter = signal<string>('all');
  readonly searchQuery = signal<string>('');

  // Cancellation Form
  readonly cancelForm: FormGroup = this.fb.group({
    cancellationReason: ['', [Validators.maxLength(500)]]
  });

  get currentLang(): string {
    return this.langService.currentLang();
  }

  // Metrics
  readonly totalCount = computed(() => this.bookings().length);

  readonly pendingCount = computed(() => 
    this.bookings().filter(b => this.matchesStatusName(b, 'pending')).length
  );

  readonly confirmedCount = computed(() => 
    this.bookings().filter(b => this.matchesStatusName(b, 'confirmed') || this.matchesStatusName(b, 'inprogress')).length
  );

  readonly completedCount = computed(() => 
    this.bookings().filter(b => this.matchesStatusName(b, 'completed')).length
  );

  readonly cancelledCount = computed(() => 
    this.bookings().filter(b => this.matchesStatusName(b, 'cancelled') || this.matchesStatusName(b, 'rejected')).length
  );

  // Filtered List
  readonly filteredBookings = computed(() => {
    const query = this.searchQuery().toLowerCase().trim();
    const filter = this.statusFilter().toLowerCase();

    return this.bookings().filter(b => {
      const matchesQuery = !query ||
        b.bookingReference?.toLowerCase().includes(query) ||
        b.serviceName?.toLowerCase().includes(query) ||
        b.pickupLocation?.toLowerCase().includes(query) ||
        b.dropoffLocation?.toLowerCase().includes(query) ||
        (b.vehicleName && b.vehicleName.toLowerCase().includes(query));

      const matchesStatus = filter === 'all' || this.matchesStatusName(b, filter);

      return matchesQuery && matchesStatus;
    });
  });

  ngOnInit(): void {
    this.loadBookings();
  }

  loadBookings(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.customerService.getMyBookings().subscribe({
      next: (res) => {
        this.loading.set(false);
        this.bookings.set(res.data || []);
      },
      error: (err: any) => {
        this.loading.set(false);
        const msg = err?.error?.message || this.transloco.translate('customer.errorLoading');
        this.errorMessage.set(msg);
      }
    });
  }

  openDetails(booking: BookingRequestItem): void {
    this.selectedBooking.set(booking);
    this.showDetailsModal.set(true);

    // Fetch refreshed single booking details including timeline histories
    this.customerService.getMyBookingById(booking.id).subscribe({
      next: (res) => {
        if (res.data) {
          this.selectedBooking.set(res.data);
        }
      }
    });
  }

  closeDetails(): void {
    this.showDetailsModal.set(false);
    this.selectedBooking.set(null);
  }

  openCancelModal(booking: BookingRequestItem): void {
    this.selectedBooking.set(booking);
    this.cancelForm.reset();
    this.showCancelModal.set(true);
  }

  closeCancelModal(): void {
    this.showCancelModal.set(false);
    this.cancelForm.reset();
  }

  confirmCancellation(): void {
    const current = this.selectedBooking();
    if (!current) return;

    this.actionLoading.set(true);
    this.errorMessage.set(null);
    const reason = this.cancelForm.value.cancellationReason || undefined;

    this.customerService.cancelMyBooking(current.id, reason).subscribe({
      next: () => {
        this.actionLoading.set(false);
        this.successMessage.set(this.transloco.translate('customer.cancelSuccess'));
        this.closeCancelModal();
        this.loadBookings();
        setTimeout(() => this.successMessage.set(null), 5000);
      },
      error: (err: any) => {
        this.actionLoading.set(false);
        const msg = err?.error?.message || this.transloco.translate('customer.cancelError');
        this.errorMessage.set(msg);
      }
    });
  }

  canCancel(booking: BookingRequestItem): boolean {
    const status = String(booking.statusName || booking.status || '').toLowerCase();
    return status === 'pending' || status === '0' || status === 'confirmed' || status === '1';
  }

  getStatusBadgeClass(booking: BookingRequestItem): string {
    const status = String(booking.statusName || booking.status || '').toLowerCase();
    switch (status) {
      case 'pending':
      case '0':
        return 'badge-pending';
      case 'confirmed':
      case '1':
        return 'badge-confirmed';
      case 'inprogress':
      case '2':
        return 'badge-inprogress';
      case 'completed':
      case '3':
        return 'badge-completed';
      case 'cancelled':
      case '4':
        return 'badge-cancelled';
      case 'rejected':
      case '5':
        return 'badge-rejected';
      default:
        return 'badge-default';
    }
  }

  private matchesStatusName(b: BookingRequestItem, target: string): boolean {
    const s = String(b.statusName || b.status || '').toLowerCase();
    if (target === 'pending') return s === 'pending' || s === '0';
    if (target === 'confirmed') return s === 'confirmed' || s === '1';
    if (target === 'inprogress') return s === 'inprogress' || s === '2';
    if (target === 'completed') return s === 'completed' || s === '3';
    if (target === 'cancelled') return s === 'cancelled' || s === '4';
    if (target === 'rejected') return s === 'rejected' || s === '5';
    return s === target;
  }
}
