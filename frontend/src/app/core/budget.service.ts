import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { BudgetItemDto, BudgetMonthResponse, BudgetPayload } from './models';

@Injectable({ providedIn: 'root' })
export class BudgetService {
  private readonly http = inject(HttpClient);

  getMonth(month: string): Observable<BudgetMonthResponse> {
    return this.http.get<BudgetMonthResponse>('/api/budget', {
      params: new HttpParams().set('month', month),
    });
  }

  create(payload: BudgetPayload): Observable<BudgetItemDto> {
    return this.http.post<BudgetItemDto>('/api/budget', payload);
  }

  update(id: number, payload: BudgetPayload): Observable<BudgetItemDto> {
    return this.http.put<BudgetItemDto>(`/api/budget/${id}`, payload);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`/api/budget/${id}`);
  }
}
