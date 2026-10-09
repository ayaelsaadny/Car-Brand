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

@Injectable({
  providedIn: 'root'
})
export class AdminVehiclesService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = inject(API_URL);

  private readonly endpoint = `${this.apiUrl}/admin/vehicles`;

  /**
   * Retrieves all vehicles for Admin & Operations
   */
  getAllVehicles(activeOnly?: boolean): Observable<ApiResponse<Vehicle[]>> {
    let params = new HttpParams();
    if (activeOnly !== undefined) {
      params = params.set('activeOnly', activeOnly.toString());
    }
    return this.http.get<ApiResponse<Vehicle[]>>(this.endpoint, { params });
  }

  /**
   * Retrieves a single vehicle by ID
   */
  getVehicleById(id: number): Observable<ApiResponse<Vehicle>> {
    return this.http.get<ApiResponse<Vehicle>>(`${this.endpoint}/${id}`);
  }

  /**
   * Adds a new luxury limousine to the fleet (Admin only)
   */
  createVehicle(payload: CreateVehicleRequest): Observable<ApiResponse<Vehicle>> {
    return this.http.post<ApiResponse<Vehicle>>(this.endpoint, payload);
  }

  /**
   * Updates vehicle details and pricing (Admin only)
   */
  updateVehicle(id: number, payload: UpdateVehicleRequest): Observable<ApiResponse<Vehicle>> {
    return this.http.put<ApiResponse<Vehicle>>(`${this.endpoint}/${id}`, payload);
  }

  /**
   * Updates vehicle active/maintenance status (Admin only)
   */
  updateStatus(id: number, isActive: boolean): Observable<ApiResponse<Vehicle>> {
    const body: UpdateVehicleStatusRequest = { isActive };
    return this.http.put<ApiResponse<Vehicle>>(`${this.endpoint}/${id}/status`, body);
  }

  /**
   * Sets vehicle as the default fleet showcase (Admin only)
   */
  setDefault(id: number): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.endpoint}/${id}/default`, {});
  }

  /**
   * Uploads multiple images for a vehicle (Admin only)
   */
  uploadImages(id: number, files: File[]): Observable<ApiResponse<VehicleImage[]>> {
    const formData = new FormData();
    files.forEach(f => formData.append('files', f));
    return this.http.post<ApiResponse<VehicleImage[]>>(`${this.endpoint}/${id}/images/upload`, formData);
  }

  /**
   * Sets primary display image (Admin only)
   */
  setPrimaryImage(id: number, imageId: number): Observable<ApiResponse<void>> {
    return this.http.put<ApiResponse<void>>(`${this.endpoint}/${id}/images/${imageId}/primary`, {});
  }

  /**
   * Deletes a single vehicle image (Admin only)
   */
  deleteImage(id: number, imageId: number): Observable<ApiResponse<void>> {
    return this.http.delete<ApiResponse<void>>(`${this.endpoint}/${id}/images/${imageId}`);
  }

  /**
   * Deactivates or permanently deletes a vehicle (Admin only)
   */
  deleteVehicle(id: number, hardDelete: boolean = false): Observable<ApiResponse<void>> {
    const params = new HttpParams().set('hardDelete', hardDelete.toString());
    return this.http.delete<ApiResponse<void>>(`${this.endpoint}/${id}`, { params });
  }
}
