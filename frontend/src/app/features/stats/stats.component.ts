import { CommonModule } from '@angular/common';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { ScriptableScaleContext, TooltipItem } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';
import { AuthService } from '../../core/auth.service';
import { BudgetService } from '../../core/budget.service';
import { CategoriesService } from '../../core/categories.service';
import {
  BudgetCategoryRow,
  BudgetTotalsDto,
  CategoryDto,
  SavingsSummary,
} from '../../core/models';
import { MonthTitlePipe, MoneyPipe, formatMoney } from '../../core/format.pipe';
import { SavingsService } from '../../core/savings.service';
import { ToastService } from '../../core/toast.service';
import { TransactionsService } from '../../core/transactions.service';

interface PieSlice {
  label: string;
  value: number;
  color: string;
}

interface TopCategory {
  name: string;
  amount: number;
  share: number;
}

const FALLBACK_COLORS = [
  '#0f766e', '#f59e0b', '#ef4444', '#3b82f6', '#8b5cf6',
  '#ec4899', '#14b8a6', '#f97316', '#6366f1', '#84cc16',
];

@Component({
  selector: 'app-stats',
  imports: [CommonModule, BaseChartDirective, MonthTitlePipe, MoneyPipe],
  template: `
    <div class="space-y-3">
      <div class="card flex items-center justify-between">
        <button type="button" class="icon-btn" (click)="shiftMonth(-1)" aria-label="Предыдущий месяц">‹</button>
        <span class="text-base font-semibold text-gray-900">{{ monthRef() | monthTitle }}</span>
        <button type="button" class="icon-btn" (click)="shiftMonth(1)" aria-label="Следующий месяц">›</button>
      </div>

      <div class="card">
        <h2 class="section-title">Расходы по категориям</h2>
        @if (expenseTotal() > 0) {
          <div class="relative mx-auto max-w-72">
            <canvas
              baseChart
              [type]="'doughnut'"
              [data]="doughnutData()"
              [options]="doughnutOptions"
              aria-label="Круговая диаграмма расходов"
            ></canvas>
            <div class="pointer-events-none absolute inset-0 flex flex-col items-center justify-center">
              <span class="text-[11px] uppercase text-gray-400">всего</span>
              <span class="text-lg font-bold text-gray-900">{{ expenseTotal() | money }} ₽</span>
            </div>
          </div>
          <div class="mt-3 space-y-1.5">
            @for (s of slices(); track s.label) {
              <div class="flex items-center gap-2 text-sm">
                <span class="h-2.5 w-2.5 rounded-full" [style.background]="s.color"></span>
                <span class="flex-1 truncate text-gray-700">{{ s.label }}</span>
                <span class="font-medium text-gray-900">{{ s.value | money }} ₽</span>
                <span class="w-11 text-right text-xs text-gray-400">{{ shareOf(s.value) }}%</span>
              </div>
            }
          </div>
        } @else {
          <div class="py-8 text-center text-sm text-gray-400">Расходов за месяц нет</div>
        }
      </div>

      <div class="card">
        <h2 class="section-title">План по категориям</h2>
        @if (budgetRows().length > 0) {
          <div class="space-y-2">
            @for (r of budgetRows(); track r.categoryId) {
              <div>
                <div class="flex items-center gap-2">
                  <span class="h-2.5 w-2.5 shrink-0 rounded-full" [style.background]="r.color || '#d1d5db'"></span>
                  <span class="min-w-0 flex-1 truncate text-sm text-gray-700">{{ rowLabel(r) }}</span>
                  <span
                    class="whitespace-nowrap text-sm font-semibold"
                    [class.text-income]="r.diff >= 0"
                    [class.text-expense]="r.diff < 0"
                  >
                    {{ r.diff | money }} ₽
                  </span>
                </div>
                <div class="mt-0.5 flex items-center gap-3 pl-4 text-xs text-gray-500">
                  <span>план {{ r.planned | money }} ₽</span>
                  <span>факт {{ r.actual | money }} ₽</span>
                </div>
              </div>
            }
          </div>
          @let totals = budgetTotals();
          @if (totals) {
            <div class="mt-3 grid grid-cols-3 gap-2 border-t border-gray-100 pt-2.5">
              <div>
                <div class="text-[11px] uppercase text-gray-400">План</div>
                <div class="text-sm font-semibold text-gray-900">{{ totals.planned | money }} ₽</div>
              </div>
              <div>
                <div class="text-[11px] uppercase text-gray-400">Расходы</div>
                <div class="text-sm font-semibold text-gray-900">{{ totals.actual | money }} ₽</div>
              </div>
              <div>
                <div class="text-[11px] uppercase text-gray-400">Разница</div>
                <div
                  class="text-sm font-semibold"
                  [class.text-income]="totals.diff >= 0"
                  [class.text-expense]="totals.diff < 0"
                >
                  {{ totals.diff | money }} ₽
                </div>
              </div>
            </div>
          }
        } @else {
          <div class="py-8 text-center text-sm text-gray-400">План на месяц не настроен</div>
        }
      </div>

      <div class="card">
        <h2 class="section-title">Накопления за 12 месяцев</h2>
        @let savings = savingsSummary();
        @if (!savings) {
          <div class="py-8 text-center text-sm text-gray-400">Загрузка…</div>
        } @else {
          <div class="h-48">
            <canvas
              baseChart
              [type]="'line'"
              [data]="savingsLineData()"
              [options]="savingsLineOptions"
              aria-label="График накоплений по месяцам"
            ></canvas>
          </div>
          <div class="mt-4 h-48">
            <canvas
              baseChart
              [type]="'bar'"
              [data]="savingsDeltaData()"
              [options]="savingsDeltaOptions"
              aria-label="Столбчатая диаграмма изменения к прошлому месяцу"
            ></canvas>
          </div>
        }
      </div>

      <div class="card">
        <h2 class="section-title">Динамика за 12 месяцев</h2>
        <div class="h-56">
          <canvas
            baseChart
            [type]="'bar'"
            [data]="barData()"
            [options]="barOptions()"
            aria-label="Столбчатая диаграмма динамики"
          ></canvas>
        </div>
      </div>

      <div class="card">
        <h2 class="section-title">Топ-5 категорий</h2>
        @if (topCategories().length > 0) {
          <div class="space-y-2">
            @for (c of topCategories(); track c.name; let i = $index) {
              <div class="flex items-center gap-3">
                <span class="flex h-7 w-7 items-center justify-center rounded-full bg-gray-100 text-xs font-bold text-gray-500">
                  {{ i + 1 }}
                </span>
                <span class="min-w-0 flex-1 truncate text-sm text-gray-700">{{ c.name }}</span>
                <span class="text-sm font-semibold text-gray-900">{{ c.amount | money }} ₽</span>
                <span class="w-11 text-right text-xs text-gray-400">{{ c.share }}%</span>
              </div>
            }
          </div>
        } @else {
          <div class="py-6 text-center text-sm text-gray-400">Нет данных</div>
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
    .section-title {
      font-size: 13px;
      font-weight: 600;
      color: #6b7280;
      margin-bottom: 10px;
      text-transform: uppercase;
      letter-spacing: 0.03em;
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
    }
    .icon-btn:active {
      background: #f3f4f6;
    }
  `,
})
export class StatsComponent {
  private readonly txns = inject(TransactionsService);
  private readonly cats = inject(CategoriesService);
  private readonly budget = inject(BudgetService);
  private readonly savings = inject(SavingsService);
  private readonly toasts = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly auth = inject(AuthService);

