import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URL } from '../tokens/api-url.token';
import {
  ApiResponse,
  Vehicle,
  VehicleImage,
  CreateVehicleRequest,
  UpdateVehicleRequest,
  UpdateVehicleStatusRequest
} from '../interfaces/api.models';

export interface AvailableVehiclesQuery {
  pickupDateTime?: string;
  returnDateTime?: string;
  passengerCount?: number;
  luggageCount?: number;
}

@Injectable({
  providedIn: 'root'
})
export class VehiclesService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = inject(API_URL);

  private readonly publicEndpoint = `${this.apiUrl}/Vehicles`;
  private readonly adminEndpoint = `${this.apiUrl}/admin/vehicles`;

  // ==========================================
  // Public Client APIs (/api/Vehicles)
  // ==========================================

  /**
   * Retrieves active vehicles for the public catalog
   */
  getPublicVehicles(): Observable<ApiResponse<Vehicle[]>> {
    return this.http.get<ApiResponse<Vehicle[]>>(this.publicEndpoint);
  }

  /**
   * Retrieves the default showcase vehicle
   */
  getDefaultVehicle(pickupDateTime?: string, returnDateTime?: string): Observable<ApiResponse<Vehicle>> {
    let params = new HttpParams();
    if (pickupDateTime) params = params.set('pickupDateTime', pickupDateTime);
    if (returnDateTime) params = params.set('returnDateTime', returnDateTime);
    return this.http.get<ApiResponse<Vehicle>>(`${this.publicEndpoint}/default`, { params });
  }

  /**
   * Checks available vehicles for given schedule
   */
  getAvailableVehicles(query: AvailableVehiclesQuery): Observable<ApiResponse<any>> {
    let params = new HttpParams();
    if (query.pickupDateTime) params = params.set('pickupDateTime', query.pickupDateTime);
    if (query.returnDateTime) params = params.set('returnDateTime', query.returnDateTime);
    if (query.passengerCount) params = params.set('passengerCount', query.passengerCount.toString());
    if (query.luggageCount) params = params.set('luggageCount', query.luggageCount.toString());
    return this.http.get<ApiResponse<any>>(`${this.publicEndpoint}/available`, { params });
  }

  /**
   * Retrieves public vehicle by ID
   */
  getPublicVehicleById(id: number): Observable<ApiResponse<Vehicle>> {
    return this.http.get<ApiResponse<Vehicle>>(`${this.publicEndpoint}/${id}`);
  }

  /**
   * Retrieves images for vehicle
   */
  getVehicleImages(id: number): Observable<ApiResponse<VehicleImage[]>> {
    return this.http.get<ApiResponse<VehicleImage[]>>(`${this.publicEndpoint}/${id}/images`);
  }

  // ==========================================
  // Admin & Staff Management APIs (/api/admin/vehicles)
  // ==========================================

  /**
   * Retrieves full fleet for operations (Admin & Employee)
   */
  getAllFleetVehicles(activeOnly: boolean = false): Observable<ApiResponse<Vehicle[]>> {
    const params = new HttpParams().set('activeOnly', activeOnly.toString());
    return this.http.get<ApiResponse<Vehicle[]>>(this.adminEndpoint, { params });
  }

  /**
   * Retrieves vehicle details by ID
   */
  getFleetVehicleById(id: number): Observable<ApiResponse<Vehicle>> {
    return this.http.get<ApiResponse<Vehicle>>(`${this.adminEndpoint}/${id}`);
  }

  /**
   * Adds a new vehicle to the fleet (Admin only)
   */
  createVehicle(payload: CreateVehicleRequest): Observable<ApiResponse<Vehicle>> {
    return this.http.post<ApiResponse<Vehicle>>(this.adminEndpoint, payload);
  }

  /**
   * Updates an existing vehicle's details and rates (Admin only)
   */
  updateVehicle(id: number, payload: UpdateVehicleRequest): Observable<ApiResponse<Vehicle>> {
    return this.http.put<ApiResponse<Vehicle>>(`${this.adminEndpoint}/${id}`, payload);
  }

  /**
   * Updates vehicle active/maintenance status (Admin only)
   */
  updateStatus(id: number, isActive: boolean): Observable<ApiResponse<Vehicle>> {
    const body: UpdateVehicleStatusRequest = { isActive };
    return this.http.put<ApiResponse<Vehicle>>(`${this.adminEndpoint}/${id}/status`, body);
  }

  /**
   * Sets vehicle as default fleet showcase (Admin only)
   */
  setDefault(id: number): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.adminEndpoint}/${id}/default`, {});
  }

  /**
   * Uploads vehicle photos (Admin only)
   */
  uploadImages(id: number, files: File[]): Observable<ApiResponse<VehicleImage[]>> {
    const formData = new FormData();
    files.forEach(f => formData.append('files', f));
    return this.http.post<ApiResponse<VehicleImage[]>>(`${this.adminEndpoint}/${id}/images/upload`, formData);
  }

  /**
   * Sets primary display image (Admin only)
   */
  setPrimaryImage(id: number, imageId: number): Observable<ApiResponse<void>> {
    return this.http.put<ApiResponse<void>>(`${this.adminEndpoint}/${id}/images/${imageId}/primary`, {});
  }

  /**
   * Removes an image from a vehicle (Admin only)
   */
  deleteImage(id: number, imageId: number): Observable<ApiResponse<void>> {
    return this.http.delete<ApiResponse<void>>(`${this.adminEndpoint}/${id}/images/${imageId}`);
  }

  /**
   * Archives or permanently removes a vehicle (Admin only)
   */
  deleteVehicle(id: number, hardDelete: boolean = false): Observable<ApiResponse<void>> {
    const params = new HttpParams().set('hardDelete', hardDelete.toString());
    return this.http.delete<ApiResponse<void>>(`${this.adminEndpoint}/${id}`, { params });
  }
}
