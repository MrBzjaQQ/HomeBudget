import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { signal } from '@angular/core';
import { AuthService } from '../../core/auth.service';
import { CategoriesService } from '../../core/categories.service';
import { TransactionListResponse } from '../../core/models';
import { ToastService } from '../../core/toast.service';
import { TransactionsService } from '../../core/transactions.service';
import { UsersService } from '../../core/users.service';
import { Component } from '@angular/core';
import { JournalComponent } from './journal.component';

@Component({ template: '<p>stub</p>' })
class StubComponent {}

const listResponse: TransactionListResponse = {
  items: [
    {
      id: 1,
      kind: 'EXPENSE',
      amount: 1250.5,
      date: '2026-08-30',
      categoryId: 5,
      categoryName: 'Продукты',
      categoryParentName: 'Еда',
      color: '#ef4444',
      userId: 1,
      userDisplayName: 'Иван',
      note: 'Пятёрочка',
      createdAt: '2026-08-30T10:00:00',
      updatedAt: '2026-08-30T10:00:00',
    },
  ],
  total: 1,
  page: 1,
  pageSize: 50,
  totals: { income: 100, expense: 1250.5, net: -1150.5 },
};

const SERVER_DATE = '2026-08-31';

describe('JournalComponent', () => {
  let listSpy: { calls: unknown[][]; fn: (p: unknown) => import('rxjs').Observable<TransactionListResponse> };
  let serverDateSignal: { set(v: string | null): void };

  beforeEach(async () => {
    listSpy = { calls: [], fn: (p) => { listSpy.calls.push([p]); return of(listResponse); } };
    serverDateSignal = signal<string | null>(SERVER_DATE);
    await TestBed.configureTestingModule({
      imports: [JournalComponent],
      providers: [
        provideRouter([
          { path: '', component: StubComponent },
          { path: 'transactions/:id/edit', component: StubComponent },
          { path: 'transactions/new', component: StubComponent },
          { path: '**', component: StubComponent },
        ]),
        {
          provide: AuthService,
          useValue: { serverDate: serverDateSignal },
        },
        {
          provide: TransactionsService,
          useValue: { list: (p: unknown) => listSpy.fn(p), delete: () => of(undefined) },
        },
        {
          provide: CategoriesService,
          useValue: {
            tree: () =>
              of([
                {
                  id: 5,
                  name: 'Еда',
                  kind: 'EXPENSE',
                  parentId: null,
                  color: null,
                  sortOrder: 0,
                  isArchived: false,
                  children: [
                    {
                      id: 6,
                      name: 'Продукты',
                      kind: 'EXPENSE',
                      parentId: 5,
                      color: '#ef4444',
                      sortOrder: 0,
                      isArchived: false,
                      children: null,
                    },
                  ],
                },
              ]),
          },
        },
        {
          provide: UsersService,
          useValue: {
            list: () => of([{ id: 1, username: 'ivan', displayName: 'Иван', isActive: true, createdAt: '2026-01-01T00:00:00' }]),
          },
        },
        ToastService,
      ],
    });
  });

  it('renders totals with ruble sign and transactions list', () => {
    const fixture = TestBed.createComponent(JournalComponent);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Еда / Продукты');
    expect(el.textContent).toContain('Пятёрочка');
    expect(el.textContent).toContain('1\u00A0250,50 ₽');
    expect(el.textContent).toContain('−1\u00A0150,50 ₽');
    expect(listSpy.calls.length).toBeGreaterThan(0);
  });

  it('requests the server current month by default', () => {
    const fixture = TestBed.createComponent(JournalComponent);
    fixture.detectChanges();

    const params = listSpy.calls[0][0] as { from: string; to: string };
    expect(params.from).toBe('2026-08-01');
    expect(params.to).toBe('2026-08-31');
  });

  it('falls back to local month when serverDate is absent', () => {
    serverDateSignal.set(null);
    const fixture = TestBed.createComponent(JournalComponent);
    fixture.detectChanges();

    const params = listSpy.calls[0][0] as { from: string };
    const now = new Date();
    const expectedFrom = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-01`;
    expect(params.from).toBe(expectedFrom);
  });

  it('refetches with kind filter', () => {
    const fixture = TestBed.createComponent(JournalComponent);
    fixture.detectChanges();
    listSpy.calls.length = 0;

    const comp = fixture.componentInstance as unknown as { setKind(k: string): void };
    comp.setKind('INCOME');
    fixture.detectChanges();

    expect(listSpy.calls.length).toBe(1);
    const params = listSpy.calls[0][0] as { kind: string };
    expect(params.kind).toBe('INCOME');
  });

  it('defaults the custom range to the visible month when enabled and formats it as dd.mm.yyyy', () => {
    const fixture = TestBed.createComponent(JournalComponent);
    fixture.detectChanges();
    listSpy.calls.length = 0;
    (fixture.componentInstance as unknown as { toggleCustomRange(): void }).toggleCustomRange();
    fixture.detectChanges();

    expect(listSpy.calls.length).toBe(1);
    const params = listSpy.calls[0][0] as { from: string; to: string };
    expect(params.from).toBe('2026-08-01');
    expect(params.to).toBe('2026-08-31');

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('01.08.2026 — 31.08.2026');
  });

  it('shows the «Сегодня» chip and jumps to the server month', () => {
    const fixture = TestBed.createComponent(JournalComponent);
    fixture.detectChanges();
    listSpy.calls.length = 0;

    const el = fixture.nativeElement as HTMLElement;
    const chip = Array.from(el.querySelectorAll('button')).find((b) => b.textContent?.trim() === 'Сегодня') as HTMLButtonElement;
    expect(chip).toBeTruthy();
    expect(chip.disabled).toBe(true);

    (fixture.componentInstance as unknown as { shiftMonth(d: number): void }).shiftMonth(1);
    fixture.detectChanges();
    expect(chip.disabled).toBe(false);

    chip.click();
    fixture.detectChanges();

    const last = listSpy.calls[listSpy.calls.length - 1][0] as { from: string; to: string };
    expect(last.from).toBe('2026-08-01');
    expect(last.to).toBe('2026-08-31');
    expect(chip.disabled).toBe(true);
  });

  it('shows empty state when there are no transactions', () => {
    listSpy.fn = () => of({ ...listResponse, items: [], total: 0 });
    const fixture = TestBed.createComponent(JournalComponent);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Операций нет');
  });

  it('shows expense amounts without minus sign, only balance keeps it', () => {
    const fixture = TestBed.createComponent(JournalComponent);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('1\u00A0250,50 ₽');
    expect(el.textContent).not.toContain('−1\u00A0250,50');
    expect(el.textContent).toContain('−1\u00A0150,50 ₽');
  });

  it('labels subcategories with their parent in the filter panel', () => {
    const fixture = TestBed.createComponent(JournalComponent);
    fixture.detectChanges();
    (fixture.componentInstance as unknown as { catPanelOpen: { set(v: boolean): void } }).catPanelOpen.set(true);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Еда / Продукты');
  });

  it('highlights selected categories with cat-sel in the filter panel', () => {
    const fixture = TestBed.createComponent(JournalComponent);
    fixture.detectChanges();
    const comp = fixture.componentInstance as unknown as {
      catPanelOpen: { set(v: boolean): void };
      toggleCat(id: number): void;
    };
    comp.catPanelOpen.set(true);
    fixture.detectChanges();

    const label = (): HTMLElement | undefined => {
      const el = fixture.nativeElement as HTMLElement;
      return Array.from(el.querySelectorAll('label')).find((l) => l.textContent?.includes('Еда / Продукты'));
    };
    expect(label()!.className).not.toContain('cat-sel');

    comp.toggleCat(6);
    fixture.detectChanges();
    expect(label()!.className).toContain('cat-sel');

    comp.toggleCat(6);
    fixture.detectChanges();
    expect(label()!.className).not.toContain('cat-sel');
  });

  it('resets sort to date desc together with filters', () => {
    const fixture = TestBed.createComponent(JournalComponent);
    fixture.detectChanges();

    const comp = fixture.componentInstance as unknown as {
      setKind(k: string): void;
      setSort(s: string): void;
      toggleDir(): void;
      resetFilters(): void;
    };
    comp.setKind('EXPENSE');
    comp.setSort('amount');
    comp.toggleDir();
    comp.resetFilters();
    fixture.detectChanges();

    const last = listSpy.calls[listSpy.calls.length - 1][0] as { sort: string; dir: string; kind?: string };
    expect(last.sort).toBe('date');
    expect(last.dir).toBe('desc');
    expect(last.kind).toBeUndefined();
  });

  it('shows the reset button when only the sort order changed', () => {
    const fixture = TestBed.createComponent(JournalComponent);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    const resetBtn = (): HTMLElement | undefined =>
      Array.from(el.querySelectorAll('button')).find((b) => b.textContent?.trim() === 'сбросить');
    expect(resetBtn()).toBeUndefined();

    (fixture.componentInstance as unknown as { setSort(s: string): void }).setSort('amount');
    fixture.detectChanges();
    expect(resetBtn()).toBeDefined();

    resetBtn()!.click();
    fixture.detectChanges();
    expect(resetBtn()).toBeUndefined();
    const last = listSpy.calls[listSpy.calls.length - 1][0] as { sort: string; dir: string };
    expect(last.sort).toBe('date');
    expect(last.dir).toBe('desc');
  });

  it('renders a desktop table with all columns and a card list for mobile', () => {
    const fixture = TestBed.createComponent(JournalComponent);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    const table = el.querySelector('table');
    expect(table).toBeTruthy();
    const headers = Array.from(table!.querySelectorAll('th')).map((th) => (th.textContent ?? '').trim());
    expect(headers).toContain('Дата');
    expect(headers).toContain('Категория');
    expect(headers).toContain('Комментарий');
    expect(headers).toContain('Кто');
    expect(headers).toContain('Сумма');
    const rows = table!.querySelectorAll('tbody tr');
    expect(rows.length).toBe(1);
    expect(rows[0].textContent).toContain('30.08.2026');
    expect(rows[0].textContent).toContain('1\u00A0250,50 ₽');
    expect(el.querySelector('.md\\:hidden')).toBeTruthy();
  });
});
