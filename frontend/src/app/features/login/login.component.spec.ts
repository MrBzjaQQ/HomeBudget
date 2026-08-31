import { Component } from '@angular/core';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { Router, provideRouter } from '@angular/router';
import { FormGroup } from '@angular/forms';
import { LoginComponent } from './login.component';

@Component({ template: '<p>home</p>' })
class StubHomeComponent {}

type LoginCtrl = { form: FormGroup; submit(): void };

describe('LoginComponent', () => {
  let fixture: import('@angular/core/testing').ComponentFixture<LoginComponent>;
  let http: HttpTestingController;
  let ctrl: LoginCtrl;

  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([
          { path: 'login', component: LoginComponent },
          { path: '', component: StubHomeComponent },
          { path: '**', component: StubHomeComponent },
        ]),
      ],
    });
    fixture = TestBed.createComponent(LoginComponent);
    ctrl = fixture.componentInstance as unknown as LoginCtrl;
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('shows error on wrong credentials (401)', () => {
    ctrl.form.patchValue({ username: 'admin', password: 'wrong' });
    fixture.detectChanges();
    ctrl.submit();

    const req = http.expectOne('/api/auth/login');
    expect(req.request.method).toBe('POST');
    req.flush({ code: 'UNAUTHORIZED', message: 'Неверный логин или пароль' }, { status: 401, statusText: 'Unauthorized' });
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Неверный логин или пароль');
  });

  it('stores token and navigates to home on success', () => {
    const router = TestBed.inject(Router);
    ctrl.form.patchValue({ username: 'admin', password: 'admin123' });
    fixture.detectChanges();
    ctrl.submit();

    const req = http.expectOne('/api/auth/login');
    req.flush(
      {
        token: 'jwt-token',
        user: { id: 1, username: 'admin', displayName: 'Админ', isActive: true, createdAt: '2026-01-01T00:00:00' },
      },
      { status: 200, statusText: 'OK' },
    );
    fixture.detectChanges();

    expect(localStorage.getItem('budget.token')).toBe('jwt-token');
    expect(router.url).toBe('/');
  });

  it('does not send request when form is invalid', () => {
    fixture.detectChanges();
    ctrl.submit();
    http.verify();
  });
});
