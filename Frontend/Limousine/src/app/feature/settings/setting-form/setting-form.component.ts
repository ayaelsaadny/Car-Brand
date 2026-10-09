import { Component, ChangeDetectionStrategy, inject, signal, input, output, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { SettingsService } from '../../../core/services/settings.service';
import { WebsiteSetting } from '../../../core/interfaces/api.models';

@Component({
  selector: 'app-setting-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    TranslocoPipe
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './setting-form.component.html',
  styleUrls: ['./setting-form.component.css']
})
export class SettingFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly settingsService = inject(SettingsService);
  private readonly transloco = inject(TranslocoService);

  setting = input<WebsiteSetting | null>(null);
  isOpen = input<boolean>(false);

  saved = output<WebsiteSetting>();
  closed = output<void>();

  readonly saving = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form: FormGroup = this.fb.group({
    key: [{ value: '', disabled: true }],
    value: ['', [Validators.required]],
    description: [{ value: '', disabled: true }],
    group: [{ value: '', disabled: true }]
  });

  constructor() {
    effect(() => {
      const s = this.setting();
      if (s) {
        this.form.patchValue({
          key: s.key,
          value: s.value,
          description: s.description || '',
          group: s.group || 'General'
        });
      } else {
        this.form.reset({
          key: '',
          value: '',
          description: '',
          group: 'General'
        });
      }
      this.errorMessage.set(null);
    });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const s = this.setting();
    if (!s) return;

    this.saving.set(true);
    this.errorMessage.set(null);

    const newValue = this.form.get('value')?.value ?? '';

    this.settingsService.updateSetting(s.key, newValue).subscribe({
      next: (res) => {
        this.saving.set(false);
        this.saved.emit(res.data);
      },
      error: (err: any) => {
        this.saving.set(false);
        const msg = err?.error?.message || this.transloco.translate('settingsPage.savedError');
        this.errorMessage.set(msg);
      }
    });
  }

  onClose(): void {
    this.closed.emit();
  }
}
