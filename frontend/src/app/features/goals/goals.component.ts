import { CommonModule } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { MoneyPipe, RuDatePipe } from '../../core/format.pipe';
import { ApiError, CreateGoalPayload, GoalDto, SavingsContainerDto } from '../../core/models';
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

  protected readonly showForm = signal(false);
  protected readonly draftName = signal('');
  protected readonly draftAmount = signal('');
  protected readonly draftContainerId = signal<number | null>(null);
  protected readonly draftDueDate = signal('');

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

  protected cancelForm(): void {
    this.showForm.set(false);
    this.resetDrafts();
  }

  protected saveGoal(): void {
    const name = this.draftName().trim();
    if (!name) {
      this.toasts.error('Введите название');
      return;
    }
    const targetAmount = Number(this.draftAmount());
    if (!Number.isFinite(targetAmount) || targetAmount <= 0) {
      this.toasts.error('Введите сумму больше нуля');
      return;
    }
    const containerId = this.draftContainerId();
    if (containerId === null) {
      this.toasts.error('Выберите счёт');
      return;
    }
    const payload: CreateGoalPayload = {
      name,
      targetAmount,
      containerId,
      dueDate: this.draftDueDate() || null,
    };
    this.goalsService
      .createGoal(payload)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.toasts.success('Цель добавлена');
          this.cancelForm();
          this.reload();
        },
        error: (err) => this.toasts.error(ApiError.from(err).message),
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
        next: (list) => this.containers.set(list),
        error: () => this.toasts.error('Не удалось загрузить данные'),
      });
  }

  private resetDrafts(): void {
    this.draftName.set('');
    this.draftAmount.set('');
    this.draftContainerId.set(null);
    this.draftDueDate.set('');
  }
}
