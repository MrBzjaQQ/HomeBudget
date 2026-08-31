import { CommonModule } from '@angular/common';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { CategoriesService, CategoryPayload } from '../../core/categories.service';
import { ApiError, CategoryDto, TxnKind } from '../../core/models';
import { ToastService } from '../../core/toast.service';

interface FlatCategory extends CategoryDto {
  level: number;
}

interface Section {
  kind: TxnKind;
  items: FlatCategory[];
}

@Component({
  selector: 'app-categories',
  imports: [CommonModule, FormsModule],
  template: `
    <div class="space-y-3">
      <div class="card">
        <h1 class="text-lg font-bold text-gray-900">Категории</h1>
      </div>

      @if (creating()) {
        <div class="card space-y-2.5">
          @let cp = creatingParent();
          <div class="text-sm font-semibold text-gray-700">
            Новая {{ cp ? 'подкатегория' : 'группа' }}
            @if (cp) { в «{{ cp.name }}» } @else { ({{ creatingKind() === 'INCOME' ? 'Доходы' : 'Расходы' }}) }
          </div>
          <input
            type="text"
            class="input"
            [ngModel]="draftName()"
            (ngModelChange)="draftName.set($event)"
            placeholder="Название"
            maxlength="100"
          />
          <div class="flex items-center gap-3">
            <input
              type="color"
              class="h-8 w-10 cursor-pointer rounded border border-gray-200"
              [ngModel]="draftColor()"
              (ngModelChange)="draftColor.set($event)"
              aria-label="Цвет"
            />
            <span class="text-xs text-gray-400">цвет</span>
            <div class="ml-auto flex gap-2">
              <button type="button" class="btn-sm btn-secondary" (click)="cancelCreate()">Отмена</button>
              <button type="button" class="btn-sm btn-primary" (click)="saveCreate()">Создать</button>
            </div>
          </div>
        </div>
      }

      @for (section of sections(); track section.kind) {
        <div class="card p-0">
          <div class="flex items-center justify-between border-b border-gray-100 px-4 py-2.5">
            <span
              class="text-sm font-semibold"
              [class.text-income]="section.kind === 'INCOME'"
              [class.text-expense]="section.kind === 'EXPENSE'"
            >
              {{ section.kind === 'INCOME' ? 'Доходы' : 'Расходы' }}
            </span>
            <button
              type="button"
              class="text-xs font-medium text-teal-700"
              (click)="startCreate(section.kind)"
            >
              + группа
            </button>
          </div>

          <div class="divide-y divide-gray-50">
            @for (cat of section.items; track cat.id) {
              @if (editingId() === cat.id) {
                <div class="space-y-2.5 px-4 py-3">
                  <input
                    type="text"
                    class="input"
                    [ngModel]="draftName()"
                    (ngModelChange)="draftName.set($event)"
                    placeholder="Название"
                    maxlength="100"
                  />
                  <div class="flex items-center gap-3">
                    <input
                      type="color"
                      class="h-8 w-10 cursor-pointer rounded border border-gray-200"
                      [ngModel]="draftColor()"
                      (ngModelChange)="draftColor.set($event)"
                      aria-label="Цвет"
                    />
                    <span class="text-xs text-gray-400">цвет</span>
                    <div class="ml-auto flex items-center gap-1">
                      <button type="button" class="icon-btn" (click)="move(-1)" title="Выше">↑</button>
                      <button type="button" class="icon-btn" (click)="move(1)" title="Ниже">↓</button>
                    </div>
                  </div>
                  <label class="flex items-center gap-2 text-sm text-gray-600">
                    <input
                      type="checkbox"
                      class="accent-teal-700"
                      [ngModel]="draftArchived()"
                      (ngModelChange)="draftArchived.set($event)"
                    />
                    в архиве
                  </label>
                  <div class="flex gap-2">
                    <button type="button" class="btn-sm btn-danger" (click)="deleteCat(cat)">Удалить</button>
                    <span class="flex-1"></span>
                    <button type="button" class="btn-sm btn-secondary" (click)="cancelEdit()">Отмена</button>
                    <button type="button" class="btn-sm btn-primary" (click)="saveEdit(cat)">Сохранить</button>
                  </div>
                </div>
              } @else {
                <div class="flex items-center gap-2 px-3 py-2 sm:gap-3 sm:px-4">
                  <span class="h-3 w-3 shrink-0 rounded-full" [style.background]="cat.color || '#d1d5db'"></span>
                  <span
                    class="min-w-0 flex-1 truncate text-sm"
                    [class.text-gray-400]="cat.isArchived"
                    [class.font-medium]="cat.level === 0"
                  >
                    <span class="text-gray-300" *ngIf="cat.level > 0">— </span>{{ cat.name }}
                    <span class="ml-1 text-[10px] uppercase text-gray-300" *ngIf="cat.isArchived">архив</span>
                  </span>
                  <button type="button" class="icon-btn" (click)="startCreateChild(cat)" title="Подкатегория">+</button>
                  <button type="button" class="icon-btn" (click)="startEdit(cat)" title="Изменить">✎</button>
                  <button
                    type="button"
                    class="icon-btn"
                    (click)="toggleArchive(cat)"
                    [title]="cat.isArchived ? 'Восстановить' : 'В архив'"
                  >
                    {{ cat.isArchived ? '↺' : '🗑' }}
                  </button>
                </div>
              }
            }
          </div>
        </div>
      }
    </div>
  `,
  styles: `
    .card {
      background: white;
      border: 1px solid #f3f4f6;
      border-radius: 14px;
      box-shadow: 0 1px 2px rgb(0 0 0 / 0.04);
      padding: 14px;
    }
    .btn-add {
      border-radius: 9999px;
      background: #0f766e;
      color: white;
      font-size: 13px;
      font-weight: 600;
      padding: 7px 14px;
    }
    .icon-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 30px;
      height: 30px;
      border-radius: 9999px;
      color: #9ca3af;
      font-size: 14px;
    }
    .icon-btn:active {
      background: #f3f4f6;
    }
    .input {
      width: 100%;
      border: 1px solid #e5e7eb;
      border-radius: 10px;
      padding: 9px 10px;
      font-size: 14px;
    }
    .input:focus {
      outline: 2px solid rgb(15 118 110 / 0.3);
      border-color: #0f766e;
    }
    .btn-sm {
      border-radius: 9px;
      font-size: 13px;
      font-weight: 500;
      padding: 7px 12px;
    }
    .btn-primary {
      background: #0f766e;
      color: white;
    }
    .btn-secondary {
      border: 1px solid #e5e7eb;
      background: white;
      color: #374151;
    }
    .btn-danger {
      border: 1px solid #fecaca;
      background: #fef2f2;
      color: #b91c1c;
    }
  `,
})
export class CategoriesComponent {
  private readonly cats = inject(CategoriesService);
  private readonly toasts = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly tree = signal<CategoryDto[]>([]);
  protected readonly creating = signal(false);
  protected readonly creatingKind = signal<TxnKind>('EXPENSE');
  protected readonly creatingParentId = signal<number | null>(null);
  protected readonly editingId = signal<number | null>(null);
  protected readonly draftName = signal('');
  protected readonly draftColor = signal('#0f766e');
  protected readonly draftArchived = signal(false);

