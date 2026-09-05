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
  template: `
    <div class="space-y-3">
      @let resp = data();

      <div class="card flex items-center justify-between">
        <h1 class="text-lg font-bold text-gray-900">Бюджет</h1>
        @if (!creating()) {
          <button type="button" class="btn-add" (click)="startCreate()">+ добавить</button>
        }
      </div>

      <div class="card flex items-center justify-between">
        <button type="button" class="icon-btn" (click)="shiftMonth(-1)" aria-label="Предыдущий месяц">‹</button>
        <span class="text-base font-semibold text-gray-900">{{ monthRef() | monthTitle }}</span>
        <button type="button" class="icon-btn" (click)="shiftMonth(1)" aria-label="Следующий месяц">›</button>
      </div>

      @if (resp) {
        @let totals = resp.totals;
        <div class="card">
          <h2 class="section-title">Итоги по месяцу</h2>
          <div class="divide-y divide-gray-50">
            <div class="flex items-center justify-between py-1.5 text-sm">
              <span class="text-gray-500">План</span>
              <span class="font-semibold text-gray-900">{{ totals.planned | money }} ₽</span>
            </div>
            <div class="flex items-center justify-between py-1.5 text-sm">
              <span class="text-gray-500">Расходы</span>
              <span class="font-semibold text-gray-900">{{ totals.actual | money }} ₽</span>
            </div>
            <div class="flex items-center justify-between py-1.5 text-sm">
              <span class="text-gray-500">Разница</span>
              <span
                class="font-semibold"
                [class.text-income]="totals.diff >= 0"
                [class.text-expense]="totals.diff < 0"
              >
                {{ totals.diff | money }} ₽
              </span>
            </div>
          </div>
        </div>
      }

      @if (creating()) {
        <div class="card space-y-2.5">
          <div class="text-sm font-semibold text-gray-700">Новая позиция</div>
          <select
            class="input"
            [ngModel]="draftCategoryId()"
            (ngModelChange)="draftCategoryId.set($event)"
            aria-label="Категория"
          >
            <option [ngValue]="null">— выберите категорию —</option>
            @for (c of formCategories(); track c.id) {
              <option [ngValue]="c.id">{{ c.label }}</option>
            }
          </select>
          <input
            type="number"
            class="input"
            [ngModel]="draftAmount()"
            (ngModelChange)="draftAmount.set($event)"
            placeholder="Сумма"
            min="0"
            step="0.01"
            inputmode="decimal"
            aria-label="Сумма"
          />
          <input
            type="text"
            class="input"
            [ngModel]="draftNote()"
            (ngModelChange)="draftNote.set($event)"
            placeholder="Комментарий (необязательно)"
            maxlength="500"
            aria-label="Комментарий"
          />
          <div class="flex gap-2">
            <button type="button" class="btn-sm btn-secondary" (click)="cancelForm()">Отмена</button>
            <button type="button" class="btn-sm btn-primary" (click)="save()">Сохранить</button>
          </div>
        </div>
      }

      @if (loading() && !resp) {
        <div class="card py-8 text-center text-sm text-gray-400">Загрузка…</div>
      }

      @if (resp && resp.items.length === 0 && !creating()) {
        <div class="card py-10 text-center">
          <p class="text-sm text-gray-400">План на месяц не задан. Добавьте первую позицию.</p>
          <button type="button" class="btn-add mt-4" (click)="startCreate()">+ добавить</button>
        </div>
      }

      @for (g of groups(); track g.row.categoryId) {
        <div class="card p-0">
          <div class="flex items-center justify-between gap-2 border-b border-gray-100 px-4 py-2.5">
            <span class="flex min-w-0 items-center gap-2">
              <span class="h-3 w-3 shrink-0 rounded-full" [style.background]="g.row.color || '#d1d5db'"></span>
              <span class="group-name truncate text-sm font-semibold text-gray-800">{{ groupLabel(g.row) }}</span>
            </span>
            <span class="shrink-0 whitespace-nowrap text-xs text-gray-500">
              план {{ g.row.planned | money }} ₽ <span class="text-gray-300">·</span> факт {{ g.row.actual | money }} ₽
            </span>
          </div>
          <div class="divide-y divide-gray-50">
            @for (item of g.items; track item.id) {
              @if (editingId() === item.id) {
                <div class="space-y-2.5 px-4 py-3">
                  <select
                    class="input"
                    [ngModel]="draftCategoryId()"
                    (ngModelChange)="draftCategoryId.set($event)"
                    aria-label="Категория"
                  >
                    <option [ngValue]="null">— выберите категорию —</option>
                    @for (c of formCategories(); track c.id) {
                      <option [ngValue]="c.id">{{ c.label }}</option>
                    }
                  </select>
                  <input
                    type="number"
                    class="input"
                    [ngModel]="draftAmount()"
                    (ngModelChange)="draftAmount.set($event)"
                    placeholder="Сумма"
                    min="0"
                    step="0.01"
                    inputmode="decimal"
                    aria-label="Сумма"
                  />
                  <input
                    type="text"
                    class="input"
                    [ngModel]="draftNote()"
                    (ngModelChange)="draftNote.set($event)"
                    placeholder="Комментарий (необязательно)"
                    maxlength="500"
                    aria-label="Комментарий"
                  />
                  <div class="flex gap-2">
                    <button type="button" class="btn-sm btn-secondary" (click)="cancelForm()">Отмена</button>
                    <button type="button" class="btn-sm btn-primary" (click)="save()">Сохранить</button>
                  </div>
                </div>
              } @else {
                <div class="flex items-center gap-1 px-4 py-2">
                  <span class="min-w-0 flex-1 truncate text-sm text-gray-800">
                    {{ item.amount | money }} ₽
                    @if (item.note) {
                      <span class="ml-2 text-xs text-gray-400">{{ item.note }}</span>
                    }
                  </span>
                  <button type="button" class="icon-btn shrink-0" (click)="startEdit(item)" title="Изменить">✎</button>
                  <button type="button" class="icon-btn icon-btn-danger shrink-0" (click)="remove(item)" title="Удалить">🗑</button>
                </div>
              }
            }
          </div>
        </div>
      }
    </div>
  `,
  styles: `
    .card {
      background: white;
      border: 1px solid #f3f4f6;
      border-radius: 14px;
      box-shadow: 0 1px 2px rgb(0 0 0 / 0.04);
      padding: 14px;
    }
    .section-title {
      font-size: 13px;
      font-weight: 600;
      color: #6b7280;
      margin-bottom: 10px;
      text-transform: uppercase;
      letter-spacing: 0.03em;
    }
    .btn-add {
      border-radius: 9999px;
      background: #0f766e;
      color: white;
      font-size: 13px;
      font-weight: 600;
      padding: 7px 14px;
    }
    .icon-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 30px;
      height: 30px;
      border-radius: 9999px;
      color: #9ca3af;
      font-size: 14px;
      cursor: pointer;
    }
    .icon-btn:hover {
      background: #f3f4f6;
      color: #374151;
    }
    .icon-btn:active {
      background: #e5e7eb;
    }
    .icon-btn-danger:hover {
      background: #fef2f2;
      color: #dc2626;
    }
    .input {
      width: 100%;
      border: 1px solid #e5e7eb;
      border-radius: 10px;
      padding: 9px 10px;
      font-size: 14px;
    }
    .input:focus {
      outline: 2px solid rgb(15 118 110 / 0.3);
      border-color: #0f766e;
    }
    .btn-sm {
      border-radius: 9px;
      font-size: 13px;
      font-weight: 500;
      padding: 7px 12px;
    }
    .btn-primary {
      background: #0f766e;
      color: white;
    }
    .btn-secondary {
      border: 1px solid #e5e7eb;
      background: white;
      color: #374151;
    }
  `,
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
