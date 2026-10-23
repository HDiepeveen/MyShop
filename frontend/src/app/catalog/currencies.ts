export const currencies = [
  { code: 'EUR', name: 'Euro (€)' },
  { code: 'SRD', name: 'Surinaamse dollar (SRD)' },
  { code: 'USD', name: 'Amerikaanse dollar ($)' },
  { code: 'GBP', name: 'Brits pond (£)' },
] as const;
export function knownCurrency(code: string) {
  return currencies.some(currency => currency.code === code);
}
