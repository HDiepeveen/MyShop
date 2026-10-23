import { currencySymbol, MoneyPipe } from './money';

describe('Money display', () => {
  it.each([['EUR', '€'], ['USD', '$'], ['SRD', 'SR$'], ['GBP', '£'], ['CAD', 'CAD']])('displays %s unambiguously', (code, symbol) => {
    expect(currencySymbol(code)).toBe(symbol);
    expect(new MoneyPipe().transform('12.30', code)).toBe(symbol + ' 12,30');
  });
  it('keeps large exact decimal strings and zero intact', () => {
    expect(new MoneyPipe().transform('9999999999999999.99', 'EUR')).toBe('€ 9999999999999999,99');
    expect(new MoneyPipe().transform(0, 'GBP')).toBe('£ 0,00');
    expect(new MoneyPipe().transform(null, 'EUR')).toBe('');
  });
});
