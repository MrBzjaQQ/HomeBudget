import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiError, CategoryDto, TransactionPayload, TxnKind } from '../../core/models';
import { CategoriesService, flattenCategoryTree } from '../../core/categories.service';
import { LastCategoryService } from '../../core/last-category.service';
import { ToastService } from '../../core/toast.service';
import { TransactionsService } from '../../core/transactions.service';
import { UsersService } from '../../core/users.service';
import { UserDto } from '../../core/models';

@Component({
  selector: 'app-transaction-form',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <form [formGroup]="form" (ngSubmit)="save()" class="space-y-4">
      <div class="card">
        <div class="grid grid-cols-2 gap-1 rounded-xl bg-gray-100 p-1">
          <button
            type="button"
            class="rounded-lg py-2.5 text-sm font-semibold transition"
            [class]="kind() === 'EXPENSE' ? 'bg-white text-expense shadow' : 'text-gray-500'"
            (click)="switchKind('EXPENSE')"
          >
            Расход
          </button>
          <button
            type="button"
            class="rounded-lg py-2.5 text-sm font-semibold transition"
            [class]="kind() === 'INCOME' ? 'bg-white text-income shadow' : 'text-gray-500'"
            (click)="switchKind('INCOME')"
          >
            Доход
          </button>
        </div>

        <div class="mt-4 text-center">
          <div class="text-xs text-gray-400">Сумма</div>
          <div class="mt-1 flex items-center justify-center gap-1">
            <input
              type="text"
              inputmode="decimal"
              formControlName="amount"
              placeholder="0,00"
              class="w-56 text-center text-4xl font-bold tracking-tight text-gray-900 focus:outline-none"
            />
            <span class="text-2xl font-semibold text-gray-400">₽</span>
          </div>
          @if (amountError()) {
            <div class="mt-2 text-xs text-red-600">{{ amountError() }}</div>
          }
        </div>
      </div>

      <div class="card grid grid-cols-2 gap-3">
        <div>
          <label class="field-label">Дата</label>
          <input type="date" formControlName="date" class="input" [max]="today" />
        </div>
        <div>
          <label class="field-label">Кто</label>
          <select class="input" formControlName="userId">
            @for (u of users(); track u.id) {
              <option [ngValue]="u.id">{{ u.displayName }}</option>
            }
          </select>
        </div>
      </div>

      <div class="card">
        <label class="field-label">Категория</label>
        @if (!categoriesReady()) {
          <div class="py-3 text-sm text-gray-400">Загрузка…</div>
        } @else {
          <div class="flex flex-wrap items-center gap-2">
            @for (cat of categoriesForKind(); track cat.id) {
              <button
                type="button"
                class="cat-pill"
                [class.cat-pill-sub]="cat.parentId !== null"
                [class.cat-pill-active]="form.controls.categoryId.value === cat.id"
                (click)="form.controls.categoryId.setValue(cat.id)"
              >
                <span
                  class="inline-block h-2 w-2 rounded-full"
                  [style.background]="cat.color || '#d1d5db'"
                ></span>
                {{ cat.name }}
              </button>
            }
          </div>
          @if (form.controls.categoryId.invalid && form.controls.categoryId.touched) {
            <div class="mt-2 text-xs text-red-600">Выберите категорию</div>
          }
        }
      </div>

      <div class="card">
        <label class="field-label">Комментарий</label>
        <textarea
          formControlName="note"
          rows="2"
          placeholder="Например: Продукты, Пятёрочка"
          class="input w-full resize-none"
          maxlength="500"
        ></textarea>
      </div>

      @if (formError()) {
        <div class="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{{ formError() }}</div>
      }

      <div class="flex gap-3">
        <button type="button" routerLink="/" class="btn-secondary flex-1">Отмена</button>
        <button type="submit" [disabled]="busy()" class="btn-primary flex-1">
          @if (busy()) {
            Сохранение…
          } @else {
            Сохранить
          }
        </button>
      </div>
    </form>
  `,
  styles: `
    .card {
      background: white;
      border: 1px solid #f3f4f6;
      border-radius: 14px;
      padding: 14px;
      box-shadow: 0 1px 2px rgb(0 0 0 / 0.04);
    }
    .field-label {
      display: block;
      font-size: 12px;
      font-weight: 500;
      color: #6b7280;
      margin-bottom: 6px;
    }
    .input {
      width: 100%;
      border: 1px solid #e5e7eb;
      border-radius: 10px;
      padding: 10px;
      font-size: 15px;
      background: white;
    }
    .input:focus {
      outline: 2px solid rgb(15 118 110 / 0.3);
      border-color: #0f766e;
    }
    .cat-pill {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      border-radius: 9999px;
      border: 1px solid #e5e7eb;
      background: white;
      padding: 7px 13px;
      font-size: 13px;
      color: #374151;
    }
    .cat-pill-sub {
      margin-left: 14px;
      padding: 5px 11px;
      font-size: 12px;
      color: #6b7280;
      background: #fafafa;
    }
    .cat-pill-active {
      background: #0f766e;
      border-color: #0f766e;
      color: white;
      font-weight: 500;
    }
    .btn-primary {
      border-radius: 12px;
      background: #0f766e;
      color: white;
      font-weight: 600;
      font-size: 15px;
      padding: 13px;
    }
    .btn-primary:disabled {
      opacity: 0.5;
    }
    .btn-secondary {
      border-radius: 12px;
      border: 1px solid #e5e7eb;
      background: white;
      color: #374151;
      font-weight: 500;
      font-size: 15px;
      padding: 13px;
    }
  `,
})
export class TransactionFormComponent implements OnInit {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly txns = inject(TransactionsService);
  private readonly cats = inject(CategoriesService);
  private readonly usersSvc = inject(UsersService);
  private readonly toasts = inject(ToastService);
  private readonly lastCat = inject(LastCategoryService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly today = todayStr();
  protected readonly users = signal<UserDto[]>([]);
  protected readonly allCategories = signal<CategoryDto[]>([]);
  protected readonly categoriesReady = signal(false);
  protected readonly busy = signal(false);
  protected readonly formError = signal<string | null>(null);

  protected readonly editingId = signal<number | null>(null);

  protected readonly kind = signal<TxnKind>('EXPENSE');

  protected readonly form = this.fb.group({
    amount: ['', [Validators.required, Validators.pattern(/^\d{1,12}([.,]\d{1,2})?$/)]],
    date: [todayStr(), [Validators.required]],
    categoryId: [null as number | null, [Validators.required]],
    userId: [0, [Validators.required]],
    note: [''],
  });

  protected readonly categoriesForKind = computed(() => {
    const kind = this.kind();
    const groups = this.allCategories()
      .filter((c) => !c.isArchived && c.kind === kind)
      .sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name, 'ru'));
    const flat: CategoryDto[] = [];
    for (const g of groups) {
      flat.push(g);
      if (g.children) {
        flat.push(
          ...g.children
            .filter((c) => !c.isArchived && c.kind === kind)
            .sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name, 'ru')),
        );
      }
    }
    return flat;
  });

  protected amountError(): string | null {
    const c = this.form.controls.amount;
    if (!c.touched && !c.dirty) {
      return null;
    }
    if (c.hasError('pattern')) {
      return 'Введите сумму, например 1250,50';
    }
    if (c.hasError('required')) {
      return 'Введите сумму';
    }
    const v = parseAmount(c.value);
    if (v !== null && v <= 0) {
      return 'Сумма должна быть больше нуля';
    }
    if (v !== null && v > 999_999_999_999.99) {
      return 'Сумма слишком большая';
    }
    return null;
  }

  ngOnInit(): void {
    const idRaw = this.route.snapshot.paramMap.get('id');
    const id = idRaw ? Number(idRaw) : null;
    if (id !== null && !Number.isNaN(id)) {
      this.editingId.set(id);
    }

    this.usersSvc.list().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (list) => {
        const active = list.filter((u) => u.isActive);
        this.users.set(active);
        if (this.form.controls.userId.value === 0 && active.length > 0) {
          this.form.controls.userId.setValue(active[0].id);
        }
      },
      error: () => this.toasts.error('Не удалось загрузить пользователей'),
    });

    this.cats.tree().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (list) => {
        this.allCategories.set(list);
        this.categoriesReady.set(true);
        this.applyLastCategory();
      },
      error: () => this.toasts.error('Не удалось загрузить категории'),
    });

    if (this.editingId() !== null) {
      this.loadExisting(this.editingId() as number);
    }
  }

  protected switchKind(kind: TxnKind): void {
    this.kind.set(kind);
    this.form.controls.categoryId.setValue(null);
    const remembered = this.lastCat.categoryFor(kind);
    const flat = flattenCategoryTree(this.allCategories());
    if (remembered !== null && flat.some((c) => c.id === remembered && !c.isArchived && c.kind === kind)) {
      this.form.controls.categoryId.setValue(remembered);
    }
  }

  protected save(): void {
    if (this.form.invalid || this.busy()) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    const amountValue = parseAmount(v.amount);
    if (amountValue === null || amountValue <= 0) {
      this.form.controls.amount.markAsTouched();
      return;
    }
    this.busy.set(true);
    this.formError.set(null);

    const kind = this.kind();
    const payload: TransactionPayload = {
      kind,
      amount: amountValue,
      date: v.date,
      categoryId: v.categoryId as number,
      userId: v.userId || undefined,
      note: v.note.trim() ? v.note.trim() : null,
    };

    const op = this.editingId() !== null
      ? this.txns.update(this.editingId() as number, payload)
      : this.txns.create(payload);

    op.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.lastCat.remember(kind, payload.categoryId as number);
        this.toasts.success(this.editingId() !== null ? 'Операция обновлена' : 'Операция добавлена');
        this.router.navigate(['/']);
      },
      error: (err) => {
        this.busy.set(false);
        const apiError = ApiError.from(err);
        const fieldError = apiError.errors ? Object.values(apiError.errors).flat()[0] : undefined;
        this.formError.set(fieldError ?? apiError.message);
      },
    });
  }

  private loadExisting(id: number): void {
    this.txns
      .get(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (t) => {
          if (!t) {
            this.toasts.error('Операция не найдена');
            this.router.navigate(['/']);
            return;
          }
          this.kind.set(t.kind);
          this.form.patchValue({
            amount: t.amount.toFixed(2).replace('.', ','),
            date: t.date,
            categoryId: t.categoryId,
            userId: t.userId,
            note: t.note ?? '',
          });
        },
        error: () => {
          this.toasts.error('Не удалось загрузить операцию');
          this.router.navigate(['/']);
        },
      });
  }

  private applyLastCategory(): void {
    const kind = this.kind();
    const remembered = this.lastCat.categoryFor(kind);
    const flat = flattenCategoryTree(this.allCategories());
    if (remembered !== null && flat.some((c) => c.id === remembered && !c.isArchived && c.kind === kind)) {
      this.form.controls.categoryId.setValue(remembered);
    }
  }
}

function todayStr(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

function parseAmount(raw: string): number | null {
  const normalized = raw.trim().replace(/\s/g, '').replace(',', '.');
  if (!normalized) {
    return null;
  }
  const n = Number(normalized);
  return Number.isFinite(n) ? n : null;
}
