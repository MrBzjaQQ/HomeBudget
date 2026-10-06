import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import { CreateGoalPayload, GoalDto, SavingsContainerDto } from '../../core/models';
import { GoalsService } from '../../core/goals.service';
import { SavingsService } from '../../core/savings.service';
import { ToastService } from '../../core/toast.service';
import { GoalsComponent } from './goals.component';

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

const goalWithPlan: GoalDto = {
  id: 1,
  name: 'Отпуск',
  targetAmount: 100000,
  containerId: 1,
  containerName: 'Вклад',
  containerColor: '#0f766e',
  dueDate: '2026-12-31',
  saved: 25000,
  monthlyRequired: 30000,
  achieved: false,
  createdAt: '2026-08-01T10:00:00Z',
};

const goalWithoutDate: GoalDto = {
  id: 2,
  name: 'Резерв',
  targetAmount: 50000,
  containerId: 2,
  containerName: 'Наличка',
  containerColor: '#f59e0b',
  dueDate: null,
  saved: 10000,
  monthlyRequired: null,
  achieved: false,
  createdAt: '2026-08-02T10:00:00Z',
};

const goalAchieved: GoalDto = {
  id: 3,
  name: 'Телевизор',
  targetAmount: 60000,
  containerId: 1,
  containerName: 'Вклад',
  containerColor: '#0f766e',
  dueDate: '2026-12-31',
  saved: 60000,
  monthlyRequired: null,
  achieved: true,
  createdAt: '2026-08-03T10:00:00Z',
};

interface GoalsComponentApi {
  startAdd(): void;
  cancelForm(): void;
  saveGoal(): void;
  removeGoal(goal: GoalDto): void;
  showForm: { (): boolean };
  draftName: { (): string; set(v: string): void };
  draftAmount: { (): string; set(v: string): void };
  draftContainerId: { (): number | null; set(v: number | null): void };
  draftDueDate: { (): string; set(v: string): void };
}