  private readonly now = new Date();
  private monthTouched = false;
  protected readonly monthRef = signal(this.serverMonth() ?? { year: this.now.getFullYear(), month: this.now.getMonth() + 1 });

  private readonly catRows = signal<import('../../core/models').SummaryCategoryRow[]>([]);
  private readonly monthRows = signal<import('../../core/models').SummaryMonthRow[]>([]);
  private readonly categoryColorMap = signal<Map<number, string>>(new Map());

  protected readonly budgetRows = signal<BudgetCategoryRow[]>([]);
  protected readonly budgetTotals = signal<BudgetTotalsDto | null>(null);

  protected readonly savingsSummary = signal<SavingsSummary | null>(null);

  private readonly windowEnd = computed(() => {
    const selected = this.monthRef();
    const server = parseServerMonth(this.auth.serverDate());
    if (!server) {
      return selected;
    }
    const selectedIdx = selected.year * 12 + selected.month - 1;
    const serverIdx = server.year * 12 + server.month - 1;
    return selectedIdx <= serverIdx ? selected : server;
  });

  protected readonly doughnutOptions = {
    cutout: '62%',
    plugins: {
      legend: { display: false },
      tooltip: {
        callbacks: {
          title: (items: TooltipItem<'doughnut'>[]) => items[0]?.label ?? '',
          label: (ctx: TooltipItem<'doughnut'>) => ` ${formatMoney(ctx.parsed)} ₽`,
        },
      },
    },
    maintainAspectRatio: false,
  };

