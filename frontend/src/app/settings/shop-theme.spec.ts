import {
  buttonText,
  contrast,
  contrastWarnings,
  defaultColors,
  shopThemes,
  themeStyles,
  validColors,
} from './shop-theme';
describe('Shop color contrast', () => {
  it('calculates black/white contrast and automatically chooses button text', () => {
    expect(contrast('#000000', '#ffffff')).toBe(21);
    expect(buttonText('#ffffff')).toBe('#000000');
    expect(buttonText('#000000')).toBe('#ffffff');
    expect(themeStyles(shopThemes[2].colors)['--shop-color-scheme']).toBe('dark');
    expect(themeStyles(defaultColors)['--shop-color-scheme']).toBe('light');
  });
  it('provides readable presets and detects low contrast custom colors', () => {
    for (const theme of shopThemes) {
      expect(validColors(theme.colors)).toBe(true);
      expect(contrastWarnings(theme.colors)).toEqual([]);
      expect(
        contrast(theme.colors.primaryColor, buttonText(theme.colors.primaryColor)),
      ).toBeGreaterThanOrEqual(4.5);
    }
    expect(contrastWarnings({ ...defaultColors, textColor: defaultColors.panelColor })).not.toEqual(
      [],
    );
  });
  it.each(['red', '#fff', '#123456\n', '#123456; color:red'])(
    'rejects unsafe color %s',
    (primaryColor) => {
      const colors = { ...defaultColors, primaryColor };
      expect(validColors(colors)).toBe(false);
      expect(themeStyles(colors)['--shop-primary']).toBe(defaultColors.primaryColor);
    },
  );
});
