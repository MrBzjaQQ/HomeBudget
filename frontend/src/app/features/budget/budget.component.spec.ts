import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Observable, of } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import { CategoriesService } from '../../core/categories.service';
import { BudgetService } from '../../core/budget.service';
import {
  BudgetItemDto,
  BudgetMonthResponse,
  BudgetPayload,
  CategoryDto,
} from '../../core/models';
import { ToastService } from '../../core/toast.service';
import { BudgetComponent } from './budget.component';

const tree: CategoryDto[] = [
  {
    id: 3,
    name: 'Продукты',
    kind: 'EXPENSE',
    parentId: null,
    color: '#E5484D',
    sortOrder: 1,
    isArchived: false,
    children: [
      {
        id: 4,
        name: 'Супермаркет',
        kind: 'EXPENSE',
        parentId: 3,
        color: null,
        sortOrder: 1,
        isArchived: false,
        children: null,
      },
    ],
  },
  {
    id: 5,
    name: 'Аптека',
    kind: 'EXPENSE',
    parentId: null,
    color: '#D6409F',
    sortOrder: 2,
    isArchived: false,
    children: null,
  },
];

const item1: BudgetItemDto = {
  id: 1,
  month: '2026-08',
  categoryId: 4,
  categoryName: 'Супермаркет',
  categoryParentName: 'Продукты',
  color: null,
  amount: 15000,
  note: 'основной запас',
  createdAt: '2026-08-01T10:00:00Z',
  updatedAt: '2026-08-01T10:00:00Z',
};

const item2: BudgetItemDto = {
  id: 2,
  month: '2026-08',
  categoryId: 3,
  categoryName: 'Продукты',
  categoryParentName: null,
  color: '#E5484D',
  amount: 3000,
  note: null,
  createdAt: '2026-08-01T10:00:00Z',
  updatedAt: '2026-08-01T10:00:00Z',
};

const monthResponse: BudgetMonthResponse = {
  month: '2026-08',
  items: [item1, item2],
  categories: [
    {
      categoryId: 4,
      name: 'Супермаркет',
      parentName: 'Продукты',
      color: null,
      planned: 15000,
      actual: 12300.5,
      diff: 2699.5,
    },
    {
      categoryId: 3,
      name: 'Продукты',
      parentName: null,
      color: '#E5484D',
      planned: 3000,
      actual: 800,
      diff: 2200,
    },
  ],
  totals: { planned: 18000, actual: 13100.5, diff: 4899.5 },
};

const emptyResponse: BudgetMonthResponse = {
  month: '2026-08',
  items: [],
  categories: [],
  totals: { planned: 0, actual: 0, diff: 0 },
};

const SERVER_DATE = '2026-08-15';

interface BudgetComponentApi {
  startCreate(): void;
  remove(item: BudgetItemDto): void;
  save(): void;
  shiftMonth(delta: number): void;
  draftCategoryId: { set(v: number | null): void };
  draftAmount: { set(v: string): void };
  draftNote: { set(v: string): void };
}

describe('BudgetComponent', () => {
  let getMonthSpy: { calls: string[]; fn: (m: string) => Observable<BudgetMonthResponse> };
  let createSpy: { calls: BudgetPayload[] };
  let deleteSpy: { calls: number[] };
  let serverDateSignal: { set(v: string | null): void };

  beforeEach(async () => {
    getMonthSpy = { calls: [], fn: () => of(monthResponse) };
    createSpy = { calls: [] };
    deleteSpy = { calls: [] };
    serverDateSignal = signal<string | null>(SERVER_DATE);
    await TestBed.configureTestingModule({
      imports: [BudgetComponent],
      providers: [
        {
          provide: AuthService,
          useValue: { serverDate: serverDateSignal },
        },
        {
          provide: BudgetService,
          useValue: {
            getMonth: (m: string) => {
              getMonthSpy.calls.push(m);
              return getMonthSpy.fn(m);
            },
            create: (p: BudgetPayload) => {
              createSpy.calls.push(p);
              return of(item1);
            },
            update: () => of(item1),
            delete: (id: number) => {
              deleteSpy.calls.push(id);
              return of(undefined);
            },
          },
        },
        {
          provide: CategoriesService,
          useValue: { tree: () => of(tree) },
        },
        ToastService,
      ],
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  function comp() {
    const fixture = TestBed.createComponent(BudgetComponent);
    fixture.detectChanges();
    return {
      fixture,
      component: fixture.componentInstance as unknown as BudgetComponentApi,
    };
  }

  it('shows the empty state with the add button when the month has no items', () => {
    getMonthSpy.fn = () => of(emptyResponse);
    const { fixture } = comp();
    const el = fixture.nativeElement as HTMLElement;

    expect(el.textContent).toContain('План на месяц не задан. Добавьте первую позицию.');
    expect(
      Array.from(el.querySelectorAll('button')).some((b) => b.textContent?.includes('+ добавить')),
    ).toBe(true);
    expect(getMonthSpy.calls[0]).toBe('2026-08');
  });

  it('renders items grouped by category in the tree order with month totals', () => {
    const { fixture } = comp();
    const el = fixture.nativeElement as HTMLElement;

    expect(el.textContent).toContain('Продукты / Супермаркет');
    expect(el.textContent).toContain('основной запас');
    expect(el.textContent).toContain('15\u00A0000,00 ₽');
    expect(el.textContent).toContain('3\u00A0000,00 ₽');
    expect(el.textContent).toContain('18\u00A0000,00 ₽');
    expect(el.textContent).toContain('13\u00A0100,50 ₽');
    expect(el.textContent).toContain('4\u00A0899,50 ₽');

    const names = Array.from(el.querySelectorAll<HTMLElement>('.group-name')).map(
      (n) => n.textContent?.trim(),
    );
    expect(names).toEqual(['Продукты', 'Продукты / Супермаркет']);
  });

  it('sends the selected month and the draft fields to create', () => {
    const { component: c } = comp();
    c.startCreate();
    c.draftCategoryId.set(4);
    c.draftAmount.set('1500.50');
    c.draftNote.set('тест');
    c.save();

    expect(createSpy.calls.length).toBe(1);
    expect(createSpy.calls[0]).toEqual({
      month: '2026-08',
      categoryId: 4,
      amount: 1500.5,
      note: 'тест',
    });
    const toasts = TestBed.inject(ToastService);
    expect(toasts.toasts().some((t) => t.kind === 'success' && t.message === 'Позиция добавлена')).toBe(
      true,
    );
  });

  it('asks for confirmation and deletes the item', () => {
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true);
    const { component: c } = comp();

    c.remove(item1);

    expect(confirmSpy).toHaveBeenCalledTimes(1);
    expect(deleteSpy.calls).toEqual([1]);
    const toasts = TestBed.inject(ToastService);
    expect(toasts.toasts().some((t) => t.kind === 'success' && t.message === 'Позиция удалена')).toBe(
      true,
    );
  });

  it('does not delete the item when confirmation is cancelled', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false);
    const { component: c } = comp();

    c.remove(item1);

    expect(deleteSpy.calls).toEqual([]);
  });

  it('requests the shifted month after the month is changed', () => {
    const { component: c } = comp();

    c.shiftMonth(-1);

    expect(getMonthSpy.calls[getMonthSpy.calls.length - 1]).toBe('2026-07');
  });
});
