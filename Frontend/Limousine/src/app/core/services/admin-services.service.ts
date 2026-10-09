import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URL } from '../tokens/api-url.token';
import {
  ApiResponse,
  ServiceItem,
  CreateServiceRequest,
  UpdateServiceRequest,
  UpdateServiceStatusRequest,
  UpdateServiceDisplayOrderRequest,
  ServiceFilterQuery
} from '../interfaces/api.models';

@Injectable({
  providedIn: 'root'
})
export class AdminServicesService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = inject(API_URL);

  private readonly endpoint = `${this.apiUrl}/admin/services`;
  private readonly publicEndpoint = `${this.apiUrl}/Services`;

  /**
   * Retrieves all services with optional query filters (Admin or Employee)
   */
  getAllServices(query?: ServiceFilterQuery, isAdmin: boolean = true): Observable<ApiResponse<ServiceItem[]>> {
    const url = isAdmin ? this.endpoint : this.publicEndpoint;
    let params = new HttpParams();
    if (query) {
      if (query.categoryId !== undefined) params = params.set('categoryId', query.categoryId.toString());
      if (query.category) params = params.set('category', query.category);
      if (query.search) params = params.set('search', query.search);
      if (query.minPrice !== undefined) params = params.set('minPrice', query.minPrice.toString());
      if (query.maxPrice !== undefined) params = params.set('maxPrice', query.maxPrice.toString());
      if (query.allowVehicleSelection !== undefined) params = params.set('allowVehicleSelection', query.allowVehicleSelection.toString());
      if (query.sortBy) params = params.set('sortBy', query.sortBy);
      if (query.activeOnly !== undefined && isAdmin) params = params.set('activeOnly', query.activeOnly.toString());
    }
    return this.http.get<ApiResponse<ServiceItem[]>>(url, { params });
  }

  /**
   * Retrieves a single service by ID
   */
  getServiceById(id: number, isAdmin: boolean = true): Observable<ApiResponse<ServiceItem>> {
    const url = isAdmin ? `${this.endpoint}/${id}` : `${this.publicEndpoint}/${id}`;
    return this.http.get<ApiResponse<ServiceItem>>(url);
  }

  /**
   * Retrieves available vehicles for a given service and date/passengers
   */
  getAvailableVehicles(serviceId: number, queryParams?: any): Observable<ApiResponse<any>> {
    let params = new HttpParams();
    if (queryParams) {
      if (queryParams.pickupDateTime) params = params.set('pickupDateTime', queryParams.pickupDateTime);
      if (queryParams.returnDateTime) params = params.set('returnDateTime', queryParams.returnDateTime);
      if (queryParams.numberOfPassengers) params = params.set('numberOfPassengers', queryParams.numberOfPassengers.toString());
      if (queryParams.numberOfLuggage) params = params.set('numberOfLuggage', queryParams.numberOfLuggage.toString());
    }
    return this.http.get<ApiResponse<any>>(`${this.publicEndpoint}/${serviceId}/available-vehicles`, { params });
  }

  /**
   * Creates a new service (Admin only)
   */
  createService(payload: CreateServiceRequest): Observable<ApiResponse<ServiceItem>> {
    return this.http.post<ApiResponse<ServiceItem>>(this.endpoint, payload);
  }

  /**
   * Updates an existing service (Admin only)
   */
  updateService(id: number, payload: UpdateServiceRequest): Observable<ApiResponse<ServiceItem>> {
    return this.http.put<ApiResponse<ServiceItem>>(`${this.endpoint}/${id}`, payload);
  }

  /**
   * Sets active/inactive status (Admin only)
   */
  updateStatus(id: number, isActive: boolean): Observable<ApiResponse<ServiceItem>> {
    const body: UpdateServiceStatusRequest = { isActive };
    return this.http.put<ApiResponse<ServiceItem>>(`${this.endpoint}/${id}/status`, body);
  }

  /**
   * Updates display order (Admin only)
   */
  updateDisplayOrder(id: number, displayOrder: number): Observable<ApiResponse<ServiceItem>> {
    const body: UpdateServiceDisplayOrderRequest = { displayOrder };
    return this.http.put<ApiResponse<ServiceItem>>(`${this.endpoint}/${id}/display-order`, body);
  }

  /**
   * Uploads an image file for a service (Admin only)
   */
  uploadImage(id: number, file: File): Observable<ApiResponse<ServiceItem>> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<ApiResponse<ServiceItem>>(`${this.endpoint}/${id}/image`, formData);
  }

  /**
   * Archives or permanently deletes a service (Admin only)
   */
  deleteService(id: number, hardDelete: boolean = false): Observable<ApiResponse<void>> {
    const params = new HttpParams().set('hardDelete', hardDelete.toString());
    return this.http.delete<ApiResponse<void>>(`${this.endpoint}/${id}`, { params });
  }
}