  protected readonly barOptions = computed(() => {
    const data = this.barData();
    const max = data.datasets.reduce((acc, ds) => Math.max(acc, ...(ds.data as number[])), 0);
    return {
      plugins: {
        legend: { display: true, position: 'bottom' as const },
        tooltip: {
          callbacks: {
            label: (ctx: TooltipItem<'bar'>) =>
              ` ${ctx.dataset.label ?? ''}: ${formatMoney(ctx.parsed.y ?? 0)} ₽`,
          },
        },
      },
      scales: {
        y: {
          beginAtZero: true,
          suggestedMax: max > 0 ? undefined : 100000,
          ticks: {
            maxTicksLimit: 6,
            callback: (v: number | string) => Number(v).toLocaleString('ru-RU', { maximumFractionDigits: 2 }),
          },
        },
      },
      maintainAspectRatio: false,
    };
  });

  protected readonly expenseTotal = computed(() =>
    this.catRows().filter((r) => r.kind === 'EXPENSE').reduce((s, r) => s + r.expense, 0),
  );

  protected readonly slices = computed<PieSlice[]>(() => {
    const byGroup = new Map<string, number>();
    for (const r of this.catRows()) {
      if (r.kind !== 'EXPENSE' || r.expense <= 0) {
        continue;
      }
      const group = r.parentName ?? r.name;
      byGroup.set(group, (byGroup.get(group) ?? 0) + r.expense);
    }
    const result: PieSlice[] = [];
    let idx = 0;
    for (const [label, value] of byGroup.entries()) {
      result.push({ label, value, color: this.pickColor(label, idx++) });
    }
    result.sort((a, b) => b.value - a.value);
    return result;
  });

  protected readonly doughnutData = computed(() => ({
    labels: this.slices().map((s) => s.label),
    datasets: [
      {
        data: this.slices().map((s) => s.value),
        backgroundColor: this.slices().map((s) => s.color),
      },
    ],
  }));

  protected readonly topCategories = computed<TopCategory[]>(() => {
    const total = this.expenseTotal();
    return this.catRows()
      .filter((r) => r.kind === 'EXPENSE' && r.expense > 0)
      .sort((a, b) => b.expense - a.expense)
      .slice(0, 5)
      .map((r) => ({
        name: r.parentName ? `${r.parentName} / ${r.name}` : r.name,
        amount: r.expense,
        share: total > 0 ? Math.round((r.expense / total) * 100) : 0,
      }));
  });

  protected readonly barData = computed(() => {
    const months = this.lastTwelveMonths();
    const byMonth = new Map(this.monthRows().map((r) => [r.month, r]));
    const income = months.map((m) => byMonth.get(m)?.income ?? 0);
    const expense = months.map((m) => byMonth.get(m)?.expense ?? 0);
    return {
      labels: months.map((m) => this.monthLabel(m)),
      datasets: [
        { label: 'Доходы', data: income, backgroundColor: '#0f766e', borderRadius: 6 },
        { label: 'Расходы', data: expense, backgroundColor: '#ef4444', borderRadius: 6 },
      ],
    };
  });

  protected readonly savingsLineData = computed(() => {
    const summary = this.savingsSummary();
    if (!summary) {
      return { labels: [] as string[], datasets: [] as { data: number[] }[] };
    }
    return {
      labels: summary.months.map((m) => this.monthLabel(m.month)),
      datasets: [
        {
          data: summary.months.map((m) => m.amount),
          borderColor: '#0f766e',
          backgroundColor: 'rgba(15,118,110,0.12)',
          fill: true,
          tension: 0.3,
        },
      ],
    };
  });

  protected readonly savingsDeltaData = computed(() => {
    const summary = this.savingsSummary();
    if (!summary) {
      return { labels: [] as string[], datasets: [] as { data: number[] }[] };
    }
    return {
      labels: summary.deltas.map((d) => this.monthLabel(d.month)),
      datasets: [
        {
          data: summary.deltas.map((d) => d.delta),
          borderRadius: 6,
          backgroundColor: summary.deltas.map((d) => (d.delta >= 0 ? '#0f766e' : '#ef4444')),
        },
      ],
    };
  });

