import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { blobToDownload } from './download.util';

@Injectable({ providedIn: 'root' })
export class ExportService {
  private readonly http = inject(HttpClient);

  month(year: number, month: number, userId?: number): Observable<Blob> {
    let params = new HttpParams().set('year', String(year)).set('month', String(month));
    if (userId !== undefined) {
      params = params.set('userId', String(userId));
    }
    return this.http
      .get('/api/export/month', { params, responseType: 'blob' as const })
      .pipe(map((blob) => blobToDownload(blob as Blob, `Бюджет_${year}-${String(month).padStart(2, '0')}.xlsx`)));
  }

  year(year: number, userId?: number): Observable<Blob> {
    let params = new HttpParams().set('year', String(year));
    if (userId !== undefined) {
      params = params.set('userId', String(userId));
    }
    return this.http
      .get('/api/export/year', { params, responseType: 'blob' as const })
      .pipe(map((blob) => blobToDownload(blob as Blob, `Бюджет_${year}.xlsx`)));
  }
}
