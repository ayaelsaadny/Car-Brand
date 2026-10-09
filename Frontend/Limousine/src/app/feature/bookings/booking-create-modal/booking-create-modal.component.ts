import { Component, ChangeDetectionStrategy, inject, signal, input, output, effect, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { BookingsService, GuestBookingRequest } from '../../../core/services/bookings.service';
import { AdminServicesService } from '../../../core/services/admin-services.service';
import { AdminVehiclesService } from '../../../core/services/admin-vehicles.service';
import { ServiceItem, Vehicle, BookingRequestItem } from '../../../core/interfaces/api.models';

@Component({
  selector: 'app-booking-create-modal',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    TranslocoPipe
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './booking-create-modal.component.html',
  styleUrls: ['./booking-create-modal.component.css']
})
export class BookingCreateModalComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly bookingsService = inject(BookingsService);
  private readonly servicesService = inject(AdminServicesService);
  private readonly vehiclesService = inject(AdminVehiclesService);
  private readonly transloco = inject(TranslocoService);

  isOpen = input<boolean>(false);

  saved = output<BookingRequestItem>();
  closed = output<void>();

  readonly saving = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);

  readonly services = signal<ServiceItem[]>([]);
  readonly vehicles = signal<Vehicle[]>([]);
  readonly loadingOptions = signal<boolean>(false);

  readonly form: FormGroup = this.fb.group({
    fullName: ['', [Validators.required, Validators.maxLength(150)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(150)]],
    phoneNumber: ['', [Validators.required, Validators.maxLength(30)]],
    serviceId: [null, [Validators.required]],
    vehicleId: [null],
    pickupLocation: ['', [Validators.required, Validators.maxLength(250)]],
    dropoffLocation: ['', [Validators.required, Validators.maxLength(250)]],
    pickupDateTime: ['', [Validators.required]],
    numberOfPassengers: [1, [Validators.required, Validators.min(1), Validators.max(50)]],
    numberOfLuggage: [1, [Validators.required, Validators.min(0), Validators.max(50)]],
    flightNumber: ['', [Validators.maxLength(50)]],
    customerNotes: ['', [Validators.maxLength(1000)]]
  });

  constructor() {
    effect(() => {
      const open = this.isOpen();
      if (open) {
        this.resetAndInitForm();
      }
    });
  }

  ngOnInit(): void {
    this.loadDropdownData();
  }

  loadDropdownData(): void {
    this.loadingOptions.set(true);

    this.servicesService.getAllServices(undefined, true).subscribe({
      next: (res) => {
        const srvs = res.data || [];
        this.services.set(srvs);
        if (srvs.length > 0 && !this.form.get('serviceId')?.value) {
          this.form.patchValue({ serviceId: srvs[0].id });
        }
      }
    });

    this.vehiclesService.getAllVehicles(true).subscribe({
      next: (res) => {
        this.vehicles.set(res.data || []);
        this.loadingOptions.set(false);
      },
      error: () => this.loadingOptions.set(false)
    });
  }

  private resetAndInitForm(): void {
    this.errorMessage.set(null);

    // Compute default pickup date (tomorrow at 10:00 AM)
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    tomorrow.setHours(10, 0, 0, 0);
    const defaultDateTime = tomorrow.toISOString().slice(0, 16);

    const firstServiceId = this.services().length > 0 ? this.services()[0].id : null;
    const firstVehicleId = this.vehicles().length > 0 ? this.vehicles()[0].id : null;

    this.form.reset({
      fullName: '',
      email: '',
      phoneNumber: '',
      serviceId: firstServiceId,
      vehicleId: firstVehicleId,
      pickupLocation: '',
      dropoffLocation: '',
      pickupDateTime: defaultDateTime,
      numberOfPassengers: 1,
      numberOfLuggage: 1,
      flightNumber: '',
      customerNotes: ''
    });

    if (this.services().length === 0 || this.vehicles().length === 0) {
      this.loadDropdownData();
    }
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.errorMessage.set(null);

    const val = this.form.value;

    const payload: GuestBookingRequest = {
      fullName: val.fullName.trim(),
      email: val.email.trim(),
      phoneNumber: val.phoneNumber.trim(),
      serviceId: Number(val.serviceId),
      vehicleId: val.vehicleId ? Number(val.vehicleId) : undefined,
      pickupLocation: val.pickupLocation.trim(),
      dropoffLocation: val.dropoffLocation.trim(),
      pickupDateTime: new Date(val.pickupDateTime).toISOString(),
      numberOfPassengers: Number(val.numberOfPassengers),
      numberOfLuggage: Number(val.numberOfLuggage),
      flightNumber: val.flightNumber?.trim() || undefined,
      customerNotes: val.customerNotes?.trim() || undefined
    };

    this.bookingsService.createGuestBooking(payload).subscribe({
      next: (res) => {
        this.saving.set(false);
        this.saved.emit(res.data);
      },
      error: (err: any) => {
        this.saving.set(false);
        const msg = err?.error?.message || err?.message || 'Failed to create booking reservation.';
        this.errorMessage.set(msg);
      }
    });
  }

  onClose(): void {
    this.closed.emit();
  }
}
