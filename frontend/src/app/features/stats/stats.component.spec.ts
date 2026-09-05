import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Observable, of } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import { BudgetService } from '../../core/budget.service';
import { CategoriesService } from '../../core/categories.service';
import { BudgetMonthResponse, SavingsYearSummary } from '../../core/models';
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

function makeSummary(year: number, amounts: number[], byMember: MemberTotal[]): SavingsYearSummary {
  const months = amounts.map((amount, i) => ({
    month: `${year}-${String(i + 1).padStart(2, '0')}`,
    amount,
  }));
  const deltas = amounts.map((amount, i) => ({
    month: months[i].month,
    delta: i === 0 ? amount : amount - amounts[i - 1],
  }));
  const total = byMember.reduce((s, m) => s + m.amount, 0);
  return { year, months, deltas, byMember, total };
}

const summary: SavingsYearSummary = makeSummary(
  2026,
  [30000, 45000, 40000, 60000, 50000, 80000, 90000, 70000, 100000, 110000, 95000, 120000],
  [
    { userId: 2, userName: 'Ваня', amount: 100000 },
    { userId: 3, userName: 'Катя', amount: 50000 },
  ],
);

const emptySummary: SavingsYearSummary = makeSummary(2026, Array(12).fill(0), []);

interface ChartDatasetLike {
  data: number[];
  backgroundColor?: string | string[];
}

interface StatsComponentApi {
  savingsSummary: () => SavingsYearSummary | null;
  savingsLineData: () => { labels: string[]; datasets: ChartDatasetLike[] };
  savingsDeltaData: () => { labels: string[]; datasets: ChartDatasetLike[] };
  shiftMonth(delta: number): void;
}

describe('StatsComponent', () => {
  let getYearSummarySpy: { calls: number[]; fn: (year: number) => Observable<SavingsYearSummary> };

  beforeEach(async () => {
    getYearSummarySpy = { calls: [], fn: () => of(summary) };
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
          useValue: { summaryCategory: () => of([]), summaryMonth: () => of([]) },
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
            getYearSummary: (year: number) => {
              getYearSummarySpy.calls.push(year);
              return getYearSummarySpy.fn(year);
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

  it('requests the year summary for the year of the selected month', () => {
    const { component: c } = comp();

    expect(getYearSummarySpy.calls[0]).toBe(2026);
    expect(c.savingsSummary()).toBe(summary);
  });

  it('renders the line chart with 12 monthly amounts', () => {
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

  it('renders the per-member totals and the grand total', () => {
    const { fixture } = comp();
    const el = fixture.nativeElement as HTMLElement;

    expect(el.textContent).toContain('Накопления за 2026');
    expect(el.textContent).toContain('Ваня');
    expect(el.textContent).toContain('Катя');
    expect(el.textContent).toContain('100\u00A0000,00 ₽');
    expect(el.textContent).toContain('50\u00A0000,00 ₽');
    expect(el.textContent).toContain('Суммарно');
    expect(el.textContent).toContain('150\u00A0000,00 ₽');
    expect(el.querySelectorAll('canvas').length).toBe(3);
  });

  it('shows the empty state when the year has no savings', () => {
    getYearSummarySpy.fn = () => of(emptySummary);
    const { fixture } = comp();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Накоплений за год нет');
  });

  it('requests the summary for the new year after the month is shifted', () => {
    const { component: c } = comp();

    c.shiftMonth(-8);

    expect(getYearSummarySpy.calls[getYearSummarySpy.calls.length - 1]).toBe(2025);
  });
});
