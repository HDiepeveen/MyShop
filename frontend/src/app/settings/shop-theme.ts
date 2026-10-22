export interface ShopColors {
  primaryColor: string;
  backgroundColor: string;
  panelColor: string;
  textColor: string;
}
export const defaultColors: ShopColors = {
  primaryColor: '#1d5543',
  backgroundColor: '#f6f7f1',
  panelColor: '#fffefa',
  textColor: '#223c33',
};
export const shopThemes = [
  { name: 'Standaard', colors: defaultColors },
  {
    name: 'Licht',
    colors: {
      primaryColor: '#334155',
      backgroundColor: '#f8fafc',
      panelColor: '#ffffff',
      textColor: '#172033',
    },
  },
  {
    name: 'Donker',
    colors: {
      primaryColor: '#a7d9ff',
      backgroundColor: '#111827',
      panelColor: '#1f2937',
      textColor: '#f3f4f6',
    },
  },
  {
    name: 'Blauw',
    colors: {
      primaryColor: '#1d4ed8',
      backgroundColor: '#eff6ff',
      panelColor: '#ffffff',
      textColor: '#172554',
    },
  },
  {
    name: 'Groen',
    colors: {
      primaryColor: '#166534',
      backgroundColor: '#f0fdf4',
      panelColor: '#ffffff',
      textColor: '#14332a',
    },
  },
];
export function validColors(value: unknown): value is ShopColors {
  if (!value || typeof value !== 'object') return false;
  return ['primaryColor', 'backgroundColor', 'panelColor', 'textColor'].every(
    (key) =>
      typeof (value as Record<string, unknown>)[key] === 'string' &&
      /^#[0-9a-f]{6}$/i.test((value as Record<string, string>)[key]) &&
      (value as Record<string, string>)[key].length === 7,
  );
}
function luminance(color: string): number {
  const channels = [1, 3, 5]
    .map((start) => parseInt(color.slice(start, start + 2), 16) / 255)
    .map((channel) => (channel <= 0.04045 ? channel / 12.92 : ((channel + 0.055) / 1.055) ** 2.4));
  return channels[0] * 0.2126 + channels[1] * 0.7152 + channels[2] * 0.0722;
}
export function contrast(first: string, second: string): number {
  const a = luminance(first),
    b = luminance(second);
  return (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05);
}
export function buttonText(primary: string): string {
  return contrast(primary, '#ffffff') >= contrast(primary, '#000000') ? '#ffffff' : '#000000';
}
export function themeStyles(colors: ShopColors): Record<string, string> {
  const safe = validColors(colors) ? colors : defaultColors;
  return {
    '--shop-primary': safe.primaryColor,
    '--shop-background': safe.backgroundColor,
    '--shop-panel': safe.panelColor,
    '--shop-text': safe.textColor,
    '--shop-readable-background-text': contrast(safe.textColor, safe.backgroundColor) >= 4.5 ? safe.textColor : buttonText(safe.backgroundColor),
    '--shop-readable-panel-text': contrast(safe.textColor, safe.panelColor) >= 4.5 ? safe.textColor : buttonText(safe.panelColor),
    '--shop-button-text': buttonText(safe.primaryColor),
    '--shop-border': safe.textColor + '40',
    '--shop-color-scheme': buttonText(safe.backgroundColor) === '#ffffff' ? 'dark' : 'light',
  };
}
export function contrastWarnings(colors: ShopColors): string[] {
  if (!validColors(colors)) return [];
  const checks = [
    [colors.textColor, colors.backgroundColor, 'Tekst op de pagina-achtergrond'],
    [colors.textColor, colors.panelColor, 'Tekst in panelen'],
    [colors.primaryColor, colors.backgroundColor, 'Links op de pagina-achtergrond'],
    [colors.primaryColor, colors.panelColor, 'Links in panelen'],
  ];
  return checks
    .filter(([a, b]) => contrast(a, b) < 4.5)
    .map(([, , label]) => label + ' heeft weinig contrast.');
}
