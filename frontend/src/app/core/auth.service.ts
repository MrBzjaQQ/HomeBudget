import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { LoginResponse, MeResponse, UserDto } from './models';

const TOKEN_KEY = 'budget.token';
const USER_KEY = 'budget.user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  readonly token = signal<string | null>(localStorage.getItem(TOKEN_KEY));
  readonly user = signal<UserDto | null>(readStoredUser());
  readonly serverDate = signal<string | null>(null);
  readonly isLoggedIn = computed(() => this.token() !== null);

  login(username: string, password: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>('/api/auth/login', { username, password })
      .pipe(
        tap((res) => {
          localStorage.setItem(TOKEN_KEY, res.token);
          localStorage.setItem(USER_KEY, JSON.stringify(res.user));
          this.token.set(res.token);
          this.user.set(res.user);
          this.serverDate.set(res.serverDate ?? null);
        }),
      );
  }

  refreshMe(): Observable<MeResponse> {
    return this.http.get<MeResponse>('/api/auth/me').pipe(
      tap((res) => {
        if (res && res.user) {
          localStorage.setItem(USER_KEY, JSON.stringify(res.user));
          this.user.set(res.user);
        }
        this.serverDate.set(res?.serverDate ?? null);
      }),
    );
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this.token.set(null);
    this.user.set(null);
    this.serverDate.set(null);
    this.router.navigate(['/login']);
  }

  handleUnauthorized(): void {
    if (this.token()) {
      localStorage.removeItem(TOKEN_KEY);
      localStorage.removeItem(USER_KEY);
      this.token.set(null);
      this.user.set(null);
    }
    this.serverDate.set(null);
    this.router.navigate(['/login']);
  }
}

function readStoredUser(): UserDto | null {
  const raw = localStorage.getItem(USER_KEY);
  if (!raw) {
    return null;
  }
  try {
    return JSON.parse(raw) as UserDto;
  } catch {
    return null;
  }
}
