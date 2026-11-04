import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URL } from '../tokens/api-url.token';
import { ApiResponse, Review } from '../interfaces/api.models';

@Injectable({
  providedIn: 'root'
})
export class AdminReviewsService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = inject(API_URL);

  private readonly endpoint = `${this.apiUrl}/admin/reviews`;

  /**
   * Retrieves all reviews for moderation (optional filter by isApproved).
   */
  getAllReviews(isApproved?: boolean): Observable<ApiResponse<Review[]>> {
    let params = new HttpParams();
    if (isApproved !== undefined) {
      params = params.set('isApproved', isApproved.toString());
    }
    return this.http.get<ApiResponse<Review[]>>(this.endpoint, { params });
  }

  /**
   * Approves a review.
   */
  approveReview(id: number): Observable<ApiResponse> {
    return this.http.put<ApiResponse>(`${this.endpoint}/${id}/approve`, {});
  }

  /**
   * Rejects a review.
   */
  rejectReview(id: number): Observable<ApiResponse> {
    return this.http.put<ApiResponse>(`${this.endpoint}/${id}/reject`, {});
  }

  /**
   * Permanently deletes a review.
   */
  deleteReview(id: number): Observable<ApiResponse> {
    return this.http.delete<ApiResponse>(`${this.endpoint}/${id}`);
  }
}
