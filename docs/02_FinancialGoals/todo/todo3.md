# Todo 3: Backend-тесты GoalsTests.cs + сценарий 409 в SavingsTests.cs

- Агент: backend-tests-writer
- Зависит от: todo2
- Параллельно с: нет
- Статус: ☑ выполнено (тесты готовы; 1 тест намеренно фиксирует баг приложения → corrective todo8)

## Контекст
Тесты фичи «Финансовые цели» (spec.md, раздел 10 «Backend»). Код уже написан в todo1–todo2: `GET/POST /api/goals`, `DELETE /api/goals/{id}`, расчёт `saved`/`monthlyRequired`/`achieved`, 409 при удалении счёта с целью. Писать только тесты, приложение не трогать.

## Что сделать
1. Создать `backend/BudgetApi.Tests/GoalsTests.cs` (xUnit, `ApiTestBase` — как `SavingsTests.cs`: логин admin/admin123, `SendAsync`, `TestAsserts.HasError`). Обязательные сценарии (spec.md раздел 10, пункты 1–14):
   1. Пустой список → 200, пустой массив.
   2. Создание → 201; цель видна в `GET /api/goals` с `containerName`, цветом счёта, `saved = 0` (записей нет), `achieved = false`.
   3. `name` пустой (и пробельный) → 400, `errors.name`; `name` > 100 символов → 400, `errors.name`.
   4. `targetAmount` = 0 → 400 `errors.targetAmount`; отрицательный → 400; > 999 999 999 999.99 → 400.
   5. `containerId` несуществующий → 400, `errors.containerId` «Счёт не найден».
   6. `dueDate` = `2026-13-01` → 400 `errors.dueDate`; `01-01-2026` → 400; пустая строка `""` → 400; валидная `2026-12-31` → 201, в ответе `dueDate = "2026-12-31"`.
   7. Создание без `dueDate` → 201, `dueDate = null`, `monthlyRequired = null`.
   8. «Накоплено»: второй пользователь (паттерн `CreateMemberAsync` из `SavingsTests.cs:12-17`) + записи накоплений (POST `/api/savings`) за счёт цели за прошлый месяц (двое по счёту) → `saved` = сумма записей этого месяца; запись за более ранний месяц НЕ меняет остаток (считается только последний месяц с записями); запись за **будущий** месяц НЕ учитывается в `saved`.
   9. Норма: (а) делится ровно — `targetAmount − saved` кратно месяцам; (б) не делится — округление вверх до рубля (проверить точное значение); (в) дата = текущий месяц → `monthlyRequired = targetAmount − saved` (месяцев = 1). Текущий месяц брать из ответа сервера (login `serverDate`), тесты детерминированными.
   10. Цель достигнута: `saved ≥ targetAmount` → `achieved = true`, `monthlyRequired = null`.
   11. Дата в прошлом → `monthlyRequired = null`, `achieved = false`.
   12. Удаление → 204; цель исчезает из списка; связанный счёт и записи накоплений на месте (`GET /api/savings/containers`, `GET /api/savings?month=`).
   13. Удаление несуществующей цели → 404, `code = "NotFound"`.
   14. Запрос без токена → 401.
2. Изменить `backend/BudgetApi.Tests/SavingsTests.cs` — добавить сценарий (spec.md раздел 10, пункт 15): создать цель на существующий счёт (POST `/api/goals`), затем `DELETE /api/savings/containers/{id}` → 409, `code = "Conflict"`, в `message` про финансовые цели.
3. Запустить `cd backend/BudgetApi.Tests && dotnet test` — весь набор (включая регрессию) зелёный.

## Ограничения
- Менять можно только файлы тестов; код приложения (Domain/Application/Infrastructure/BudgetApi) не править. Если тест выявляет баг в приложении — зафиксировать в «Результате выполнения» и не чинить приложение.
- Тесты не требуют запущенной БД (InMemory, `ApiTestBase`); дата «сегодня» — серверная локальная (`Validation.Today()`), детерминированность — через месяцы в прошлом/будущем относительно `serverDate` из логина.
- Стили тестов — по `SavingsTests.cs` (имена методов `Subject_Action_Expected`, хелперы создания пользователя/счёта).

## Результат (критерий приёмки)
- [ ] `GoalsTests.cs` содержит все 14 сценариев из spec.md (пункты 1–14).
- [ ] `SavingsTests.cs` содержит сценарий удаления счёта с целью → 409.
- [ ] `dotnet test` (из `backend/BudgetApi.Tests`) — 0 падений, 0 провалов (полный набор).

## Результат выполнения
_(заполнит manage-development после выполнения)_
- Статус: выполнено (кроме «0 провалов» — заблокировано багом приложения, corrective todo8)
- Что сделано:
  - `GoalsTests.cs`: все 14 сценариев spec.md §10 покрыты (20 тестов; существовали с todo2, проверены на соответствие).
  - `SavingsTests.cs`: сценарий 15 `Container_Delete_WithGoal_Returns409` (строки 211-224) — на месте.
  - Исправлен тест `Create_EmptyBody_Returns400_WithEmptyBodyMessage`: вместо POST без Content-Type (давал 415) — явный JSON `null`-тело с `application/json`; ассерты по контракту: 400 + `ValidationFailed` + «Пустое тело запроса».
- Итоги тестов: `dotnet test` — 123 / 124, 1 провал: `GoalsTests.Create_EmptyBody_Returns400_WithEmptyBodyMessage` — **намеренно падает**, фиксирует баг приложения: `ModelStateSanitizer` (`backend/BudgetApi/Common/ModelStateSanitizer.cs`) не распознаёт новый формат .NET 10 пустого тела (ключ `""` + `"A non-empty request body is required."`) → в ответ утекает английское сообщение, вместо «Пустое тело запроса». Фикс — todo8.
