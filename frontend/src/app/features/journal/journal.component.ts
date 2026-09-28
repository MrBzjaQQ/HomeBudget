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
  templateUrl: './journal.component.html',
  styleUrl: './journal.component.css',
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
