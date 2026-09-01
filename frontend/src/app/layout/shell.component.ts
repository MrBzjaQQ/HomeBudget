import { NgSwitch, NgSwitchCase } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth.service';

interface NavItem {
  path: string;
  label: string;
  icon: string;
}

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, NgSwitch, NgSwitchCase],
  template: `
    <div class="min-h-dvh bg-gray-50">
      <header class="sticky top-0 z-30 border-b border-gray-200 bg-white/95 backdrop-blur">
        <div class="mx-auto flex h-14 max-w-5xl items-center px-4">
          <a routerLink="/" class="text-lg font-bold text-brand">Бюджет</a>
          <nav class="ml-4 hidden items-center gap-1 md:flex" aria-label="Основная навигация">
            <a routerLink="/" routerLinkActive="nav-link-active" [routerLinkActiveOptions]="{ exact: true }" class="nav-link">
              Журнал
            </a>
            <a routerLink="/stats" routerLinkActive="nav-link-active" class="nav-link">Аналитика</a>
            <a routerLink="/categories" routerLinkActive="nav-link-active" class="nav-link">Категории</a>
            <a routerLink="/export" routerLinkActive="nav-link-active" class="nav-link">Экспорт</a>
            <a routerLink="/settings" routerLinkActive="nav-link-active" class="nav-link">Настройки</a>
          </nav>
          <div class="ml-auto flex items-center gap-2 md:gap-3">
            <a routerLink="/transactions/new" class="btn-new-op hidden md:inline-flex">+ Операция</a>
            <span class="max-w-28 truncate text-sm text-gray-500">{{ auth.user()?.displayName }}</span>
            <button type="button" class="btn-logout" title="Выйти" (click)="logout()">Выйти</button>
          </div>
        </div>
      </header>

      <main class="mx-auto max-w-5xl px-0 pb-24 pt-4 md:px-4 md:pb-8">
        <router-outlet />
      </main>

      <nav
        class="fixed inset-x-0 bottom-0 z-40 border-t border-gray-200 bg-white pb-[env(safe-area-inset-bottom)] md:hidden"
      >
        <div class="grid grid-cols-5">
          @for (item of nav; track item.path) {
            <a
              [routerLink]="item.path"
              routerLinkActive="nav-active"
              [class]="item.path === '/transactions/new' ? 'nav-plus' : 'nav-item'"
              [attr.aria-label]="item.label"
            >
              @if (item.path === '/transactions/new') {
                <span class="plus-circle">
                  <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                    <path d="M12 5v14M5 12h14" stroke-linecap="round" />
                  </svg>
                </span>
              } @else {
                <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                  <ng-container [ngSwitch]="item.icon">
                    <path
                      *ngSwitchCase="'home'"
                      d="M3 10.5 12 3l9 7.5M5 9.5V21h5v-6h4v6h5V9.5"
                      stroke-linecap="round"
                      stroke-linejoin="round"
                    />
                    <path
                      *ngSwitchCase="'chart'"
                      d="M4 20V10M10 20V4M16 20v-7M20 20H3"
                      stroke-linecap="round"
                    />
                    <path
                      *ngSwitchCase="'folder'"
                      d="M3 7a2 2 0 0 1 2-2h4l2 2h8a2 2 0 0 1 2 2v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V7Z"
                      stroke-linejoin="round"
                    />
                    <path
                      *ngSwitchCase="'more'"
                      d="M5 12h.01M12 12h.01M19 12h.01"
                      stroke-linecap="round"
                      stroke-width="3"
                    />
                  </ng-container>
                </svg>
                <span class="text-[10px]">{{ item.label }}</span>
              }
            </a>
          }
        </div>
      </nav>
    </div>
  `,
  styles: `
    .nav-item {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 2px;
      padding: 8px 0 6px;
      color: #6b7280;
    }
    .nav-plus {
      display: flex;
      align-items: center;
      justify-content: center;
      padding-top: 6px;
    }
    .plus-circle {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 48px;
      height: 48px;
      margin-top: -20px;
      border-radius: 9999px;
      background: #0f766e;
      color: white;
      box-shadow: 0 4px 12px rgb(15 118 110 / 0.4);
    }
    .nav-active {
      color: #0f766e !important;
      font-weight: 600;
    }
    .nav-link {
      border-radius: 9999px;
      padding: 6px 14px;
      font-size: 14px;
      font-weight: 500;
      color: #4b5563;
    }
    .nav-link:hover {
      background: #f3f4f6;
      color: #111827;
    }
    .nav-link.nav-link-active {
      background: #e6f4f2;
    }
    .btn-new-op {
      border-radius: 9999px;
      background: #0f766e;
      color: white;
      font-size: 13px;
      font-weight: 600;
      padding: 7px 14px;
    }
    .btn-new-op:hover {
      background: #0d5f59;
    }
    .btn-logout {
      cursor: pointer;
      border-radius: 9999px;
      border: 1px solid #e5e7eb;
      background: white;
      color: #6b7280;
      font-size: 12px;
      font-weight: 600;
      padding: 5px 12px;
      transition: background-color 0.15s ease, border-color 0.15s ease, color 0.15s ease;
    }
    .btn-logout:hover {
      background: #fef2f2;
      border-color: #fecaca;
      color: #b91c1c;
    }
  `,
})
export class ShellComponent {
  protected readonly auth = inject(AuthService);

  protected logout(): void {
    this.auth.logout();
  }

  protected readonly nav: NavItem[] = [
    { path: '/', label: 'Журнал', icon: 'home' },
    { path: '/stats', label: 'Аналитика', icon: 'chart' },
    { path: '/transactions/new', label: 'Добавить', icon: '' },
    { path: '/categories', label: 'Категории', icon: 'folder' },
    { path: '/more', label: 'Ещё', icon: 'more' },
  ];
}
