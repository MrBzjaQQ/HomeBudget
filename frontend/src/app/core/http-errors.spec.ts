import { firstApiErrorMessage } from './http-errors';

describe('firstApiErrorMessage', () => {
  it('prefers the first field message from errors (string value, backend format)', async () => {
    const err = {
      status: 400,
      error: {
        code: 'ValidationFailed',
        message: 'Некорректные данные',
        errors: { username: 'Такой логин уже занят' },
      },
    };
    expect(await firstApiErrorMessage(err, 'fallback')).toBe('Такой логин уже занят');
  });

  it('supports array field values too', async () => {
    const err = {
      status: 400,
      error: {
        code: 'ValidationFailed',
        message: 'Некорректные данные',
        errors: { username: ['Такой логин уже занят'] },
      },
    };
    expect(await firstApiErrorMessage(err, 'fallback')).toBe('Такой логин уже занят');
  });

  it('uses the envelope message when there are no field errors', async () => {
    const err = { status: 400, error: { code: 'ValidationFailed', message: 'Некорректный год' } };
    expect(await firstApiErrorMessage(err, 'fallback')).toBe('Некорректный год');
  });

  it('parses a Blob body (export endpoints return files)', async () => {
    const body = new Blob([JSON.stringify({ code: 'ValidationFailed', message: 'Некорректный год' })], {
      type: 'application/json',
    });
    expect(await firstApiErrorMessage({ status: 400, error: body }, 'fallback')).toBe('Некорректный год');
  });

  it('returns the fallback for errors without a parseable envelope', async () => {
    expect(await firstApiErrorMessage(new Error('NetworkError when attempting to fetch resource'), 'Нет связи')).toBe('Нет связи');
    expect(await firstApiErrorMessage({ status: 0, error: 'network failure' }, 'Нет связи')).toBe('Нет связи');
    expect(await firstApiErrorMessage(undefined, 'fb')).toBe('fb');
  });
});
