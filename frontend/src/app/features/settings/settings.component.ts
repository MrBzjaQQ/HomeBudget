import { CommonModule } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { FormsModule, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../core/auth.service';
import { UserDto } from '../../core/models';
import { firstApiErrorMessage } from '../../core/http-errors';
import { ToastService } from '../../core/toast.service';
import { UsersService } from '../../core/users.service';

@Component({
  selector: 'app-settings',
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  template: `
    <div class="space-y-3">
      <div class="card">
        <h1 class="text-lg font-bold text-gray-900">Настройки</h1>
      </div>

      <div class="card">
        <h2 class="section-title">Мой профиль</h2>
        <div class="mb-3 text-sm text-gray-600">
          {{ auth.user()?.displayName }}
          <span class="text-gray-400">(@{{ auth.user()?.username }})</span>
        </div>
        <form [formGroup]="passwordForm" (ngSubmit)="changePassword()" class="space-y-3">
          <div>
            <label class="field-label">Новый пароль</label>
            <input type="password" formControlName="password" class="input" autocomplete="new-password" />
            @if (passwordForm.controls.password.hasError('minlength') && passwordForm.controls.password.touched) {
              <div class="err">Минимум 6 символов</div>
            }
          </div>
          <div>
            <label class="field-label">Повторите пароль</label>
            <input type="password" formControlName="password2" class="input" autocomplete="new-password" />
          </div>
          @if (passwordMismatch()) {
            <div class="err">Пароли не совпадают</div>
          }
          <button type="submit" [disabled]="busyPassword()" class="btn-primary w-full">
            {{ busyPassword() ? 'Сохранение…' : 'Сменить пароль' }}
          </button>
        </form>
      </div>

      <div class="card">
        <div class="flex items-center justify-between">
          <h2 class="section-title mb-0">Аккаунты</h2>
          <button type="button" class="btn-add" (click)="toggleAdd()" [class]="addOpen() ? 'btn-add-active' : ''">
            {{ addOpen() ? 'Скрыть' : '+ Добавить' }}
          </button>
        </div>
        <p class="mt-1 text-xs text-gray-400">Все члены семьи. Неактивные не видны в фильтрах, но история сохраняется.</p>

        @if (addOpen()) {
          <form [formGroup]="addForm" (ngSubmit)="addUser()" class="mt-3 space-y-2.5 rounded-xl border border-gray-100 bg-gray-50 p-3">
            <div class="grid grid-cols-2 gap-2">
              <div>
                <label class="field-label">Логин</label>
                <input type="text" formControlName="username" class="input" autocomplete="off" />
              </div>
              <div>
                <label class="field-label">Имя</label>
                <input type="text" formControlName="displayName" class="input" />
              </div>
            </div>
            <div>
              <label class="field-label">Пароль</label>
              <input type="password" formControlName="password" class="input" autocomplete="new-password" />
            </div>
            <button type="submit" [disabled]="busyAdd()" class="btn-primary w-full">
              {{ busyAdd() ? 'Создание…' : 'Создать аккаунт' }}
            </button>
          </form>
        }

        <div class="mt-3 divide-y divide-gray-100">
          @for (u of users(); track u.id) {
            @if (editingId() === u.id) {
              <div class="space-y-2.5 py-3">
                <div>
                  <label class="field-label">Имя</label>
                  <input type="text" class="input" [ngModel]="draftName()" (ngModelChange)="draftName.set($event)" />
                </div>
                <div>
                  <label class="field-label">Новый пароль (необязательно)</label>
                  <input type="password" class="input" [ngModel]="draftPassword()" (ngModelChange)="draftPassword.set($event)" autocomplete="new-password" />
                </div>
                <label class="flex items-center gap-2 text-sm text-gray-600">
                  <input type="checkbox" class="accent-teal-700" [ngModel]="draftActive()" (ngModelChange)="draftActive.set($event)" />
                  активен
                </label>
                <div class="flex gap-2">
                  <span class="flex-1"></span>
                  <button type="button" class="btn-sm btn-secondary" (click)="cancelEdit()">Отмена</button>
                  <button type="button" class="btn-sm btn-primary" (click)="saveUser(u)">Сохранить</button>
                </div>
              </div>
            } @else {
              <div class="flex items-center gap-3 py-2.5">
                <span
                  class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full text-sm font-bold"
                  [class]="u.isActive ? 'bg-teal-100 text-teal-800' : 'bg-gray-100 text-gray-400'"
                >
                  {{ u.displayName.charAt(0).toUpperCase() }}
                </span>
                <span class="min-w-0 flex-1">
                  <span class="block truncate text-sm font-medium" [class.text-gray-400]="!u.isActive">
                    {{ u.displayName }}
                    @if (u.id === auth.user()?.id) {
                      <span class="text-[10px] font-semibold uppercase text-teal-600">это вы</span>
                    }
                  </span>
                  <span class="block text-xs text-gray-400">@{{ u.username }} · {{ u.isActive ? 'активен' : 'неактивен' }}</span>
                </span>
                <button type="button" class="icon-btn" (click)="startEdit(u)" title="Изменить">✎</button>
              </div>
            }
          }
        </div>
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
    .section-title {
      font-size: 13px;
      font-weight: 600;
      color: #6b7280;
      margin-bottom: 10px;
      text-transform: uppercase;
      letter-spacing: 0.03em;
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
    }
    .input:focus {
      outline: 2px solid rgb(15 118 110 / 0.3);
      border-color: #0f766e;
    }
    .err {
      margin-top: 4px;
      font-size: 12px;
      color: #dc2626;
    }
    .btn-primary {
      border-radius: 12px;
      background: #0f766e;
      color: white;
      font-weight: 600;
      font-size: 15px;
      padding: 12px;
      cursor: pointer;
    }
    .btn-primary:hover:not(:disabled) {
      background: #0d5f59;
    }
    .btn-primary:disabled {
      opacity: 0.5;
      cursor: default;
    }
    .btn-add {
      border-radius: 9999px;
      border: 1px solid #0f766e;
      background: white;
      color: #0f766e;
      font-size: 13px;
      font-weight: 600;
      padding: 6px 13px;
      cursor: pointer;
    }
    .btn-add:hover:not(.btn-add-active) {
      background: #e6f4f2;
    }
    .btn-add-active {
      background: #0f766e;
      color: white;
    }
    .btn-sm {
      border-radius: 9px;
      font-size: 13px;
      font-weight: 500;
      padding: 7px 12px;
    }
    .btn-secondary {
      border: 1px solid #e5e7eb;
      background: white;
      color: #374151;
    }
    .icon-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 32px;
      height: 32px;
      border-radius: 9999px;
      color: #9ca3af;
      cursor: pointer;
    }
    .icon-btn:hover {
      background: #f3f4f6;
      color: #374151;
    }
    .icon-btn:active {
      background: #f3f4f6;
    }
  `,
})
export class SettingsComponent {
  private readonly fb = inject(NonNullableFormBuilder);
  protected readonly auth = inject(AuthService);
  private readonly usersSvc = inject(UsersService);
  private readonly toasts = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly users = signal<UserDto[]>([]);
  protected readonly addOpen = signal(false);
  protected readonly busyPassword = signal(false);
  protected readonly busyAdd = signal(false);
  protected readonly editingId = signal<number | null>(null);
  protected readonly draftName = signal('');
  protected readonly draftPassword = signal('');
  protected readonly draftActive = signal(true);

  protected readonly passwordForm = this.fb.group({
    password: ['', [Validators.required, Validators.minLength(6)]],
    password2: ['', [Validators.required]],
  });

  protected readonly addForm = this.fb.group({
    username: ['', [Validators.required, Validators.maxLength(50)]],
    displayName: ['', [Validators.required, Validators.maxLength(100)]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  protected passwordMismatch(): boolean {
    const v = this.passwordForm.getRawValue();
    return v.password !== '' && v.password !== v.password2;
  }

  constructor() {
    this.reload();
  }

  private reload(): void {
    this.usersSvc.list().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (list) => this.users.set(list),
      error: () => this.toasts.error('Не удалось загрузить аккаунты'),
    });
  }

  protected changePassword(): void {
    const v = this.passwordForm.getRawValue();
    if (v.password !== v.password2) {
      return;
    }
    const me = this.auth.user();
    if (!me) {
      return;
    }
    this.busyPassword.set(true);
    this.usersSvc
      .update(me.id, { password: v.password })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.busyPassword.set(false);
          this.passwordForm.reset();
          this.toasts.success('Пароль изменён');
        },
        error: (err) => {
          this.busyPassword.set(false);
          firstApiErrorMessage(err, 'Не удалось сменить пароль').then((msg) => this.toasts.error(msg));
        },
      });
  }

  protected toggleAdd(): void {
    this.addOpen.update((v) => !v);
  }

  protected addUser(): void {
    if (this.addForm.invalid) {
      this.addForm.markAllAsTouched();
      return;
    }
    const v = this.addForm.getRawValue();
    this.busyAdd.set(true);
    this.usersSvc
      .create({ username: v.username, displayName: v.displayName, password: v.password })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.busyAdd.set(false);
          this.addForm.reset();
          this.addOpen.set(false);
          this.toasts.success('Аккаунт создан');
          this.reload();
        },
        error: (err) => {
          this.busyAdd.set(false);
          firstApiErrorMessage(err, 'Не удалось создать аккаунт').then((msg) => this.toasts.error(msg));
        },
      });
  }

  protected startEdit(u: UserDto): void {
    this.editingId.set(u.id);
    this.draftName.set(u.displayName);
    this.draftPassword.set('');
    this.draftActive.set(u.isActive);
  }

  protected cancelEdit(): void {
    this.editingId.set(null);
  }

  protected saveUser(u: UserDto): void {
    const name = this.draftName().trim();
    if (!name) {
      this.toasts.error('Имя не может быть пустым');
      return;
    }
    const payload: { displayName: string; password?: string; isActive: boolean } = {
      displayName: name,
      isActive: this.draftActive(),
    };
    if (this.draftPassword()) {
      payload.password = this.draftPassword();
    }
    this.usersSvc
      .update(u.id, payload)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.toasts.success('Аккаунт обновлён');
          this.cancelEdit();
          this.reload();
        },
        error: (err) =>
          firstApiErrorMessage(err, 'Не удалось обновить аккаунт').then((msg) => this.toasts.error(msg)),
      });
  }
}
