import { ApiError, ApiErrorBody } from './models';

/**
 * Extracts the most specific message from an API error.
 * Prefers the first field-level message from `errors` (e.g. {username: ["Такой логин уже занят"]}),
 * then the envelope `message`, then the fallback (network errors / missing envelope).
 */
export function firstApiErrorMessage(error: unknown, fallback: string): Promise<string> {
  return extractErrorBody(error).then((body) => {
    if (!body) {
      return fallback;
    }
    const errors = body.errors;
    if (errors) {
      const firstKey = Object.keys(errors)[0];
      const value = firstKey ? errors[firstKey] : undefined;
      if (Array.isArray(value)) {
        if (value.length > 0 && value[0]) {
          return value[0];
        }
      } else if (typeof value === 'string' && value) {
        return value;
      }
    }
    return body.message || fallback;
  });
}

function extractErrorBody(error: unknown): Promise<ApiErrorBody | null> {
  if (error instanceof ApiError) {
    return Promise.resolve({ code: error.code, message: error.message, errors: error.errors });
  }
  const raw = (error as { error?: unknown } | null)?.error;
  if (raw instanceof Blob) {
    return raw
      .text()
      .then(parseBody)
      .catch(() => null);
  }
  if (typeof raw === 'string') {
    return Promise.resolve(parseBody(raw));
  }
  if (raw && typeof raw === 'object') {
    return Promise.resolve(raw as ApiErrorBody);
  }
  return Promise.resolve(null);
}

function parseBody(text: string): ApiErrorBody | null {
  try {
    const parsed: unknown = JSON.parse(text);
    return parsed && typeof parsed === 'object' ? (parsed as ApiErrorBody) : null;
  } catch {
    return null;
  }
}
