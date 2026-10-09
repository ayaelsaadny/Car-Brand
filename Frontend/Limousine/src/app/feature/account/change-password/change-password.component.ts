import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoDirective, TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { AccountService } from '../../../core/services/account.service';
import { LanguageService } from '../../../core/services/language.service';

@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    TranslocoPipe
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './change-password.component.html',
  styleUrls: ['./change-password.component.css']
})
export class ChangePasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly accountService = inject(AccountService);
  private readonly langService = inject(LanguageService);
  private readonly transloco = inject(TranslocoService);

  readonly saving = signal<boolean>(false);
  readonly successMessage = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);

  readonly passwordForm: FormGroup = this.fb.group({
    currentPassword: ['', [Validators.required]],
    newPassword: ['', [Validators.required, Validators.minLength(6)]],
    confirmNewPassword: ['', [Validators.required]]
  }, {
    validators: (group) => {
      const np = group.get('newPassword')?.value;
      const cnp = group.get('confirmNewPassword')?.value;
      return np === cnp ? null : { passwordMismatch: true };
    }
  });

  get currentLang(): string {
    return this.langService.currentLang();
  }

  onSubmit(): void {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.successMessage.set(null);
    this.errorMessage.set(null);

    const { currentPassword, newPassword, confirmNewPassword } = this.passwordForm.value;

    this.accountService.changePassword({
      currentPassword,
      newPassword,
      confirmNewPassword
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.successMessage.set(this.transloco.translate('account.passwordChangedSuccess'));
        this.passwordForm.reset();
      },
      error: (err) => {
        this.saving.set(false);
        const msg = err?.error?.message || this.transloco.translate('account.passwordChangeError');
        this.errorMessage.set(msg);
      }
    });
  }
}
