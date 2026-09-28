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
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.css',
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
    const username = v.username.trim();
    const displayName = v.displayName.trim();
    if (username === '' || displayName === '') {
      this.addForm.markAllAsTouched();
      return;
    }
    this.busyAdd.set(true);
    this.usersSvc
      .create({ username, displayName, password: v.password })
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
