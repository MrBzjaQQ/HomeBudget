import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { AuthService } from './auth.service';
import { authGuard } from './auth.guard';

@Component({ template: '<p>stub</p>' })
class StubComponent {}

describe('authGuard', () => {
  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: '', component: StubComponent, canActivate: [authGuard] },
          { path: 'login', component: StubComponent },
        ]),
      ],
    });
  });

  it('allows access when token is present', async () => {
    localStorage.setItem('budget.token', 'abc');
    const router = TestBed.inject(Router);
    await router.navigateByUrl('/');
    expect(router.url).toBe('/');
  });

  it('redirects to /login when not authenticated', async () => {
    const router = TestBed.inject(Router);
    await router.navigateByUrl('/');
    expect(router.url).toBe('/login');
  });
});

describe('AuthService', () => {
  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: '', component: StubComponent },
          { path: 'login', component: StubComponent },
        ]),
      ],
    });
  });

  it('is provided in the root', () => {
    expect(TestBed.inject(AuthService)).toBeTruthy();
  });

  it('logout clears token and user', () => {
    const auth = TestBed.inject(AuthService);
    auth.token.set('abc');
    auth.logout();
    expect(auth.token()).toBeNull();
    expect(auth.isLoggedIn()).toBe(false);
    expect(localStorage.getItem('budget.token')).toBeNull();
  });
});
