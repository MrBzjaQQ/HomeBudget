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
  templateUrl: './categories.component.html',
  styleUrl: './categories.component.css',
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
