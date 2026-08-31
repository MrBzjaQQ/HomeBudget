import { Injectable, signal } from '@angular/core';

const KEY = 'budget.lastCategory';

export interface LastCategory {
  kind: 'INCOME' | 'EXPENSE';
  categoryId: number;
}

@Injectable({ providedIn: 'root' })
export class LastCategoryService {
  private readonly stored = readStored();
  readonly kind = signal<LastCategory['kind']>(this.stored?.kind ?? 'EXPENSE');
  readonly categoryId = signal<number | null>(this.stored?.categoryId ?? null);

  remember(kind: LastCategory['kind'], categoryId: number): void {
    this.kind.set(kind);
    this.categoryId.set(categoryId);
    localStorage.setItem(KEY, JSON.stringify({ kind, categoryId } satisfies LastCategory));
  }

  categoryFor(kind: LastCategory['kind']): number | null {
    return this.stored?.kind === kind ? this.stored.categoryId : null;
  }
}

function readStored(): LastCategory | null {
  const raw = localStorage.getItem(KEY);
  if (!raw) {
    return null;
  }
  try {
    return JSON.parse(raw) as LastCategory;
  } catch {
    return null;
  }
}
