import { Component, ChangeDetectionStrategy, inject, input, output, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { Vehicle, CreateVehicleRequest, UpdateVehicleRequest } from '../../../core/interfaces/api.models';

@Component({
  selector: 'app-vehicle-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    TranslocoPipe
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './vehicle-form.component.html',
  styleUrls: ['./vehicle-form.component.css']
})
export class VehicleFormComponent {
  private readonly fb = inject(FormBuilder);

  vehicle = input<Vehicle | null>(null);
  isOpen = input<boolean>(false);
  isSaving = input<boolean>(false);

  saved = output<{ id?: number; payload: CreateVehicleRequest | UpdateVehicleRequest }>();
  closed = output<void>();

  readonly form: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    make: ['', [Validators.required, Validators.maxLength(100)]],
    model: ['', [Validators.required, Validators.maxLength(100)]],
    year: [new Date().getFullYear(), [Validators.required, Validators.min(1900), Validators.max(2100)]],
    licensePlate: ['', [Validators.required, Validators.maxLength(50)]],
    color: ['', [Validators.maxLength(50)]],
    passengerCapacity: [4, [Validators.required, Validators.min(1), Validators.max(100)]],
    luggageCapacity: [3, [Validators.required, Validators.min(0), Validators.max(100)]],
    pricePerHour: [100, [Validators.required, Validators.min(0.01)]],
    pricePerKm: [null, [Validators.min(0)]],
    description: ['', [Validators.maxLength(1000)]],
    features: ['', [Validators.maxLength(1000)]],
    isActive: [true],
    isDefault: [false]
  });

  constructor() {
    effect(() => {
      const open = this.isOpen();
      if (!open) return;

      const v = this.vehicle();
      if (v) {
        this.form.patchValue({
          name: v.name,
          make: v.make,
          model: v.model,
          year: v.year,
          licensePlate: v.licensePlate,
          color: v.color || '',
          passengerCapacity: v.passengerCapacity,
          luggageCapacity: v.luggageCapacity,
          pricePerHour: v.pricePerHour,
          pricePerKm: v.pricePerKm ?? null,
          description: v.description || '',
          features: v.features || '',
          isActive: v.isActive,
          isDefault: v.isDefault
        });
      } else {
        this.form.reset({
          name: '',
          make: '',
          model: '',
          year: new Date().getFullYear(),
          licensePlate: '',
          color: '',
          passengerCapacity: 4,
          luggageCapacity: 3,
          pricePerHour: 100,
          pricePerKm: null,
          description: '',
          features: '',
          isActive: true,
          isDefault: false
        });
      }
    });
  }

  isEditing(): boolean {
    return !!this.vehicle();
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const val = this.form.value;
    const payload: CreateVehicleRequest | UpdateVehicleRequest = {
      name: val.name,
      make: val.make,
      model: val.model,
      year: Number(val.year),
      licensePlate: val.licensePlate,
      color: val.color || undefined,
      passengerCapacity: Number(val.passengerCapacity),
      luggageCapacity: Number(val.luggageCapacity),
      pricePerHour: Number(val.pricePerHour),
      pricePerKm: val.pricePerKm !== null && val.pricePerKm !== '' ? Number(val.pricePerKm) : undefined,
      description: val.description || undefined,
      features: val.features || undefined,
      isActive: Boolean(val.isActive),
      isDefault: Boolean(val.isDefault)
    };

    const currentId = this.vehicle()?.id;
    this.saved.emit({ id: currentId, payload });
  }

  onClose(): void {
    this.closed.emit();
  }
}
