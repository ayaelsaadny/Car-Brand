import { Injectable, inject, signal, computed, PLATFORM_ID } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { isPlatformBrowser } from '@angular/common';
import { Observable, tap } from 'rxjs';
import { API_URL } from '../tokens/api-url.token';
import { ApiResponse, AuthResponse, User } from '../interfaces/api.models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly apiUrl = inject(API_URL);

  private readonly userSignal = signal<User | null>(this.getStoredUser());

  readonly currentUser = computed(() => this.userSignal());
  readonly isLoggedIn = computed(() => !!this.userSignal());
  readonly userRole = computed(() => String(this.userSignal()?.role || ''));

  // Role permissions signals
  readonly isAdmin = computed(() => {
    const role = this.userRole().toLowerCase();
    return role === 'admin' || role === '1';
  });
  readonly isEmployee = computed(() => {
    const role = this.userRole().toLowerCase();
    return role === 'employee' || role === '2';
  });
  readonly isCustomer = computed(() => {
    const role = this.userRole().toLowerCase();
    return role === 'customer' || role === '3';
  });
  readonly isStaff = computed(() => this.isAdmin() || this.isEmployee());

  hasRole(role: string): boolean {
    const current = this.userRole().toLowerCase();
    const target = (role || '').toLowerCase();
    if (current === target) return true;
    if (target === 'admin' && (current === '1' || current === 'admin')) return true;
    if (target === 'employee' && (current === '2' || current === 'employee')) return true;
    if (target === 'customer' && (current === '3' || current === 'customer')) return true;
    return false;
  }

  private getStoredUser(): User | null {
    if (isPlatformBrowser(this.platformId)) {
      try {
        const stored = localStorage.getItem('limo_user');
        return stored ? JSON.parse(stored) : null;
      } catch {
        return null;
      }
    }
    return null;
  }

  getToken(): string | null {
    if (isPlatformBrowser(this.platformId)) {
      return localStorage.getItem('limo_token');
    }
    return null;
  }

  login(credentials: { email: string; password: string }): Observable<ApiResponse<AuthResponse>> {
    return this.http.post<ApiResponse<AuthResponse>>(`${this.apiUrl}/Auth/login`, credentials).pipe(
      tap(response => {
        if (response?.data?.token) {
          this.handleAuthSuccess(response.data);
        }
      })
    );
  }

  adminLogin(credentials: { email: string; password: string }): Observable<ApiResponse<AuthResponse>> {
    return this.http.post<ApiResponse<AuthResponse>>(`${this.apiUrl}/Auth/admin/login`, credentials).pipe(
      tap(response => {
        if (response?.data?.token) {
          this.handleAuthSuccess(response.data);
        }
      })
    );
  }

  register(userData: {
    fullName: string;
    email: string;
    password: string;
    phoneNumber?: string;
    confirmPassword?: string;
  }): Observable<ApiResponse<AuthResponse>> {
    const payload = {
      ...userData,
      confirmPassword: userData.confirmPassword || userData.password,
      phoneNumber: userData.phoneNumber || '+0000000000'
    };
    return this.http.post<ApiResponse<AuthResponse>>(`${this.apiUrl}/Auth/register`, payload).pipe(
      tap(response => {
        if (response?.data?.token) {
          this.handleAuthSuccess(response.data);
        }
      })
    );
  }

  getCurrentUser(): Observable<ApiResponse<User>> {
    return this.http.get<ApiResponse<User>>(`${this.apiUrl}/Auth/me`).pipe(
      tap(response => {
        if (response?.data) {
          this.userSignal.set(response.data);
          if (isPlatformBrowser(this.platformId)) {
            localStorage.setItem('limo_user', JSON.stringify(response.data));
          }
        }
      })
    );
  }

  private handleAuthSuccess(data: AuthResponse): void {
    const user: User = data.user || {
      id: data.userId || 0,
      fullName: data.fullName || '',
      email: data.email || '',
      role: data.role || 'Customer'
    };

    if (isPlatformBrowser(this.platformId)) {
      localStorage.setItem('limo_token', data.token);
      localStorage.setItem('limo_user', JSON.stringify(user));
    }
    this.userSignal.set(user);
  }

  updateCurrentUserProfile(fullName: string, phoneNumber?: string): void {
    const curr = this.userSignal();
    if (curr) {
      const updated: User = {
        ...curr,
        fullName,
        phoneNumber: phoneNumber ?? curr.phoneNumber
      };
      this.userSignal.set(updated);
      if (isPlatformBrowser(this.platformId)) {
        localStorage.setItem('limo_user', JSON.stringify(updated));
      }
    }
  }

  logout(): void {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.removeItem('limo_token');
      localStorage.removeItem('limo_user');
    }
    this.userSignal.set(null);
  }
}
