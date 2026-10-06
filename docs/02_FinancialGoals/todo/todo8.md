# Todo 8: [corrective] ModelStateSanitizer — «пустое тело» в формате .NET 10

- Агент: backend-developer
- Зависит от: todo3
- Параллельно с: нет
- Статус: ☑ выполнено (2026-09-28)

## Контекст
При проверке todo3 тест `GoalsTests.Create_EmptyBody_Returns400_WithEmptyBodyMessage` (явный JSON `null`-тело, `Content-Type: application/json`) намеренно падает, фиксируя баг приложения, найденный тестировщиком:

- В .NET 10 model-binding для пустого/null-тела изменил формат ошибки: вместо пары `ключ "request"` + `"The request field is required."` (на который рассчитан `ModelStateSanitizer`, строки 24-29) приходит **ключ `""` (пустая строка) + сообщение `"A non-empty request body is required."`**.
- Из-за этого ветка `hadEmptyBody → EmptyBodyMessage` не срабатывает, и пользователь получает:
  `{"code":"ValidationFailed","message":"Некорректные данные","errors":{"":"A non-empty request body is required."}}`
  — английский текст фреймворка утекает в ответ.
- Нарушает контракт: spec.md §3 (сообщения на русском), §8 (пустое тело → 400 «Пустое тело запроса»), конвенция `backend/BudgetApi/BRIEF.md` («Пустое тело (request is null) → ApiErrorException.BadRequest("Пустое тело запроса")»).
- **Системность:** единый `InvalidModelStateResponseFactory` + `ModelStateSanitizer` используются всеми POST/PUT с телом (users, categories, transactions, budget, savings, goals) — баг затрагивает все эндпоинты, а не только goals. Проверено тестировщиком на `/api/goals` (null и пустое тело) и `/api/savings/containers` (null).

## Что сделать
1. Изменить `backend/BudgetApi/Common/ModelStateSanitizer.cs` — в `Sanitize` распознать новый формат .NET 10 «пустое тело»: модель-state-ошибка с сообщением `"A non-empty request body is required."` (независимо от ключа — в т.ч. `""`) → `hadEmptyBody = true`. Старый формат (ключ `"request"` + `"The request field is required."`) оставить — не ломать совместимость.
   - Важно: при `hadEmptyBody` в ответе не должно оставаться «сырых» ошибок (текущая логика: если `errors.Count > 0` — отдаёт errors; пустое тело должно дать `{"code":"ValidationFailed","message":"Пустое тело запроса"}` без `errors` — проверить, что новый формат не попадает в `errors`).
2. Прогнать `cd backend/BudgetApi.Tests && dotnet test` (полный набор) — **все 124 теста зелёные**, в т.ч. `GoalsTests.Create_EmptyBody_Returns400_WithEmptyBodyMessage` (ассерты: 400 + `ValidationFailed` + «Пустое тело запроса» — менять тест НЕ надо).

## Ограничения
- Править только `backend/BudgetApi/Common/ModelStateSanitizer.cs` (и, если потребуется, его тесты-компаньоны, если есть); тест `GoalsTests.Create_EmptyBody_Returns400_WithEmptyBodyMessage` не менять — он фиксирует контракт.
- Не менять `ExceptionHandlingMiddleware`, `Program.cs` (фабрика уже верная), контроллеры.
- Не трогать ветки `hadMalformedBody` / «сырых» полевых ошибок — они работают.

## Результат (критерий приёмки)
- [ ] `POST /api/goals` (и любой другой POST с телом) с пустым/null-телом отвечает `400 {"code":"ValidationFailed","message":"Пустое тело запроса"}` — без английского текста и без ключа `""` в `errors`.
- [ ] `dotnet test` (полный набор, 124+ теста) — 0 провалов.

## Результат выполнения
_(заполнит manage-development после выполнения)_
- Статус: выполнено
- Что сделано:
  - Изменён `backend/BudgetApi/Common/ModelStateSanitizer.cs` (`Sanitize`): в начале внутреннего цикла по ошибкам ModelState — распознавание нового формата .NET 10: сообщение `"A non-empty request body is required."` (любой ключ, в т.ч. `""`) → `hadEmptyBody = true` + `continue` (английское сообщение не попадает в `errors`). Старый формат (ключ `"request"` + `"The request field is required."`) сохранён; остальные ветки, middleware, фабрика, контроллеры и тесты не тронуты.
- Итоги тестов: `dotnet test` (полный набор) — **124 / 124 Passed, 0 Failed**; целевой тест `Create_EmptyBody_Returns400_WithEmptyBodyMessage` — зелёный (400 + `ValidationFailed` + «Пустое тело запроса»).
