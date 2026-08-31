import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth.guard';
import { LoginComponent } from './features/login/login.component';
import { JournalComponent } from './features/journal/journal.component';
import { TransactionFormComponent } from './features/transaction-form/transaction-form.component';
import { CategoriesComponent } from './features/categories/categories.component';
import { ExportComponent } from './features/export/export.component';
import { SettingsComponent } from './features/settings/settings.component';
import { MoreComponent } from './features/more/more.component';
import { ShellComponent } from './layout/shell.component';

export const routes: Routes = [
  {
    path: 'login',
    component: LoginComponent,
    canActivate: [guestGuard],
    title: 'Вход — Бюджет',
  },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', component: JournalComponent, title: 'Журнал — Бюджет' },
      {
        path: 'transactions/new',
        component: TransactionFormComponent,
        title: 'Новая операция — Бюджет',
      },
      {
        path: 'transactions/:id/edit',
        component: TransactionFormComponent,
        title: 'Редактирование — Бюджет',
      },
      {
        path: 'stats',
        loadComponent: () =>
          import('./features/stats/stats.component').then((m) => m.StatsComponent),
        title: 'Аналитика — Бюджет',
      },
      { path: 'categories', component: CategoriesComponent, title: 'Категории — Бюджет' },
      { path: 'export', component: ExportComponent, title: 'Экспорт — Бюджет' },
      { path: 'settings', component: SettingsComponent, title: 'Настройки — Бюджет' },
      { path: 'more', component: MoreComponent, title: 'Ещё — Бюджет' },
    ],
  },
  { path: '**', redirectTo: '' },
];
