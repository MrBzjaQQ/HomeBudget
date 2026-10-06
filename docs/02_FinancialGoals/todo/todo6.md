# Todo 6: Frontend-тесты goals.component.spec.ts

- Агент: frontend-tests-writer
- Зависит от: todo5
- Параллельно с: нет
- Статус: ☐ в работе / ☐ выполнено / ☐ ошибка

## Контекст
vitest-тесты страницы «Финансовые цели» (spec.md, раздел 10 «Frontend»). Компонент готов (todo5): `frontend/src/app/features/goals/goals.component.{ts,html,css}`. Писать только тесты; образец паттерна — `frontend/src/app/features/savings/savings.component.spec.ts` (useValue-спай сервисов, `serverDate: signal('…')`, реальный `ToastService`, `vi.spyOn(window, 'confirm')`).

## Что сделать
1. Создать `frontend/src/app/features/goals/goals.component.spec.ts`:
   - фикстуры `GoalDto` вверху файла (цель с датой и нормой, цель без даты, достигнутая цель; счёт с цветом), константа `SERVER_DATE`;
   - `beforeEach`: `TestBed.configureTestingModule({ imports: [GoalsComponent], providers: [...] })` — `GoalsService` → useValue-спай (методы `listGoals/createGoal/deleteGoal` возвращают фикстуры/записывают вызовы), `SavingsService` → useValue-спай (`listContainers` → фикстура счетов), `AuthService` → `{ serverDate: signal(SERVER_DATE) }`, реальный `ToastService`;
   - хелпер `comp()` (`createComponent` + `detectChanges`), `afterEach` — `vi.restoreAllMocks()`;
   - сценарии (spec.md раздел 10 «Frontend», пункты 1–7):
     1. Пустое состояние: список пуст → текст «Целей пока нет», `listGoals` вызван; если счетов нет → подсказка про «Накопления», кнопки «+ добавить» нет.
     2. Рендер списка: название, «Цель: … ₽» и «Накоплено: … ₽» в формате `money` (например, `100 000,00 ₽`), дата через `ruDate` (`31.12.2026`), строка «Откладывать: … ₽/мес» при `monthlyRequired`.
     3. `achieved = true` → бейдж «Цель достигнута», строки нормы нет.
     4. Создание: заполнить инлайн-форму (название, сумма, счёт, дата) → `createGoal` вызван с payload `{name, targetAmount: number, containerId, dueDate: 'yyyy-MM-dd'}`; success-тост «Цель добавлена»; форма сброшена; `listGoals` перезапущен.
     5. Клиентская валидация: пустое название → запрос не отправлен; сумма `0` или пустая → запрос не отправлен; не выбран счёт → запрос не отправлен (проверить по отсутствию вызова `createGoal` и/или error-тосту).
     6. Удаление: `vi.spyOn(window, 'confirm').mockReturnValue(true)` → `deleteGoal(id)` вызван, success-тост; `mockReturnValue(false)` → `deleteGoal` не вызван.
     7. Select счёта заполнен из `savingsService.listContainers` (фикстура: 2 счёта → 2 опции + плейсхолдер).
2. Запустить `cd frontend && npx ng test --include='**/goals.component.spec.ts'`, затем полный `npx ng test` — зелёный.

## Ограничения
- Менять можно только новый spec-файл (и, при необходимости, только свой spec); код приложения не править. Если тест выявляет баг компонента — зафиксировать в «Результате выполнения» и не чинить компонент.
- Паттерн тестов строго по `savings.component.spec.ts` (useValue-спай, `vi.spyOn` на `window.confirm`, ассерты по `textContent`/`querySelector`); без Karma/Jasmine — только vitest.
- Ассерты сумм — по точному формату `ru-RU` с запятой (`100 000,00`).

## Результат (критерий приёмки)
- [ ] `goals.component.spec.ts` содержит все 7 сценариев из spec.md (пункты 1–7).
- [ ] `npx ng test --include='**/goals.component.spec.ts'` — 0 провалов.
- [ ] Полный `npx ng test` (из `frontend/`) — 0 провалов (регрессия).

## Результат выполнения
_(заполнит manage-development после выполнения)_
- Статус:
- Что сделано:
- Итоги тестов:
