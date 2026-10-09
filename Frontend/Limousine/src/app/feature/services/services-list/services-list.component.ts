import { Component, ChangeDetectionStrategy, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { AdminServicesService } from '../../../core/services/admin-services.service';
import { AdminCategoriesService } from '../../../core/services/admin-categories.service';
import { AuthService } from '../../../core/services/auth.service';
import { LanguageService } from '../../../core/services/language.service';
import { ServiceItem, Category } from '../../../core/interfaces/api.models';
import { ServiceFormComponent } from '../service-form/service-form.component';

@Component({
  selector: 'app-services-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslocoPipe,
    ServiceFormComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './services-list.component.html',
  styleUrls: ['./services-list.component.css']
})
export class ServicesListComponent implements OnInit {
  private readonly servicesService = inject(AdminServicesService);
  private readonly categoriesService = inject(AdminCategoriesService);
  readonly authService = inject(AuthService);
  private readonly langService = inject(LanguageService);
  private readonly transloco = inject(TranslocoService);

  readonly services = signal<ServiceItem[]>([]);
  readonly categories = signal<Category[]>([]);
  readonly loading = signal<boolean>(true);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  // Form Modal State
  readonly isFormOpen = signal<boolean>(false);
  readonly editingService = signal<ServiceItem | null>(null);

  // Delete Confirm Modal State
  readonly isDeleteConfirmOpen = signal<boolean>(false);
  readonly deletingServiceId = signal<number | null>(null);

  // Available Vehicles Inspection Modal State (Useful for both Admin and Staff)
  readonly selectedServiceForVehicles = signal<ServiceItem | null>(null);
  readonly availableVehiclesList = signal<any[]>([]);
  readonly vehiclesLoading = signal<boolean>(false);

  // Filters
  readonly searchQuery = signal<string>('');
  readonly selectedCategoryId = signal<number | 'all'>('all');
  readonly statusFilter = signal<'all' | 'active' | 'inactive'>('all');

  // Role Permissions
  readonly isAdmin = computed(() => this.authService.isAdmin());
  readonly isEmployee = computed(() => this.authService.isEmployee());

  get currentLang(): string {
    return this.langService.currentLang();
  }

  readonly stats = computed(() => {
    const list = this.services();
    return {
      total: list.length,
      active: list.filter(s => s.isActive).length,
      inactive: list.filter(s => !s.isActive).length
    };
  });

  readonly filteredServices = computed(() => {
    const query = this.searchQuery().toLowerCase().trim();
    const catId = this.selectedCategoryId();
    const filter = this.statusFilter();

    return this.services().filter(s => {
      const matchesQuery = !query ||
        s.name.toLowerCase().includes(query) ||
        (s.description && s.description.toLowerCase().includes(query)) ||
        (s.categoryName && s.categoryName.toLowerCase().includes(query)) ||
        (s.fromLocation && s.fromLocation.toLowerCase().includes(query)) ||
        (s.toLocation && s.toLocation.toLowerCase().includes(query));

      const matchesCategory = catId === 'all' || s.categoryId === catId;

      const matchesStatus = filter === 'all' ||
        (filter === 'active' && s.isActive) ||
        (filter === 'inactive' && !s.isActive);

      return matchesQuery && matchesCategory && matchesStatus;
    });
  });

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    const adminRole = this.isAdmin();

    // 1. Load categories for dropdown filter
    this.categoriesService.getAllCategories(undefined, false, adminRole).subscribe({
      next: (catRes) => {
        this.categories.set(catRes.data || []);
      }
    });

    // 2. Load services (role-aware: /api/admin/services or /api/Services)
    this.servicesService.getAllServices(undefined, adminRole).subscribe({
      next: (res) => {
        this.loading.set(false);
        this.services.set(res.data || []);
      },
      error: (err: any) => {
        this.loading.set(false);
        const msg = err?.error?.message || this.transloco.translate('servicesPage.errorLoading');
        this.errorMessage.set(msg);
      }
    });
  }

  openCreateModal(): void {
    this.editingService.set(null);
    this.isFormOpen.set(true);
  }

  openEditModal(service: ServiceItem): void {
    this.editingService.set(service);
    this.isFormOpen.set(true);
  }

  closeFormModal(): void {
    this.isFormOpen.set(false);
    this.editingService.set(null);
  }

  onServiceSaved(savedService: ServiceItem): void {
    this.closeFormModal();
    const isEdit = !!this.editingService();
    const key = isEdit ? 'servicesPage.updatedSuccess' : 'servicesPage.createdSuccess';
    this.successMessage.set(this.transloco.translate(key));
    setTimeout(() => this.successMessage.set(null), 4000);
    this.loadData();
  }

  toggleStatus(service: ServiceItem): void {
    if (!this.isAdmin()) return;

    const nextStatus = !service.isActive;
    this.errorMessage.set(null);

    this.servicesService.updateStatus(service.id, nextStatus).subscribe({
      next: () => {
        this.services.update(list =>
          list.map(s => s.id === service.id ? { ...s, isActive: nextStatus } : s)
        );
        this.successMessage.set(this.transloco.translate('servicesPage.statusSuccess'));
        setTimeout(() => this.successMessage.set(null), 3000);
      },
      error: (err: any) => {
        const msg = err?.error?.message || this.transloco.translate('servicesPage.errorSaving');
        this.errorMessage.set(msg);
      }
    });
  }

  confirmDelete(id: number): void {
    if (!this.isAdmin()) return;
    this.deletingServiceId.set(id);
    this.isDeleteConfirmOpen.set(true);
  }

  closeDeleteConfirm(): void {
    this.isDeleteConfirmOpen.set(false);
    this.deletingServiceId.set(null);
  }

  executeDelete(): void {
    const id = this.deletingServiceId();
    if (!id || !this.isAdmin()) return;

    this.servicesService.deleteService(id, false).subscribe({
      next: () => {
        this.closeDeleteConfirm();
        this.services.update(list => list.filter(s => s.id !== id));
        this.successMessage.set(this.transloco.translate('servicesPage.deletedSuccess'));
        setTimeout(() => this.successMessage.set(null), 4000);
      },
      error: (err: any) => {
        this.closeDeleteConfirm();
        const msg = err?.error?.message || this.transloco.translate('servicesPage.errorDeleting');
        this.errorMessage.set(msg);
      }
    });
  }

  viewAvailableVehicles(service: ServiceItem): void {
    this.selectedServiceForVehicles.set(service);
    this.vehiclesLoading.set(true);
    this.availableVehiclesList.set([]);

    const now = new Date();
    const query = {
      pickupDateTime: new Date(now.getTime() + 2 * 3600 * 1000).toISOString(),
      numberOfPassengers: 2,
      numberOfLuggage: 1
    };

    this.servicesService.getAvailableVehicles(service.id, query).subscribe({
      next: (res) => {
        this.vehiclesLoading.set(false);
        this.availableVehiclesList.set(res.data?.availableVehicles || res.data || []);
      },
      error: () => {
        this.vehiclesLoading.set(false);
      }
    });
  }

  closeVehiclesModal(): void {
    this.selectedServiceForVehicles.set(null);
    this.availableVehiclesList.set([]);
  }
}
