# BRIEF: frontend/src/app

## Назначение

Всё приложение: маршрутизация, корневой компонент, ядро (сервисы/модели), каркас (shell) и фичевые компоненты. Angular 22, signals, zoneless, standalone-компоненты, Tailwind 4.

## Дерево

```
app/
├── app.config.ts        # provideZonelessChangeDetection, provideRouter(routes), provideHttpClient(withInterceptors)
├── app.routes.ts        # /login (guestGuard); shell (authGuard) → '', transactions/new, transactions/:id/edit,
│                        #   stats|budget|savings (lazy loadComponent), categories, export, settings, more
├── app.ts / app.html    # корневой компонент: <router-outlet> + <app-toast>
├── core/                # «ядро», общие зависимости
│   ├── models.ts            # типы DTO (TransactionDto, CategoryDto, UserDto, ...), TxnKind
│   ├── auth.service.ts      # login/logout, current user, serverDate, токен в localStorage
│   ├── auth.guard.ts        # authGuard (нет токена → /login), guestGuard
│   ├── auth.interceptor.ts  # Bearer-заголовок + 401 → logout
│   ├── http-errors.ts       # ApiError.from(err): {code, message, errors}
│   ├── format.pipe.ts       # MoneyPipe, RuDatePipe, MonthTitlePipe
│   ├── toast.service.ts     # всплывающие уведомления
│   ├── transactions.service.ts / categories.service.ts (+flattenCategoryTree) /
│   ├── budget.service.ts / savings.service.ts / users.service.ts / export.service.ts
│   ├── last-category.service.ts  # запоминание последней категории (UX)
│   └── download.util.ts     # скачивание Blob (xlsx)
├── layout/
│   └── shell.component.ts/html/css   # сайдбар-навигация + <router-outlet>
└── features/            # каждая фича — папка с компонентом (ts + html + css) и spec
    ├── journal/           # журнал операций: фильтры, пагинация, сортировка, итоги (главная страница)
    ├── transaction-form/  # форма создания/редактирования операции
    ├── stats/             # аналитика (Chart.js: donut + line/bar) — lazy
    ├── budget/            # плановый бюджет: план/факт по категориям — lazy
    ├── savings/           # накопления: счета, записи, сводка 12 мес. — lazy
    ├── categories/        # CRUD категорий (дерево)
    ├── export/            # экспорт Excel (месяц/год)
    ├── settings/          # профиль: пароль, аккаунты семьи
    ├── login/             # вход
    └── more/              # «Ещё»: ссылки + выход
```

## Правила

- Компонент — 3 файла: `x.component.ts` + `x.component.html` + `x.component.css`; тест — `x.component.spec.ts` (vitest, TestBed, реальный шаблон).
- Состояние — signals; подписки RxJS завершать `takeUntilDestroyed`.
- HTTP — только через сервисы `core/`; ошибки — `ApiError.from(err)` + `ToastService`.
- Новый маршрут — в `app.routes.ts`; тяжёлые страницы — `loadComponent`.
- Типы ответов API — в `core/models.ts`, не дублировать в компонентах.
- Даты в API — `DateOnly` (строка `yyyy-MM-dd`); «сегодня» брать из `serverDate` (auth.service), не из `new Date()`.

## Навигация

- Выше: `../BRIEF.md` (src), `../../BRIEF.md` (frontend), `../../../BRIEF.md` (корень).
- Контракт API и его слои: `../../../backend/BRIEF.md`.
