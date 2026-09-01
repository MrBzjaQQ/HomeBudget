import { TestBed } from '@angular/core/testing';
import { FormGroup } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import { UserDto } from '../../core/models';
import { ToastService } from '../../core/toast.service';
import { CreateUserPayload, UpdateUserPayload, UsersService } from '../../core/users.service';
import { SettingsComponent } from './settings.component';

const admin: UserDto = { id: 1, username: 'admin', displayName: 'Админ', isActive: true, createdAt: '2026-01-01T00:00:00' };
const wife: UserDto = { id: 2, username: 'anna', displayName: 'Анна', isActive: true, createdAt: '2026-01-01T00:00:00' };

type SettingsCtrl = {
  addForm: FormGroup;
  addUser(): void;
  startEdit(u: UserDto): void;
  draftName: { set(v: string): void };
  saveUser(u: UserDto): void;
};

describe('SettingsComponent', () => {
  let createFail: boolean;
  let updateFail: boolean;

  beforeEach(async () => {
    createFail = false;
    updateFail = false;
    await TestBed.configureTestingModule({
      imports: [SettingsComponent],
      providers: [
        { provide: AuthService, useValue: { user: () => admin } },
        {
          provide: UsersService,
          useValue: {
            list: () => of([admin, wife]),
            create: (p: CreateUserPayload) =>
              createFail
                ? throwError(() => ({
                    status: 400,
                    error: {
                      code: 'ValidationFailed',
                      message: 'Некорректные данные',
                      errors: { username: 'Такой логин уже занят' },
                    },
                  }))
                : of({ ...admin, id: 3, username: p.username, displayName: p.displayName }),
            update: (_id: number, _p: UpdateUserPayload) =>
              updateFail
                ? throwError(() => ({
                    status: 400,
                    error: {
                      code: 'ValidationFailed',
                      message: 'Некорректные данные',
                      errors: { password: 'Пароль слишком короткий' },
                    },
                  }))
                : of(wife),
          },
        },
        ToastService,
      ],
    });
  });

  function comp() {
    const fixture = TestBed.createComponent(SettingsComponent);
    fixture.detectChanges();
    return { fixture, component: fixture.componentInstance as unknown as SettingsCtrl };
  }

  it('shows the first field error from the 400 envelope when the login is taken', async () => {
    createFail = true;
    const { component: c } = comp();
    c.addForm.patchValue({ username: 'admin', displayName: 'Копия', password: 'secret1' });
    c.addUser();
    await new Promise((r) => setTimeout(r, 0));

    const list = TestBed.inject(ToastService).toasts();
    expect(list.some((t) => t.kind === 'error' && t.message === 'Такой логин уже занят')).toBe(true);
  });

  it('shows the first field error from the 400 envelope on update', async () => {
    updateFail = true;
    const { component: c } = comp();
    c.startEdit(wife);
    c.draftName.set('Анна 2');
    c.saveUser(wife);
    await new Promise((r) => setTimeout(r, 0));

    const list = TestBed.inject(ToastService).toasts();
    expect(list.some((t) => t.kind === 'error' && t.message === 'Пароль слишком короткий')).toBe(true);
  });
});
