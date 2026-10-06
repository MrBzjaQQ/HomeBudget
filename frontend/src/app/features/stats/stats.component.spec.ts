import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Observable, of } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import { BudgetService } from '../../core/budget.service';
import { CategoriesService } from '../../core/categories.service';
import { GoalsService } from '../../core/goals.service';
import { BudgetMonthResponse, GoalDto, SavingsSummary } from '../../core/models';
import { SavingsService } from '../../core/savings.service';
import { ToastService } from '../../core/toast.service';
import { TransactionsService } from '../../core/transactions.service';
import { StatsComponent } from './stats.component';

const SERVER_DATE = '2026-08-15';

const emptyBudget: BudgetMonthResponse = {
  month: '2026-08',
  items: [],
  categories: [],
  totals: { planned: 0, actual: 0, diff: 0 },
};

interface MemberTotal {
  userId: number;
  userName: string;
  amount: number;
}

function makeSummary(to: string, amounts: number[], byMember: MemberTotal[]): SavingsSummary {
  const [ty, tm] = to.split('-').map(Number);
  const months = amounts.map((amount, i) => {
    const d = new Date(ty, tm - 1 - (amounts.length - 1 - i), 1);
    return {
      month: `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`,
      amount,
    };
  });
  const deltas = amounts.map((amount, i) => ({
    month: months[i].month,
    delta: i === 0 ? amount : amount - amounts[i - 1],
  }));
  const total = byMember.reduce((s, m) => s + m.amount, 0);
  return { to, months, deltas, byMember, total };
}

const summary: SavingsSummary = makeSummary(
  '2026-08',
  [30000, 45000, 40000, 60000, 50000, 80000, 90000, 70000, 100000, 110000, 95000, 120000],
  [
    { userId: 2, userName: 'Ваня', amount: 100000 },
    { userId: 3, userName: 'Катя', amount: 50000 },
  ],
);

const emptySummary: SavingsSummary = makeSummary('2026-08', Array(12).fill(0), []);

interface ChartDatasetLike {
  data: number[];
  backgroundColor?: string | string[];
}

interface ChartDataLike {
  labels: string[];
  datasets: ChartDatasetLike[];
}

interface StatsComponentApi {
  savingsSummary: () => SavingsSummary | null;
  savingsLineData: () => ChartDataLike;
  savingsDeltaData: () => ChartDataLike;
  barData: () => ChartDataLike;
  shiftMonth(delta: number): void;
}

