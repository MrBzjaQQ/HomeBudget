# Spec: Финансовые цели

Ссылка на intent: `intent.md` в этой папке.

## 1. Что делать

- **FR-1.** Новая страница «Финансовые цели» (`/goals`) показывает список всех целей семьи. По каждой цели видны: название, целевая сумма, накопленная сумма, прогресс (накоплено относительно целевой, не больше 100%), дата достижения (если назначена) и, если назначена, сколько нужно откладывать в месяц.
- **FR-2.** Пользователь может создать цель: название (обязательное, до 100 символов), целевая сумма (> 0, до 999 999 999 999.99 ₽), счёт накоплений (обязательный выбор из существующих счетов), дата достижения (необязательная, полная дата).
- **FR-3.** Пользователь может удалить цель (с подтверждением). Удаление цели не трогает связанный счёт и записи накоплений.
- **FR-4.** «Накоплено» по цели = текущий остаток связанного счёта накоплений: сумма записей накоплений этого счёта за последний месяц, в котором есть записи и который не позже текущего серверного месяца; если у счёта нет записей — 0.
- **FR-5.** Ежемесячная норма показывается, если у цели есть дата достижения, цель ещё не достигнута (накоплено < целевой) и дата не прошла. Норма = (целевая − накоплено) ÷ число месяцев до даты достижения (включая текущий месяц), округлённая в большую сторону до рубля. В остальных случаях норма не показывается; если накоплено ≥ целевой — цель помечается «Цель достигнута».
- **FR-6.** Цель общая для всей семьи (без поля «кто»), как счета накоплений.
- **FR-7.** Счёт накоплений, к которому привязана хотя бы одна цель, удалять нельзя (ошибка 409 с понятным сообщением) — расширение существующего правила «счёт с записями не удаляется».
- **FR-8.** Навигация: ссылка «Финансовые цели» в desktop-шапке сразу после «Накопления»; строка «Финансовые цели» в мобильном списке «Ещё» сразу после «Накопления». Мобильная нижняя панель (5 ячеек) не меняется.

## 2. Что НЕ делать

- Не делаем редактирование целей (смена названия, суммы, даты, счёта) — только создание и удаление.
- Не делаем цели «на человека» (поля «кто» нет, привязки к пользователю нет).
- Не делаем аналитику и графики по целям — только список на новой странице.
- Не делаем экспорт целей в Excel.
- Не делаем ручное ведение «накоплено» — сумма всегда вычисляется из записей накоплений.
- Не делаем автоматические переносы/пересчёты записей накоплений; данные и страница «Накопления» не меняют поведение (кроме FR-7).
- Не делаем архив/отложенные цели.

## 3. Ограничения

- Стек: Angular 22 (signals, zoneless, Tailwind 4) + ASP.NET Core 10 (Clean Architecture) + PostgreSQL 16 (EF Core, snake_case).
- Контроллеры не касаются DbContext — только `IGoalsService` из `Budget.Application`; данные через `IUnitOfWork`.
- JSON-контракт: camelCase; ошибки `{code, message, errors}`; коды `ValidationFailed` (400), `NotFound` (404), `Conflict` (409); сообщения об ошибках — на русском, формируются в сервисе.
- Суммы — `DECIMAL(15,2)`, положительная целевая сумма ≤ `Validation.MaxAmount`.
- Дата достижения — полная дата (`DATE` / `DateOnly`), строкой в API строго `yyyy-MM-dd`.
- Миграции: новая миграция создаётся в `Budget.Infrastructure` (CLI из `BudgetApi`); идентификаторы существующих миграций не менять. Схема применяется автоматически при старте API.
- У цели нет поля `UpdatedAt` (редактирования нет) — `TouchUpdated()` не трогаем.
- Эндпоинты только для авторизованных (JWT); данные цели не зависят от текущего пользователя.

## 4. Варианты реализации

**Вариант A. Цель привязана к счёту накоплений; «накоплено» вычисляется на сервере из записей счёта.**
+ Единственный источник правды (записи «Накоплений»): «накоплено» всегда актуально, пользователь ничего не вводит вручную; норму в месяц считает сервер (знает текущую дату), легко тестировать.
− Чуть сложнее расчёт остатка (последний месяц с записями); две цели на один счёт покажут одинаковое «накоплено» (принято: цель — способ взглянуть на счёт).