  protected readonly sections = computed<Section[]>(() => {
    const flat = this.flatten(this.tree());
    return [
      { kind: 'INCOME', items: flat.filter((c) => c.kind === 'INCOME') },
      { kind: 'EXPENSE', items: flat.filter((c) => c.kind === 'EXPENSE') },
    ];
  });

  protected readonly creatingParent = computed(() => {
    const id = this.creatingParentId();
    if (id === null) {
      return null;
    }
    return this.flatten(this.tree()).find((c) => c.id === id) ?? null;
  });

  constructor() {
    this.reload();
  }

  private reload(): void {
    this.cats.tree(undefined, true).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (list) => this.tree.set(list),
      error: () => this.toasts.error('Не удалось загрузить категории'),
    });
  }

  private flatten(nodes: CategoryDto[]): FlatCategory[] {
    const out: FlatCategory[] = [];
    const walk = (list: CategoryDto[], level: number): void => {
      for (const n of list) {
        out.push({ ...n, level });
        if (n.children) {
          walk(n.children, level + 1);
        }
      }
    };
    walk(nodes, 0);
    return out;
  }

  private nextSortOrder(kind: TxnKind, parentId: number | null): number {
    const siblings = this.flatten(this.tree()).filter(
      (c) => c.kind === kind && c.parentId === parentId && !c.isArchived,
    );
    return siblings.reduce((max, c) => Math.max(max, c.sortOrder), 0) + 1;
  }

  protected startCreate(kind: TxnKind): void {
    this.cancelEdit();
    this.creating.set(true);
    this.creatingKind.set(kind);
    this.creatingParentId.set(null);
    this.draftName.set('');
    this.draftColor.set('#0f766e');
    this.draftArchived.set(false);
  }

  protected startCreateChild(cat: FlatCategory): void {
    if (cat.isArchived) {
      return;
    }
    this.cancelEdit();
    this.creating.set(true);
    this.creatingKind.set(cat.kind);
    this.creatingParentId.set(cat.id);
    this.draftName.set('');
    this.draftColor.set(cat.color || '#0f766e');
    this.draftArchived.set(false);
  }

  protected cancelCreate(): void {
    this.creating.set(false);
    this.creatingParentId.set(null);
  }

  protected startEdit(cat: FlatCategory): void {
    this.cancelCreate();
    this.editingId.set(cat.id);
    this.draftName.set(cat.name);
    this.draftColor.set(cat.color || '#0f766e');
    this.draftArchived.set(cat.isArchived);
  }

  protected cancelEdit(): void {
    this.editingId.set(null);
  }

  protected saveCreate(): void {
    const name = this.draftName().trim();
    if (!name) {
      this.toasts.error('Введите название');
      return;
    }
    const parent = this.creatingParent();
    const kind = parent ? parent.kind : this.creatingKind();
    const payload: CategoryPayload = {
      name,
      kind,
      parentId: this.creatingParentId(),
      color: this.draftColor(),
      sortOrder: this.nextSortOrder(kind, this.creatingParentId()),
    };
    this.cats.create(payload).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.toasts.success('Категория создана');
        this.cancelCreate();
        this.reload();
      },
      error: (err) => this.toasts.error(ApiError.from(err).message),
    });
  }

  protected saveEdit(cat: FlatCategory): void {
    const name = this.draftName().trim();
    if (!name) {
      this.toasts.error('Введите название');
      return;
    }
    const payload: CategoryPayload = {
      name,
      kind: cat.kind,
      parentId: cat.parentId,
      color: this.draftColor(),
      isArchived: this.draftArchived(),
    };
    this.cats.update(cat.id, payload).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.toasts.success('Категория обновлена');
        this.cancelEdit();
        this.reload();
      },
      error: (err) => this.toasts.error(ApiError.from(err).message),
    });
  }

  protected deleteCat(cat: FlatCategory): void {
    if (!window.confirm(`Удалить категорию «${cat.name}»?`)) {
      return;
    }
    this.cats.delete(cat.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.toasts.success('Категория удалена');
        this.cancelEdit();
        this.reload();
      },
      error: (err) => this.toasts.error(ApiError.from(err).message),
    });
  }

  protected toggleArchive(cat: FlatCategory): void {
    const payload: CategoryPayload = {
      name: cat.name,
      kind: cat.kind,
      parentId: cat.parentId,
      color: cat.color,
      isArchived: !cat.isArchived,
    };
    this.cats.update(cat.id, payload).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.toasts.success(cat.isArchived ? 'Категория восстановлена' : 'Категория в архиве');
        this.reload();
      },
      error: (err) => this.toasts.error(ApiError.from(err).message),
    });
  }

  protected move(dir: -1 | 1): void {
    const id = this.editingId();
    if (id === null) {
      return;
    }
    const current = this.flatten(this.tree()).find((c) => c.id === id);
    if (!current) {
      return;
    }
    const siblings = this.flatten(this.tree()).filter(
      (c) => c.parentId === current.parentId && c.kind === current.kind && !c.isArchived,
    );
    const idx = siblings.findIndex((c) => c.id === id);
    const target = idx + dir;
    if (idx < 0 || target < 0 || target >= siblings.length) {
      return;
    }
    const reordered = [...siblings];
    [reordered[idx], reordered[target]] = [reordered[target], reordered[idx]];
    for (let i = 0; i < reordered.length; i++) {
      const c = reordered[i];
      if (c.sortOrder !== i) {
        const payload: CategoryPayload = {
          name: c.name,
          kind: c.kind,
          parentId: c.parentId,
          color: c.color,
          isArchived: c.isArchived,
          sortOrder: i,
        };
        this.cats.update(c.id, payload).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
          error: (err) => this.toasts.error(ApiError.from(err).message),
        });
      }
    }
    this.reload();
  }
}
