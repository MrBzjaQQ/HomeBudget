# BRIEF: frontend

## Назначение

Angular SPA «Домашний бюджет». Angular 22: signals + zoneless change detection, standalone-компоненты, Tailwind CSS 4, Chart.js. Тесты — **vitest** (builder `@angular/build:unit-test`), не Karma/Jasmine.

## Дерево

```
frontend/
├── angular.json            # build/serve: @angular/build:application; test: @angular/build:unit-test
│                           #   (setupFiles: src/test-setup.ts); production-бюджеты
├── package.json
├── proxy.conf.json         # dev-прокси /api → http://localhost:5080 (ПОРТ ОБЯЗАТЕЛЕН)
├── tsconfig*.json          # strict; .editorconfig: 2 пробела, одинарные кавычки
├── public/                 # статика (иконки и т.п.)
└── src/                    # см. src/BRIEF.md
    ├── main.ts             # bootstrapApplication(App, appConfig)
    ├── index.html
    ├── styles.css          # глобальные стили + Tailwind
    ├── test-setup.ts       # подмена localStorage для vitest
    └── app/                # см. src/app/BRIEF.md
```

## Команды

```bash
npm install
npm start                  # dev-сервер :4200, прокси /api → :5080
npm run build              # production-сборка → dist/budget-web/browser
npx ng test                # все vitest-тесты
npx ng test --include='**/app.spec.ts'   # один файл
```

## Правила

- Компонент = 3 файла: `*.component.ts` (логика) + `*.component.html` (шаблон) + `*.component.css` (стили, если есть); в декораторе `templateUrl`/`styleUrl`.
- Стили компонентов — вложенные (encapsulated) CSS + Tailwind-классы в шаблонах.
- Сигналы для состояния; HTTP — через сервисы в `src/app/core` (HttpClient + интерцептор JWT).
- Формат денег/дат — пайпы `MoneyPipe`, `RuDatePipe`, `MonthTitlePipe` (core/format.pipe.ts).
- Ошибки API — `ApiError.from(err)` (core/http-errors.ts); тосты — `ToastService`.
- Тесты компонента — рядом: `*.component.spec.ts`; рендерят реальный шаблон (TestBed), сервисы подменяются useValue-спаями.

## Навигация

- Выше: `../BRIEF.md` (корень проекта).
- Ниже: `src/BRIEF.md` (структура src), `src/app/BRIEF.md` (модули и компоненты).
- Контракт API — `../backend/BRIEF.md`.
