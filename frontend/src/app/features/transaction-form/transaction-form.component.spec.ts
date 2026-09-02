import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import { CategoriesService } from '../../core/categories.service';
import { TransactionDto, UserDto } from '../../core/models';
import { ToastService } from '../../core/toast.service';
import { TransactionsService } from '../../core/transactions.service';
import { UsersService } from '../../core/users.service';
import { TransactionFormComponent } from './transaction-form.component';

@Component({ template: '<p>stub</p>' })
class StubComponent {}

const SERVER_DATE = '2026-08-31';

const createdDto: TransactionDto = {
  id: 99,
  kind: 'EXPENSE',
  amount: 1250.5,
  date: '2026-08-30',
  categoryId: 5,
  categoryName: 'Продукты',
  categoryParentName: null,
  color: null,
  userId: 1,
  userDisplayName: 'Иван',
  note: null,
  createdAt: '2026-08-30T10:00:00',
  updatedAt: '2026-08-30T10:00:00',
};

const user: UserDto = {
  id: 1,
  username: 'ivan',
  displayName: 'Иван',
  isActive: true,
  createdAt: '2026-01-01T00:00:00',
};

type FormCtrl = {
  form: {
    controls: Record<
      string,
      { setValue(v: unknown): void; value: unknown; markAsTouched(): void }
    >;
  };
  save(): void;
  switchKind(kind: string): void;
  kind: () => string;
  today: () => string;
  amountError(): string | null;
  categoryBranches: () => { title: string | null; count: number; items: { id: number; name: string; kind: string; parentId: number | null }[] }[];
};

