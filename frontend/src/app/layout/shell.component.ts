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
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.css',
})
export class ShellComponent {
  protected readonly auth = inject(AuthService);

  protected logout(): void {
    this.auth.logout();
  }

  protected readonly nav: NavItem[] = [
    { path: '/', label: 'Журнал', icon: 'home' },
    { path: '/budget', label: 'Бюджет', icon: 'calendar' },
    { path: '/transactions/new', label: 'Добавить', icon: '' },
    { path: '/stats', label: 'Аналитика', icon: 'chart' },
    { path: '/more', label: 'Ещё', icon: 'more' },
  ];
}
