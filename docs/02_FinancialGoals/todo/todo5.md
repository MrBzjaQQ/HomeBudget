# Todo 5: Страница «Финансовые цели» (компонент + маршрут + навигация)

- Агент: frontend-developer
- Зависит от: todo4
- Параллельно с: нет
- Статус: ☑ выполнено (в работе → выполнено 2026-09-28)

## Контекст
UI фичи (spec.md, разделы 1 FR-1/FR-2/FR-3/FR-5/FR-8 и 9). Модели и `GoalsService` готовы (todo4). Паттерн-образец — фича «Накопления» (`frontend/src/app/features/savings/`): signals, инлайн-форма, `window.confirm`, toast'ы, локальные стили. Реализует FR-1…FR-3, FR-8.

## Что сделать
1. Создать `frontend/src/app/features/goals/goals.component.ts` (стиль — `savings.component.ts:20-60`):
   - `@Component`, `templateUrl: './goals.component.html'`, `styleUrl: './goals.component.css'`, `imports: [CommonModule, FormsModule, MoneyPipe, RuDatePipe]` (пайпы из `core/format.pipe.ts` — импортировать напрямую, как в savings);
   - инжект: `GoalsService`, `SavingsService`, `ToastService`, `DestroyRef`;
   - сигналы: `goals: GoalDto[]`, `loading`, `containers: SavingsContainerDto[]`; инлайн-форма создания: `showForm` (bool), `draftName` (string), `draftAmount` (string), `draftContainerId` (number | null), `draftDueDate` (string) — только создание, редактирования нет;
   - `reload()` → `goalsService.listGoals()` → `goals` (ошибка → `toasts.error('Не удалось загрузить данные')`); `loadContainers()` → `savingsService.listContainers()` → `containers`;
   - `startAdd()` (доступна, только если есть хотя бы один счёт), `cancelForm()`, `saveGoal()`: клиентская валидация — пустое название / сумма не число или ≤ 0 / не выбран счёт → `toasts.error(...)` и выход без запроса; валидный ввод → `createGoal({name: trimmed, targetAmount: Number(draftAmount), containerId, dueDate: draftDueDate || null})` → `toasts.success('Цель добавлена')` → `reload()` + сброс формы; ошибка → `toasts.error(ApiError.from(err).message)`;
   - `removeGoal(g: GoalDto)` → `window.confirm('Удалить цель «…»?')` (по образцу `savings.component.ts:219-240`) → `deleteGoal(g.id)` → `toasts.success('Цель удалена')` → `reload()`; ошибка → toast.
2. Создать `frontend/src/app/features/goals/goals.component.html` (структура по `savings.component.html`):
   - одна карточка: заголовок «Финансовые цели» (`.section-title`) + кнопка «+ добавить» (`.btn-add`);
   - пустое состояние: целей нет → «Целей пока нет. Добавьте первую.» + кнопка; ни одного счёта → «Сначала добавьте счёт на странице «Накопления»» (кнопки добавления нет);
   - инлайн-форма создания (по образцу формы счёта `savings.component.html:32-65`): название (`input.input`, `maxlength=100`), целевая сумма (`input.input type=number`, `min=0`, `step=0.01`, `inputmode=decimal`), счёт (`select.input` из `containers`, плейсхолдер «— выберите счёт —»), дата достижения (`input.input type=date`, необязательная); кнопки «Отмена»/«Сохранить» (`btn-sm btn-secondary` / `btn-sm btn-primary`);
   - строка цели: название (акцент, жирный); прогресс-бар (ширина = `min(100, saved / targetAmount * 100)%`, цвет brand `#0f766e`, при `achieved` — полный); строки «Цель: {{ targetAmount | money }} ₽» и «Накоплено: {{ saved | money }} ₽»; при `dueDate` — «К {{ dueDate | ruDate }}»; при `monthlyRequired !== null` — «Откладывать: {{ monthlyRequired | money }} ₽/мес»; при `achieved` — бейдж «Цель достигнута» (зелёный); справа — `.icon-btn` удаления (aria-label «Удалить цель»).
