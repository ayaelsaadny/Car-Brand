import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URL } from '../tokens/api-url.token';
import {
  ApiResponse,
  Category,
  CreateCategoryRequest,
  UpdateCategoryRequest,
  UpdateCategoryStatusRequest,
  UpdateCategoryDisplayOrderRequest
} from '../interfaces/api.models';

@Injectable({
  providedIn: 'root'
})
export class AdminCategoriesService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = inject(API_URL);

  private readonly endpoint = `${this.apiUrl}/admin/categories`;
  private readonly publicEndpoint = `${this.apiUrl}/Categories`;

  /**
   * Retrieves all categories for Admin or Employee
   */
  getAllCategories(activeOnly?: boolean, includeServices: boolean = false, isAdmin: boolean = true): Observable<ApiResponse<Category[]>> {
    const url = isAdmin ? this.endpoint : this.publicEndpoint;
    let params = new HttpParams().set('includeServices', includeServices.toString());
    if (activeOnly !== undefined && isAdmin) {
      params = params.set('activeOnly', activeOnly.toString());
    }
    return this.http.get<ApiResponse<Category[]>>(url, { params });
  }

  /**
   * Retrieves a category by ID
   */
  getCategoryById(id: number, includeServices: boolean = false, isAdmin: boolean = true): Observable<ApiResponse<Category>> {
    const url = isAdmin ? `${this.endpoint}/${id}` : `${this.publicEndpoint}/${id}`;
    const params = new HttpParams().set('includeServices', includeServices.toString());
    return this.http.get<ApiResponse<Category>>(url, { params });
  }

  /**
   * Retrieves services belonging to a category
   */
  getCategoryServices(id: number): Observable<ApiResponse<any[]>> {
    return this.http.get<ApiResponse<any[]>>(`${this.publicEndpoint}/${id}/services`);
  }

  /**
   * Creates a new category (Admin only)
   */
  createCategory(payload: CreateCategoryRequest): Observable<ApiResponse<Category>> {
    return this.http.post<ApiResponse<Category>>(this.endpoint, payload);
  }

  /**
   * Updates an existing category (Admin only)
   */
  updateCategory(id: number, payload: UpdateCategoryRequest): Observable<ApiResponse<Category>> {
    return this.http.put<ApiResponse<Category>>(`${this.endpoint}/${id}`, payload);
  }

  /**
   * Updates category active status (Admin only)
   */
  updateStatus(id: number, isActive: boolean): Observable<ApiResponse<Category>> {
    const body: UpdateCategoryStatusRequest = { isActive };
    return this.http.put<ApiResponse<Category>>(`${this.endpoint}/${id}/status`, body);
  }

  /**
   * Updates category display order (Admin only)
   */
  updateDisplayOrder(id: number, displayOrder: number): Observable<ApiResponse<Category>> {
    const body: UpdateCategoryDisplayOrderRequest = { displayOrder };
    return this.http.put<ApiResponse<Category>>(`${this.endpoint}/${id}/display-order`, body);
  }

  /**
   * Uploads an image file for a category (Admin only)
   */
  uploadImage(id: number, file: File): Observable<ApiResponse<Category>> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<ApiResponse<Category>>(`${this.endpoint}/${id}/image`, formData);
  }

  /**
   * Archives or permanently deletes a category (Admin only)
   */
  deleteCategory(id: number, hardDelete: boolean = false): Observable<ApiResponse<void>> {
    const params = new HttpParams().set('hardDelete', hardDelete.toString());
    return this.http.delete<ApiResponse<void>>(`${this.endpoint}/${id}`, { params });
  }
}
