import { Component, ChangeDetectionStrategy, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { AdminCategoriesService } from '../../../core/services/admin-categories.service';
import { AuthService } from '../../../core/services/auth.service';
import { LanguageService } from '../../../core/services/language.service';
import { Category } from '../../../core/interfaces/api.models';
import { CategoryFormComponent } from '../category-form/category-form.component';

@Component({
  selector: 'app-categories-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslocoPipe,
    CategoryFormComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './categories-list.component.html',
  styleUrls: ['./categories-list.component.css']
})
export class CategoriesListComponent implements OnInit {
  private readonly categoriesService = inject(AdminCategoriesService);
  readonly authService = inject(AuthService);
  private readonly langService = inject(LanguageService);
  private readonly transloco = inject(TranslocoService);

  readonly categories = signal<Category[]>([]);
  readonly loading = signal<boolean>(true);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  // Form Modal State
  readonly isFormOpen = signal<boolean>(false);
  readonly editingCategory = signal<Category | null>(null);

  // Delete Confirm Modal State
  readonly isDeleteConfirmOpen = signal<boolean>(false);
  readonly deletingCategoryId = signal<number | null>(null);

  // Services Inspection Modal State (Useful for both Admin and Staff)
  readonly selectedCategoryServices = signal<Category | null>(null);
  readonly servicesList = signal<any[]>([]);
  readonly servicesLoading = signal<boolean>(false);

  // Search & Filter
  readonly searchQuery = signal<string>('');
  readonly statusFilter = signal<'all' | 'active' | 'inactive'>('all');

  // Role Permissions
  readonly isAdmin = computed(() => this.authService.isAdmin());
  readonly isEmployee = computed(() => this.authService.isEmployee());

  get currentLang(): string {
    return this.langService.currentLang();
  }

  readonly stats = computed(() => {
    const list = this.categories();
    return {
      total: list.length,
      active: list.filter(c => c.isActive).length,
      inactive: list.filter(c => !c.isActive).length
    };
  });

  readonly filteredCategories = computed(() => {
    const query = this.searchQuery().toLowerCase().trim();
    const filter = this.statusFilter();

    return this.categories().filter(c => {
      const matchesQuery = !query ||
        c.name.toLowerCase().includes(query) ||
        (c.description && c.description.toLowerCase().includes(query));

      const matchesStatus = filter === 'all' ||
        (filter === 'active' && c.isActive) ||
        (filter === 'inactive' && !c.isActive);

      return matchesQuery && matchesStatus;
    });
  });

  ngOnInit(): void {
    this.loadCategories();
  }

  loadCategories(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    const adminRole = this.isAdmin();
    this.categoriesService.getAllCategories(undefined, true, adminRole).subscribe({
      next: (res) => {
        this.loading.set(false);
        this.categories.set(res.data || []);
      },
      error: (err: any) => {
        this.loading.set(false);
        const msg = err?.error?.message || this.transloco.translate('categoriesPage.errorLoading');
        this.errorMessage.set(msg);
      }
    });
  }

  openCreateModal(): void {
    this.editingCategory.set(null);
    this.isFormOpen.set(true);
  }

  openEditModal(category: Category): void {
    this.editingCategory.set(category);
    this.isFormOpen.set(true);
  }

  closeFormModal(): void {
    this.isFormOpen.set(false);
    this.editingCategory.set(null);
  }

  onCategorySaved(savedCategory: Category): void {
    this.closeFormModal();
    const isEdit = !!this.editingCategory();
    const key = isEdit ? 'categoriesPage.updatedSuccess' : 'categoriesPage.createdSuccess';
    this.successMessage.set(this.transloco.translate(key));
    setTimeout(() => this.successMessage.set(null), 4000);
    this.loadCategories();
  }

  toggleStatus(category: Category): void {
    if (!this.isAdmin()) return;

    const nextStatus = !category.isActive;
    this.errorMessage.set(null);

    this.categoriesService.updateStatus(category.id, nextStatus).subscribe({
      next: () => {
        this.categories.update(list =>
          list.map(c => c.id === category.id ? { ...c, isActive: nextStatus } : c)
        );
        this.successMessage.set(this.transloco.translate('categoriesPage.statusSuccess'));
        setTimeout(() => this.successMessage.set(null), 3000);
      },
      error: (err: any) => {
        const msg = err?.error?.message || this.transloco.translate('categoriesPage.errorSaving');
        this.errorMessage.set(msg);
      }
    });
  }

  confirmDelete(id: number): void {
    if (!this.isAdmin()) return;
    this.deletingCategoryId.set(id);
    this.isDeleteConfirmOpen.set(true);
  }

  closeDeleteConfirm(): void {
    this.isDeleteConfirmOpen.set(false);
    this.deletingCategoryId.set(null);
  }

  executeDelete(): void {
    const id = this.deletingCategoryId();
    if (!id || !this.isAdmin()) return;

    this.categoriesService.deleteCategory(id, false).subscribe({
      next: () => {
        this.closeDeleteConfirm();
        this.categories.update(list => list.filter(c => c.id !== id));
        this.successMessage.set(this.transloco.translate('categoriesPage.deletedSuccess'));
        setTimeout(() => this.successMessage.set(null), 4000);
      },
      error: (err: any) => {
        this.closeDeleteConfirm();
        const msg = err?.error?.message || this.transloco.translate('categoriesPage.errorDeleting');
        this.errorMessage.set(msg);
      }
    });
  }

  viewCategoryServices(category: Category): void {
    this.selectedCategoryServices.set(category);
    this.servicesLoading.set(true);
    this.servicesList.set([]);

    this.categoriesService.getCategoryServices(category.id).subscribe({
      next: (res) => {
        this.servicesLoading.set(false);
        this.servicesList.set(res.data || []);
      },
      error: () => {
        this.servicesLoading.set(false);
      }
    });
  }

  closeServicesModal(): void {
    this.selectedCategoryServices.set(null);
    this.servicesList.set([]);
  }
}
