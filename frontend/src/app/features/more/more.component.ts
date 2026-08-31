import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-more',
  imports: [RouterLink],
  template: `
    <div class="space-y-3">
      <div class="card">
        <h1 class="text-lg font-bold text-gray-900">Ещё</h1>
      </div>

      <nav class="card divide-y divide-gray-100 p-0">
        <a routerLink="/export" class="nav-row">
          <span class="nav-ico">⬇</span>
          <span class="flex-1">
            <span class="block text-sm font-medium text-gray-900">Экспорт в Excel</span>
            <span class="block text-xs text-gray-400">месяц или год, вся семья или кто-то один</span>
          </span>
          <span class="text-gray-300">›</span>
        </a>
        <a routerLink="/categories" class="nav-row">
          <span class="nav-ico">🗂</span>
          <span class="flex-1">
            <span class="block text-sm font-medium text-gray-900">Категории</span>
            <span class="block text-xs text-gray-400">группы, подкатегории, архив</span>
          </span>
          <span class="text-gray-300">›</span>
        </a>
        <a routerLink="/settings" class="nav-row">
          <span class="nav-ico">⚙</span>
          <span class="flex-1">
            <span class="block text-sm font-medium text-gray-900">Настройки</span>
            <span class="block text-xs text-gray-400">пароль, аккаунты семьи</span>
          </span>
          <span class="text-gray-300">›</span>
        </a>
      </nav>

      <button type="button" class="btn-logout w-full" (click)="logout()">Выйти</button>
    </div>
  `,
  styles: `
    .card {
      background: white;
      border: 1px solid #f3f4f6;
      border-radius: 14px;
      padding: 16px;
      box-shadow: 0 1px 2px rgb(0 0 0 / 0.04);
    }
    .nav-row {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 13px 16px;
    }
    .nav-row:active {
      background: #f9fafb;
    }
    .nav-ico {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 36px;
      height: 36px;
      border-radius: 10px;
      background: #f0fdfa;
      font-size: 16px;
    }
    .btn-logout {
      border-radius: 12px;
      border: 1px solid #fecaca;
      background: #fef2f2;
      color: #b91c1c;
      font-weight: 600;
      font-size: 15px;
      padding: 13px;
    }
  `,
})
export class MoreComponent {
  protected readonly auth = inject(AuthService);

  protected logout(): void {
    this.auth.logout();
  }
}
