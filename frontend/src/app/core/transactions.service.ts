import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import {
  SummaryCategoryRow,
  SummaryMonthRow,
  TransactionDto,
  TransactionListParams,
  TransactionListResponse,
  TransactionPayload,
} from './models';

interface SummaryEnvelope<T> {
  groupBy: string;
  rows: T[];
}

@Injectable({ providedIn: 'root' })
export class TransactionsService {
  private readonly http = inject(HttpClient);

  list(params: TransactionListParams): Observable<TransactionListResponse> {
    let httpParams = new HttpParams();
    if (params.from) {
      httpParams = httpParams.set('from', params.from);
    }
    if (params.to) {
      httpParams = httpParams.set('to', params.to);
    }
    if (params.userId !== undefined) {
      httpParams = httpParams.set('userId', String(params.userId));
    }
    if (params.categoryId && params.categoryId.length > 0) {
      for (const id of params.categoryId) {
        httpParams = httpParams.append('categoryId', String(id));
      }
    }
    if (params.kind) {
      httpParams = httpParams.set('kind', params.kind);
    }
    if (params.q) {
      httpParams = httpParams.set('q', params.q);
    }
    httpParams = httpParams
      .set('page', String(params.page ?? 1))
      .set('pageSize', String(params.pageSize ?? 50));
    if (params.sort) {
      httpParams = httpParams.set('sort', params.sort);
    }
    if (params.dir) {
      httpParams = httpParams.set('dir', params.dir);
    }
    return this.http.get<TransactionListResponse>('/api/transactions', { params: httpParams });
  }

  get(id: number): Observable<TransactionDto> {
    return this.http.get<TransactionDto>(`/api/transactions/${id}`);
  }

  create(payload: TransactionPayload): Observable<TransactionDto> {
    return this.http.post<TransactionDto>('/api/transactions', payload);
  }

  update(id: number, payload: TransactionPayload): Observable<TransactionDto> {
    return this.http.put<TransactionDto>(`/api/transactions/${id}`, payload);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`/api/transactions/${id}`);
  }

  summaryCategory(from: string, to: string): Observable<SummaryCategoryRow[]> {
    return this.http
      .get<SummaryEnvelope<SummaryCategoryRow>>('/api/transactions/summary', {
        params: new HttpParams().set('from', from).set('to', to).set('groupBy', 'category'),
      })
      .pipe(map((envelope) => envelope.rows));
  }

  summaryMonth(from: string, to: string): Observable<SummaryMonthRow[]> {
    return this.http
      .get<SummaryEnvelope<SummaryMonthRow>>('/api/transactions/summary', {
        params: new HttpParams().set('from', from).set('to', to).set('groupBy', 'month'),
      })
      .pipe(map((envelope) => envelope.rows));
  }
}
