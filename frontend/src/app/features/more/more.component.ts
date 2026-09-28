import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-more',
  imports: [RouterLink],
  templateUrl: './more.component.html',
  styleUrl: './more.component.css',
})
export class MoreComponent {
  protected readonly auth = inject(AuthService);

  protected logout(): void {
    this.auth.logout();
  }
}
