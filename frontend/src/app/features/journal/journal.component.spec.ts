import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
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

describe('JournalComponent', () => {
  let listSpy: { calls: unknown[][]; fn: (p: unknown) => import('rxjs').Observable<TransactionListResponse> };

  beforeEach(async () => {
    listSpy = { calls: [], fn: (p) => { listSpy.calls.push([p]); return of(listResponse); } };
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

  it('renders totals and transactions list', () => {
    const fixture = TestBed.createComponent(JournalComponent);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Еда / Продукты');
    expect(el.textContent).toContain('Пятёрочка');
    expect(el.textContent).toContain('1\u00A0250,50');
    expect(el.textContent).toContain('−1\u00A0150,50');
    expect(listSpy.calls.length).toBeGreaterThan(0);
  });

  it('requests current month by default', () => {
    const fixture = TestBed.createComponent(JournalComponent);
    fixture.detectChanges();

    const params = listSpy.calls[0][0] as { from: string; to: string };
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

  it('defaults the custom range to the visible month when enabled', () => {
    const fixture = TestBed.createComponent(JournalComponent);
    fixture.detectChanges();
    listSpy.calls.length = 0;
    (fixture.componentInstance as unknown as { toggleCustomRange(): void }).toggleCustomRange();
    fixture.detectChanges();

    expect(listSpy.calls.length).toBe(1);
    const params = listSpy.calls[0][0] as { from: string; to: string };
    const now = new Date();
    const mm = String(now.getMonth() + 1).padStart(2, '0');
    expect(params.from).toBe(`${now.getFullYear()}-${mm}-01`);
    const lastDay = new Date(now.getFullYear(), now.getMonth() + 1, 0).getDate();
    expect(params.to).toBe(`${now.getFullYear()}-${mm}-${String(lastDay).padStart(2, '0')}`);
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
    expect(el.textContent).toContain('1\u00A0250,50');
    expect(el.textContent).not.toContain('−1\u00A0250,50');
    expect(el.textContent).toContain('−1\u00A0150,50');
  });

  it('labels subcategories with their parent in the filter panel', () => {
    const fixture = TestBed.createComponent(JournalComponent);
    fixture.detectChanges();
    (fixture.componentInstance as unknown as { catPanelOpen: { set(v: boolean): void } }).catPanelOpen.set(true);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Еда / Продукты');
  });
});
