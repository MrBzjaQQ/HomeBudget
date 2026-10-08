# Todo 10: Финальная сборка и проверка контейнера

- Агент: менеджер / ui-tester
- Зависит от: todo1, todo2, todo3, todo4, todo5, todo6, todo7, todo8, todo9
- Параллельно с: нет
- Статус: ☑ выполнено

## Контекст
Финальная проверка FR-9/FR-10 после завершения всех backend изменений. Docker Compose работает из корня репозитория; UI-сценарии и ограничения описаны в `spec.md`, разделы 3, 5 и 10.

## Что сделать
1. Из корня репозитория выполнить `docker compose up -d --build` с локально установленными `POSTGRES_PASSWORD` и `JWT_KEY`; не выводить их в отчёт.
2. Убедиться, что app и postgres контейнеры работают/healthy, приложение отвечает на `http://localhost:8080`, seed-учётная запись `admin/admin123` позволяет войти, SPA и API-backed раздел открываются без ошибок.
3. Запустить полный backend `dotnet test` из `backend/BudgetApi.Tests` и записать passed/total; выполнить backend build без XML warnings и проверить модель EF на незапланированную миграцию.
4. При проблемах зарегистрировать точную corrective-задачу, не обновлять master-spec и не считать acceptance пройденным.
5. После успешной приёмки предоставить UI verdict `pass` с краткими сценариями; только затем менеджер делегирует master-spec update.

## Ограничения
- Не запускать API, frontend или БД отдельными процессами.
- Не выполнять `docker compose down -v` и не удалять данные/volumes.
- Не обновлять master-spec без успешного UI pass и подтверждения всех предыдущих todo.

## Результат (критерий приёмки)
- [ ] Docker Compose пересобран и сервисы работают; `http://localhost:8080` и базовые пользовательские сценарии пройдены.
- [ ] Backend тесты и сборка зелёные, итоги зафиксированы; нет непредусмотренного изменения EF-схемы.
- [ ] Зафиксирован UI verdict и сведения, необходимые для передачи `master-spec-updater`.

## Результат выполнения
_(заполнит manage-development после выполнения)_
- Статус: выполнено
- Что сделано: `docker compose up -d --build` выполнен из корня; app доступен на localhost:8080 (HTTP 200), оба контейнера работают, PostgreSQL healthy. UI проверил вход, журнал и категории; тесты и EF model check прошли.
- Итоги backend тестов/build: `dotnet test` — 138/138 passed; Domain/Application/Infrastructure/API build — 0 warnings/errors; EF: no pending model changes (EF tools 10.0.5 < runtime 10.0.11).
- UI verdict: PASS — SPA, вход admin/admin123, журнал (пустое состояние), навигация и категории (200 OK), после авторизации ошибок UI/network нет.
