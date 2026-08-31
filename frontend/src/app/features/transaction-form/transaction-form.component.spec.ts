import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';
import { CategoriesService } from '../../core/categories.service';
import { TransactionDto, UserDto } from '../../core/models';
import { ToastService } from '../../core/toast.service';
import { TransactionsService } from '../../core/transactions.service';
import { UsersService } from '../../core/users.service';
import { TransactionFormComponent } from './transaction-form.component';

@Component({ template: '<p>stub</p>' })
class StubComponent {}

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
  form: { controls: Record<string, { setValue(v: unknown): void }> };
  save(): void;
  switchKind(kind: string): void;
  kind: () => string;
  categoriesForKind: () => { id: number; name: string; kind: string; parentId: number | null }[];
};

describe('TransactionFormComponent', () => {
  let createCalls: unknown[];
  let fixture: import('@angular/core/testing').ComponentFixture<TransactionFormComponent>;
  let ctrl: FormCtrl;

  beforeEach(async () => {
    createCalls = [];
    await TestBed.configureTestingModule({
      imports: [TransactionFormComponent],
      providers: [
        provideRouter([
          { path: '', component: StubComponent },
          { path: '**', component: StubComponent },
        ]),
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
        { provide: UsersService, useValue: { list: () => of([user]) } },
        ToastService,
      ],
    });
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

  it('lists subcategories of the current kind', () => {
    const cats = ctrl.categoriesForKind();
    expect(cats.map((c) => c.name)).toEqual(['Продукты', 'Супермаркет']);
    expect(cats[1].parentId).toBe(5);
  });

  it('switches category list when kind changes', () => {
    ctrl.switchKind('INCOME');
    fixture.detectChanges();
    expect(ctrl.kind()).toBe('INCOME');
    expect(ctrl.categoriesForKind().map((c) => c.name)).toEqual(['Зарплата']);
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Зарплата');
    expect(el.textContent).not.toContain('Супермаркет');
  });

  it('resets category selection when switching kind', () => {
    ctrl.form.controls['categoryId'].setValue(5);
    ctrl.switchKind('INCOME');
    expect((ctrl.form.controls['categoryId'] as unknown as { value: unknown }).value).toBeNull();
  });
});
