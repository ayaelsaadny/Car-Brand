import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslocoDirective, TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { AuthService } from '../../../core/services/auth.service';
import { LanguageService } from '../../../core/services/language.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    TranslocoPipe
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly langService = inject(LanguageService);
  private readonly router = inject(Router);
  private readonly transloco = inject(TranslocoService);

  readonly loading = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);

  readonly registerForm: FormGroup = this.fb.group({
    fullName: ['', [Validators.required, Validators.minLength(3)]],
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: ['', [Validators.pattern('^[+0-9\\s-]{8,20}$')]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', [Validators.required]]
  }, {
    validators: (group) => {
      const pass = group.get('password')?.value;
      const conf = group.get('confirmPassword')?.value;
      return pass === conf ? null : { passwordMismatch: true };
    }
  });

  get currentLang(): string {
    return this.langService.currentLang();
  }

  onSubmit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);

    const formValue = this.registerForm.value;

    this.authService.register({
      fullName: formValue.fullName,
      email: formValue.email,
      phoneNumber: formValue.phoneNumber,
      password: formValue.password,
      confirmPassword: formValue.confirmPassword
    }).subscribe({
      next: () => {
        this.loading.set(false);
        const lang = this.currentLang;
        this.router.navigateByUrl(`/${lang}/account/profile`);
      },
      error: (err) => {
        this.loading.set(false);
        const msg = err?.error?.message || this.transloco.translate('auth.registerError');
        this.errorMessage.set(msg);
      }
    });
  }
}
