import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URL } from '../tokens/api-url.token';
import { ApiResponse, CustomerProfile, UpdateProfileRequest, ChangePasswordRequest } from '../interfaces/api.models';

@Injectable({
  providedIn: 'root'
})
export class AccountService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = inject(API_URL);

  /**
   * Retrieves the current user's profile
   */
  getProfile(): Observable<ApiResponse<CustomerProfile>> {
    return this.http.get<ApiResponse<CustomerProfile>>(`${this.apiUrl}/Account/profile`);
  }

  /**
   * Updates the current user's profile
   */
  updateProfile(dto: UpdateProfileRequest): Observable<ApiResponse<CustomerProfile>> {
    return this.http.put<ApiResponse<CustomerProfile>>(`${this.apiUrl}/Account/profile`, dto);
  }

  /**
   * Changes the current user's password
   */
  changePassword(dto: ChangePasswordRequest): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.apiUrl}/Account/change-password`, dto);
  }
}
