export const attributeTypes = [
  { code: 0, name: 'Text', label: 'Tekst' },
  { code: 1, name: 'Integer', label: 'Geheel getal' },
  { code: 2, name: 'Decimal', label: 'Decimaal getal' },
  { code: 3, name: 'Boolean', label: 'Ja / nee' },
  { code: 4, name: 'Date', label: 'Datum' },
  { code: 5, name: 'Choice', label: 'Enkele keuze' },
  { code: 6, name: 'MultiChoice', label: 'Meerdere keuzes' },
] as const;
export function attributeTypeLabel(name: string) {
  return attributeTypes.find((type) => type.name === name)?.label ?? 'Onbekend type';
}
