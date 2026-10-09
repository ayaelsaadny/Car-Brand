import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URL } from '../tokens/api-url.token';
import {
  ApiResponse,
  CustomerProfile,
  UpdateProfileRequest,
  BookingRequestItem
} from '../interfaces/api.models';

@Injectable({
  providedIn: 'root'
})
export class CustomerService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = inject(API_URL);

  private readonly endpoint = `${this.apiUrl}/Customer`;

  /**
   * Retrieves the logged-in customer's profile (/api/Customer/profile or /me)
   */
  getProfile(): Observable<ApiResponse<CustomerProfile>> {
    return this.http.get<ApiResponse<CustomerProfile>>(`${this.endpoint}/profile`);
  }

  /**
   * Updates the logged-in customer's profile (/api/Customer/profile)
   */
  updateProfile(dto: UpdateProfileRequest): Observable<ApiResponse<CustomerProfile>> {
    return this.http.put<ApiResponse<CustomerProfile>>(`${this.endpoint}/profile`, dto);
  }

  /**
   * Retrieves all bookings belonging to the logged-in customer (/api/Customer/me/bookings)
   */
  getMyBookings(): Observable<ApiResponse<BookingRequestItem[]>> {
    return this.http.get<ApiResponse<BookingRequestItem[]>>(`${this.endpoint}/me/bookings`);
  }

  /**
   * Retrieves specific booking details by ID owned by the customer (/api/Customer/me/bookings/{id})
   */
  getMyBookingById(id: number): Observable<ApiResponse<BookingRequestItem>> {
    return this.http.get<ApiResponse<BookingRequestItem>>(`${this.endpoint}/me/bookings/${id}`);
  }

  /**
   * Cancels a booking belonging to the customer (/api/Bookings/{id}/cancel)
   */
  cancelMyBooking(id: number, cancellationReason?: string): Observable<ApiResponse<BookingRequestItem>> {
    return this.http.post<ApiResponse<BookingRequestItem>>(`${this.apiUrl}/Bookings/${id}/cancel`, {
      cancellationReason
    });
  }
}
