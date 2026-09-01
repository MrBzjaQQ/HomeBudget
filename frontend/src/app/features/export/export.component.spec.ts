import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ExportComponent } from './export.component';

type ExportCtrl = { download(): void };

describe('ExportComponent', () => {
  let fixture: import('@angular/core/testing').ComponentFixture<ExportComponent>;
  let http: HttpTestingController;
  let ctrl: ExportCtrl;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ExportComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    fixture = TestBed.createComponent(ExportComponent);
    ctrl = fixture.componentInstance as unknown as ExportCtrl;
    http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/users').flush([]);
  });

  afterEach(() => http.verify());

  it('shows the server message from the 400 envelope in the inline error box', async () => {
    fixture.detectChanges();
    ctrl.download();

    const req = http.expectOne((r) => r.url.startsWith('/api/export/'));
    const body = new Blob([JSON.stringify({ code: 'ValidationFailed', message: 'Некорректный год' })], {
      type: 'application/json',
    });
    req.flush(body, { status: 400, statusText: 'Bad Request' });
    await new Promise((r) => setTimeout(r, 0));
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Некорректный год');
    expect(el.textContent).not.toContain('Неизвестная ошибка');
  });

  it('shows the fallback message when the envelope is absent (network error)', async () => {
    fixture.detectChanges();
    ctrl.download();

    const req = http.expectOne((r) => r.url.startsWith('/api/export/'));
    const html = new Blob(['<html><body>502 gateway timeout</body></html>'], { type: 'text/html' });
    req.flush(html, { status: 502, statusText: 'Bad Gateway' });
    await new Promise((r) => setTimeout(r, 0));
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Не удалось сформировать файл');
  });
});
