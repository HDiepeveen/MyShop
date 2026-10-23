import { Pipe, PipeTransform } from '@angular/core';

export function currencySymbol(currency: string | null | undefined): string {
  const code = currency?.trim().toUpperCase() ?? '';
  return ({ EUR: '€', SRD: 'SR$', USD: '$', GBP: '£' } as Record<string, string>)[code] ?? code;
}

@Pipe({ name: 'currencySymbol' })
export class CurrencySymbolPipe implements PipeTransform {
  transform(currency: string | null | undefined) { return currencySymbol(currency); }
}

@Pipe({ name: 'money' })
export class MoneyPipe implements PipeTransform {
  transform(amount: string | number | null | undefined, currency: string | null | undefined): string {
    if (amount == null) return '';
    const text = String(amount);
    const match = /^(-?\d+)(?:\.(\d{1,2}))?$/.exec(text);
    const formatted = match ? match[1] + ',' + (match[2] ?? '').padEnd(2, '0') : text;
    return currencySymbol(currency) + ' ' + formatted;
  }
}