describe('TransactionFormComponent', () => {
  let createCalls: unknown[];
  let fixture: import('@angular/core/testing').ComponentFixture<TransactionFormComponent>;
  let ctrl: FormCtrl;
  let serverDateSignal: { set(v: string | null): void };

  function configureTestBed(usersList: UserDto[], logged: UserDto) {
    const sd = signal<string | null>(SERVER_DATE);
    serverDateSignal = sd;
    createCalls = [];
    return TestBed.configureTestingModule({
      imports: [TransactionFormComponent],
      providers: [
        provideRouter([
          { path: '', component: StubComponent },
          { path: '**', component: StubComponent },
        ]),
        { provide: AuthService, useValue: { serverDate: sd, user: () => logged } },
        {
          provide: TransactionsService,
          useValue: {
            create: (p: unknown) => {
              createCalls.push(p);
              return of(createdDto);
            },
            update: () => of(createdDto),
            get: () => of(createdDto),
          },
        },
        {
          provide: CategoriesService,
          useValue: {
            tree: () =>
              of([
                {
                  id: 5,
                  name: 'Продукты',
                  kind: 'EXPENSE',
                  parentId: null,
                  color: '#ef4444',
                  sortOrder: 0,
                  isArchived: false,
                  children: [
                    {
                      id: 6,
                      name: 'Супермаркет',
                      kind: 'EXPENSE',
                      parentId: 5,
                      color: null,
                      sortOrder: 0,
                      isArchived: false,
                      children: null,
                    },
                  ],
                },
                {
                  id: 7,
                  name: 'Зарплата',
                  kind: 'INCOME',
                  parentId: null,
                  color: '#2E7D32',
                  sortOrder: 0,
                  isArchived: false,
                  children: null,
                },
              ]),
          },
        },
        { provide: UsersService, useValue: { list: () => of(usersList) } },
        ToastService,
      ],
    });
  }

  beforeEach(async () => {
    localStorage.clear();
    await configureTestBed([user], user);
    fixture = TestBed.createComponent(TransactionFormComponent);
    ctrl = fixture.componentInstance as unknown as FormCtrl;
    fixture.detectChanges();
  });

  function fillForm(amount: string) {
    ctrl.form.controls['amount'].setValue(amount);
    ctrl.form.controls['categoryId'].setValue(5);
  }

  it('rejects invalid amount format and does not call create', () => {
    fillForm('abc');
    ctrl.save();
    expect(createCalls.length).toBe(0);
  });

  it('rejects zero amount', () => {
    fillForm('0');
    ctrl.save();
    expect(createCalls.length).toBe(0);
  });

  it('creates transaction with parsed comma amount', () => {
    fillForm('1250,50');
    ctrl.save();
    expect(createCalls.length).toBe(1);
    const payload = createCalls[0] as { kind: string; amount: number; categoryId: number; userId: number };
    expect(payload.kind).toBe('EXPENSE');
    expect(payload.amount).toBe(1250.5);
    expect(payload.categoryId).toBe(5);
    expect(payload.userId).toBe(1);
  });

  it('navigates back to journal after successful save', () => {
    fillForm('100');
    ctrl.save();
    expect(TestBed.inject(Router).url).toBe('/');
  });

  it('defaults the owner to the logged-in user, not the first in the list', async () => {
    TestBed.resetTestingModule();
    const second: UserDto = {
      id: 2,
      username: 'anna',
      displayName: 'Анна',
      isActive: true,
      createdAt: '2026-01-01T00:00:00',
    };
    await configureTestBed([user, second], second);
    const f2 = TestBed.createComponent(TransactionFormComponent);
    f2.detectChanges();
    const c2 = f2.componentInstance as unknown as FormCtrl;
    expect(c2.form.controls['userId'].value).toBe(2);
  });

  it('groups categories: subs nested under group, childless group is a plain pill', () => {
    const branches = ctrl.categoryBranches();
    expect(branches.length).toBe(1);
    expect(branches[0].title).toBe('Продукты');
    expect(branches[0].count).toBe(1);
    expect(branches[0].items.map((c) => c.name)).toEqual(['Супермаркет']);
    expect(branches[0].items[0].parentId).toBe(5);
  });

  it('switches category list when kind changes', () => {
    ctrl.switchKind('INCOME');
    fixture.detectChanges();
    expect(ctrl.kind()).toBe('INCOME');
    const branches = ctrl.categoryBranches();
    expect(branches.length).toBe(1);
    expect(branches[0].title).toBeNull();
    expect(branches[0].items.map((c) => c.name)).toEqual(['Зарплата']);
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Зарплата');
    expect(el.textContent).not.toContain('Супермаркет');
  });

  it('resets category selection when switching kind', () => {
    ctrl.form.controls['categoryId'].setValue(5);
    ctrl.switchKind('INCOME');
    expect((ctrl.form.controls['categoryId'] as unknown as { value: unknown }).value).toBeNull();
  });

  it('defaults date and max to the server date', () => {
    expect(ctrl.form.controls['date'].value).toBe(SERVER_DATE);
    expect(ctrl.today()).toBe(SERVER_DATE);
    const dateInput = fixture.nativeElement.querySelector('input[type="date"]') as HTMLInputElement;
    expect(dateInput.max).toBe(SERVER_DATE);
  });

  it('updates the date default when serverDate arrives later and the field is pristine', () => {
    serverDateSignal.set(null);
    const fixture2 = TestBed.createComponent(TransactionFormComponent);
    const ctrl2 = fixture2.componentInstance as unknown as FormCtrl;
    fixture2.detectChanges();
    expect(ctrl2.form.controls['date'].value).not.toBe(SERVER_DATE);

    serverDateSignal.set('2026-08-25');
    fixture2.detectChanges();
    expect(ctrl2.form.controls['date'].value).toBe('2026-08-25');
    expect(ctrl2.today()).toBe('2026-08-25');
  });

  it('shows a specific message when the amount exceeds the maximum', () => {
    fillForm('1000000000000,00');
    ctrl.form.controls['amount'].markAsTouched();
    expect(ctrl.amountError()).toBe('Сумма слишком большая (макс. 999 999 999 999,99)');
  });

  it('shows no validation messages on a fresh untouched form', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).not.toContain('Введите сумму');
    expect(el.textContent).not.toContain('Выберите категорию');
  });

  it('shows the amount message after the field is touched while empty', () => {
    ctrl.form.controls['amount'].markAsTouched();
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Введите сумму');
  });

  it('shows all validation messages after a submit attempt with empty form', () => {
    ctrl.save();
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Введите сумму');
    expect(el.textContent).toContain('Выберите категорию');
    expect(createCalls.length).toBe(0);
  });
});
