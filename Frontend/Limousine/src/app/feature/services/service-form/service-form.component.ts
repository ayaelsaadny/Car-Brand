import { Component, ChangeDetectionStrategy, inject, signal, input, output, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { AdminServicesService } from '../../../core/services/admin-services.service';
import { ServiceItem, Category, CreateServiceRequest, UpdateServiceRequest } from '../../../core/interfaces/api.models';

@Component({
  selector: 'app-service-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    TranslocoPipe
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './service-form.component.html',
  styleUrls: ['./service-form.component.css']
})
export class ServiceFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly servicesService = inject(AdminServicesService);
  private readonly transloco = inject(TranslocoService);

  service = input<ServiceItem | null>(null);
  categories = input<Category[]>([]);
  isOpen = input<boolean>(false);

  saved = output<ServiceItem>();
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
    categoryId: [null, [Validators.required]],
    price: [null, [Validators.min(0)]],
    fromLocation: ['', [Validators.maxLength(250)]],
    toLocation: ['', [Validators.maxLength(250)]],
    displayOrder: [0, [Validators.required, Validators.min(0)]],
    imageUrl: ['', [Validators.required]],
    isActive: [true],
    allowVehicleSelection: [true]
  });

  constructor() {
    effect(() => {
      const open = this.isOpen();
      if (!open) return;

      const srv = this.service();
      const cats = this.categories();
      if (srv) {
        this.form.patchValue({
          name: srv.name,
          description: srv.description || '',
          nameFr: '',
          descriptionFr: '',
          categoryId: srv.categoryId,
          price: srv.price ?? null,
          fromLocation: srv.fromLocation || '',
          toLocation: srv.toLocation || '',
          displayOrder: srv.displayOrder,
          imageUrl: srv.imageUrl || srv.image || '',
          isActive: srv.isActive,
          allowVehicleSelection: srv.allowVehicleSelection
        });
        this.previewUrl.set(srv.imageUrl || srv.image || null);
      } else {
        const defaultCatId = cats.length > 0 ? cats[0].id : null;
        this.form.reset({
          name: '',
          description: '',
          nameFr: '',
          descriptionFr: '',
          categoryId: defaultCatId,
          price: null,
          fromLocation: '',
          toLocation: '',
          displayOrder: 0,
          imageUrl: '',
          isActive: true,
          allowVehicleSelection: true
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
    const srv = this.service();

    const translations: Record<string, { name: string; description?: string }> = {};
    if (val.name) {
      translations['en'] = { name: val.name, description: val.description || undefined };
    }
    if (val.nameFr) {
      translations['fr'] = { name: val.nameFr, description: val.descriptionFr || undefined };
    }

    if (srv) {
      // Update existing service
      const payload: UpdateServiceRequest = {
        name: val.name,
        description: val.description || undefined,
        categoryId: Number(val.categoryId),
        price: val.price !== null && val.price !== '' ? Number(val.price) : undefined,
        fromLocation: val.fromLocation || undefined,
        toLocation: val.toLocation || undefined,
        displayOrder: Number(val.displayOrder) || 0,
        imageUrl: val.imageUrl || undefined,
        isActive: Boolean(val.isActive),
        allowVehicleSelection: Boolean(val.allowVehicleSelection),
        translations: Object.keys(translations).length > 0 ? translations : undefined
      };

      this.servicesService.updateService(srv.id, payload).subscribe({
        next: (res) => {
          if (this.selectedFile()) {
            this.uploadServiceFile(srv.id, res.data);
          } else {
            this.saving.set(false);
            this.saved.emit(res.data);
          }
        },
        error: (err: any) => {
          this.saving.set(false);
          const msg = err?.error?.message || this.transloco.translate('servicesPage.errorSaving');
          this.errorMessage.set(msg);
        }
      });
    } else {
      // Create new service
      const payload: CreateServiceRequest = {
        name: val.name,
        description: val.description || undefined,
        categoryId: Number(val.categoryId),
        price: val.price !== null && val.price !== '' ? Number(val.price) : undefined,
        fromLocation: val.fromLocation || undefined,
        toLocation: val.toLocation || undefined,
        displayOrder: Number(val.displayOrder) || 0,
        imageUrl: val.imageUrl || undefined,
        isActive: Boolean(val.isActive),
        allowVehicleSelection: Boolean(val.allowVehicleSelection),
        translations: Object.keys(translations).length > 0 ? translations : undefined
      };

      this.servicesService.createService(payload).subscribe({
        next: (res) => {
          if (this.selectedFile() && res.data?.id) {
            this.uploadServiceFile(res.data.id, res.data);
          } else {
            this.saving.set(false);
            this.saved.emit(res.data);
          }
        },
        error: (err: any) => {
          this.saving.set(false);
          const msg = err?.error?.message || this.transloco.translate('servicesPage.errorSaving');
          this.errorMessage.set(msg);
        }
      });
    }
  }

  private uploadServiceFile(id: number, fallbackData: ServiceItem): void {
    const file = this.selectedFile();
    if (!file) {
      this.saving.set(false);
      this.saved.emit(fallbackData);
      return;
    }

    this.servicesService.uploadImage(id, file).subscribe({
      next: (uploadRes) => {
        this.saving.set(false);
        this.saved.emit(uploadRes.data || fallbackData);
      },
      error: () => {
        this.saving.set(false);
        this.saved.emit(fallbackData);
      }
    });
  }

  onClose(): void {
    this.closed.emit();
  }
}
