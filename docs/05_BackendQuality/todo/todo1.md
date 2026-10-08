# Todo 1: Разнести DTO аутентификации, пользователей и категорий

- Агент: backend-developer
- Зависит от: нет
- Параллельно с: todo2, todo3, todo4, todo5
- Статус: ☑ выполнено

## Контекст
Реализует FR-1 и FR-2 (см. `spec.md`, разделы 1, 7 и 8): один DTO-тип на файл, не наследуемые DTO-record сделать sealed, публичную форму сохранить.

## Что сделать
1. Перенести типы `LoginRequest`, `LoginResponse`, `MeResponse` из `backend/Budget.Application/Auth/AuthDtos.cs` в отдельные файлы в `Auth/`.
2. Перенести `UserDto`, `CreateUserRequest`, `UpdateUserRequest` из `Users/UserDtos.cs` в индивидуальные файлы; mapper `UserDtos` оставить в отдельном `Users/UserDtos.cs`.
3. Перенести `CategoryTreeDto`, `CreateCategoryRequest`, `UpdateCategoryRequest` из `Categories/CategoryDtos.cs` в индивидуальные файлы.
4. Все DTO, являющиеся record и не имеющие производных record, объявить `sealed`; проверить и обновить все ссылки/using.
5. Удалить старые файлы-мульти-типы после переноса. Сборка проекта: `dotnet build` из `backend/Budget.Application`.

## Ограничения
- Не менять публичные типы/сигнатуры, свойства, nullability и JSON-контракт.
- Не добавлять XML-документацию в этой подзадаче; она выполняется отдельным todo.
- Не перемещать mapper `UserDtos` в DTO-файл и не менять его поведение.

## Результат (критерий приёмки)
- [ ] Каждый перечисленный DTO/class/record находится в отдельном файле, новые DTO-record sealed при отсутствии наследников.
- [ ] Application собирается; полный backend test suite запускается после слияния всех веток рефакторинга.

## Результат выполнения
_(заполнит manage-development после выполнения)_
- Статус: выполнено
- Что сделано: DTO Auth, Users и Categories перенесены по одному типу на файл; DTO-record sealed; `UserDtos` оставлен отдельным mapper-файлом.
- Итоги тестов: `dotnet build` Budget.Application — успешно (0 предупреждений, 0 ошибок); `dotnet test` BudgetApi.Tests — 135/135 пройдено.
