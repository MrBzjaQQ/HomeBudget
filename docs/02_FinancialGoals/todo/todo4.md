# Todo 4: Frontend-модели + goals.service.ts

- Агент: frontend-developer
- Зависит от: нет
- Параллельно с: todo1
- Статус: ☑ выполнено (в работе → выполнено 2026-09-28)

## Контекст
Контрактная часть фичи «Финансовые цели» (spec.md, разделы 8 «DTO» и 9 «Frontend-план»). Backend-контракт зафиксирован в spec.md: `GET /api/goals` → `GoalDto[]`, `POST /api/goals` → 201 `GoalDto`, `DELETE /api/goals/{id}` → 204. Эта подзадача — только модели и сервис, компонент появится в todo5.

## Что сделать
1. Изменить `frontend/src/app/core/models.ts` — добавить (рядом с моделями `Savings*`, строки 145-209, стиль тех же интерфейсов):
   - `GoalDto`: `id: number; name: string; targetAmount: number; containerId: number; containerName: string; containerColor: string | null; dueDate: string | null; // 'yyyy-MM-dd' saved: number; monthlyRequired: number | null; achieved: boolean; createdAt: string;`
   - `CreateGoalPayload`: `name: string; targetAmount: number; containerId: number; dueDate?: string | null; // 'yyyy-MM-dd'`
2. Создать `frontend/src/app/core/goals.service.ts` — `@Injectable({ providedIn: 'root' })`, `HttpClient` через `inject()` (стиль — `savings.service.ts`):
   - `listGoals(): Observable<GoalDto[]>` — GET `/api/goals`;
   - `createGoal(payload: CreateGoalPayload): Observable<GoalDto>` — POST `/api/goals`;
   - `deleteGoal(id: number): Observable<void>` — DELETE `/api/goals/${id}`.

## Ограничения
- Стиль проекта: 2 пробела, одинарные кавычки, без `any` (`.editorconfig`); импорты — как в `savings.service.ts`.
- Не менять существующие модели и сервисы.
- Значения `saved`/`monthlyRequired` — такие, какие пришли от сервера (без клиентских расчётов); `dueDate` — строка `yyyy-MM-dd`.

## Результат (критерий приёмки)
- [ ] `frontend/src/app/core/models.ts` содержит `GoalDto` и `CreateGoalPayload` с точными полями из spec.md (раздел 9).
- [ ] `frontend/src/app/core/goals.service.ts` существует, `providedIn: 'root'`, три метода с указанными путями.
- [ ] Проект компилируется без ошибок TypeScript (например, `npx ng build` из `frontend/` завершается успешно).

## Результат выполнения
_(заполнит manage-development после выполнения)_
- Статус: выполнено
- Что сделано:
  - Изменён: `frontend/src/app/core/models.ts` — добавлены `GoalDto` и `CreateGoalPayload` (полями точно по spec.md §8–9, рядом с `Savings*`).
  - Создан: `frontend/src/app/core/goals.service.ts` — `providedIn: 'root'`, `listGoals` (GET `/api/goals`), `createGoal` (POST), `deleteGoal` (DELETE `/api/goals/{id}`); стиль как в `savings.service.ts`.
- Итоги тестов: `npx ng build` — успешно (0 ошибок); `npx ng test` — 92 / 92 пройдено (15 файлов).
