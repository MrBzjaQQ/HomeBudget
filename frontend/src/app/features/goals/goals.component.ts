import { CommonModule } from '@angular/common';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { MoneyPipe, RuDatePipe } from '../../core/format.pipe';
import {
  ApiError,
  CreateGoalPayload,
  GoalDto,
  SavingsContainerDto,
  UpdateGoalPayload,
} from '../../core/models';
import { GoalsService } from '../../core/goals.service';
import { SavingsService } from '../../core/savings.service';
import { ToastService } from '../../core/toast.service';

@Component({
  selector: 'app-goals',
  imports: [CommonModule, FormsModule, MoneyPipe, RuDatePipe],
  templateUrl: './goals.component.html',
  styleUrl: './goals.component.css',
})
export class GoalsComponent {
  private readonly goalsService = inject(GoalsService);
  private readonly savings = inject(SavingsService);
  private readonly toasts = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly goals = signal<GoalDto[]>([]);
  protected readonly loading = signal(true);
  protected readonly containers = signal<SavingsContainerDto[]>([]);
  protected readonly containersLoadFailed = signal(false);

  protected readonly showForm = signal(false);
  protected readonly editingGoalId = signal<number | null>(null);
  protected readonly editingGoal = computed(
    () => this.goals().find((goal) => goal.id === this.editingGoalId()) ?? null,
  );
  protected readonly saving = signal(false);
  protected readonly draftName = signal('');
  protected readonly draftAmount = signal('');
  protected readonly draftContainerId = signal<number | null>(null);
  protected readonly draftDueDate = signal('');
  protected readonly formError = signal<string | null>(null);
  private readonly maxGoalAmount = 999_999_999_999.99;

  constructor() {
    this.loadContainers();
    this.reload();
  }

  protected startAdd(): void {
    if (this.containers().length === 0) {
      return;
    }
    this.resetDrafts();
    this.showForm.set(true);
  }

  protected startEdit(goal: GoalDto): void {
    this.formError.set(null);
    this.editingGoalId.set(goal.id);
    this.draftName.set(goal.name);
    this.draftAmount.set(String(goal.targetAmount));
    this.draftContainerId.set(goal.containerId);
    this.draftDueDate.set(goal.dueDate ?? '');
    this.showForm.set(true);
  }

  protected cancelForm(): void {
    if (this.saving()) {
      return;
    }
    this.showForm.set(false);
    this.editingGoalId.set(null);
    this.resetDrafts();
  }

  protected saveGoal(): void {
    if (this.saving()) {
      return;
    }
    this.formError.set(null);
    const name = this.draftName().trim();
    if (!name) {
      this.setFormError('Введите название цели.');
      return;
    }
    if (name.length > 100) {
      this.setFormError('Название цели не должно превышать 100 символов.');
      return;
    }
    const targetAmount = Number(this.draftAmount());
    if (!Number.isFinite(targetAmount) || targetAmount <= 0) {
      this.setFormError('Введите целевую сумму больше нуля.');
      return;
    }
    if (targetAmount > this.maxGoalAmount) {
      this.setFormError('Целевая сумма не должна превышать 999 999 999 999,99 ₽.');
      return;
    }
    const containerId = this.draftContainerId();
    const currentGoal = this.editingGoal();
    const isAvailableContainer = this.containers().some((container) => container.id === containerId);
    const isCurrentContainer = currentGoal?.containerId === containerId;
    if (containerId === null || (!isAvailableContainer && !isCurrentContainer)) {
      this.setFormError('Выберите счёт.');
      return;
    }
    const dueDate = this.draftDueDate();
    if (dueDate && !/^\d{4}-\d{2}-\d{2}$/.test(dueDate)) {
      this.setFormError('Укажите корректную дату достижения.');
      return;
    }
    const payload: UpdateGoalPayload = {
      name,
      targetAmount,
      containerId,
      dueDate: dueDate || null,
    };
    const editingGoalId = this.editingGoalId();
    if (editingGoalId === null) {
      this.createGoal(payload);
      return;
    }
    if (this.containersLoadFailed()) {
      this.toasts.error('Не удалось загрузить счета. Повторите попытку позже');
      return;
    }
    this.saving.set(true);
    this.goalsService
      .updateGoal(editingGoalId, payload)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.toasts.success('Цель обновлена');
          this.cancelForm();
          this.reload();
        },
        error: (err) => {
          this.saving.set(false);
          const message = ApiError.from(err).message;
          this.formError.set(message);
          this.toasts.error(message);
        },
      });
  }

  protected removeGoal(g: GoalDto): void {
    if (!window.confirm(`Удалить цель «${g.name}»?`)) {
      return;
    }
    this.goalsService
      .deleteGoal(g.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.toasts.success('Цель удалена');
          this.reload();
        },
        error: (err) => this.toasts.error(ApiError.from(err).message),
      });
  }

  protected progressPercent(g: GoalDto): number {
    if (g.achieved) {
      return 100;
    }
    if (g.targetAmount <= 0) {
      return 0;
    }
    return Math.min(100, (g.saved / g.targetAmount) * 100);
  }

  protected hasContainer(id: number): boolean {
    return this.containers().some((container) => container.id === id);
  }

  private reload(): void {
    this.loading.set(true);
    this.goalsService
      .listGoals()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (list) => {
          this.goals.set(list);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.toasts.error('Не удалось загрузить данные');
        },
      });
  }

  private loadContainers(): void {
    this.savings
      .listContainers()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (list) => {
          this.containers.set(list);
          this.containersLoadFailed.set(false);
        },
        error: () => {
          this.containersLoadFailed.set(true);
          this.toasts.error('Не удалось загрузить данные');
        },
      });
  }

  private createGoal(payload: CreateGoalPayload): void {
    this.saving.set(true);
    this.goalsService
      .createGoal(payload)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.toasts.success('Цель добавлена');
          this.cancelForm();
          this.reload();
        },
        error: (err) => {
          this.saving.set(false);
          const message = ApiError.from(err).message;
          this.formError.set(message);
          this.toasts.error(message);
        },
      });
  }

  protected clearFormError(): void {
    this.formError.set(null);
  }

  private setFormError(message: string): void {
    this.formError.set(message);
    this.toasts.error(message);
  }

  private resetDrafts(): void {
    this.formError.set(null);
    this.draftName.set('');
    this.draftAmount.set('');
    this.draftContainerId.set(null);
    this.draftDueDate.set('');
  }
}
