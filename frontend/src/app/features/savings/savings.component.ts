import { CommonModule } from '@angular/common';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/auth.service';
import { MonthTitlePipe, MoneyPipe, formatMoney } from '../../core/format.pipe';
import {
  ApiError,
  SavingsContainerDto,
  SavingsContainerPayload,
  SavingsEntryDto,
  SavingsEntryPayload,
  SavingsMonthResponse,
  UserDto,
} from '../../core/models';
import { SavingsService } from '../../core/savings.service';
import { ToastService } from '../../core/toast.service';
import { UsersService } from '../../core/users.service';

@Component({
  selector: 'app-savings',
  imports: [CommonModule, FormsModule, MonthTitlePipe, MoneyPipe],
  templateUrl: './savings.component.html',
  styleUrl: './savings.component.css',
})
export class SavingsComponent {
  private readonly savings = inject(SavingsService);
  private readonly users = inject(UsersService);
  private readonly toasts = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly auth = inject(AuthService);

  private readonly now = new Date();
  private monthTouched = false;
  protected readonly monthRef = signal(
    this.serverMonth() ?? { year: this.now.getFullYear(), month: this.now.getMonth() + 1 },
  );

  protected readonly data = signal<SavingsMonthResponse | null>(null);
  protected readonly loading = signal(true);
  protected readonly containers = signal<SavingsContainerDto[]>([]);
  protected readonly members = signal<UserDto[]>([]);

  protected readonly showContainerForm = signal(false);
  protected readonly editingContainerId = signal<number | null>(null);
  protected readonly draftName = signal('');
  protected readonly draftColor = signal('#0f766e');

  protected readonly showEntryForm = signal(false);
  protected readonly editingEntryId = signal<number | null>(null);
  protected readonly draftUserId = signal<number | null>(null);
  protected readonly draftContainerId = signal<number | null>(null);
  protected readonly draftAmount = signal('');

  protected readonly canAddEntry = computed(
    () => this.containers().length > 0 && this.members().length > 0,
  );

  constructor() {
    toObservable(this.auth.serverDate)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((sd) => {
        if (!sd || this.monthTouched) {
          return;
        }
        const s = parseServerMonth(sd);
        if (!s) {
          return;
        }
        const cur = this.monthRef();
        if (cur.year !== s.year || cur.month !== s.month) {
          this.monthRef.set(s);
          this.reload();
        }
      });
    this.loadContainers();
    this.loadMembers();
    this.reload();
  }

  private serverMonth(): { year: number; month: number } | null {
    return parseServerMonth(this.auth.serverDate());
  }

  protected shiftMonth(delta: number): void {
    this.monthTouched = true;
    this.monthRef.update((m) => {
      const d = new Date(m.year, m.month - 1 + delta, 1);
      return { year: d.getFullYear(), month: d.getMonth() + 1 };
    });
    this.cancelEntryForm();
    this.reload();
  }

  protected startAddContainer(): void {
    this.editingContainerId.set(null);
    this.showContainerForm.set(true);
    this.draftName.set('');
    this.draftColor.set('#0f766e');
  }

  protected startEditContainer(c: SavingsContainerDto): void {
    this.showContainerForm.set(true);
    this.editingContainerId.set(c.id);
    this.draftName.set(c.name);
    this.draftColor.set(c.color || '#0f766e');
  }

  protected cancelContainerForm(): void {
    this.showContainerForm.set(false);
    this.editingContainerId.set(null);
  }

  protected saveContainer(): void {
    const name = this.draftName().trim();
    if (!name) {
      this.toasts.error('Введите название');
      return;
    }
    const payload: SavingsContainerPayload = { name, color: this.draftColor() };
    const id = this.editingContainerId();
    const request =
      id === null
        ? this.savings.createContainer(payload)
        : this.savings.updateContainer(id, payload);
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.toasts.success(id === null ? 'Счёт добавлен' : 'Счёт обновлён');
        this.cancelContainerForm();
        this.loadContainers();
      },
      error: (err) => this.toasts.error(ApiError.from(err).message),
    });
  }

  protected removeContainer(c: SavingsContainerDto): void {
    if (!window.confirm(`Удалить счёт «${c.name}»?`)) {
      return;
    }
    this.savings
      .deleteContainer(c.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.toasts.success('Счёт удалён');
          if (this.editingContainerId() === c.id) {
            this.cancelContainerForm();
          }
          this.loadContainers();
        },
        error: (err) => this.toasts.error(ApiError.from(err).message),
      });
  }

  protected startAddEntry(): void {
    if (!this.canAddEntry()) {
      return;
    }
    this.showEntryForm.set(true);
    this.editingEntryId.set(null);
    this.resetEntryDrafts();
  }

  protected startEditEntry(e: SavingsEntryDto): void {
    this.showEntryForm.set(true);
    this.editingEntryId.set(e.id);
    this.draftUserId.set(e.userId);
    this.draftContainerId.set(e.containerId);
    this.draftAmount.set(String(e.amount));
  }

  protected cancelEntryForm(): void {
    this.showEntryForm.set(false);
    this.editingEntryId.set(null);
    this.resetEntryDrafts();
  }

  private resetEntryDrafts(): void {
    this.draftUserId.set(null);
    this.draftContainerId.set(null);
    this.draftAmount.set('');
  }

  protected saveEntry(): void {
    const userId = this.draftUserId();
    const containerId = this.draftContainerId();
    const amount = Number(this.draftAmount());
    if (userId === null) {
      this.toasts.error('Выберите, за кого вносится запись');
      return;
    }
    if (containerId === null) {
      this.toasts.error('Выберите счёт');
      return;
    }
    if (!Number.isFinite(amount) || amount <= 0) {
      this.toasts.error('Введите сумму больше нуля');
      return;
    }
    const payload: SavingsEntryPayload = {
      month: this.monthKey(),
      userId,
      containerId,
      amount,
    };
    const id = this.editingEntryId();
    const request =
      id === null ? this.savings.createEntry(payload) : this.savings.updateEntry(id, payload);
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.toasts.success(id === null ? 'Запись добавлена' : 'Запись обновлена');
        this.cancelEntryForm();
        this.reload();
      },
      error: (err) => this.toasts.error(ApiError.from(err).message),
    });
  }

  protected removeEntry(e: SavingsEntryDto): void {
    if (
      !window.confirm(
        `Удалить запись «${e.userName}, ${e.containerName}, ${formatMoney(e.amount)} ₽»?`,
      )
    ) {
      return;
    }
    this.savings
      .deleteEntry(e.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.toasts.success('Запись удалена');
          if (this.editingEntryId() === e.id) {
            this.cancelEntryForm();
          }
          this.reload();
        },
        error: (err) => this.toasts.error(ApiError.from(err).message),
      });
  }

  private reload(): void {
    this.loading.set(true);
    this.savings
      .getMonth(this.monthKey())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (resp) => {
          this.data.set(resp);
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

  private loadMembers(): void {
    this.users
      .list()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (list) => this.members.set(list.filter((u) => u.isActive && u.username !== 'admin')),
        error: () => this.toasts.error('Не удалось загрузить данные'),
      });
  }

  private monthKey(): string {
    const { year, month } = this.monthRef();
    return `${year}-${String(month).padStart(2, '0')}`;
  }
}

function parseServerMonth(sd: string | null): { year: number; month: number } | null {
  if (!sd) {
    return null;
  }
  const [y, m] = sd.split('-').map(Number);
  return y && m >= 1 && m <= 12 ? { year: y, month: m } : null;
}