**Вариант B. У цели своё поле «накоплено», пользователь обновляет его вручную.**
+ Простейшая модель данных, без вычислений.
− Дублирование данных, уже вводимых в «Накоплениях»; «накоплено» быстро отстает от фактического остатка; норма в месяц считается от устаревшего числа.

**Вариант C. Цель не привязана к счёту; «накоплено» = общий остаток всех счетов семьи.**
+ Не надо выбирать счёт.
− Все цели показывают одну и ту же сумму «накоплено» (общий остаток), норма в месяц бессмысленна для отдельных целей.

## 5. Решения

Выбран **Вариант A**: цель ссылается на счёт накоплений (`SavingsContainer`), «накоплено» — текущий остаток этого счёта (FR-4), норма в месяц считается сервером (FR-5). Обоснование: приложение уже ведёт остатки по счетам («Накопления»), это единственный готовый источник «сколько накоплено»; ручной ввод дублировал бы данные и расходился с фактом (минусы B), а общий остаток всех счетов делает цели ненаблюдаемыми (минус C). Привязка к счёту также даёт естественную защиту: счёт с целями не удаляется (FR-7, паттерн 409 из `SavingsService.DeleteContainerAsync`, `backend/Budget.Application/Savings/SavingsService.cs:129-132`).

Ответы на открытые вопросы из `intent.md`:

1. **«Сколько накоплено»** — вычисляется из привязанного счёта накоплений (последний месяц с записями, не позже текущего серверного; пусто → 0). Ручного ввода «накоплено» нет.
2. **Редактирование** — в этой версии нет (ТЗ прямо говорит «создать и удалить»); только создание и удаление.
3. **Норма в месяц при достижении/истёкшем сроке** — не показывается: при «Цель достигнута» бейдж, при прошедшей дате — только дата без нормы. Формула нормы: месяцы = (год_даты − текущий год) × 12 + (месяц_даты − текущий месяц) + 1 (текущий месяц учитывается); если дата < текущей серверной даты — срок истёк. Округление — в большую сторону до рубля (гарантирует достижение к дате).
4. **Цели общие для семьи** — да, без привязки к пользователю (счета и страница «Накопления» тоже общие).

Дополнительно: дата достижения хранится как полная дата (`DateOnly`), в API — строка `yyyy-MM-dd`; отображение на странице — через существующий пайп `ruDate` (`frontend/src/app/core/format.pipe.ts:24-33`).

## 6. Вопросы

Нет — все открытые вопросы из `intent.md` решены в разделе 5.

## 7. Дерево проекта (затрагиваемые места)

```
backend/
├── Budget.Domain/
│   └── Entities/
│       ├── FinancialGoal.cs                      [N] сущность цели
│       └── SavingsContainer.cs                   [M] навигация на цели (Goals)
├── Budget.Application/
│   ├── Goals/
│   │   ├── IGoalsService.cs                      [N] контракт
│   │   ├── GoalsService.cs                       [N] бизнес-логика: список (+остаток счёта, норма),
│   │   │                                           создание, удаление, валидация
│   │   └── GoalsDtos.cs                          [N] CreateGoalRequest, GoalDto
│   ├── Savings/
│   │   └── SavingsService.cs                     [M] DeleteContainerAsync: 409, если у счёта есть цели (FR-7)
│   └── DependencyInjection.cs                    [M] регистрация IGoalsService
├── Budget.Infrastructure/
│   ├── Data/
│   │   ├── BudgetDbContext.cs                    [M] DbSet + конфигурация FinancialGoal (FK, индексы, check)
│   │   └── Migrations/
│   │       └── …_AddFinancialGoals.cs            [N] миграция (таблица financial_goals)
│   └── Data/Migrations/BudgetDbContextModelSnapshot.cs  [M] снимок модели (генерируется EF)
├── BudgetApi/
│   └── Controllers/
│       └── GoalsController.cs                    [N] GET/POST /api/goals, DELETE /api/goals/{id}
└── BudgetApi.Tests/
    ├── GoalsTests.cs                             [N] тесты фичи
    └── SavingsTests.cs                           [M] сценарий: удаление счёта с целью → 409

frontend/
└── src/app/
    ├── core/
    │   ├── goals.service.ts                      [N] listGoals / createGoal / deleteGoal
    │   └── models.ts                             [M] GoalDto, CreateGoalPayload
    ├── app.routes.ts                             [M] lazy-маршрут goals (после savings)
    ├── layout/
    │   └── shell.component.html                  [M] ссылка «Финансовые цели» после «Накопления»
    ├── features/
    │   ├── more/more.component.html              [M] строка «Финансовые цели» (🎯) после «Накопления»
    │   └── goals/
    │       ├── goals.component.ts                [N] компонент страницы
    │       ├── goals.component.html              [N] шаблон (список + инлайн-форма)
    │       ├── goals.component.css               [N] стили (.card/.input/.btn-*/.icon-btn по образцу savings)
    │       └── goals.component.spec.ts           [N] vitest-тесты
```

