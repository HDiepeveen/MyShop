export function countryName(value: string | null | undefined): string {
  const text = value?.trim() ?? '';
  return (
    ({ NL: 'Nederland', SR: 'Suriname' } as Record<string, string>)[text.toUpperCase()] ?? text
  );
}
