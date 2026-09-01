import { CommonModule } from '@angular/common';
import { Component, DestroyRef, computed, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { toObservable } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { CategoriesService, flattenCategoryTree } from '../../core/categories.service';
import { ApiError, CategoryDto, TransactionDto, TransactionListResponse, TransactionListParams, TxnKind } from '../../core/models';
import { MonthTitlePipe, MoneyPipe, RuDatePipe } from '../../core/format.pipe';
import { ToastService } from '../../core/toast.service';
import { TransactionsService } from '../../core/transactions.service';
import { UsersService } from '../../core/users.service';
import { UserDto } from '../../core/models';

interface MonthRef {
  year: number;
  month: number;
}

@Component({
  selector: 'app-journal',
  imports: [CommonModule, FormsModule, RouterLink, MoneyPipe, RuDatePipe, MonthTitlePipe],
  template: `
    <div class="space-y-3">
      <div class="card">
        <div class="flex items-center justify-between">
          <button
            type="button"
            class="icon-btn"
            (click)="shiftMonth(-1)"
            aria-label="Предыдущий месяц"
          >
            ‹
          </button>
          <div class="text-center">
            <div class="text-base font-semibold text-gray-900" *ngIf="!customRange()">
              {{ monthRef() | monthTitle }}
            </div>
            <div class="text-base font-semibold text-gray-900" *ngIf="customRange()">
              {{ rangeFrom() | ruDate }} — {{ rangeTo() | ruDate }}
            </div>
            <div class="text-xs text-gray-400">журнал операций</div>
          </div>
          <div class="flex items-center gap-1.5">
            @if (!customRange() && serverMonth()) {
              <button
                type="button"
                class="today-btn"
                [disabled]="isServerMonth()"
                (click)="goToday()"
              >
                Сегодня
              </button>
            }
            <button
              type="button"
              class="icon-btn"
              (click)="shiftMonth(1)"
              aria-label="Следующий месяц"
            >
              ›
            </button>
          </div>
        </div>

        @let t = totals();
        @if (t) {
          <div class="mt-3 grid grid-cols-3 gap-2 border-t border-gray-100 pt-3">
            <div>
              <div class="text-[11px] uppercase tracking-wide text-gray-400">Доходы</div>
              <div class="text-sm font-bold text-income sm:text-base">{{ t.income | money }} ₽</div>
            </div>
            <div class="text-center">
              <div class="text-[11px] uppercase tracking-wide text-gray-400">Расходы</div>
              <div class="text-sm font-bold text-expense sm:text-base">{{ t.expense | money }} ₽</div>
            </div>
            <div class="text-right">
              <div class="text-[11px] uppercase tracking-wide text-gray-400">Остаток</div>
              <div
                class="text-sm font-bold sm:text-base"
                [class.text-income]="t.net >= 0"
                [class.text-expense]="t.net < 0"
              >
                {{ t.net | money: true }} ₽
              </div>
            </div>
          </div>
        }
      </div>

      <div class="card space-y-2.5">
        <div class="flex flex-wrap items-center gap-2">
          <button
            type="button"
            class="chip"
            [class.chip-active]="kindFilter() === null"
            (click)="setKind(null)"
          >
            Все
          </button>
          <button
            type="button"
            class="chip"
            [class.chip-active]="kindFilter() === 'INCOME'"
            (click)="setKind('INCOME')"
          >
            Доходы
          </button>
          <button
            type="button"
            class="chip"
            [class.chip-active]="kindFilter() === 'EXPENSE'"
            (click)="setKind('EXPENSE')"
          >
            Расходы
          </button>
          <button
            type="button"
            class="chip"
            [class.chip-active]="selectedCats().length > 0"
            (click)="catPanelOpen.set(!catPanelOpen())"
          >
            Категории@if (selectedCats().length > 0) { ({{ selectedCats().length }}) }
          </button>
          <button
            type="button"
            class="chip"
            [class.chip-active]="customRange()"
            (click)="toggleCustomRange()"
          >
            Период
          </button>
          <span class="ml-auto flex items-center gap-1">
            <button
              type="button"
              class="chip"
              [class.chip-active]="sort() === 'date'"
              (click)="setSort('date')"
            >
              По дате
            </button>
            <button
              type="button"
              class="chip"
              [class.chip-active]="sort() === 'amount'"
              (click)="setSort('amount')"
            >
              По сумме
            </button>
            <button type="button" class="chip" (click)="toggleDir()" [attr.aria-label]="'Направление сортировки'">
              {{ dir() === 'desc' ? '↓' : '↑' }}
            </button>
          </span>
        </div>

        @if (catPanelOpen()) {
          <div class="max-h-64 space-y-1 overflow-y-auto rounded-lg border border-gray-200 bg-gray-50 p-2">
            @for (group of categoryGroups(); track group.kind) {
              <div class="px-1 pt-1 text-[11px] font-semibold uppercase text-gray-400">
                {{ group.kind === 'INCOME' ? 'Доходы' : 'Расходы' }}
              </div>
              @for (cat of group.items; track cat.id) {
                <label
                  class="flex cursor-pointer items-center gap-2 rounded px-1 py-1 text-sm text-gray-700 hover:bg-white"
                  [class.cat-sel]="isCatSelected(cat.id)"
                >
                  <input
                    type="checkbox"
                    class="accent-teal-700"
                    [checked]="isCatSelected(cat.id)"
                    (change)="toggleCat(cat.id)"
                  />
                  <span
                    class="inline-block h-2.5 w-2.5 rounded-full"
                    [style.background]="cat.color || '#d1d5db'"
                  ></span>
                  {{ catLabel(cat) }}
                </label>
              }
            }
          </div>
        }

        <div class="flex flex-wrap items-center gap-2">
          <select
            class="input h-9"
            [value]="whoFilter() === null ? '' : whoFilter()"
            (change)="setWho($any($event.target).value)"
            aria-label="Кто"
          >
            <option value="">Кто: все</option>
            @for (u of users(); track u.id) {
              <option [value]="u.id">{{ u.displayName }}</option>
            }
          </select>
          <input
            type="text"
            class="input h-9 min-w-40 flex-1"
            placeholder="Поиск по комментарию…"
            [value]="searchInput()"
            (input)="searchInput.set($any($event.target).value)"
          />
          @if (hasFilters()) {
            <button type="button" class="link-btn text-xs font-medium text-teal-700 underline" (click)="resetFilters()">
              сбросить
            </button>
          }
        </div>

        @if (customRange()) {
          <div class="flex flex-wrap items-center gap-2">
            <label class="text-xs text-gray-500">
              с
              <input type="date" class="input h-9" [value]="rangeFrom()" (change)="setRangeFrom($any($event.target).value)" />
            </label>
            <label class="text-xs text-gray-500">
              по
              <input type="date" class="input h-9" [value]="rangeTo()" (change)="setRangeTo($any($event.target).value)" />
            </label>
            <button type="button" class="link-btn text-xs font-medium text-teal-700 underline" (click)="toggleCustomRange()">
              месяц
            </button>
          </div>
        }
      </div>

      <div class="card p-0">
        @if (loading()) {
          <div class="p-6 text-center text-sm text-gray-400">Загрузка…</div>
        } @else {
          @let d = data();
          @if (!d || d.items.length === 0) {
          <div class="p-8 text-center">
            <div class="text-3xl">🗒</div>
            <div class="mt-2 text-sm text-gray-500">Операций нет</div>
            <a routerLink="/transactions/new" class="mt-3 inline-block rounded-lg bg-teal-700 px-4 py-2 text-sm font-semibold text-white">
              + Добавить
            </a>
          </div>
        } @else {
          <div class="divide-y divide-gray-100 md:hidden">
            @for (t of d.items; track t.id) {
              <a
                [routerLink]="['/transactions', t.id, 'edit']"
                class="flex items-center gap-3 px-3 py-2.5 transition hover:bg-gray-50 sm:px-4"
              >
                <span
                  class="inline-block h-2.5 w-2.5 shrink-0 rounded-full"
                  [style.background]="t.color || '#d1d5db'"
                ></span>
                <span class="min-w-0 flex-1">
                  <span class="block truncate text-sm font-medium text-gray-900">
                    {{ t.categoryParentName ? t.categoryParentName + ' / ' + t.categoryName : t.categoryName }}
                  </span>
                  <span class="block truncate text-xs text-gray-400">
                    {{ t.date | ruDate }} · {{ t.userDisplayName }}@if (t.note) { · {{ t.note }} }
                  </span>
                </span>
                <span class="text-right">
                  <span
                    class="block text-sm font-bold"
                    [class.text-income]="t.kind === 'INCOME'"
                    [class.text-expense]="t.kind === 'EXPENSE'"
                  >
                    {{ t.amount | money }} ₽
                  </span>
                </span>
                <button
                  type="button"
                  class="icon-btn icon-btn-danger"
                  (click)="$event.preventDefault(); $event.stopPropagation(); remove(t)"
                  aria-label="Удалить"
                >
                  ✕
                </button>
              </a>
            }
          </div>

          <div class="hidden md:block">
            <table class="w-full border-collapse text-sm">
              <thead>
                <tr class="border-b border-gray-100 text-left text-[11px] uppercase tracking-wide text-gray-400">
                  <th class="px-3 py-2.5 font-semibold">Дата</th>
                  <th class="px-3 py-2.5 font-semibold">Категория</th>
                  <th class="px-3 py-2.5 font-semibold">Комментарий</th>
                  <th class="px-3 py-2.5 font-semibold">Кто</th>
                  <th class="px-3 py-2.5 text-right font-semibold">Сумма</th>
                  <th class="w-20 px-3 py-2.5" aria-label="Действия"></th>
                </tr>
              </thead>
              <tbody>
                @for (t of d.items; track t.id) {
                  <tr
                    class="cursor-pointer border-b border-gray-50 transition-colors last:border-0 hover:bg-gray-50"
                    (click)="edit(t)"
                  >
                    <td class="whitespace-nowrap px-3 py-2.5 text-gray-600">{{ t.date | ruDate }}</td>
                    <td class="px-3 py-2.5">
                      <span class="flex items-center gap-2">
                        <span
                          class="inline-block h-2.5 w-2.5 shrink-0 rounded-full"
                          [style.background]="t.color || '#d1d5db'"
                        ></span>
                        <span class="truncate font-medium text-gray-900">
                          {{ t.categoryParentName ? t.categoryParentName + ' / ' + t.categoryName : t.categoryName }}
                        </span>
                      </span>
                    </td>
                    <td class="max-w-64 truncate px-3 py-2.5 text-gray-500">{{ t.note || '—' }}</td>
                    <td class="whitespace-nowrap px-3 py-2.5 text-gray-600">{{ t.userDisplayName }}</td>
                    <td
                      class="whitespace-nowrap px-3 py-2.5 text-right font-bold"
                      [class.text-income]="t.kind === 'INCOME'"
                      [class.text-expense]="t.kind === 'EXPENSE'"
                    >
                      {{ t.amount | money }} ₽
                    </td>
                    <td class="px-2 py-1.5">
                      <span class="flex items-center justify-end gap-1">
                        <button
                          type="button"
                          class="icon-btn icon-btn-sm"
                          (click)="$event.stopPropagation(); edit(t)"
                          aria-label="Изменить"
                        >
                          ✎
                        </button>
                        <button
                          type="button"
                          class="icon-btn icon-btn-sm icon-btn-danger"
                          (click)="$event.stopPropagation(); remove(t)"
                          aria-label="Удалить"
                        >
                          ✕
                        </button>
                      </span>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>

          @if (d.total > d.items.length) {
            <div class="flex items-center justify-between border-t border-gray-100 px-3 py-2 sm:px-4">
              <button
                type="button"
                class="icon-btn"
                [disabled]="page() <= 1"
                (click)="setPage(page() - 1)"
                [class.opacity-30]="page() <= 1"
              >
                ‹
              </button>
              <span class="text-xs text-gray-500">
                стр. {{ page() }} из {{ totalPages() }} · всего {{ d.total }}
              </span>
              <button
                type="button"
                class="icon-btn"
                [disabled]="page() >= totalPages()"
                (click)="setPage(page() + 1)"
                [class.opacity-30]="page() >= totalPages()"
              >
                ›
              </button>
            </div>
          }
          }
        }
      </div>
    </div>
  `,
  styles: `
    .card {
      background: white;
      border: 1px solid #f3f4f6;
      border-radius: 14px;
      padding: 14px;
      box-shadow: 0 1px 2px rgb(0 0 0 / 0.04);
    }
    .chip {
      border-radius: 9999px;
      border: 1px solid #e5e7eb;
      background: white;
      padding: 5px 12px;
      font-size: 13px;
      color: #4b5563;
      cursor: pointer;
    }
    .chip:hover:not(.chip-active) {
      background: #f3f4f6;
    }
    .chip-active {
      background: #0f766e;
      border-color: #0f766e;
      color: white;
      font-weight: 500;
    }
    .cat-sel {
      background: #d1fae5;
      color: #065f46;
      font-weight: 600;
    }
    .cat-sel:hover {
      background: #b5f0d2;
    }
    .link-btn {
      border-radius: 6px;
      padding: 2px 6px;
      margin: -2px -6px;
      cursor: pointer;
    }
    .link-btn:hover {
      background: #e6f4f2;
    }
    .icon-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 34px;
      height: 34px;
      border-radius: 9999px;
      font-size: 20px;
      color: #6b7280;
      cursor: pointer;
    }
    .icon-btn:hover:not(:disabled) {
      background: #f3f4f6;
      color: #374151;
    }
    .icon-btn:active:not(:disabled) {
      background: #e5e7eb;
    }
    .icon-btn:disabled {
      cursor: default;
    }
    .icon-btn-sm {
      width: 28px;
      height: 28px;
      font-size: 14px;
    }
    .icon-btn-danger {
      color: #d1d5db;
    }
    .icon-btn-danger:hover:not(:disabled) {
      background: #fef2f2;
      color: #dc2626;
    }
    .today-btn {
      border-radius: 9999px;
      padding: 4px 10px;
      font-size: 12px;
      font-weight: 500;
      color: #0f766e;
      cursor: pointer;
    }
    .today-btn:hover:not(:disabled) {
      background: #e6f4f2;
    }
    .today-btn:disabled {
      opacity: 0.4;
      cursor: default;
    }
    .input {
      border: 1px solid #e5e7eb;
      border-radius: 10px;
      padding: 0 10px;
      background: white;
      font-size: 16px;
    }
    .input:focus {
      outline: 2px solid rgb(15 118 110 / 0.3);
      border-color: #0f766e;
    }
  `,
})
export class JournalComponent {
  private readonly txns = inject(TransactionsService);
  private readonly cats = inject(CategoriesService);
  private readonly usersSvc = inject(UsersService);
  private readonly toasts = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private monthTouched = false;

  private readonly now = new Date();
  protected readonly serverMonth = computed<MonthRef | null>(() => {
    const sd = this.auth.serverDate();
    if (!sd) {
      return null;
    }
    const [y, m] = sd.split('-').map(Number);
    if (!y || !m || m < 1 || m > 12) {
      return null;
    }
    return { year: y, month: m };
  });
  protected readonly monthRef = signal<MonthRef>(this.serverMonth() ?? {
    year: this.now.getFullYear(),
    month: this.now.getMonth() + 1,
  });
  protected readonly isServerMonth = computed(() => {
    const s = this.serverMonth();
    if (!s) {
      return false;
    }
    const m = this.monthRef();
    return m.year === s.year && m.month === s.month;
  });
  protected readonly customRange = signal(false);
  protected readonly rangeFrom = signal<string>(fmtDate(new Date(this.now.getFullYear(), this.now.getMonth(), 1)));
  protected readonly rangeTo = signal<string>(fmtDate(new Date(this.now.getFullYear(), this.now.getMonth() + 1, 0)));

  protected readonly users = signal<UserDto[]>([]);
  protected readonly whoFilter = signal<number | null>(null);
  protected readonly kindFilter = signal<TxnKind | null>(null);
  protected readonly selectedCats = signal<number[]>([]);
  protected readonly catPanelOpen = signal(false);
  protected readonly searchInput = signal('');
  private readonly debouncedQ = signal('');

  protected readonly page = signal(1);
  private readonly pageSize = 50;
  protected readonly sort = signal<'date' | 'amount'>('date');
  protected readonly dir = signal<'asc' | 'desc'>('desc');

  protected readonly data = signal<TransactionListResponse | null>(null);
  protected readonly loading = signal(true);
  protected readonly totals = signal<TransactionListResponse['totals'] | null>(null);

  protected readonly categories = signal<CategoryDto[]>([]);

  protected readonly hasFilters = computed(
    () =>
      this.whoFilter() !== null ||
      this.kindFilter() !== null ||
      this.selectedCats().length > 0 ||
      this.searchInput() !== '' ||
      this.customRange() ||
      this.sort() !== 'date' ||
      this.dir() !== 'desc',
  );

  protected readonly totalPages = computed(() => {
    const total = this.data()?.total ?? 0;
    return Math.max(1, Math.ceil(total / this.pageSize));
  });

  constructor() {
    toObservable(this.searchInput)
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((q) => this.debouncedQ.set(q ?? ''));

    effect(() => {
      const s = this.serverMonth();
      if (!s || this.monthTouched) {
        return;
      }
      const m = this.monthRef();
      if (m.year !== s.year || m.month !== s.month) {
        this.monthRef.set(s);
      }
    });

    effect(() => {
      const p = this.buildParams();
      this.loading.set(true);
      this.txns.list(p).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (res) => {
          this.data.set(res);
          this.totals.set(res.totals);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
    });

    this.cats.tree().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (list) => this.categories.set(list),
      error: () => this.toasts.error('Не удалось загрузить категории'),
    });
    this.usersSvc.list().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (list) => this.users.set(list.filter((u) => u.isActive)),
      error: () => this.toasts.error('Не удалось загрузить пользователей'),
    });
  }

  protected readonly categoryGroups = computed(() => {
    const visible = flattenCategoryTree(this.categories()).filter((c) => !c.isArchived);
    return [
      { kind: 'INCOME' as TxnKind, items: visible.filter((c) => c.kind === 'INCOME') },
      { kind: 'EXPENSE' as TxnKind, items: visible.filter((c) => c.kind === 'EXPENSE') },
    ];
  });

  protected isCatSelected(id: number): boolean {
    return this.selectedCats().includes(id);
  }

  protected catLabel(cat: CategoryDto): string {
    if (!cat.parentId) {
      return cat.name;
    }
    const parent = this.categories().find((c) => c.id === cat.parentId);
    return parent ? `${parent.name} / ${cat.name}` : cat.name;
  }

  protected shiftMonth(delta: number): void {
    if (this.customRange()) {
      return;
    }
    this.monthTouched = true;
    this.monthRef.update((m) => {
      const d = new Date(m.year, m.month - 1 + delta, 1);
      return { year: d.getFullYear(), month: d.getMonth() + 1 };
    });
    this.page.set(1);
  }

  protected goToday(): void {
    const s = this.serverMonth();
    if (!s) {
      return;
    }
    this.customRange.set(false);
    this.monthRef.set(s);
    this.monthTouched = true;
    this.page.set(1);
  }

  protected edit(t: TransactionDto): void {
    this.router.navigate(['/transactions', t.id, 'edit']);
  }

  protected toggleCustomRange(): void {
    const next = !this.customRange();
    this.customRange.set(next);
    if (next) {
      const { year, month } = this.monthRef();
      this.rangeFrom.set(fmtDate(new Date(year, month - 1, 1)));
      this.rangeTo.set(fmtDate(new Date(year, month, 0)));
    }
    this.page.set(1);
  }

  protected setRangeFrom(v: string): void {
    this.rangeFrom.set(v);
    this.page.set(1);
  }

  protected setRangeTo(v: string): void {
    this.rangeTo.set(v);
    this.page.set(1);
  }

  protected setKind(kind: TxnKind | null): void {
    this.kindFilter.set(kind);
    this.page.set(1);
  }

  protected toggleCat(id: number): void {
    this.selectedCats.update((list) => (list.includes(id) ? list.filter((x) => x !== id) : [...list, id]));
    this.page.set(1);
  }

  protected setWho(v: unknown): void {
    const n = v === null || v === undefined || v === '' ? null : Number(v);
    this.whoFilter.set(n === null || Number.isNaN(n) ? null : n);
    this.page.set(1);
  }

  protected setPage(p: number): void {
    this.page.set(p);
  }

  protected setSort(sort: 'date' | 'amount'): void {
    this.sort.set(sort);
    this.page.set(1);
  }

  protected toggleDir(): void {
    this.dir.update((d) => (d === 'desc' ? 'asc' : 'desc'));
    this.page.set(1);
  }

  protected resetFilters(): void {
    this.whoFilter.set(null);
    this.kindFilter.set(null);
    this.selectedCats.set([]);
    this.searchInput.set('');
    this.debouncedQ.set('');
    this.customRange.set(false);
    this.catPanelOpen.set(false);
    this.sort.set('date');
    this.dir.set('desc');
    const s = this.serverMonth();
    this.monthRef.set(s ?? { year: this.now.getFullYear(), month: this.now.getMonth() + 1 });
    this.monthTouched = true;
    this.page.set(1);
  }

  protected remove(t: TransactionDto): void {
    const amount = t.amount.toLocaleString('ru-RU', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    if (!window.confirm(`Удалить операцию «${t.categoryName}» на ${amount} ₽?`)) {
      return;
    }
    this.txns.delete(t.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.toasts.success('Операция удалена');
        this.refetch();
      },
      error: (err) => this.toasts.error(ApiError.from(err).message),
    });
  }

  private refetch(): void {
    const total = this.data()?.total ?? 0;
    const maxPage = Math.max(1, Math.ceil(total / this.pageSize));
    this.page.set(Math.min(this.page(), maxPage));
    this.loading.set(true);
    this.txns.list(this.buildParams()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        this.data.set(res);
        this.totals.set(res.totals);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  private buildParams(): TransactionListParams {
    let from: string | undefined;
    let to: string | undefined;
    if (this.customRange()) {
      from = this.rangeFrom() || undefined;
      to = this.rangeTo() || undefined;
    } else {
      const { year, month } = this.monthRef();
      from = fmtDate(new Date(year, month - 1, 1));
      to = fmtDate(new Date(year, month, 0));
    }
    const params: TransactionListParams = {
      from,
      to,
      page: this.page(),
      pageSize: this.pageSize,
      sort: this.sort(),
      dir: this.dir(),
    };
    if (this.whoFilter() !== null) {
      params.userId = this.whoFilter() ?? undefined;
    }
    if (this.kindFilter()) {
      params.kind = this.kindFilter() ?? undefined;
    }
    if (this.selectedCats().length > 0) {
      params.categoryId = this.selectedCats();
    }
    if (this.debouncedQ().trim()) {
      params.q = this.debouncedQ().trim();
    }
    return params;
  }
}

function fmtDate(d: Date): string {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}
