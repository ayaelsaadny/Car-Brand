import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { CustomerService } from '../../../core/services/customer.service';
import { AuthService } from '../../../core/services/auth.service';
import { LanguageService } from '../../../core/services/language.service';
import { CustomerProfile, UpdateProfileRequest } from '../../../core/interfaces/api.models';
import { northAmericanPhoneValidator, formatNorthAmericanPhone } from '../../../core/utils/phone.utils';

@Component({
  selector: 'app-customer-profile',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    TranslocoPipe
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './customer-profile.component.html',
  styleUrls: ['./customer-profile.component.css']
})
export class CustomerProfileComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly customerService = inject(CustomerService);
  private readonly authService = inject(AuthService);
  private readonly langService = inject(LanguageService);
  private readonly transloco = inject(TranslocoService);

  readonly loading = signal<boolean>(true);
  readonly saving = signal<boolean>(false);
  readonly successMessage = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly profile = signal<CustomerProfile | null>(null);

  readonly profileForm: FormGroup = this.fb.group({
    fullName: ['', [Validators.required, Validators.minLength(3)]],
    phoneNumber: ['', [northAmericanPhoneValidator()]],
    address: ['']
  });

  get currentLang(): string {
    return this.langService.currentLang();
  }

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.customerService.getProfile().subscribe({
      next: (res) => {
        this.loading.set(false);
        if (res.data) {
          const p = res.data;
          this.profile.set(p);
          this.profileForm.patchValue({
            fullName: p.fullName || `${p.firstName || ''} ${p.lastName || ''}`.trim(),
            phoneNumber: p.phoneNumber || '',
            address: p.address || ''
          });
        }
      },
      error: (err: any) => {
        this.loading.set(false);
        const msg = err?.error?.message || this.transloco.translate('account.errorLoadingProfile');
        this.errorMessage.set(msg);
      }
    });
  }

  onSubmit(): void {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.successMessage.set(null);
    this.errorMessage.set(null);

    const val = this.profileForm.value;
    const fullName = val.fullName?.trim() || '';
    const phoneNumber = val.phoneNumber?.trim() || '';
    const address = val.address?.trim() || '';

    const formattedPhone = phoneNumber ? formatNorthAmericanPhone(phoneNumber) : '';

    const dto: UpdateProfileRequest = {
      fullName,
      phoneNumber: formattedPhone,
      address
    };

    this.customerService.updateProfile(dto).subscribe({
      next: (res) => {
        this.saving.set(false);
        this.successMessage.set(this.transloco.translate('account.profileUpdatedSuccess'));
        if (res.data) {
          this.profile.set(res.data);
        }
        this.authService.updateCurrentUserProfile(fullName, formattedPhone);
        setTimeout(() => this.successMessage.set(null), 4000);
      },
      error: (err: any) => {
        this.saving.set(false);
        const msg = err?.error?.message || this.transloco.translate('account.errorUpdatingProfile');
        this.errorMessage.set(msg);
      }
    });
  }
}
