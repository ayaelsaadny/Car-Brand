import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslocoDirective, TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { AuthService } from '../../../core/services/auth.service';
import { LanguageService } from '../../../core/services/language.service';

type LoginRoleMode = 'client' | 'employee' | 'admin';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    TranslocoPipe
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly langService = inject(LanguageService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly transloco = inject(TranslocoService);

  readonly activeRole = signal<LoginRoleMode>('client');
  readonly loading = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);

  readonly loginForm: FormGroup = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  get currentLang(): string {
    return this.langService.currentLang();
  }

  setRole(role: LoginRoleMode): void {
    this.activeRole.set(role);
    this.errorMessage.set(null);

    // Pre-fill demo credentials for quick convenience while testing
    if (role === 'admin') {
      this.loginForm.patchValue({
        email: 'admin@admin.com',
        password: 'admin123'
      });
    } else if (role === 'employee') {
      this.loginForm.patchValue({
        email: 'employee@limousine.com',
        password: 'employee123'
      });
    } else {
      this.loginForm.patchValue({
        email: '',
        password: ''
      });
    }
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);

    const { email, password } = this.loginForm.value;
    const mode = this.activeRole();

    // Use adminLogin endpoint for Admin/Employee or standard login
    const loginObs = (mode === 'admin' || mode === 'employee')
      ? this.authService.adminLogin({ email, password })
      : this.authService.login({ email, password });

    loginObs.subscribe({
      next: (res) => {
        this.loading.set(false);
        const returnUrl = this.route.snapshot.queryParams['returnUrl'];
        const lang = this.currentLang;

        if (returnUrl) {
          this.router.navigateByUrl(returnUrl);
          return;
        }

        if (this.authService.isAdmin()) {
          this.router.navigateByUrl(`/${lang}/dashboard`);
        } else if (this.authService.isEmployee()) {
          this.router.navigateByUrl(`/${lang}/dashboard`);
        } else {
          this.router.navigateByUrl(`/${lang}/account/profile`);
        }
      },
      error: (err) => {
        this.loading.set(false);
        const msg = err?.error?.message || this.transloco.translate('auth.invalidCredentials');
        this.errorMessage.set(msg);
      }
    });
  }
}
