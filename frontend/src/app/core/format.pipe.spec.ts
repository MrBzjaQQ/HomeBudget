import { MonthTitlePipe, MoneyPipe, RuDatePipe } from './format.pipe';

describe('MoneyPipe', () => {
  const pipe = new MoneyPipe();

  it('formates with thousand separator and 2 decimals', () => {
    expect(pipe.transform(1250.5)).toBe('1\u00A0250,50');
  });

  it('returns 0,00 for null/undefined', () => {
    expect(pipe.transform(null)).toBe('0,00');
    expect(pipe.transform(undefined)).toBe('0,00');
  });

  it('uses minus sign for negatives without sign flag', () => {
    expect(pipe.transform(-42)).toBe('−42,00');
  });

  it('adds + sign with sign flag', () => {
    expect(pipe.transform(42, true)).toBe('+42,00');
  });

  it('uses − sign for negatives with sign flag', () => {
    expect(pipe.transform(-42, true)).toBe('−42,00');
  });

  it('omits sign for zero with sign flag', () => {
    expect(pipe.transform(0, true)).toBe('0,00');
  });

  it('returns plain absolute value for positives without sign flag', () => {
    expect(pipe.transform(42)).toBe('42,00');
  });
});

describe('RuDatePipe', () => {
  const pipe = new RuDatePipe();

  it('formats ISO date to ru short form', () => {
    expect(pipe.transform('2026-09-14')).toBe('14.09.2026');
  });

  it('returns empty string for null', () => {
    expect(pipe.transform(null)).toBe('');
  });
});

describe('MonthTitlePipe', () => {
  const pipe = new MonthTitlePipe();

  it('formats month ref object', () => {
    const title = pipe.transform({ year: 2026, month: 9 });
    expect(title.startsWith('Сентябрь')).toBe(true);
    expect(title).toContain('2026');
  });

  it('formats year+month numbers', () => {
    const title = pipe.transform(2026, 1);
    expect(title.startsWith('Январь')).toBe(true);
  });
});
