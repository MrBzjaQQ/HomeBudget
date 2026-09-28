import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { ApiError, CategoryDto, TransactionPayload, TxnKind } from '../../core/models';
import { AuthService } from '../../core/auth.service';
import { CategoriesService, flattenCategoryTree } from '../../core/categories.service';
import { LastCategoryService } from '../../core/last-category.service';
import { ToastService } from '../../core/toast.service';
import { TransactionsService } from '../../core/transactions.service';
import { UsersService } from '../../core/users.service';
import { UserDto } from '../../core/models';

const MAX_AMOUNT = 999_999_999_999.99;

interface CategoryBranch {
  title: string | null;
  count: number;
  items: CategoryDto[];
}

@Component({
  selector: 'app-transaction-form',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './transaction-form.component.html',
  styleUrl: './transaction-form.component.css',
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
  private readonly auth = inject(AuthService);

  protected readonly today = computed(() => this.auth.serverDate() ?? todayStr());
  protected readonly users = signal<UserDto[]>([]);
  protected readonly allCategories = signal<CategoryDto[]>([]);
  protected readonly categoriesReady = signal(false);
  protected readonly busy = signal(false);
  protected readonly formError = signal<string | null>(null);
  protected readonly attemptedSubmit = signal(false);

  protected readonly editingId = signal<number | null>(null);

  protected readonly kind = signal<TxnKind>('EXPENSE');

  protected readonly form = this.fb.group({
    amount: ['', [Validators.required, Validators.pattern(/^\d{1,12}([.,]\d{1,2})?$/)]],
    date: [this.auth.serverDate() ?? todayStr(), [Validators.required]],
    categoryId: [null as number | null, [Validators.required]],
    userId: [0, [Validators.required]],
    note: [''],
  });

  protected readonly categoryBranches = computed<CategoryBranch[]>(() => {
    const kind = this.kind();
    const groups = this.allCategories()
      .filter((c) => !c.isArchived && c.kind === kind)
      .sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name, 'ru'));
    const branches: CategoryBranch[] = [];
    for (const g of groups) {
      const subs = (g.children ?? [])
        .filter((c) => !c.isArchived && c.kind === kind)
        .sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name, 'ru'));
      if (subs.length > 0) {
        branches.push({ title: g.name, count: subs.length, items: subs });
      } else {
        branches.push({ title: null, count: 0, items: [g] });
      }
    }
    return branches;
  });

  protected amountError(): string | null {
    const c = this.form.controls.amount;
    if (!c.touched && !c.dirty && !this.attemptedSubmit()) {
      return null;
    }
    const v = parseAmount(c.value);
    if (v !== null && v > MAX_AMOUNT) {
      return 'Сумма слишком большая (макс. 999 999 999 999,99)';
    }
    if (c.hasError('pattern')) {
      return 'Введите сумму, например 1250,50';
    }
    if (c.hasError('required')) {
      return 'Введите сумму';
    }
    if (v !== null && v <= 0) {
      return 'Сумма должна быть больше нуля';
    }
    return null;
  }

  protected selectCategory(id: number): void {
    this.form.controls.categoryId.setValue(id);
  }

  constructor() {
    toObservable(this.auth.serverDate).subscribe((sd) => {
      if (sd && this.editingId() === null) {
        const c = this.form.controls.date;
        if (c.pristine && c.value !== sd) {
          c.setValue(sd);
        }
      }
    });
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
          const me = this.auth.user();
          const defaultUser = active.find((u) => u.id === me?.id) ?? active[0];
          this.form.controls.userId.setValue(defaultUser.id);
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
    this.attemptedSubmit.set(true);
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