Не меняется: `TouchUpdated()` (цель без `UpdatedAt`), `format.pipe.ts` (пайпы `money`/`ruDate` уже есть), нижний мобильный бар, `DbSeeder` (цели не сидируются).

## 8. Backend-план

**Сущность `FinancialGoal`** (`Budget.Domain/Entities/FinancialGoal.cs`):
- `Id` int (identity PK);
- `Name` string — обязательно, ≤ 100;
- `TargetAmount` decimal — целевая сумма, > 0;
- `ContainerId` int — FK → `savings_containers.id`, `ON DELETE RESTRICT` + навигация `Container`;
- `DueDate` DateOnly? — дата достижения, nullable;
- `CreatedAt` DateTime — по умолчанию `now()`.
- Уникальности по имени нет (две цели могут называться одинаково).
- В `SavingsContainer` — навигация `ICollection<FinancialGoal> Goals`.

**Конфигурация** (`BudgetDbContext.OnModelCreating`, образец блока `SavingsEntry` — `Budget.Infrastructure/Data/BudgetDbContext.cs:92-108`): `Name` max 100 required; `TargetAmount` precision (15,2) + check `target_amount > 0`; `DueDate` — date; `CreatedAt` default `now()`; FK `Container` → `DeleteBehavior.Restrict`; индекс по `ContainerId`.

**Миграция**: `AddFinancialGoals` (таблица `financial_goals`, snake_case): `dotnet ef migrations add AddFinancialGoals --project ..\Budget.Infrastructure` из `backend/BudgetApi`.

**Сервис `IGoalsService`/`GoalsService`** (`Budget.Application/Goals/`), primary constructor с `IUnitOfWork` (образец `SavingsService`):
- `ListAsync()` → список целей, по каждой: данные цели + `ContainerName` + цвет счёта + `Saved` (остаток по FR-4) + `MonthlyRequired` (норма по FR-5, `null` если не показывать) + `Achieved` (`Saved >= TargetAmount`). Сортировка: `CreatedAt ASC, Id ASC`. Остаток счёта берётся по последнему месяцу с записями, не позже текущего серверного месяца (`Validation.Today()`); записи за будущие месяцы не учитываются.
- `CreateAsync(CreateGoalRequest)` → валидация (400, `errors`):
  | Поле | Правило | Сообщение |
  | --- | --- | --- |
  | `name` | обязателен, trim, не пустой, ≤ 100 | «Название не может быть пустым» / «Название не длиннее 100 символов» |
  | `targetAmount` | обязателен, > 0, ≤ `Validation.MaxAmount` | «Целевая сумма должна быть больше нуля» / «Сумма не должна превышать 999 999 999 999.99» |
  | `containerId` | обязателен, счёт существует | «Счёт не найден» |
  | `dueDate` | необязательная; если задана — строго `yyyy-MM-dd` | «Формат даты: yyyy-MM-dd» |
  - `CreatedAt = DateTime.UtcNow`, ответ — `GoalDto`.
- `DeleteAsync(id)` → 404 «Цель не найдена»; удаление цели (счёт и записи не трогаются).

**Правка `SavingsService.DeleteContainerAsync`** (`Budget.Application/Savings/SavingsService.cs:124-136`): после существующей проверки на записи — дополнительная проверка: если по счёту есть цели → 409 `ApiErrorException.Conflict("По счёту есть финансовые цели — сначала удалите их")`.