  protected readonly savingsLineOptions = {
    maintainAspectRatio: false,
    scales: {
      y: {
        beginAtZero: true,
        ticks: {
          maxTicksLimit: 6,
          callback: (v: number | string) => Number(v).toLocaleString('ru-RU', { maximumFractionDigits: 2 }),
        },
      },
    },
    plugins: {
      legend: { display: false },
      tooltip: {
        callbacks: {
          label: (ctx: TooltipItem<'line'>) => ` ${formatMoney(ctx.parsed.y ?? 0)} ₽`,
        },
      },
    },
  };

  protected readonly savingsDeltaOptions = {
    maintainAspectRatio: false,
    scales: {
      y: {
        beginAtZero: false,
        ticks: {
          maxTicksLimit: 6,
          callback: (v: number | string) => Number(v).toLocaleString('ru-RU', { maximumFractionDigits: 2 }),
        },
        grid: {
          color: (ctx: ScriptableScaleContext) => (ctx.tick.value === 0 ? '#0f766e' : '#e5e7eb'),
        },
      },
    },
    plugins: {
      legend: { display: false },
      tooltip: {
        callbacks: {
          label: (ctx: TooltipItem<'bar'>) => ` ${formatMoney(ctx.parsed.y ?? 0)} ₽`,
        },
      },
    },
  };

  constructor() {
    this.cats.tree(undefined, true).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (list) => {
        const map = new Map<number, string>();
        const walk = (nodes: CategoryDto[]): void => {
          for (const n of nodes) {
            if (n.color) {
              map.set(n.id, n.color);
            }
            if (n.children) {
              walk(n.children);
            }
          }
        };
        walk(list);
        this.categoryColorMap.set(map);
      },
    });
    toObservable(this.auth.serverDate).subscribe((sd) => {
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
    this.reload();
  }

  protected shareOf(value: number): number {
    const total = this.expenseTotal();
    return total > 0 ? Math.round((value / total) * 100) : 0;
  }

  private reload(): void {
    const { from, to, from12, to12 } = this.periods();
    this.txns
      .summaryCategory(from, to)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (rows) => this.catRows.set(rows),
        error: () => this.toasts.error('Не удалось загрузить данные'),
      });
    this.txns
      .summaryMonth(from12, to12)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (rows) => this.monthRows.set(rows),
        error: () => this.toasts.error('Не удалось загрузить данные'),
      });
    this.budget
      .getMonth(this.monthKey())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (resp) => {
          this.budgetRows.set(resp.categories);
          this.budgetTotals.set(resp.totals);
        },
        error: () => this.toasts.error('Не удалось загрузить данные'),
      });
    this.savings
      .getSummary(this.monthKey())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (summary) => this.savingsSummary.set(summary),
        error: () => this.toasts.error('Не удалось загрузить данные'),
      });
  }

  private monthKey(): string {
    const { year, month } = this.monthRef();
    return `${year}-${String(month).padStart(2, '0')}`;
  }

  protected rowLabel(row: BudgetCategoryRow): string {
    return row.parentName ? `${row.parentName} / ${row.name}` : row.name;
  }

  private periods(): { from: string; to: string; from12: string; to12: string } {
    const { year, month } = this.monthRef();
    const from = `${year}-${String(month).padStart(2, '0')}-01`;
    const to = fmtDate(new Date(year, month, 0));
    const end = this.windowEnd();
    const from12 = fmtDate(new Date(end.year, end.month - 12, 1));
    const to12 = fmtDate(new Date(end.year, end.month, 0));
    return { from, to, from12, to12 };
  }

  private lastTwelveMonths(): string[] {
    const { year, month } = this.windowEnd();
    const months: string[] = [];
    for (let i = 11; i >= 0; i--) {
      const d = new Date(year, month - 1 - i, 1);
      months.push(`${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`);
    }
    return months;
  }

  private monthLabel(ym: string): string {
    const [y, m] = ym.split('-').map(Number);
    const label = new Date(y, m - 1, 1).toLocaleDateString('ru-RU', { month: 'short' });
    return label.replace('.', '');
  }

  private pickColor(group: string, fallbackIdx: number): string {
    const byId = this.categoryColorMap();
    const row = this.catRows().find((r) => (r.parentName ?? r.name) === group && r.kind === 'EXPENSE');
    if (row && byId.has(row.categoryId)) {
      return byId.get(row.categoryId) as string;
    }
    return FALLBACK_COLORS[fallbackIdx % FALLBACK_COLORS.length];
  }
}

function fmtDate(d: Date): string {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

function parseServerMonth(sd: string | null): { year: number; month: number } | null {
  if (!sd) {
    return null;
  }
  const [y, m] = sd.split('-').map(Number);
  return y && m >= 1 && m <= 12 ? { year: y, month: m } : null;
}
