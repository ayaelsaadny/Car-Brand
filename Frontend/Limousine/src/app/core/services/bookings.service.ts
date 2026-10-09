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

export interface CreateBookingRequest {
  serviceId: number;
  vehicleId?: number;
  pickupLocation: string;
  dropoffLocation: string;
  pickupDateTime: string;
  returnDateTime?: string;
  numberOfPassengers: number;
  numberOfLuggage: number;
  flightNumber?: string;
  customerNotes?: string;
  specialRequests?: string;
}

export interface GuestBookingRequest extends CreateBookingRequest {
  fullName: string;
  email: string;
  phoneNumber: string;
}

@Injectable({
  providedIn: 'root'
})
export class BookingsService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = inject(API_URL);

  private readonly clientEndpoint = `${this.apiUrl}/Bookings`;
  private readonly adminEndpoint = `${this.apiUrl}/admin/bookings`;

  // ==========================================
  // Client / Public Bookings APIs
  // ==========================================

  /**
   * Submit authenticated booking request
   */
  createBooking(dto: CreateBookingRequest): Observable<ApiResponse<BookingRequestItem>> {
    return this.http.post<ApiResponse<BookingRequestItem>>(this.clientEndpoint, dto);
  }

  /**
   * Submit guest booking request
   */
  createGuestBooking(dto: GuestBookingRequest): Observable<ApiResponse<BookingRequestItem>> {
    return this.http.post<ApiResponse<BookingRequestItem>>(`${this.clientEndpoint}/guest`, dto);
  }

  /**
   * Initialize booking form (services, vehicles, customer defaults)
   */
  getBookingFormInit(serviceId?: number): Observable<ApiResponse<any>> {
    let params = new HttpParams();
    if (serviceId) params = params.set('serviceId', serviceId.toString());
    return this.http.get<ApiResponse<any>>(`${this.clientEndpoint}/form-init`, { params });
  }

  /**
   * Get pre-fill profile data for booking form
   */
  getBookingFormProfile(): Observable<ApiResponse<any>> {
    return this.http.get<ApiResponse<any>>(`${this.clientEndpoint}/form-profile`);
  }

  /**
   * Get customer's own bookings
   */
  getMyBookings(): Observable<ApiResponse<BookingRequestItem[]>> {
    return this.http.get<ApiResponse<BookingRequestItem[]>>(`${this.clientEndpoint}/my`);
  }

  /**
   * Lookup booking by reference code
   */
  getByReference(reference: string): Observable<ApiResponse<BookingRequestItem>> {
    return this.http.get<ApiResponse<BookingRequestItem>>(`${this.clientEndpoint}/reference/${reference}`);
  }

  // ==========================================
  // Admin & Staff Operations APIs
  // ==========================================

  /**
   * Search bookings with pagination and filters
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
    return this.http.get<ApiResponse<PagedResult<BookingRequestItem>>>(this.adminEndpoint, { params });
  }

  /**
   * Get all fleet bookings unpaged
   */
  getAllBookings(status?: number | string): Observable<ApiResponse<BookingRequestItem[]>> {
    let params = new HttpParams();
    if (status !== undefined && status !== 'all') {
      params = params.set('status', status.toString());
    }
    return this.http.get<ApiResponse<BookingRequestItem[]>>(`${this.adminEndpoint}/all`, { params });
  }

  /**
   * Get single booking by ID
   */
  getBookingById(id: number, isAdmin: boolean = true): Observable<ApiResponse<BookingRequestItem>> {
    const url = isAdmin ? `${this.adminEndpoint}/${id}` : `${this.clientEndpoint}/${id}`;
    return this.http.get<ApiResponse<BookingRequestItem>>(url);
  }

  /**
   * Accept pending booking request (Staff & Admin)
   */
  acceptBooking(id: number, payload?: AcceptBookingRequest): Observable<ApiResponse<BookingRequestItem>> {
    return this.http.post<ApiResponse<BookingRequestItem>>(`${this.adminEndpoint}/${id}/accept`, payload || {});
  }

  /**
   * Reject pending booking request (Staff & Admin)
   */
  rejectBooking(id: number, payload: RejectBookingRequest): Observable<ApiResponse<BookingRequestItem>> {
    return this.http.post<ApiResponse<BookingRequestItem>>(`${this.adminEndpoint}/${id}/reject`, payload);
  }

  /**
   * Mark booking as completed (Admin only)
   */
  completeBooking(id: number, payload?: CompleteBookingRequest): Observable<ApiResponse<BookingRequestItem>> {
    return this.http.post<ApiResponse<BookingRequestItem>>(`${this.adminEndpoint}/${id}/complete`, payload || {});
  }

  /**
   * Cancel booking (Admin, Staff, or Customer)
   */
  cancelBooking(id: number, payload: CancelBookingRequest, isAdmin: boolean = true): Observable<ApiResponse<BookingRequestItem>> {
    const url = isAdmin ? `${this.adminEndpoint}/${id}/cancel` : `${this.clientEndpoint}/${id}/cancel`;
    return this.http.post<ApiResponse<BookingRequestItem>>(url, payload);
  }

  /**
   * Update status directly (Admin)
   */
  updateStatus(id: number, payload: UpdateBookingStatusRequest): Observable<ApiResponse<BookingRequestItem>> {
    return this.http.put<ApiResponse<BookingRequestItem>>(`${this.adminEndpoint}/${id}/status`, payload);
  }
}