describe('StatsComponent', () => {
  let getSummarySpy: { calls: string[]; fn: (to: string) => Observable<SavingsSummary> };
  let summaryMonthSpy: { calls: Array<[string, string]> };
  let listGoalsSpy: { calls: number; fn: () => Observable<GoalDto[]> };

  beforeEach(async () => {
    getSummarySpy = { calls: [], fn: () => of(summary) };
    summaryMonthSpy = { calls: [] };
    listGoalsSpy = { calls: 0, fn: () => of([]) };
    HTMLCanvasElement.prototype.getContext = (() => null) as unknown as typeof HTMLCanvasElement.prototype.getContext;
    await TestBed.configureTestingModule({
      imports: [StatsComponent],
      providers: [
        {
          provide: AuthService,
          useValue: { serverDate: signal<string | null>(SERVER_DATE) },
        },
        {
          provide: TransactionsService,
          useValue: {
            summaryCategory: () => of([]),
            summaryMonth: (from: string, to: string) => {
              summaryMonthSpy.calls.push([from, to]);
              return of([]);
            },
          },
        },
        {
          provide: CategoriesService,
          useValue: { tree: () => of([]) },
        },
        {
          provide: BudgetService,
          useValue: { getMonth: () => of(emptyBudget) },
        },
        {
          provide: SavingsService,
          useValue: {
            getSummary: (to: string) => {
              getSummarySpy.calls.push(to);
              return getSummarySpy.fn(to);
            },
          },
        },
        {
          provide: GoalsService,
          useValue: {
            listGoals: () => {
              listGoalsSpy.calls++;
              return listGoalsSpy.fn();
            },
          },
        },
        ToastService,
      ],
    });
  });

  function comp() {
    const fixture = TestBed.createComponent(StatsComponent);
    fixture.detectChanges();
    return {
      fixture,
      component: fixture.componentInstance as unknown as StatsComponentApi,
    };
  }

  it('requests the savings summary for the selected month', () => {
    const { component: c } = comp();

    expect(getSummarySpy.calls[0]).toBe('2026-08');
    expect(c.savingsSummary()).toBe(summary);
  });

  it('запрашивает_цели_один_раз_при_инициализации', () => {
    comp();

    expect(listGoalsSpy.calls).toBe(1);
  });

  it('показывает_название_суммы_и_процент_частично_достигнутой_цели', () => {
    listGoalsSpy.fn = () =>
      of([
        {
          id: 1,
          name: 'Отпуск',
          targetAmount: 100000,
          containerId: 2,
          containerName: 'Накопления',
          containerColor: null,
          dueDate: null,
          saved: 40000,
          monthlyRequired: null,
          achieved: false,
          createdAt: '2026-01-01T00:00:00Z',
        },
      ]);
    const { fixture } = comp();
    const el = fixture.nativeElement as HTMLElement;
    const progress = el.querySelector('[role="progressbar"]');
    const cardText = el.textContent?.replace(/\s+/g, ' ') ?? '';

    expect(cardText).toContain('Отпуск');
    expect(cardText).toContain('40 000,00 ₽ / 100 000,00 ₽');
    expect(progress?.getAttribute('aria-valuenow')).toBe('40');
    expect(progress?.getAttribute('aria-valuetext')).toBe('40%');
  });

  it('показывает_достигнутую_цель_со_статусом_и_процентом_100', () => {
    listGoalsSpy.fn = () =>
      of([
        {
          id: 1,
          name: 'Резерв',
          targetAmount: 50000,
          containerId: 2,
          containerName: 'Накопления',
          containerColor: null,
          dueDate: null,
          saved: 50000,
          monthlyRequired: null,
          achieved: true,
          createdAt: '2026-01-01T00:00:00Z',
        },
      ]);
    const { fixture } = comp();
    const el = fixture.nativeElement as HTMLElement;
    const progress = el.querySelector('[role="progressbar"]');

    expect(el.textContent).toContain('Цель достигнута');
    expect(progress?.getAttribute('aria-valuenow')).toBe('100');
    expect(progress?.getAttribute('aria-valuetext')).toBe('100%');
  });

  it('показывает_пустое_состояние_при_отсутствии_целей', () => {
    listGoalsSpy.fn = () => of([]);
    const { fixture } = comp();
    const el = fixture.nativeElement as HTMLElement;

    expect(el.textContent).toContain('Финансовых целей пока нет');
    expect(el.querySelector('[role="alert"]')).toBeNull();
  });

  it('показывает_ошибку_запроса_отдельно_от_пустого_состояния', () => {
    listGoalsSpy.fn = () =>
      new Observable<GoalDto[]>((subscriber) =>
        subscriber.error({ error: { code: 'GOALS_FAILED', message: 'Не удалось загрузить цели' } }),
      );
    const { fixture } = comp();
    const el = fixture.nativeElement as HTMLElement;

    expect(el.querySelector('[role="alert"]')?.textContent).toContain('Не удалось загрузить цели');
    expect(el.textContent).not.toContain('Финансовых целей пока нет');
  });

  it('не_запрашивает_цели_повторно_при_смене_месяца', () => {
    const { component: c } = comp();

    c.shiftMonth(-1);

    expect(listGoalsSpy.calls).toBe(1);
  });

  it('renders the line chart with 12 monthly amounts of the window', () => {
    const { component: c } = comp();
    const line = c.savingsLineData();

    expect(line.labels.length).toBe(12);
    expect(line.datasets.length).toBe(1);
    expect(line.datasets[0].data).toEqual(summary.months.map((m) => m.amount));
  });

  it('renders the delta bar chart with 12 values and red bars for negative deltas', () => {
    const { component: c } = comp();
    const bar = c.savingsDeltaData();

    expect(bar.labels.length).toBe(12);
    expect(bar.datasets[0].data).toEqual(summary.deltas.map((d) => d.delta));
    const bg = bar.datasets[0].backgroundColor;
    expect(Array.isArray(bg)).toBe(true);
    const colors = bg as string[];
    expect(colors).toContain('#ef4444');
    expect(colors).toContain('#0f766e');
    summary.deltas.forEach((d, i) => {
      expect(colors[i]).toBe(d.delta >= 0 ? '#0f766e' : '#ef4444');
    });
  });

  it('renders the savings charts', () => {
    const { fixture } = comp();
    const el = fixture.nativeElement as HTMLElement;

    expect(el.textContent).toContain('Накопления за 12 месяцев');
    expect(el.querySelectorAll('canvas').length).toBe(3);
  });


  it('requests the summary for the new month after the month is shifted', () => {
    const { component: c } = comp();

    c.shiftMonth(-8);

    expect(getSummarySpy.calls[getSummarySpy.calls.length - 1]).toBe('2025-12');
  });

  it('requests the dynamics window of 12 months ending at the selected month', () => {
    const { component: c } = comp();
    const bars = c.barData();

    expect(summaryMonthSpy.calls[0]).toEqual(['2025-09-01', '2026-08-31']);
    expect(bars.labels.length).toBe(12);
    expect(bars.labels[0]).toBe('сент');
    expect(bars.labels[11]).toBe('авг');
  });

  it('does not move the windows into the future when a future month is selected', () => {
    const { component: c } = comp();

    c.shiftMonth(3);

    expect(summaryMonthSpy.calls[summaryMonthSpy.calls.length - 1]).toEqual(['2025-09-01', '2026-08-31']);
    expect(c.barData().labels.length).toBe(12);
    expect(c.barData().labels[11]).toBe('авг');
  });
});
