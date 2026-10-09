import { Component, ChangeDetectionStrategy, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { VehiclesService } from '../../../core/services/vehicles.service';
import { AuthService } from '../../../core/services/auth.service';
import { LanguageService } from '../../../core/services/language.service';
import { Vehicle, CreateVehicleRequest, UpdateVehicleRequest } from '../../../core/interfaces/api.models';
import { VehicleFormComponent } from '../vehicle-form/vehicle-form.component';

@Component({
  selector: 'app-vehicles-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslocoPipe,
    VehicleFormComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './vehicles-list.component.html',
  styleUrls: ['./vehicles-list.component.css']
})
export class VehiclesListComponent implements OnInit {
  private readonly vehiclesService = inject(VehiclesService);
  readonly authService = inject(AuthService);
  private readonly langService = inject(LanguageService);
  private readonly transloco = inject(TranslocoService);

  readonly vehicles = signal<Vehicle[]>([]);
  readonly loading = signal<boolean>(true);
  readonly saving = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  // Modal State
  readonly isFormOpen = signal<boolean>(false);
  readonly selectedVehicle = signal<Vehicle | null>(null);
  readonly isDeleteConfirmOpen = signal<boolean>(false);
  readonly deletingVehicleId = signal<number | null>(null);

  // Filters
  readonly searchQuery = signal<string>('');
  readonly statusFilter = signal<'all' | 'active' | 'inactive'>('all');

  // Permissions
  readonly isAdmin = computed(() => this.authService.isAdmin());

  get currentLang(): string {
    return this.langService.currentLang();
  }

  readonly filteredVehicles = computed(() => {
    const query = this.searchQuery().toLowerCase().trim();
    const filter = this.statusFilter();

    return this.vehicles().filter(v => {
      const matchesQuery = !query ||
        v.name.toLowerCase().includes(query) ||
        v.make.toLowerCase().includes(query) ||
        v.model.toLowerCase().includes(query) ||
        v.licensePlate.toLowerCase().includes(query) ||
        (v.color && v.color.toLowerCase().includes(query));

      const matchesStatus = filter === 'all' ||
        (filter === 'active' && v.isActive) ||
        (filter === 'inactive' && !v.isActive);

      return matchesQuery && matchesStatus;
    });
  });

  readonly stats = computed(() => {
    const list = this.vehicles();
    return {
      total: list.length,
      active: list.filter(v => v.isActive).length,
      inactive: list.filter(v => !v.isActive).length
    };
  });

  ngOnInit(): void {
    this.loadVehicles();
  }

  loadVehicles(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.vehiclesService.getAllFleetVehicles(false).subscribe({
      next: (res) => {
        this.loading.set(false);
        this.vehicles.set(res.data || []);
      },
      error: (err) => {
        this.loading.set(false);
        const msg = err?.error?.message || this.transloco.translate('vehiclesPage.errorLoading');
        this.errorMessage.set(msg);
      }
    });
  }

  openCreateModal(): void {
    this.selectedVehicle.set(null);
    this.isFormOpen.set(true);
  }

  openEditModal(vehicle: Vehicle): void {
    this.selectedVehicle.set(vehicle);
    this.isFormOpen.set(true);
  }

  closeFormModal(): void {
    this.isFormOpen.set(false);
    this.selectedVehicle.set(null);
  }

  onVehicleSaved(event: { id?: number; payload: CreateVehicleRequest | UpdateVehicleRequest }): void {
    this.saving.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    if (event.id) {
      // Edit mode
      this.vehiclesService.updateVehicle(event.id, event.payload as UpdateVehicleRequest).subscribe({
        next: (res) => {
          this.saving.set(false);
          this.closeFormModal();
          this.successMessage.set(this.transloco.translate('vehiclesPage.updatedSuccess'));
          setTimeout(() => this.successMessage.set(null), 4000);
          if (res.data) {
            this.vehicles.update(list => list.map(v => v.id === event.id ? res.data : v));
          } else {
            this.loadVehicles();
          }
        },
        error: (err) => {
          this.saving.set(false);
          const msg = err?.error?.message || this.transloco.translate('vehiclesPage.errorSaving');
          this.errorMessage.set(msg);
        }
      });
    } else {
      // Create mode
      this.vehiclesService.createVehicle(event.payload as CreateVehicleRequest).subscribe({
        next: (res) => {
          this.saving.set(false);
          this.closeFormModal();
          this.successMessage.set(this.transloco.translate('vehiclesPage.createdSuccess'));
          setTimeout(() => this.successMessage.set(null), 4000);
          if (res.data) {
            this.vehicles.update(list => [...list, res.data]);
          } else {
            this.loadVehicles();
          }
        },
        error: (err) => {
          this.saving.set(false);
          const msg = err?.error?.message || this.transloco.translate('vehiclesPage.errorSaving');
          this.errorMessage.set(msg);
        }
      });
    }
  }

  toggleStatus(vehicle: Vehicle): void {
    if (!this.isAdmin()) return;
    const nextStatus = !vehicle.isActive;
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.vehiclesService.updateStatus(vehicle.id, nextStatus).subscribe({
      next: () => {
        this.vehicles.update(list =>
          list.map(v => v.id === vehicle.id ? { ...v, isActive: nextStatus } : v)
        );
        this.successMessage.set(this.transloco.translate('vehiclesPage.statusSuccess'));
        setTimeout(() => this.successMessage.set(null), 4000);
      },
      error: (err) => {
        const msg = err?.error?.message || this.transloco.translate('vehiclesPage.errorSaving');
        this.errorMessage.set(msg);
      }
    });
  }

  setDefault(vehicle: Vehicle): void {
    if (!this.isAdmin()) return;
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.vehiclesService.setDefault(vehicle.id).subscribe({
      next: () => {
        this.vehicles.update(list =>
          list.map(v => ({ ...v, isDefault: v.id === vehicle.id }))
        );
        this.successMessage.set('Showcase default vehicle updated.');
        setTimeout(() => this.successMessage.set(null), 4000);
      },
      error: (err) => {
        const msg = err?.error?.message || this.transloco.translate('vehiclesPage.errorSaving');
        this.errorMessage.set(msg);
      }
    });
  }

  confirmDelete(id: number): void {
    if (!this.isAdmin()) return;
    this.deletingVehicleId.set(id);
    this.isDeleteConfirmOpen.set(true);
  }

  closeDeleteConfirm(): void {
    this.isDeleteConfirmOpen.set(false);
    this.deletingVehicleId.set(null);
  }

  executeDelete(): void {
    const id = this.deletingVehicleId();
    if (!id || !this.isAdmin()) return;

    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.vehiclesService.deleteVehicle(id, false).subscribe({
      next: () => {
        this.closeDeleteConfirm();
        this.vehicles.update(list => list.filter(v => v.id !== id));
        this.successMessage.set(this.transloco.translate('vehiclesPage.deletedSuccess'));
        setTimeout(() => this.successMessage.set(null), 4000);
      },
      error: (err) => {
        this.closeDeleteConfirm();
        const msg = err?.error?.message || this.transloco.translate('vehiclesPage.errorDeleting');
        this.errorMessage.set(msg);
      }
    });
  }
}
