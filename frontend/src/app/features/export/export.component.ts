import { CommonModule } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ExportService } from '../../core/export.service';
import { firstApiErrorMessage } from '../../core/http-errors';
import { ToastService } from '../../core/toast.service';
import { UsersService } from '../../core/users.service';
import { UserDto } from '../../core/models';

type Scope = 'month' | 'year';

@Component({
  selector: 'app-export',
  imports: [CommonModule, FormsModule],
  templateUrl: './export.component.html',
  styleUrl: './export.component.css',
})
export class ExportComponent {
  private readonly exportSvc = inject(ExportService);
  private readonly usersSvc = inject(UsersService);
  private readonly toasts = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly now = new Date();
  protected readonly scope = signal<Scope>('month');
  protected readonly month = signal(this.now.getMonth() + 1);
  protected readonly year = signal(this.now.getFullYear());
  protected readonly who = signal<number | null>(null);
  protected readonly users = signal<UserDto[]>([]);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly months = Array.from({ length: 12 }, (_, i) => i + 1);

  constructor() {
    this.usersSvc.list().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (list) => this.users.set(list),
    });
  }

  protected onWhoChange(v: unknown): void {
    this.who.set(typeof v === 'number' ? v : null);
  }

  protected monthName(m: number): string {
    const label = new Date(this.year(), m - 1, 1).toLocaleDateString('ru-RU', {
      month: 'long',
      year: 'numeric',
    });
    return label.charAt(0).toUpperCase() + label.slice(1);
  }

  protected download(): void {
    this.busy.set(true);
    this.error.set(null);
    const op = this.scope() === 'month'
      ? this.exportSvc.month(this.year(), this.month(), this.who() ?? undefined)
      : this.exportSvc.year(this.year(), this.who() ?? undefined);
    op.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.busy.set(false);
        this.toasts.success('Файл сохранён');
      },
       error: (err) => {
         this.busy.set(false);
         firstApiErrorMessage(err, 'Не удалось сформировать файл').then((msg) => {
           this.error.set(msg);
         });
       },
    });
  }
}