**DTO** (`Budget.Application/Goals/GoalsDtos.cs`):
- Запрос создания: `{ "name": "Отпуск", "targetAmount": 150000, "containerId": 1, "dueDate": "2026-12-31" }` (`dueDate` — строка или null).
- Ответ `GoalDto`: `{ "id", "name", "targetAmount", "containerId", "containerName", "containerColor" (null если нет), "dueDate" ("yyyy-MM-dd" | null), "saved", "monthlyRequired" (число | null), "achieved" (bool), "createdAt" }`.

**Контроллер `GoalsController`** (`BudgetApi/Controllers/`, `[ApiController] [Authorize] [Route("api/goals")]`, DI `IGoalsService` — образец `SavingsController`):

| Метод | Путь | Ответ |
| --- | --- | --- |
| GET | `/api/goals` | 200 — массив `GoalDto` |
| POST | `/api/goals` | 201 — `GoalDto`; 400 — валидация; пустое тело → 400 «Пустое тело запроса» |
| DELETE | `/api/goals/{id:int}` | 204; 404 — «Цель не найдена» |

**DI**: `services.AddScoped<IGoalsService, GoalsService>()` в `Budget.Application/DependencyInjection.cs` (рядом с `ISavingsService`, строка 22).

## 9. Frontend-план

**Модели** (`core/models.ts`): `GoalDto` (`id, name, targetAmount, containerId, containerName, containerColor: string | null, dueDate: string | null, saved: number, monthlyRequired: number | null, achieved: boolean, createdAt: string`) и `CreateGoalPayload` (`name, targetAmount, containerId, dueDate?: string | null`).

**Сервис** (`core/goals.service.ts`, `@Injectable({ providedIn: 'root' })`, образец `savings.service.ts`):
- `listGoals(): Observable<GoalDto[]>` — GET `/api/goals`;
- `createGoal(payload): Observable<GoalDto>` — POST `/api/goals`;
- `deleteGoal(id): Observable<void>` — DELETE `/api/goals/{id}`.

**Страница** (`features/goals/goals.component.{ts,html,css}`, паттерн — `features/savings/`):
- Одна карточка «Финансовые цели» с кнопкой «+ добавить» (`.btn-add`).
- Строка цели: название (акцент), прогресс-бар (накоплено/целевая, cap 100%, цвет brand; при достижении — полный), строки «Цель: … ₽» / «Накоплено: … ₽» (пайп `money`), при `dueDate` — дата (пайп `ruDate`), при `monthlyRequired != null` — «Откладывать: … ₽/мес», при `achieved` — бейдж «Цель достигнута»; справа — `.icon-btn` удаления.
- Инлайн-форма создания (по образцу формы счёта из `savings.component.html:32-65`): название (text, ≤ 100), целевая сумма (number, min 0, step 0.01), счёт (select из `savingsService.listContainers()`), дата достижения (input type="date", необязательная). Клиентская валидация перед запросом (пустое название / сумма ≤ 0 / не выбран счёт → toast, без запроса).
- Удаление — `window.confirm` → `deleteGoal` → перезагрузка списка; toast'ы успеха/ошибки (`ApiError.from(err).message`).
- Состояния: пустое — «Целей пока нет. Добавьте первую.» + кнопка; нет ни одного счёта — подсказка сначала добавить счёт на странице «Накопления» (формы нет); ошибка загрузки — toast «Не удалось загрузить данные».
- Загрузка: `reload()` → `goalsService.listGoals()`; `loadContainers()` → `savingsService.listContainers()` (для select счёта).
- Стили — локальные `.card/.input/.btn-sm/.btn-primary/.btn-secondary/.btn-add/.icon-btn`, скопировать из `savings.component.css:1-63` (глобальных классов нет).

**Маршрут** (`app.routes.ts`): дочерний `goals` после `savings`, lazy `loadComponent` → `features/goals/goals.component`, `title: 'Финансовые цели — Бюджет'`.

**Навигация**: `shell.component.html` — ссылка «Финансовые цели» (`/goals`) после «Накопления» (строка 11), классы `nav-link`/`nav-link-active`; `more.component.html` — строка после «Накопления»: иконка `🎯`, название «Финансовые цели», подпись «цели и план откладывать в месяц», `routerLink="/goals"`.

## 10. Тесты