3. Создать `frontend/src/app/features/goals/goals.component.css` — скопировать классы `.card`, `.section-title`, `.btn-add`, `.icon-btn` (+`:hover/:active`, `.icon-btn-danger`), `.input`, `.btn-sm`, `.btn-primary`, `.btn-secondary` из `savings.component.css:1-63` (глобальных классов нет — дублирование локально, это практика проекта) + стили прогресс-бара и бейджа.
4. Изменить `frontend/src/app/app.routes.ts` — дочерний маршрут `goals` сразу после `savings` (строки 47-52): lazy `loadComponent: () => import('./features/goals/goals.component').then((m) => m.GoalsComponent)`, `title: 'Финансовые цели — Бюджет'`.
5. Изменить `frontend/src/app/layout/shell.component.html` — ссылка после «Накопления» (строка 11): `<a routerLink="/goals" routerLinkActive="nav-link-active" class="nav-link">Финансовые цели</a>`.
6. Изменить `frontend/src/app/features/more/more.component.html` — строка сразу после блока «Накопления» (строки 7-14), структура как у соседей: `<a routerLink="/goals" class="nav-row">`, `<span class="nav-ico">🎯</span>`, заголовок «Финансовые цели», подпись «цели и план откладывать в месяц», стрелка `›`.

## Ограничения
- Angular 22 signals/zoneless: без NgZone, без `async`-пайпа для данных фичи (паттерн savings: подписки через `toObservable`/`takeUntilDestroyed` только там, где нужно — здесь достаточно прямых вызовов в обработчиках);
- не менять мобильную нижнюю панель (5 ячеек) и другие фичи;
- не менять `savings.service.ts` — только читать `listContainers()`;
- «накоплено» и норму НЕ вычислять на клиенте — только рендерить поля `saved`/`monthlyRequired`/`achieved` из API;
- ошибки API — всегда `ApiError.from(err).message` в `toasts.error`.

## Результат (критерий приёмки)
- [ ] Страница открывается по `/goals`, видна в desktop-шапке (после «Накопления») и в мобильном «Ещё»; нижний бар не изменён.
- [ ] Список целей рендерит название, прогресс, «Цель/Накоплено», дату (`ruDate`), норму «₽/мес» и бейдж «Цель достигнута»; пустое состояние и подсказка про счета работают.
- [ ] Создание цели из инлайн-формы: валидация на клиенте, `createGoal` с payload `{name, targetAmount, containerId, dueDate}`, toast'ы; удаление через confirm.
- [ ] `npx ng build` (из `frontend/`) — без ошибок; существующие тесты `npx ng test` не сломаны.

## Результат выполнения
_(заполнит manage-development после выполнения)_
- Статус: выполнено
- Что сделано:
  - Созданы: `frontend/src/app/features/goals/goals.component.ts` (signals: goals/loading/containers + инлайн-форма создания, reload/loadContainers, saveGoal с клиентской валидацией, removeGoal через confirm), `goals.component.html` (карточка, пустые состояния, инлайн-форма, строки целей с прогресс-баром, «Цель/Накоплено», «К …» (ruDate), «Откладывать: … ₽/мес», бейдж «Цель достигнута», .icon-btn удаления), `goals.component.css` (локальные классы + .progress-track/.progress-fill, .badge-achieved).
  - Изменены: `app.routes.ts` (lazy-маршрут `goals` после `savings`, title «Финансовые цели — Бюджет»), `shell.component.html` (ссылка после «Накопления»), `more.component.html` (строка 🎯 после «Накоплений»).
  - Нижняя панель и другие фичи не тронуты; saved/норма не вычисляются на клиенте.
- Итоги тестов: `npx ng build` — успешно (lazy-чанк goals-component 9.82 kB); `npx ng test` — 92 / 92 пройдено (15 файлов), Failed: 0.
