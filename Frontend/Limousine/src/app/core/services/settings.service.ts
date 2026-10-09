import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, forkJoin } from 'rxjs';
import { API_URL } from '../tokens/api-url.token';
import { ApiResponse, WebsiteSetting } from '../interfaces/api.models';

export interface UpdateSettingRequest {
  value: string;
}

@Injectable({
  providedIn: 'root'
})
export class SettingsService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = inject(API_URL);

  private readonly endpoint = `${this.apiUrl}/Settings`;

  /**
   * Retrieves all system and website settings (/api/Settings)
   */
  getAllSettings(): Observable<ApiResponse<WebsiteSetting[]>> {
    return this.http.get<ApiResponse<WebsiteSetting[]>>(this.endpoint);
  }

  /**
   * Retrieves a single setting by its unique key (/api/Settings/{key})
   */
  getSettingByKey(key: string): Observable<ApiResponse<WebsiteSetting>> {
    return this.http.get<ApiResponse<WebsiteSetting>>(`${this.endpoint}/${key}`);
  }

  /**
   * Updates a single setting value (Admin only: PUT /api/Settings/{key})
   */
  updateSetting(key: string, value: string): Observable<ApiResponse<WebsiteSetting>> {
    return this.http.put<ApiResponse<WebsiteSetting>>(`${this.endpoint}/${key}`, { value });
  }

  /**
   * Updates multiple settings simultaneously
   */
  updateSettingsBatch(items: { key: string; value: string }[]): Observable<ApiResponse<WebsiteSetting>[]> {
    if (items.length === 0) {
      return forkJoin([]);
    }
    const requests = items.map(item => this.updateSetting(item.key, item.value));
    return forkJoin(requests);
  }
}