**Backend — `backend/BudgetApi.Tests/GoalsTests.cs`** (xUnit, `ApiTestBase`, InMemory; образцы — `SavingsTests.cs`):
1. Пустой список → 200, пустой массив.
2. Создание → 201; цель видна в GET с `containerName`, цветом счёта, `saved = 0` (записей нет).
3. `name` пустой / > 100 → 400, `errors.name`.
4. `targetAmount` = 0, отрицательный, > MaxAmount → 400, `errors.targetAmount`.
5. `containerId` несуществующий → 400, `errors.containerId`.
6. `dueDate` невалидная (`2026-13-01`, `01-01-2026`, пустая строка) → 400, `errors.dueDate`; валидная → 201.
7. Без `dueDate` → 201, `dueDate = null`, `monthlyRequired = null`.
8. «Накоплено»: записи счёта за прошлый месяц (два пользователя) → `saved` = сумма; запись за более ранний месяц не меняет остаток (считается только последний месяц); запись за **будущий** месяц не учитывается.
9. Норма: `targetAmount − saved` делится ровно и не ровно (округление вверх); дата = текущий месяц → месяцов = 1 → норма = остаток суммы.
10. Цель достигнута (`saved ≥ targetAmount`) → `achieved = true`, `monthlyRequired = null`.
11. Дата в прошлом → `monthlyRequired = null`, `achieved = false`.
12. Удаление → 204, цель исчезает; связанный счёт и записи на месте.
13. Удаление несуществующей → 404.
14. 401 без токена.
15. `SavingsTests.cs` (+1 сценарий): создание цели на счёт → `DELETE /api/savings/containers/{id}` → 409 `Conflict` (сообщение про финансовые цели).

**Frontend — `frontend/src/app/features/goals/goals.component.spec.ts`** (vitest, паттерн `savings.component.spec.ts`: useValue-спай `GoalsService` + `SavingsService`, `AuthService.serverDate: signal('…')`, реальный `ToastService`, `vi.spyOn(window, 'confirm')`):
1. Пустое состояние: текст «Целей пока нет», запрос `listGoals` отправлен.
2. Рендер списка: название, суммы через `money`-формат, дата через `ruDate`, «Откладывать: … ₽/мес» при `monthlyRequired`.
3. `achieved = true` → бейдж «Цель достигнута», без строки нормы.
4. Создание из инлайн-формы: payload `{name, targetAmount, containerId, dueDate}` → `createGoal`, success-тост, перезагрузка.
5. Клиентская валидация: пустое название / сумма ≤ 0 / не выбран счёт → запрос не отправляется.
6. Удаление: confirm → true → `deleteGoal`, success-тост; confirm → false → запрос не отправляется.
7. Select счёта заполнен из `savingsService.listContainers`.

**UI (Playwright, ui-tester)** — ключевые сценарии:
1. Страница открывается из шапки и из «Ещё»; пустое состояние.
2. Создание цели (с датой и без): появляется в списке, «накоплено» = остаток счёта, при дате — норма в месяц.
3. Ввод записи накоплений в «Накоплениях» на привязанный счёт → «накоплено» и норма на странице целей обновляются после перезагрузки.
4. Удаление цели (confirm) → исчезает; счёт и записи накоплений не тронуты.
5. Попытка удалить счёт с целью на странице «Накопления» → toast с ошибкой 409.

## 11. Граф зависимости задач

```
T1 (backend): сущность FinancialGoal + конфигурация DbContext + миграция AddFinancialGoals
T2 (backend): GoalsDtos + IGoalsService/GoalsService + DI + GoalsController + 409 в SavingsService.DeleteContainerAsync   ← T1
T3 (backend): GoalsTests.cs + сценарий 409 в SavingsTests.cs, dotnet test (весь набор)                                    ← T2

T4 (frontend): GoalDto/CreateGoalPayload в models.ts + core/goals.service.ts
T5 (frontend): goals.component (ts/html/css) + маршрут + ссылки в shell и more                                           ← T4
T6 (frontend): goals.component.spec.ts, npx ng test                                                                       ← T5

T7 (UI): проверка в браузере (Playwright, ui-tester) по сценариям раздела 10                                              ← T3, T6
```

Цепочка backend (T1 → T2 → T3) и цепочка frontend (T4 → T5 → T6) независимы и выполняются параллельно (контракты зафиксированы в разделах 8–9); T7 — после обеих.
