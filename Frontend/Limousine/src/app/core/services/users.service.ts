import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URL } from '../tokens/api-url.token';
import { ApiResponse, UserItem, ToggleUserStatusRequest } from '../interfaces/api.models';

@Injectable({
  providedIn: 'root'
})
export class UsersService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = inject(API_URL);

  /**
   * Retrieves all users (Admin only)
   */
  getUsers(): Observable<ApiResponse<UserItem[]>> {
    return this.http.get<ApiResponse<UserItem[]>>(`${this.apiUrl}/Users`);
  }

  /**
   * Retrieves a single user by ID (Admin only)
   */
  getUserById(id: number): Observable<ApiResponse<UserItem>> {
    return this.http.get<ApiResponse<UserItem>>(`${this.apiUrl}/Users/${id}`);
  }

  /**
   * Toggles activation status for a user (Admin only)
   */
  toggleStatus(id: number, isActive: boolean): Observable<ApiResponse<void>> {
    const body: ToggleUserStatusRequest = { isActive };
    return this.http.put<ApiResponse<void>>(`${this.apiUrl}/Users/${id}/status`, body);
  }
}
