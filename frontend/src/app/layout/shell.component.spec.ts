import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { UserDto } from '../core/models';
import { ShellComponent } from './shell.component';

@Component({ template: '' })
class StubComponent {}

const admin: UserDto = { id: 1, username: 'admin', displayName: 'Админ', isActive: true, createdAt: '2026-01-01T00:00:00' };

describe('ShellComponent', () => {
  let logoutSpy: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    logoutSpy = vi.fn();
    await TestBed.configureTestingModule({
      imports: [ShellComponent],
      providers: [
        provideRouter([{ path: '**', component: StubComponent }]),
        { provide: AuthService, useValue: { user: () => admin, logout: logoutSpy } },
      ],
    });
  });

  function logoutButton(): HTMLButtonElement {
    const fixture = TestBed.createComponent(ShellComponent);
    fixture.detectChanges();
    const btn = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('button')).find(
      (b) => b.textContent?.trim() === 'Выйти',
    );
    expect(btn, 'header must contain a «Выйти» button').toBeTruthy();
    return btn as HTMLButtonElement;
  }

  it('has a logout button in the header', () => {
    logoutButton();
  });

  it('logs out when the header logout button is clicked', () => {
    const btn = logoutButton();
    btn.click();
    expect(logoutSpy).toHaveBeenCalledTimes(1);
  });
});
