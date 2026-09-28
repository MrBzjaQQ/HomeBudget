import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  SavingsContainerDto,
  SavingsContainerPayload,
  SavingsEntryDto,
  SavingsEntryPayload,
  SavingsMonthResponse,
  SavingsSummary,
} from './models';

@Injectable({ providedIn: 'root' })
export class SavingsService {
  private readonly http = inject(HttpClient);

  listContainers(): Observable<SavingsContainerDto[]> {
    return this.http.get<SavingsContainerDto[]>('/api/savings/containers');
  }

  createContainer(payload: SavingsContainerPayload): Observable<SavingsContainerDto> {
    return this.http.post<SavingsContainerDto>('/api/savings/containers', payload);
  }

  updateContainer(id: number, payload: SavingsContainerPayload): Observable<SavingsContainerDto> {
    return this.http.put<SavingsContainerDto>(`/api/savings/containers/${id}`, payload);
  }

  deleteContainer(id: number): Observable<void> {
    return this.http.delete<void>(`/api/savings/containers/${id}`);
  }

  getMonth(month: string): Observable<SavingsMonthResponse> {
    return this.http.get<SavingsMonthResponse>('/api/savings', {
      params: new HttpParams().set('month', month),
    });
  }

  createEntry(payload: SavingsEntryPayload): Observable<SavingsEntryDto> {
    return this.http.post<SavingsEntryDto>('/api/savings', payload);
  }

  updateEntry(id: number, payload: SavingsEntryPayload): Observable<SavingsEntryDto> {
    return this.http.put<SavingsEntryDto>(`/api/savings/${id}`, payload);
  }

  deleteEntry(id: number): Observable<void> {
    return this.http.delete<void>(`/api/savings/${id}`);
  }

  getSummary(to: string): Observable<SavingsSummary> {
    return this.http.get<SavingsSummary>('/api/savings/summary', {
      params: new HttpParams().set('to', to),
    });
  }
}
