import { CommonModule } from '@angular/common';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/auth.service';
import { CategoriesService } from '../../core/categories.service';
import { BudgetService } from '../../core/budget.service';
import { MonthTitlePipe, MoneyPipe, formatMoney } from '../../core/format.pipe';
import {
  ApiError,
  BudgetCategoryRow,
  BudgetItemDto,
  BudgetMonthResponse,
  BudgetPayload,
  CategoryDto,
} from '../../core/models';
import { ToastService } from '../../core/toast.service';

interface FormCategory {
  id: number;
  label: string;
}

interface BudgetGroup {
  row: BudgetCategoryRow;
  items: BudgetItemDto[];
}

@Component({
  selector: 'app-budget',
  imports: [CommonModule, FormsModule, MonthTitlePipe, MoneyPipe],
  templateUrl: './budget.component.html',
  styleUrl: './budget.component.css',
})
export class BudgetComponent {
  private readonly budget = inject(BudgetService);
  private readonly cats = inject(CategoriesService);
  private readonly toasts = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly auth = inject(AuthService);

  private readonly now = new Date();
  private monthTouched = false;
  protected readonly monthRef = signal(
    this.serverMonth() ?? { year: this.now.getFullYear(), month: this.now.getMonth() + 1 },
  );

  protected readonly data = signal<BudgetMonthResponse | null>(null);
  protected readonly loading = signal(true);
  protected readonly tree = signal<CategoryDto[]>([]);

  protected readonly creating = signal(false);
  protected readonly editingId = signal<number | null>(null);
  protected readonly draftCategoryId = signal<number | null>(null);
  protected readonly draftAmount = signal('');
  protected readonly draftNote = signal('');

  protected readonly formCategories = computed<FormCategory[]>(() => {
    const out: FormCategory[] = [];
    const walk = (nodes: CategoryDto[], parent: string | null): void => {
      for (const n of nodes) {
        out.push({ id: n.id, label: parent ? `${parent} / ${n.name}` : n.name });
        if (n.children) {
          walk(n.children, n.name);
        }
      }
    };
    walk(this.tree(), null);
    return out;
  });

  protected readonly groups = computed<BudgetGroup[]>(() => {
    const resp = this.data();
    if (!resp) {
      return [];
    }
    const order = new Map<number, number>();
    const walk = (nodes: CategoryDto[]): void => {
      for (const n of nodes) {
        order.set(n.id, order.size);
        if (n.children) {
          walk(n.children);
        }
      }
    };
    walk(this.tree());

    const itemsByCategory = new Map<number, BudgetItemDto[]>();
    for (const item of [...resp.items].sort((a, b) => a.id - b.id)) {
      const list = itemsByCategory.get(item.categoryId) ?? [];
      list.push(item);
      itemsByCategory.set(item.categoryId, list);
    }

    return resp.categories
      .slice()
      .sort(
        (a, b) =>
          (order.get(a.categoryId) ?? Number.MAX_SAFE_INTEGER) -
          (order.get(b.categoryId) ?? Number.MAX_SAFE_INTEGER),
      )
      .map((row) => ({ row, items: itemsByCategory.get(row.categoryId) ?? [] }));
  });

  constructor() {
    this.cats.tree('EXPENSE').pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (list) => this.tree.set(list),
      error: () => this.toasts.error('Не удалось загрузить категории'),
    });
    toObservable(this.auth.serverDate)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((sd) => {
        if (!sd || this.monthTouched) {
          return;
        }
        const s = parseServerMonth(sd);
        if (!s) {
          return;
        }
        const cur = this.monthRef();
        if (cur.year !== s.year || cur.month !== s.month) {
          this.monthRef.set(s);
          this.reload();
        }
      });
    this.reload();
  }

  private serverMonth(): { year: number; month: number } | null {
    return parseServerMonth(this.auth.serverDate());
  }

  protected shiftMonth(delta: number): void {
    this.monthTouched = true;
    this.monthRef.update((m) => {
      const d = new Date(m.year, m.month - 1 + delta, 1);
      return { year: d.getFullYear(), month: d.getMonth() + 1 };
    });
    this.cancelForm();
    this.reload();
  }

  protected groupLabel(row: BudgetCategoryRow): string {
    return row.parentName ? `${row.parentName} / ${row.name}` : row.name;
  }

  protected startCreate(): void {
    this.creating.set(true);
    this.editingId.set(null);
    this.resetDrafts();
  }

  protected startEdit(item: BudgetItemDto): void {
    this.creating.set(false);
    this.editingId.set(item.id);
    this.draftCategoryId.set(item.categoryId);
    this.draftAmount.set(String(item.amount));
    this.draftNote.set(item.note ?? '');
  }

  protected cancelForm(): void {
    this.creating.set(false);
    this.editingId.set(null);
  }

  private resetDrafts(): void {
    this.draftCategoryId.set(null);
    this.draftAmount.set('');
    this.draftNote.set('');
  }

  protected save(): void {
    const categoryId = this.draftCategoryId();
    const amount = Number(this.draftAmount());
    if (categoryId === null) {
      this.toasts.error('Выберите категорию');
      return;
    }
    if (!Number.isFinite(amount) || amount <= 0) {
      this.toasts.error('Введите сумму больше нуля');
      return;
    }
    const payload: BudgetPayload = {
      month: this.monthKey(),
      categoryId,
      amount,
      note: this.draftNote().trim() || null,
    };
    const id = this.editingId();
    const request = id === null ? this.budget.create(payload) : this.budget.update(id, payload);
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.toasts.success(id === null ? 'Позиция добавлена' : 'Позиция обновлена');
        this.cancelForm();
        this.reload();
      },
      error: (err) => this.toasts.error(ApiError.from(err).message),
    });
  }

  protected remove(item: BudgetItemDto): void {
    const label = item.categoryParentName
      ? `${item.categoryParentName} / ${item.categoryName}`
      : item.categoryName;
    if (!window.confirm(`Удалить позицию бюджета «${label}, ${formatMoney(item.amount)} ₽»?`)) {
      return;
    }
    this.budget.delete(item.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.toasts.success('Позиция удалена');
        if (this.editingId() === item.id) {
          this.cancelForm();
        }
        this.reload();
      },
      error: (err) => this.toasts.error(ApiError.from(err).message),
    });
  }

  private reload(): void {
    this.loading.set(true);
    this.budget
      .getMonth(this.monthKey())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (resp) => {
          this.data.set(resp);
          this.loading.set(false);
        },
        error: (err) => {
          this.loading.set(false);
          this.toasts.error(ApiError.from(err).message);
        },
      });
  }

  private monthKey(): string {
    const { year, month } = this.monthRef();
    return `${year}-${String(month).padStart(2, '0')}`;
  }
}

function parseServerMonth(sd: string | null): { year: number; month: number } | null {
  if (!sd) {
    return null;
  }
  const [y, m] = sd.split('-').map(Number);
  return y && m >= 1 && m <= 12 ? { year: y, month: m } : null;
}
