import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, catchError, of } from 'rxjs';
import { environment } from '../../../enviroment/enviroment';
import { ApiResponse, Vehicle, ServiceItem, Category, WebsiteSetting } from '../interfaces/api.models';

@Injectable({
  providedIn: 'root'
})
export class PublicService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  getServices(): Observable<ServiceItem[]> {
    return this.http.get<ApiResponse<ServiceItem[]>>(`${this.apiUrl}/Services`).pipe(
      map(res => res.data || []),
      catchError(err => {
        console.error('Error fetching services:', err);
        return of([]);
      })
    );
  }

  getVehicles(): Observable<Vehicle[]> {
    return this.http.get<ApiResponse<Vehicle[]>>(`${this.apiUrl}/Vehicles`).pipe(
      map(res => res.data || []),
      catchError(err => {
        console.error('Error fetching vehicles:', err);
        return of([]);
      })
    );
  }

  getCategories(): Observable<Category[]> {
    return this.http.get<ApiResponse<Category[]>>(`${this.apiUrl}/Categories`).pipe(
      map(res => res.data || []),
      catchError(err => {
        console.error('Error fetching categories:', err);
        return of([]);
      })
    );
  }

  getSettings(): Observable<WebsiteSetting[]> {
    return this.http.get<ApiResponse<WebsiteSetting[]>>(`${this.apiUrl}/Settings`).pipe(
      map(res => res.data || []),
      catchError(err => {
        console.error('Error fetching settings:', err);
        return of([]);
      })
    );
  }
}