describe('GoalsComponent', () => {
  let listGoalsSpy: { calls: number; goals: GoalDto[] };
  let createGoalSpy: { calls: CreateGoalPayload[] };
  let deleteGoalSpy: { calls: number[] };
  let containersFixture: SavingsContainerDto[];
  let serverDateSignal: { set(v: string | null): void };

  beforeEach(async () => {
    listGoalsSpy = { calls: 0, goals: [goalWithPlan, goalWithoutDate] };
    createGoalSpy = { calls: [] };
    deleteGoalSpy = { calls: [] };
    containersFixture = [container1, container2];
    serverDateSignal = signal<string | null>(SERVER_DATE);
    await TestBed.configureTestingModule({
      imports: [GoalsComponent],
      providers: [
        {
          provide: AuthService,
          useValue: { serverDate: serverDateSignal },
        },
        {
          provide: GoalsService,
          useValue: {
            listGoals: () => {
              listGoalsSpy.calls += 1;
              return of(listGoalsSpy.goals);
            },
            createGoal: (p: CreateGoalPayload) => {
              createGoalSpy.calls.push(p);
              return of({
                ...goalWithPlan,
                id: 10,
                name: p.name,
                targetAmount: p.targetAmount,
                containerId: p.containerId,
                dueDate: p.dueDate ?? null,
                saved: 0,
                monthlyRequired: null,
                achieved: false,
              });
            },
            deleteGoal: (id: number) => {
              deleteGoalSpy.calls.push(id);
              return of(undefined);
            },
          },
        },
        {
          provide: SavingsService,
          useValue: {
            listContainers: () => of(containersFixture),
          },
        },
        ToastService,
      ],
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  function comp() {
    const fixture = TestBed.createComponent(GoalsComponent);
    fixture.detectChanges();
    return {
      fixture,
      component: fixture.componentInstance as unknown as GoalsComponentApi,
    };
  }

  it('shows the empty state and requests the goals list', () => {
    listGoalsSpy.goals = [];
    const { fixture } = comp();
    const el = fixture.nativeElement as HTMLElement;

    expect(el.textContent).toContain('Целей пока нет');
    expect(listGoalsSpy.calls).toBeGreaterThanOrEqual(1);
  });

  it('asks to add a container on the savings page when there are no containers', () => {
    containersFixture = [];
    const { fixture } = comp();
    const el = fixture.nativeElement as HTMLElement;

    expect(el.textContent).toContain('Сначала добавьте счёт на странице «Накопления»');
    const addButton = Array.from(el.querySelectorAll('button')).find((b) =>
      b.textContent?.includes('+ добавить'),
    );
    expect(addButton).toBeUndefined();
  });

  it('renders goals with amounts in the ru-RU format, the due date and the monthly norm', () => {
    const { fixture } = comp();
    const el = fixture.nativeElement as HTMLElement;

    expect(el.textContent).toContain('Отпуск');
    expect(el.textContent).toContain('Цель: 100\u00A0000,00 ₽');
    expect(el.textContent).toContain('Накоплено: 25\u00A0000,00 ₽');
    expect(el.textContent).toContain('К 31.12.2026');
    expect(el.textContent).toContain('Откладывать: 30\u00A0000,00 ₽/мес');
    expect(el.textContent).toContain('Резерв');
    expect(el.textContent).toContain('Цель: 50\u00A0000,00 ₽');
    expect(el.textContent).toContain('Накоплено: 10\u00A0000,00 ₽');
  });

  it('shows the achieved badge and hides the monthly norm for an achieved goal', () => {
    listGoalsSpy.goals = [goalAchieved];
    const { fixture } = comp();
    const el = fixture.nativeElement as HTMLElement;

    expect(el.querySelector('.badge-achieved')?.textContent?.trim()).toBe('Цель достигнута');
    expect(el.textContent).not.toContain('Откладывать:');
  });

  it('creates a goal from the inline form and reloads the list', () => {
    const { component: c, fixture } = comp();
    c.startAdd();
    c.draftName.set('Отпуск');
    c.draftAmount.set('150000');
    c.draftContainerId.set(1);
    c.draftDueDate.set('2026-12-31');
    c.saveGoal();

    expect(createGoalSpy.calls).toEqual([
      { name: 'Отпуск', targetAmount: 150000, containerId: 1, dueDate: '2026-12-31' },
    ]);
    const toasts = TestBed.inject(ToastService);
    expect(
      toasts.toasts().some((t) => t.kind === 'success' && t.message === 'Цель добавлена'),
    ).toBe(true);
    expect(c.showForm()).toBe(false);
    expect(c.draftName()).toBe('');
    expect(c.draftAmount()).toBe('');
    expect(c.draftContainerId()).toBeNull();
    expect(c.draftDueDate()).toBe('');
    expect(listGoalsSpy.calls).toBeGreaterThanOrEqual(2);

    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    const buttons = Array.from(el.querySelectorAll('button')).map((b) => b.textContent?.trim());
    expect(buttons.some((t) => t?.includes('Сохранить'))).toBe(false);
    expect(buttons.some((t) => t?.includes('+ добавить'))).toBe(true);
  });

  it('does not create the goal when the name is empty', () => {
    const { component: c } = comp();
    c.startAdd();
    c.draftName.set('   ');
    c.draftAmount.set('150000');
    c.draftContainerId.set(1);
    c.saveGoal();

    expect(createGoalSpy.calls).toEqual([]);
    const toasts = TestBed.inject(ToastService);
    expect(toasts.toasts().some((t) => t.kind === 'error' && t.message === 'Введите название')).toBe(
      true,
    );
  });

  it('does not create the goal when the amount is empty', () => {
    const { component: c } = comp();
    c.startAdd();
    c.draftName.set('Отпуск');
    c.draftAmount.set('');
    c.draftContainerId.set(1);
    c.saveGoal();

    expect(createGoalSpy.calls).toEqual([]);
    const toasts = TestBed.inject(ToastService);
    expect(
      toasts.toasts().some((t) => t.kind === 'error' && t.message === 'Введите сумму больше нуля'),
    ).toBe(true);
  });

  it('does not create the goal when the amount is zero', () => {
    const { component: c } = comp();
    c.startAdd();
    c.draftName.set('Отпуск');
    c.draftAmount.set('0');
    c.draftContainerId.set(1);
    c.saveGoal();

    expect(createGoalSpy.calls).toEqual([]);
    const toasts = TestBed.inject(ToastService);
    expect(
      toasts.toasts().some((t) => t.kind === 'error' && t.message === 'Введите сумму больше нуля'),
    ).toBe(true);
  });

  it('does not create the goal when no container is selected', () => {
    const { component: c } = comp();
    c.startAdd();
    c.draftName.set('Отпуск');
    c.draftAmount.set('150000');
    c.saveGoal();

    expect(createGoalSpy.calls).toEqual([]);
    const toasts = TestBed.inject(ToastService);
    expect(toasts.toasts().some((t) => t.kind === 'error' && t.message === 'Выберите счёт')).toBe(
      true,
    );
  });

  it('asks for confirmation and deletes the goal', () => {
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true);
    const { component: c } = comp();

    c.removeGoal(goalWithPlan);

    expect(confirmSpy).toHaveBeenCalledTimes(1);
    expect(deleteGoalSpy.calls).toEqual([1]);
    const toasts = TestBed.inject(ToastService);
    expect(
      toasts.toasts().some((t) => t.kind === 'success' && t.message === 'Цель удалена'),
    ).toBe(true);
    expect(listGoalsSpy.calls).toBeGreaterThanOrEqual(2);
  });

  it('does not delete the goal when confirmation is cancelled', () => {
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(false);
    const { component: c } = comp();

    c.removeGoal(goalWithPlan);

    expect(confirmSpy).toHaveBeenCalledTimes(1);
    expect(deleteGoalSpy.calls).toEqual([]);
  });

  it('fills the container select from the savings containers', () => {
    const { component: c, fixture } = comp();
    c.startAdd();
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;

    const select = el.querySelector('select[aria-label="Счёт"]') as HTMLSelectElement;
    const options = Array.from(select.querySelectorAll('option')).map((o) => o.textContent?.trim());
    expect(options).toEqual(['— выберите счёт —', 'Вклад', 'Наличка']);
  });
});
