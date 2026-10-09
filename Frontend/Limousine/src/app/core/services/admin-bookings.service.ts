import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URL } from '../tokens/api-url.token';
import {
  ApiResponse,
  BookingRequestItem,
  PagedResult,
  BookingSearchFilter,
  UpdateBookingStatusRequest,
  AcceptBookingRequest,
  RejectBookingRequest,
  CancelBookingRequest,
  CompleteBookingRequest
} from '../interfaces/api.models';

@Injectable({
  providedIn: 'root'
})
export class AdminBookingsService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = inject(API_URL);

  private readonly endpoint = `${this.apiUrl}/admin/bookings`;
  private readonly clientEndpoint = `${this.apiUrl}/Bookings`;

  /**
   * Search bookings with pagination and filters (Admin)
   */
  searchBookings(filter?: BookingSearchFilter): Observable<ApiResponse<PagedResult<BookingRequestItem>>> {
    let params = new HttpParams();
    if (filter) {
      if (filter.search) params = params.set('search', filter.search);
      if (filter.customerName) params = params.set('customerName', filter.customerName);
      if (filter.customerEmail) params = params.set('customerEmail', filter.customerEmail);
      if (filter.customerPhone) params = params.set('customerPhone', filter.customerPhone);
      if (filter.bookingReference) params = params.set('bookingReference', filter.bookingReference);
      if (filter.status !== undefined && filter.status !== 'all') params = params.set('status', filter.status.toString());
      if (filter.serviceId !== undefined) params = params.set('serviceId', filter.serviceId.toString());
      if (filter.vehicleId !== undefined) params = params.set('vehicleId', filter.vehicleId.toString());
      if (filter.fromDate) params = params.set('fromDate', filter.fromDate);
      if (filter.toDate) params = params.set('toDate', filter.toDate);
      if (filter.pageNumber !== undefined) params = params.set('pageNumber', filter.pageNumber.toString());
      if (filter.pageSize !== undefined) params = params.set('pageSize', filter.pageSize.toString());
    }
    return this.http.get<ApiResponse<PagedResult<BookingRequestItem>>>(this.endpoint, { params });
  }

  /**
   * Get all bookings unpaged (Admin)
   */
  getAllBookings(status?: number | string): Observable<ApiResponse<BookingRequestItem[]>> {
    let params = new HttpParams();
    if (status !== undefined && status !== 'all') {
      params = params.set('status', status.toString());
    }
    return this.http.get<ApiResponse<BookingRequestItem[]>>(`${this.endpoint}/all`, { params });
  }

  /**
   * Get client's own bookings (/api/Bookings/my)
   */
  getMyBookings(): Observable<ApiResponse<BookingRequestItem[]>> {
    return this.http.get<ApiResponse<BookingRequestItem[]>>(`${this.clientEndpoint}/my`);
  }

  /**
   * Get booking details by ID
   */
  getBookingById(id: number, isAdmin: boolean = true): Observable<ApiResponse<BookingRequestItem>> {
    const url = isAdmin ? `${this.endpoint}/${id}` : `${this.clientEndpoint}/${id}`;
    return this.http.get<ApiResponse<BookingRequestItem>>(url);
  }

  /**
   * Update status directly (Admin)
   */
  updateStatus(id: number, payload: UpdateBookingStatusRequest): Observable<ApiResponse<BookingRequestItem>> {
    return this.http.put<ApiResponse<BookingRequestItem>>(`${this.endpoint}/${id}/status`, payload);
  }

  /**
   * Accept a pending booking (Admin)
   */
  acceptBooking(id: number, payload?: AcceptBookingRequest): Observable<ApiResponse<BookingRequestItem>> {
    return this.http.post<ApiResponse<BookingRequestItem>>(`${this.endpoint}/${id}/accept`, payload || {});
  }

  /**
   * Reject a pending booking (Admin)
   */
  rejectBooking(id: number, payload: RejectBookingRequest): Observable<ApiResponse<BookingRequestItem>> {
    return this.http.post<ApiResponse<BookingRequestItem>>(`${this.endpoint}/${id}/reject`, payload);
  }

  /**
   * Complete an in-progress/confirmed booking (Admin)
   */
  completeBooking(id: number, payload?: CompleteBookingRequest): Observable<ApiResponse<BookingRequestItem>> {
    return this.http.post<ApiResponse<BookingRequestItem>>(`${this.endpoint}/${id}/complete`, payload || {});
  }

  /**
   * Cancel booking (Admin or Customer)
   */
  cancelBooking(id: number, payload: CancelBookingRequest, isAdmin: boolean = true): Observable<ApiResponse<BookingRequestItem>> {
    const url = isAdmin ? `${this.endpoint}/${id}/cancel` : `${this.clientEndpoint}/${id}/cancel`;
    return this.http.post<ApiResponse<BookingRequestItem>>(url, payload);
  }
}
