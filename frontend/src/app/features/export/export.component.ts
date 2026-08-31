import { CommonModule } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ExportService } from '../../core/export.service';
import { ApiError } from '../../core/models';
import { ToastService } from '../../core/toast.service';
import { UsersService } from '../../core/users.service';
import { UserDto } from '../../core/models';

type Scope = 'month' | 'year';

@Component({
  selector: 'app-export',
  imports: [CommonModule, FormsModule],
  template: `
    <div class="space-y-3">
      <div class="card">
        <h1 class="text-lg font-bold text-gray-900">Экспорт в Excel</h1>
        <p class="mt-1 text-sm text-gray-500">
          Файл формируется на сервере. Годовой файл — 13 листов: 12 месяцев + «Сводка».
        </p>

        <div class="mt-4 grid grid-cols-2 gap-1 rounded-xl bg-gray-100 p-1">
          <button
            type="button"
            class="rounded-lg py-2.5 text-sm font-semibold transition"
            [class]="scope() === 'month' ? 'bg-white text-teal-800 shadow' : 'text-gray-500'"
            (click)="scope.set('month')"
          >
            Месяц
          </button>
          <button
            type="button"
            class="rounded-lg py-2.5 text-sm font-semibold transition"
            [class]="scope() === 'year' ? 'bg-white text-teal-800 shadow' : 'text-gray-500'"
            (click)="scope.set('year')"
          >
            Год
          </button>
        </div>

        <div class="mt-4 space-y-3">
          @if (scope() === 'month') {
            <div class="flex items-center gap-3">
              <label class="field-label flex-1">
                Месяц
                <select class="input" [ngModel]="month()" (ngModelChange)="month.set($event)">
                  @for (m of months; track m) {
                    <option [ngValue]="m">{{ monthName(m) }}</option>
                  }
                </select>
              </label>
              <label class="field-label w-28">
                Год
                <input type="number" class="input" [ngModel]="year()" (ngModelChange)="year.set($event)" [min]="2000" [max]="2100" />
              </label>
            </div>
          } @else {
            <label class="field-label block">
              Год
              <input type="number" class="input" [ngModel]="year()" (ngModelChange)="year.set($event)" [min]="2000" [max]="2100" />
            </label>
          }

          <label class="field-label block">
            Кто
            <select class="input" [ngModel]="who()" (ngModelChange)="onWhoChange($event)">
              <option [ngValue]="null">Вся семья</option>
              @for (u of users(); track u.id) {
                <option [ngValue]="u.id" [disabled]="!u.isActive">{{ u.displayName }}@if (!u.isActive) { (неактивен) }</option>
              }
            </select>
          </label>
        </div>

        <button
          type="button"
          class="btn-download mt-5 w-full"
          [disabled]="busy()"
          (click)="download()"
        >
          @if (busy()) {
            <span class="spinner"></span> Формируем файл…
          } @else {
            ⬇ Скачать Excel
          }
        </button>

        @if (error()) {
          <div class="mt-3 rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{{ error() }}</div>
        }
      </div>

      <div class="card text-sm text-gray-500">
        <div class="font-semibold text-gray-700">Что внутри</div>
        <ul class="mt-2 list-disc space-y-1 pl-5">
          <li>Месяц — один лист с операциями, итогами и расходами по категориям.</li>
          <li>Год — 12 листов (пустые месяцы — со нулями) + лист «Сводка» с накопительным остатком.</li>
          <li>Шифр файла: Бюджет_ГГГГ-ММ.xlsx / Бюджет_ГГГГ.xlsx</li>
        </ul>
      </div>
    </div>
  `,
  styles: `
    .card {
      background: white;
      border: 1px solid #f3f4f6;
      border-radius: 14px;
      padding: 16px;
      box-shadow: 0 1px 2px rgb(0 0 0 / 0.04);
    }
    .field-label {
      display: block;
      font-size: 12px;
      font-weight: 500;
      color: #6b7280;
    }
    .field-label .input {
      margin-top: 6px;
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
    .btn-download {
      border-radius: 12px;
      background: #0f766e;
      color: white;
      font-weight: 600;
      font-size: 15px;
      padding: 14px;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 8px;
    }
    .btn-download:disabled {
      opacity: 0.6;
    }
    .spinner {
      width: 16px;
      height: 16px;
      border: 2px solid rgb(255 255 255 / 0.4);
      border-top-color: white;
      border-radius: 9999px;
      animation: spin 0.8s linear infinite;
    }
    @keyframes spin {
      to {
        transform: rotate(360deg);
      }
    }
  `,
})
export class ExportComponent {
  private readonly exportSvc = inject(ExportService);
  private readonly usersSvc = inject(UsersService);
  private readonly toasts = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly now = new Date();
  protected readonly scope = signal<Scope>('month');
  protected readonly month = signal(this.now.getMonth() + 1);
  protected readonly year = signal(this.now.getFullYear());
  protected readonly who = signal<number | null>(null);
  protected readonly users = signal<UserDto[]>([]);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly months = Array.from({ length: 12 }, (_, i) => i + 1);

  constructor() {
    this.usersSvc.list().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (list) => this.users.set(list),
    });
  }

  protected onWhoChange(v: unknown): void {
    this.who.set(typeof v === 'number' ? v : null);
  }

  protected monthName(m: number): string {
    const label = new Date(this.year(), m - 1, 1).toLocaleDateString('ru-RU', {
      month: 'long',
      year: 'numeric',
    });
    return label.charAt(0).toUpperCase() + label.slice(1);
  }

  protected download(): void {
    this.busy.set(true);
    this.error.set(null);
    const op = this.scope() === 'month'
      ? this.exportSvc.month(this.year(), this.month(), this.who() ?? undefined)
      : this.exportSvc.year(this.year(), this.who() ?? undefined);
    op.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.busy.set(false);
        this.toasts.success('Файл сохранён');
      },
      error: (err) => {
        this.busy.set(false);
        this.error.set(ApiError.from(err).message);
      },
    });
  }
}
