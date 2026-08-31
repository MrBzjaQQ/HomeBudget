import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { CategoryDto, TxnKind } from './models';

export interface CategoryPayload {
  name: string;
  kind: TxnKind;
  parentId?: number | null;
  color?: string | null;
  sortOrder?: number;
  isArchived?: boolean;
}

@Injectable({ providedIn: 'root' })
export class CategoriesService {
  private readonly http = inject(HttpClient);

  tree(kind?: TxnKind, includeArchived = false): Observable<CategoryDto[]> {
    let params = new HttpParams();
    if (kind) {
      params = params.set('kind', kind);
    }
    if (includeArchived) {
      params = params.set('includeArchived', 'true');
    }
    return this.http.get<CategoryDto[]>('/api/categories', { params });
  }

  create(payload: CategoryPayload): Observable<CategoryDto> {
    return this.http.post<CategoryDto>('/api/categories', payload);
  }

  update(id: number, payload: CategoryPayload): Observable<CategoryDto> {
    return this.http.put<CategoryDto>(`/api/categories/${id}`, payload);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`/api/categories/${id}`);
  }
}

export function flattenCategoryTree(tree: CategoryDto[]): CategoryDto[] {
  const flat: CategoryDto[] = [];
  const walk = (nodes: CategoryDto[]): void => {
    for (const n of nodes) {
      flat.push(n);
      if (n.children) {
        walk(n.children);
      }
    }
  };
  walk(tree);
  return flat;
}
