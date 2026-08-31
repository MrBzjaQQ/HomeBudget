import { ApiError } from './models';

describe('ApiError', () => {
  it('extracts code and message from HttpErrorResponse-like object', () => {
    const err = ApiError.from({
      status: 409,
      error: { code: 'CONFLICT', message: 'Уже существует' },
    });
    expect(err.status).toBe(409);
    expect(err.code).toBe('CONFLICT');
    expect(err.message).toBe('Уже существует');
  });

  it('falls back to defaults for unknown errors', () => {
    const err = ApiError.from(new TypeError('boom'));
    expect(err.status).toBe(0);
    expect(err.code).toBe('UNKNOWN');
  });

  it('passes through existing ApiError', () => {
    const original = new ApiError(404, 'NOT_FOUND', 'Не найдено');
    expect(ApiError.from(original)).toBe(original);
  });
});
