import { Pipe, PipeTransform } from '@angular/core';

export function formatMoney(value: number): string {
  return Math.abs(value).toLocaleString('ru-RU', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });
}

@Pipe({ name: 'money' })
export class MoneyPipe implements PipeTransform {
  transform(value: number | null | undefined, signed = false): string {
    if (value === null || value === undefined || Number.isNaN(value)) {
      return '0,00';
    }
    const fixed = formatMoney(value);
    if (signed && value !== 0) {
      return value > 0 ? `+${fixed}` : `−${fixed}`;
    }
    return value < 0 ? `−${fixed}` : fixed;
  }
}

@Pipe({ name: 'ruDate' })
export class RuDatePipe implements PipeTransform {
  transform(value: string | Date | null | undefined): string {
    if (!value) {
      return '';
    }
    const d = typeof value === 'string' ? new Date(`${value}T00:00:00`) : value;
    return d.toLocaleDateString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric' });
  }
}

@Pipe({ name: 'monthTitle' })
export class MonthTitlePipe implements PipeTransform {
  transform(value: { year: number; month: number } | number, month?: number): string {
    const year = typeof value === 'number' ? value : value.year;
    const m = typeof value === 'number' ? (month ?? 1) : value.month;
    const d = new Date(year, m - 1, 1);
    const title = d.toLocaleDateString('ru-RU', { month: 'long', year: 'numeric' });
    return title.charAt(0).toUpperCase() + title.slice(1);
  }
}
