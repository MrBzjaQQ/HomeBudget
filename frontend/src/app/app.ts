import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth.service';
import { ToastService } from './core/toast.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
})
export class App {
  protected readonly toasts = inject(ToastService);

  constructor() {
    const auth = inject(AuthService);
    if (auth.isLoggedIn()) {
      auth.refreshMe().subscribe({ error: () => {} });
    }
  }
}
