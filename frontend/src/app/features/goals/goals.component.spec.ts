import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import {
  CreateGoalPayload,
  GoalDto,
  SavingsContainerDto,
  UpdateGoalPayload,
} from '../../core/models';
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
  startEdit(goal: GoalDto): void;
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
  let updateGoalSpy: { calls: Array<{ id: number; payload: UpdateGoalPayload }>; shouldFail: boolean };
  let deleteGoalSpy: { calls: number[] };
  let containersFixture: SavingsContainerDto[];
  let serverDateSignal: { set(v: string | null): void };

  beforeEach(async () => {
    listGoalsSpy = { calls: 0, goals: [goalWithPlan, goalWithoutDate] };
    createGoalSpy = { calls: [] };
    updateGoalSpy = { calls: [], shouldFail: false };
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
            updateGoal: (id: number, payload: UpdateGoalPayload) => {
              updateGoalSpy.calls.push({ id, payload });
              return updateGoalSpy.shouldFail
                ? throwError(() => ({
                    error: { code: 'VALIDATION_FAILED', message: 'Не удалось обновить цель' },
                  }))
                : of({ ...goalWithPlan, ...payload, id });
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
    }).compileComponents();
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

  it('кнопка_редактирования_имеет_доступное_имя_и_показывает_карандаш_без_видимого_текста', () => {
    const { fixture } = comp();
    const el = fixture.nativeElement as HTMLElement;
    const editButtons = el.querySelectorAll('button[aria-label="Редактировать цель"]');
    const editButton = editButtons[0] as HTMLButtonElement;

    expect(editButtons).toHaveLength(2);
    expect(editButton.getAttribute('aria-label')).toBe('Редактировать цель');
    expect(editButton.getAttribute('title')).toBe('Редактировать цель');
    expect(editButton.querySelector('[aria-hidden="true"]')?.textContent?.trim()).toBe('✎');
    expect(editButton.textContent).not.toContain('Редактировать');
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
    const { component: c, fixture } = comp();
    c.startAdd();
    c.draftName.set('   ');
    c.draftAmount.set('150000');
    c.draftContainerId.set(1);
    c.saveGoal();
    fixture.detectChanges();

    expect(createGoalSpy.calls).toEqual([]);
    expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')?.textContent).toContain(
      'Введите название цели.',
    );
  });

  it('открывает_редактирование_с_текущими_названием_суммой_счётом_и_датой', async () => {
    const { component: c, fixture } = comp();
    const el = fixture.nativeElement as HTMLElement;
    const editButton = el.querySelector<HTMLButtonElement>(
      'button[aria-label="Редактировать цель"]',
    );

    editButton?.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect((el.querySelector('[aria-label="Название цели"]') as HTMLInputElement).value).toBe(
      'Отпуск',
    );
    expect((el.querySelector('[aria-label="Целевая сумма"]') as HTMLInputElement).value).toBe(
      '100000',
    );
    expect(c.draftContainerId()).toBe(1);
    expect(
      (el.querySelector('[aria-label="Счёт"]') as HTMLSelectElement).selectedOptions[0].textContent,
    ).toBe('Вклад');
    expect((el.querySelector('[aria-label="Дата достижения"]') as HTMLInputElement).value).toBe(
      '2026-12-31',
    );
  });

  it('открывает_редактирование_соответствующей_цели_по_клику_на_кнопку_с_иконкой', async () => {
    const { component: c, fixture } = comp();
    const el = fixture.nativeElement as HTMLElement;
    const editButtons = el.querySelectorAll('button[aria-label="Редактировать цель"]');

    (editButtons[1] as HTMLButtonElement).click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(c.showForm()).toBe(true);
    expect((el.querySelector('[aria-label="Название цели"]') as HTMLInputElement).value).toBe(
      'Резерв',
    );
    expect(c.draftContainerId()).toBe(2);
  });

  it('отправляет_id_и_полный_payload_при_сохранении_изменённой_цели', async () => {
    const { component: c, fixture } = comp();
    const el = fixture.nativeElement as HTMLElement;
    el.querySelector<HTMLButtonElement>('button[aria-label="Редактировать цель"]')?.click();
    await fixture.whenStable();
    fixture.detectChanges();
    c.draftName.set('Новый отпуск');
    c.draftAmount.set('180000');
    c.draftContainerId.set(2);
    c.draftDueDate.set('2027-06-30');
    fixture.detectChanges();
    Array.from(el.querySelectorAll('button'))
      .find((button) => button.textContent?.includes('Сохранить изменения'))
      ?.click();
    await fixture.whenStable();

    expect(updateGoalSpy.calls).toEqual([
      {
        id: 1,
        payload: {
          name: 'Новый отпуск',
          targetAmount: 180000,
          containerId: 2,
          dueDate: '2027-06-30',
        },
      },
    ]);
  });

  it('закрывает_форму_показывает_успех_и_перезагружает_цели_после_обновления', () => {
    const { component: c } = comp();
    c.startEdit(goalWithPlan);
    c.saveGoal();

    expect(updateGoalSpy.calls).toHaveLength(1);
    expect(
      TestBed.inject(ToastService)
        .toasts()
        .some((toast) => toast.kind === 'success' && toast.message === 'Цель обновлена'),
    ).toBe(true);
    expect(c.showForm()).toBe(false);
    expect(listGoalsSpy.calls).toBeGreaterThanOrEqual(2);
  });

  it('отменяет_редактирование_без_обновления_и_очищает_черновики', () => {
    const { component: c, fixture } = comp();
    c.startEdit(goalWithPlan);
    fixture.detectChanges();
    const cancelButton = Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll('button'),
    ).find((button) => button.textContent?.includes('Отмена'));

    cancelButton?.click();

    expect(updateGoalSpy.calls).toEqual([]);
    expect(c.showForm()).toBe(false);
    expect(c.draftName()).toBe('');
    expect(c.draftAmount()).toBe('');
    expect(c.draftContainerId()).toBeNull();
    expect(c.draftDueDate()).toBe('');
  });

  it('не_обновляет_цель_при_нулевой_сумме_и_показывает_ошибку_в_форме', async () => {
    const { component: c, fixture } = comp();
    c.startEdit(goalWithPlan);
    await fixture.whenStable();
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    (el.querySelector('[aria-label="Целевая сумма"]') as HTMLInputElement).value = '0';
    (el.querySelector('[aria-label="Целевая сумма"]') as HTMLInputElement).dispatchEvent(
      new Event('input'),
    );
    await fixture.whenStable();
    fixture.detectChanges();
    Array.from(el.querySelectorAll('button'))
      .find((button) => button.textContent?.includes('Сохранить изменения'))
      ?.click();
    fixture.detectChanges();

    expect(updateGoalSpy.calls).toEqual([]);
    expect(el.querySelector('[role="alert"]')?.textContent).toContain(
      'Введите целевую сумму больше нуля.',
    );
  });

  it('показывает_ошибку_api_и_оставляет_форму_редактирования_открытой', async () => {
    updateGoalSpy.shouldFail = true;
    const { fixture } = comp();
    const el = fixture.nativeElement as HTMLElement;
    el.querySelector<HTMLButtonElement>('button[aria-label="Редактировать цель"]')?.click();
    await fixture.whenStable();
    fixture.detectChanges();
    Array.from(el.querySelectorAll('button'))
      .find((button) => button.textContent?.includes('Сохранить изменения'))
      ?.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(updateGoalSpy.calls).toHaveLength(1);
    expect(el.querySelector('[role="alert"]')?.textContent).toContain('Не удалось обновить цель');
    expect(el.textContent).toContain('Редактирование цели');
    expect((el.querySelector('[aria-label="Название цели"]') as HTMLInputElement).value).toBe(
      'Отпуск',
    );
  });

  it('очищает_ошибку_api_при_вводе_в_форму', async () => {
    updateGoalSpy.shouldFail = true;
    const { fixture } = comp();
    const el = fixture.nativeElement as HTMLElement;
    el.querySelector<HTMLButtonElement>('button[aria-label="Редактировать цель"]')?.click();
    fixture.detectChanges();
    Array.from(el.querySelectorAll('button'))
      .find((button) => button.textContent?.includes('Сохранить изменения'))
      ?.click();
    fixture.detectChanges();
    expect(el.querySelector('[role="alert"]')?.textContent).toContain('Не удалось обновить цель');

    const nameInput = el.querySelector('[aria-label="Название цели"]') as HTMLInputElement;
    nameInput.value = 'Новая цель';
    nameInput.dispatchEvent(new Event('input'));
    await fixture.whenStable();
    fixture.detectChanges();

    expect(el.querySelector('[role="alert"]')).toBeNull();
    expect(el.textContent).toContain('Редактирование цели');
  });

  it('очищает_ошибку_api_при_отмене_редактирования', () => {
    updateGoalSpy.shouldFail = true;
    const { fixture } = comp();
    const el = fixture.nativeElement as HTMLElement;
    el.querySelector<HTMLButtonElement>('button[aria-label="Редактировать цель"]')?.click();
    fixture.detectChanges();
    Array.from(el.querySelectorAll('button'))
      .find((button) => button.textContent?.includes('Сохранить изменения'))
      ?.click();
    fixture.detectChanges();
    expect(el.querySelector('[role="alert"]')?.textContent).toContain('Не удалось обновить цель');

    Array.from(el.querySelectorAll('button'))
      .find((button) => button.textContent?.includes('Отмена'))
      ?.click();
    fixture.detectChanges();

    expect(el.querySelector('[role="alert"]')).toBeNull();
    expect(el.textContent).not.toContain('Редактирование цели');
  });

  it('does not create the goal when the amount is empty', () => {
    const { component: c, fixture } = comp();
    c.startAdd();
    c.draftName.set('Отпуск');
    c.draftAmount.set('');
    c.draftContainerId.set(1);
    c.saveGoal();
    fixture.detectChanges();

    expect(createGoalSpy.calls).toEqual([]);
    expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')?.textContent).toContain(
      'Введите целевую сумму больше нуля.',
    );
  });

  it('does not create the goal when the amount is zero', () => {
    const { component: c, fixture } = comp();
    c.startAdd();
    c.draftName.set('Отпуск');
    c.draftAmount.set('0');
    c.draftContainerId.set(1);
    c.saveGoal();
    fixture.detectChanges();

    expect(createGoalSpy.calls).toEqual([]);
    expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')?.textContent).toContain(
      'Введите целевую сумму больше нуля.',
    );
  });

  it('does not create the goal when no container is selected', () => {
    const { component: c, fixture } = comp();
    c.startAdd();
    c.draftName.set('Отпуск');
    c.draftAmount.set('150000');
    c.saveGoal();
    fixture.detectChanges();

    expect(createGoalSpy.calls).toEqual([]);
    expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')?.textContent).toContain(
      'Выберите счёт.',
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

  it('удаляет_цель_по_уникальной_кнопке_с_доступным_именем', () => {
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true);
    const { fixture } = comp();
    const el = fixture.nativeElement as HTMLElement;
    const editButton = el.querySelector<HTMLButtonElement>(
      'button[aria-label="Редактировать цель"]',
    );
    const actionGroup = editButton?.parentElement;
    const deleteButtons = actionGroup?.querySelectorAll('button[aria-label="Удалить цель"]');
    const deleteButton = deleteButtons?.[0] as HTMLButtonElement;

    expect(deleteButtons).toHaveLength(1);
    expect(deleteButton.getAttribute('aria-label')).toBe('Удалить цель');
    deleteButton.click();

    expect(confirmSpy).toHaveBeenCalledTimes(1);
    expect(deleteGoalSpy.calls).toEqual([1]);
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
