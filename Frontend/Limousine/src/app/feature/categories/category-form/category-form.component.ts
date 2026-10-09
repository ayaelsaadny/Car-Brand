import { Component, ChangeDetectionStrategy, inject, signal, input, output, effect, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { AdminCategoriesService } from '../../../core/services/admin-categories.service';
import { Category, CreateCategoryRequest, UpdateCategoryRequest } from '../../../core/interfaces/api.models';

@Component({
  selector: 'app-category-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    TranslocoPipe
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './category-form.component.html',
  styleUrls: ['./category-form.component.css']
})
export class CategoryFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly categoriesService = inject(AdminCategoriesService);
  private readonly transloco = inject(TranslocoService);

  category = input<Category | null>(null);
  isOpen = input<boolean>(false);

  saved = output<Category>();
  closed = output<void>();

  readonly saving = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly activeTab = signal<'en' | 'fr'>('en');
  readonly selectedFile = signal<File | null>(null);
  readonly previewUrl = signal<string | null>(null);

  readonly form: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', [Validators.maxLength(500)]],
    nameFr: ['', [Validators.maxLength(100)]],
    descriptionFr: ['', [Validators.maxLength(500)]],
    displayOrder: [0, [Validators.required, Validators.min(0)]],
    imageUrl: ['', [Validators.required]],
    isActive: [true]
  });

  constructor() {
    effect(() => {
      const open = this.isOpen();
      if (!open) return;

      const cat = this.category();
      if (cat) {
        this.form.patchValue({
          name: cat.name,
          description: cat.description || '',
          nameFr: '',
          descriptionFr: '',
          displayOrder: cat.displayOrder,
          imageUrl: cat.imageUrl || cat.image || '',
          isActive: cat.isActive
        });
        this.previewUrl.set(cat.imageUrl || cat.image || null);
      } else {
        this.form.reset({
          name: '',
          description: '',
          nameFr: '',
          descriptionFr: '',
          displayOrder: 0,
          imageUrl: '',
          isActive: true
        });
        this.previewUrl.set(null);
      }
      this.selectedFile.set(null);
      this.errorMessage.set(null);
      this.activeTab.set('en');
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      this.selectedFile.set(file);
      this.form.patchValue({ imageUrl: file.name });
      this.form.get('imageUrl')?.updateValueAndValidity();
      const reader = new FileReader();
      reader.onload = () => {
        this.previewUrl.set(reader.result as string);
      };
      reader.readAsDataURL(file);
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
    const cat = this.category();

    const translations: Record<string, { name: string; description?: string }> = {};
    if (val.name) {
      translations['en'] = { name: val.name, description: val.description || undefined };
    }
    if (val.nameFr) {
      translations['fr'] = { name: val.nameFr, description: val.descriptionFr || undefined };
    }

    if (cat) {
      // Update existing category
      const payload: UpdateCategoryRequest = {
        name: val.name,
        description: val.description || undefined,
        displayOrder: Number(val.displayOrder) || 0,
        imageUrl: val.imageUrl || undefined,
        isActive: val.isActive,
        translations: Object.keys(translations).length > 0 ? translations : undefined
      };

      this.categoriesService.updateCategory(cat.id, payload).subscribe({
        next: (res) => {
          if (this.selectedFile()) {
            this.uploadCategoryFile(cat.id, res.data);
          } else {
            this.saving.set(false);
            this.saved.emit(res.data);
          }
        },
        error: (err: any) => {
          this.saving.set(false);
          const msg = err?.error?.message || this.transloco.translate('categoriesPage.errorSaving');
          this.errorMessage.set(msg);
        }
      });
    } else {
      // Create new category
      const payload: CreateCategoryRequest = {
        name: val.name,
        description: val.description || undefined,
        displayOrder: Number(val.displayOrder) || 0,
        imageUrl: val.imageUrl || undefined,
        isActive: val.isActive,
        translations: Object.keys(translations).length > 0 ? translations : undefined
      };

      this.categoriesService.createCategory(payload).subscribe({
        next: (res) => {
          if (this.selectedFile() && res.data?.id) {
            this.uploadCategoryFile(res.data.id, res.data);
          } else {
            this.saving.set(false);
            this.saved.emit(res.data);
          }
        },
        error: (err: any) => {
          this.saving.set(false);
          const msg = err?.error?.message || this.transloco.translate('categoriesPage.errorSaving');
          this.errorMessage.set(msg);
        }
      });
    }
  }

  private uploadCategoryFile(id: number, fallbackData: Category): void {
    const file = this.selectedFile();
    if (!file) {
      this.saving.set(false);
      this.saved.emit(fallbackData);
      return;
    }

    this.categoriesService.uploadImage(id, file).subscribe({
      next: (uploadRes) => {
        this.saving.set(false);
        this.saved.emit(uploadRes.data || fallbackData);
      },
      error: () => {
        this.saving.set(false);
        // Even if image upload fails, category was successfully saved
        this.saved.emit(fallbackData);
      }
    });
  }

  onClose(): void {
    this.closed.emit();
  }
}
