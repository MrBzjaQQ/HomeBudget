# BRIEF: frontend/src

## Назначение

Корень исходников Angular-приложения. Точка входа — `main.ts` → `App` (app/app.ts). Глобальные стили — `styles.css` (Tailwind 4). Тестовый setup — `test-setup.ts` (in-memory localStorage для vitest).

## Дерево

```
src/
├── main.ts           # bootstrapApplication(App, appConfig)
├── index.html        # <app-root>
├── styles.css        # @import "tailwindcss" + глобальные классы/переменные
├── test-setup.ts     # setupFiles для @angular/build:unit-test
└── app/              # всё приложение — см. app/BRIEF.md
    ├── app.config.ts     # provideZonelessChangeDetection, provideRouter, provideHttpClient
    ├── app.routes.ts     # маршруты (login публичный, остальное — authGuard)
    ├── app.ts/html       # корневой компонент (toasts + router-outlet)
    ├── core/             # сервисы, гварды, пайпы, модели (см. app/BRIEF.md)
    ├── layout/           # shell (сайдбар + outlet)
    └── features/         # фичевые компоненты: journal, transaction-form, stats, budget,
                          # savings, categories, export, settings, login, more
```

## Правила

- В `src` кроме `app/` живут только точки входа и глобальное — новое в `app/`.
- Тесты — vitest; файл тестов рядом с тестируемым (`*.spec.ts`).
- `test-setup.ts` подключается в angular.json → не удалять и не рендерить в прод.

## Навигация

- Выше: `../BRIEF.md` (frontend), `../../BRIEF.md` (корень).
- Ниже: `app/BRIEF.md`.
