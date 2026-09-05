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
  template: `
    <div class="space-y-3">
      @let resp = data();

      <div class="card flex items-center justify-between">
        <button
          type="button"
          class="icon-btn"
          (click)="shiftMonth(-1)"
          aria-label="Предыдущий месяц"
        >
          ‹
        </button>
        <div class="text-center">
          <h1 class="text-lg font-bold text-gray-900">Накопления</h1>
          <span class="text-sm font-semibold text-gray-600">{{ monthRef() | monthTitle }}</span>
        </div>
        <button type="button" class="icon-btn" (click)="shiftMonth(1)" aria-label="Следующий месяц">
          ›
        </button>
      </div>

      <div class="card p-0">
        <div class="flex items-center justify-between border-b border-gray-100 px-4 py-2.5">
          <span class="text-sm font-semibold text-gray-800">Счета накоплений</span>
          @if (!showContainerForm()) {
            <button type="button" class="btn-add" (click)="startAddContainer()">
              + добавить счёт
            </button>
          }
        </div>

        @if (showContainerForm()) {
          <div class="space-y-2.5 border-b border-gray-100 px-4 py-3">
            <div class="text-sm font-semibold text-gray-700">
              {{ editingContainerId() === null ? 'Новый счёт' : 'Изменить счёт' }}
            </div>
            <input
              type="text"
              class="input"
              [ngModel]="draftName()"
              (ngModelChange)="draftName.set($event)"
              placeholder="Название"
              maxlength="100"
              aria-label="Название счёта"
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
                <button type="button" class="btn-sm btn-secondary" (click)="cancelContainerForm()">
                  Отмена
                </button>
                <button type="button" class="btn-sm btn-primary" (click)="saveContainer()">
                  Сохранить
                </button>
              </div>
            </div>
          </div>
        }

        @if (containers().length === 0) {
          @if (!showContainerForm()) {
            <div class="px-4 py-8 text-center">
              <p class="text-sm text-gray-400">Счетов пока нет. Добавьте первый.</p>
              <button type="button" class="btn-add mt-3" (click)="startAddContainer()">
                + добавить счёт
              </button>
            </div>
          }
        } @else {
          <div class="divide-y divide-gray-50">
            @for (c of containers(); track c.id) {
              <div class="flex items-center gap-2 px-3 py-2 sm:gap-3 sm:px-4">
                <span
                  class="h-3 w-3 shrink-0 rounded-full"
                  [style.background]="c.color || '#d1d5db'"
                ></span>
                <span class="min-w-0 flex-1 truncate text-sm text-gray-800">{{ c.name }}</span>
                <button
                  type="button"
                  class="icon-btn shrink-0"
                  (click)="startEditContainer(c)"
                  title="Изменить"
                >
                  ✎
                </button>
                <button
                  type="button"
                  class="icon-btn icon-btn-danger shrink-0"
                  (click)="removeContainer(c)"
                  title="Удалить"
                >
                  🗑
                </button>
              </div>
            }
          </div>
        }
      </div>

      <div class="card p-0">
        <div class="flex items-center justify-between border-b border-gray-100 px-4 py-2.5">
          <span class="text-sm font-semibold text-gray-800"
            >Накопления за {{ monthRef() | monthTitle }}</span
          >
          @if (!showEntryForm() && canAddEntry()) {
            <button type="button" class="btn-add" (click)="startAddEntry()">+ добавить</button>
          }
        </div>

        @if (!canAddEntry()) {
          <div class="px-4 py-8 text-center text-sm text-gray-400">
            @if (containers().length === 0) {
              Сначала добавьте счёт накоплений.
            } @else {
              Сначала добавьте члена семьи в настройках.
            }
          </div>
        } @else if (loading() && !resp) {
          <div class="px-4 py-8 text-center text-sm text-gray-400">Загрузка…</div>
        } @else {
          @if (showEntryForm() && editingEntryId() === null) {
            <div class="space-y-2.5 border-b border-gray-100 px-4 py-3">
              <div class="text-sm font-semibold text-gray-700">Новая запись</div>
              <select
                class="input"
                [ngModel]="draftUserId()"
                (ngModelChange)="draftUserId.set($event)"
                aria-label="Кто"
              >
                <option [ngValue]="null">— кто —</option>
                @for (u of members(); track u.id) {
                  <option [ngValue]="u.id">{{ u.displayName }}</option>
                }
              </select>
              <select
                class="input"
                [ngModel]="draftContainerId()"
                (ngModelChange)="draftContainerId.set($event)"
                aria-label="Счёт"
              >
                <option [ngValue]="null">— выберите счёт —</option>
                @for (c of containers(); track c.id) {
                  <option [ngValue]="c.id">{{ c.name }}</option>
                }
              </select>
              <input
                type="number"
                class="input"
                [ngModel]="draftAmount()"
                (ngModelChange)="draftAmount.set($event)"
                placeholder="Сумма"
                min="0"
                step="0.01"
                inputmode="decimal"
                aria-label="Сумма"
              />
              <div class="flex gap-2">
                <button type="button" class="btn-sm btn-secondary" (click)="cancelEntryForm()">
                  Отмена
                </button>
                <button type="button" class="btn-sm btn-primary" (click)="saveEntry()">
                  Сохранить
                </button>
              </div>
            </div>
          }

          @if (resp) {
            <div
              class="flex items-center justify-between border-b border-gray-100 px-4 py-2 text-sm"
            >
              <span class="text-gray-500">Всего</span>
              <span class="font-semibold text-gray-900">{{ resp.total | money }} ₽</span>
            </div>

            @if (resp.entries.length === 0 && !showEntryForm()) {
              <div class="px-4 py-8 text-center">
                <p class="text-sm text-gray-400">Записей за месяц нет. Добавьте первую.</p>
                <button type="button" class="btn-add mt-3" (click)="startAddEntry()">
                  + добавить
                </button>
              </div>
            }

            <div class="divide-y divide-gray-50">
              @for (e of resp.entries; track e.id) {
                @if (editingEntryId() === e.id) {
                  <div class="space-y-2.5 px-4 py-3">
                    <div class="text-sm font-semibold text-gray-700">Изменить запись</div>
                    <select
                      class="input"
                      [ngModel]="draftUserId()"
                      (ngModelChange)="draftUserId.set($event)"
                      aria-label="Кто"
                    >
                      <option [ngValue]="null">— кто —</option>
                      @for (u of members(); track u.id) {
                        <option [ngValue]="u.id">{{ u.displayName }}</option>
                      }
                    </select>
                    <select
                      class="input"
                      [ngModel]="draftContainerId()"
                      (ngModelChange)="draftContainerId.set($event)"
                      aria-label="Счёт"
                    >
                      <option [ngValue]="null">— выберите счёт —</option>
                      @for (c of containers(); track c.id) {
                        <option [ngValue]="c.id">{{ c.name }}</option>
                      }
                    </select>
                    <input
                      type="number"
                      class="input"
                      [ngModel]="draftAmount()"
                      (ngModelChange)="draftAmount.set($event)"
                      placeholder="Сумма"
                      min="0"
                      step="0.01"
                      inputmode="decimal"
                      aria-label="Сумма"
                    />
                    <div class="flex gap-2">
                      <button
                        type="button"
                        class="btn-sm btn-secondary"
                        (click)="cancelEntryForm()"
                      >
                        Отмена
                      </button>
                      <button type="button" class="btn-sm btn-primary" (click)="saveEntry()">
                        Сохранить
                      </button>
                    </div>
                  </div>
                } @else {
                  <div class="flex items-center gap-2 px-3 py-2 sm:gap-3 sm:px-4">
                    <span
                      class="w-20 shrink-0 truncate text-sm font-medium text-gray-900"
                      title="Кто"
                      >{{ e.userName }}</span
                    >
                    <span class="flex min-w-0 flex-1 items-center gap-2">
                      <span
                        class="h-2.5 w-2.5 shrink-0 rounded-full"
                        [style.background]="e.color || '#d1d5db'"
                      ></span>
                      <span class="truncate text-sm text-gray-700">{{ e.containerName }}</span>
                    </span>
                    <span class="shrink-0 whitespace-nowrap text-sm font-semibold text-gray-900"
                      >{{ e.amount | money }} ₽</span
                    >
                    <button
                      type="button"
                      class="icon-btn shrink-0"
                      (click)="startEditEntry(e)"
                      title="Изменить"
                    >
                      ✎
                    </button>
                    <button
                      type="button"
                      class="icon-btn icon-btn-danger shrink-0"
                      (click)="removeEntry(e)"
                      title="Удалить"
                    >
                      🗑
                    </button>
                  </div>
                }
              }
            </div>
          }
        }
      </div>
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
      cursor: pointer;
    }
    .icon-btn:hover {
      background: #f3f4f6;
      color: #374151;
    }
    .icon-btn:active {
      background: #e5e7eb;
    }
    .icon-btn-danger:hover {
      background: #fef2f2;
      color: #dc2626;
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
  `,
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
