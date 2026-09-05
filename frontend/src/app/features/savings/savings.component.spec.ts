import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Observable, of } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import {
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
import { SavingsComponent } from './savings.component';

const SERVER_DATE = '2026-08-15';

const container1: SavingsContainerDto = {
  id: 1,
  name: 'Вклад',
  color: '#0f766e',
  sortOrder: 1,
  createdAt: '2026-08-01T10:00:00Z',
};

const container2: SavingsContainerDto = {
  id: 2,
  name: 'Наличка',
  color: '#f59e0b',
  sortOrder: 2,
  createdAt: '2026-08-01T10:00:00Z',
};

const entry1: SavingsEntryDto = {
  id: 1,
  month: '2026-08',
  userId: 2,
  userName: 'Ваня',
  containerId: 1,
  containerName: 'Вклад',
  color: '#0f766e',
  amount: 100000,
  note: null,
  createdAt: '2026-08-01T10:00:00Z',
  updatedAt: '2026-08-01T10:00:00Z',
};

const entry2: SavingsEntryDto = {
  id: 2,
  month: '2026-08',
  userId: 3,
  userName: 'Катя',
  containerId: 2,
  containerName: 'Наличка',
  color: '#f59e0b',
  amount: 50000,
  note: null,
  createdAt: '2026-08-01T10:00:00Z',
  updatedAt: '2026-08-01T10:00:00Z',
};

const monthResponse: SavingsMonthResponse = {
  month: '2026-08',
  entries: [entry1, entry2],
  total: 150000,
};

const emptyResponse: SavingsMonthResponse = {
  month: '2026-08',
  entries: [],
  total: 0,
};

const admin: UserDto = {
  id: 1,
  username: 'admin',
  displayName: 'Админ',
  isActive: true,
  createdAt: '2026-01-01T10:00:00Z',
};

const vanya: UserDto = {
  id: 2,
  username: 'vanya',
  displayName: 'Ваня',
  isActive: true,
  createdAt: '2026-01-01T10:00:00Z',
};

const katya: UserDto = {
  id: 3,
  username: 'katya',
  displayName: 'Катя',
  isActive: true,
  createdAt: '2026-01-01T10:00:00Z',
};

const inactive: UserDto = {
  id: 4,
  username: 'old',
  displayName: 'Бывший',
  isActive: false,
  createdAt: '2026-01-01T10:00:00Z',
};

interface SavingsComponentApi {
  shiftMonth(delta: number): void;
  startAddContainer(): void;
  startAddEntry(): void;
  startEditEntry(entry: SavingsEntryDto): void;
  saveContainer(): void;
  saveEntry(): void;
  removeEntry(entry: SavingsEntryDto): void;
  draftName: { set(v: string): void };
  draftColor: { set(v: string): void };
  draftUserId: { set(v: number | null): void };
  draftContainerId: { set(v: number | null): void };
  draftAmount: { set(v: string): void };
}

describe('SavingsComponent', () => {
  let getMonthSpy: { calls: string[]; fn: (m: string) => Observable<SavingsMonthResponse> };
  let createContainerSpy: { calls: SavingsContainerPayload[] };
  let createEntrySpy: { calls: SavingsEntryPayload[] };
  let updateEntrySpy: { calls: Array<{ id: number; payload: SavingsEntryPayload }> };
  let deleteEntrySpy: { calls: number[] };
  let serverDateSignal: { set(v: string | null): void };

  beforeEach(async () => {
    getMonthSpy = { calls: [], fn: () => of(monthResponse) };
    createContainerSpy = { calls: [] };
    createEntrySpy = { calls: [] };
    updateEntrySpy = { calls: [] };
    deleteEntrySpy = { calls: [] };
    serverDateSignal = signal<string | null>(SERVER_DATE);
    await TestBed.configureTestingModule({
      imports: [SavingsComponent],
      providers: [
        {
          provide: AuthService,
          useValue: { serverDate: serverDateSignal },
        },
        {
          provide: SavingsService,
          useValue: {
            listContainers: () => of([container1, container2]),
            createContainer: (p: SavingsContainerPayload) => {
              createContainerSpy.calls.push(p);
              return of({ ...container1, id: 3, name: p.name, color: p.color ?? null });
            },
            updateContainer: () => of(container1),
            deleteContainer: () => of(undefined),
            getMonth: (m: string) => {
              getMonthSpy.calls.push(m);
              return getMonthSpy.fn(m);
            },
            createEntry: (p: SavingsEntryPayload) => {
              createEntrySpy.calls.push(p);
              return of(entry1);
            },
            updateEntry: (id: number, p: SavingsEntryPayload) => {
              updateEntrySpy.calls.push({ id, payload: p });
              return of(entry2);
            },
            deleteEntry: (id: number) => {
              deleteEntrySpy.calls.push(id);
              return of(undefined);
            },
          },
        },
        {
          provide: UsersService,
          useValue: { list: () => of([admin, vanya, katya, inactive]) },
        },
        ToastService,
      ],
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  function comp() {
    const fixture = TestBed.createComponent(SavingsComponent);
    fixture.detectChanges();
    return {
      fixture,
      component: fixture.componentInstance as unknown as SavingsComponentApi,
    };
  }

  it('shows the empty state when the month has no entries', () => {
    getMonthSpy.fn = () => of(emptyResponse);
    const { fixture } = comp();
    const el = fixture.nativeElement as HTMLElement;

    expect(el.textContent).toContain('Записей за месяц нет. Добавьте первую.');
    expect(
      Array.from(el.querySelectorAll('button')).some((b) => b.textContent?.includes('+ добавить')),
    ).toBe(true);
    expect(getMonthSpy.calls[0]).toBe('2026-08');
  });

  it('renders entries with member, container and the month total', () => {
    const { fixture } = comp();
    const el = fixture.nativeElement as HTMLElement;

    expect(el.textContent).toContain('Ваня');
    expect(el.textContent).toContain('Катя');
    expect(el.textContent).toContain('Вклад');
    expect(el.textContent).toContain('Наличка');
    expect(el.textContent).toContain('100\u00A0000,00 ₽');
    expect(el.textContent).toContain('50\u00A0000,00 ₽');
    expect(el.textContent).toContain('150\u00A0000,00 ₽');
  });

  it('excludes the admin and inactive users from the "who" select', () => {
    const { component: c, fixture } = comp();
    c.startAddEntry();
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;

    const select = el.querySelector('select[aria-label="Кто"]') as HTMLSelectElement;
    const options = Array.from(select.querySelectorAll('option')).map((o) => o.textContent?.trim());
    expect(options).toContain('Ваня');
    expect(options).toContain('Катя');
    expect(options).not.toContain('Админ');
    expect(options).not.toContain('Бывший');
  });

  it('requests the shifted month after the month is changed', () => {
    const { component: c } = comp();

    c.shiftMonth(-1);

    expect(getMonthSpy.calls[getMonthSpy.calls.length - 1]).toBe('2026-07');
  });

  it('creates a container from the inline form', () => {
    const { component: c } = comp();
    c.startAddContainer();
    c.draftName.set('Новый счёт');
    c.draftColor.set('#3b82f6');
    c.saveContainer();

    expect(createContainerSpy.calls).toEqual([{ name: 'Новый счёт', color: '#3b82f6' }]);
    const toasts = TestBed.inject(ToastService);
    expect(toasts.toasts().some((t) => t.kind === 'success' && t.message === 'Счёт добавлен')).toBe(
      true,
    );
  });

  it('creates an entry with the selected month and member', () => {
    const { component: c } = comp();
    c.startAddEntry();
    c.draftUserId.set(3);
    c.draftContainerId.set(2);
    c.draftAmount.set('20000');
    c.saveEntry();

    expect(createEntrySpy.calls).toEqual([
      { month: '2026-08', userId: 3, containerId: 2, amount: 20000 },
    ]);
    const toasts = TestBed.inject(ToastService);
    expect(
      toasts.toasts().some((t) => t.kind === 'success' && t.message === 'Запись добавлена'),
    ).toBe(true);
  });

  it('updates an existing entry with the edited amount', () => {
    const { component: c } = comp();
    c.startEditEntry(entry2);
    c.draftAmount.set('75000');
    c.saveEntry();

    expect(updateEntrySpy.calls).toEqual([
      { id: 2, payload: { month: '2026-08', userId: 3, containerId: 2, amount: 75000 } },
    ]);
  });

  it('asks for confirmation and deletes the entry', () => {
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true);
    const { component: c } = comp();

    c.removeEntry(entry1);

    expect(confirmSpy).toHaveBeenCalledTimes(1);
    expect(deleteEntrySpy.calls).toEqual([1]);
    const toasts = TestBed.inject(ToastService);
    expect(
      toasts.toasts().some((t) => t.kind === 'success' && t.message === 'Запись удалена'),
    ).toBe(true);
  });

  it('does not delete the entry when confirmation is cancelled', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false);
    const { component: c } = comp();

    c.removeEntry(entry1);

    expect(deleteEntrySpy.calls).toEqual([]);
  });
});
