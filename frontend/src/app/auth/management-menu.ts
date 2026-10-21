export const managementMenu = [
  { code: 'overview', name: 'Overzicht', path: '/beheer' },
  { code: 'categories', name: 'Categorieën', path: '/categorieen' },
  { code: 'productTypes', name: 'Producttypen', path: '/producttypen' },
  { code: 'products', name: 'Producten', path: '/producten' },
  { code: 'orders', name: 'Bestellingen', path: '/bestellingen' },
  { code: 'customers', name: 'Klanten', path: '/klanten' },
  { code: 'payments', name: 'Betaalopties', path: '/instellingen/betalen' },
  { code: 'delivery', name: 'Bezorgopties', path: '/instellingen/bezorgen' },
  { code: 'email', name: 'E-mailinstellingen', path: '/instellingen/email' },
  { code: 'billing', name: 'Facturatie en btw', path: '/instellingen/facturatie' },
  { code: 'appearance', name: 'Vormgeving', path: '/instellingen/vormgeving' },
  { code: 'seo', name: 'Website en SEO', path: '/instellingen/seo' },
  { code: 'companyAccess', name: 'Bedrijfsbeheerders en rollen', path: '/instellingen/bedrijfsbeheerders' },
] as const;
export function managementPermission(url: string): string | null {
  const path = url.split(/[?#]/)[0];
  if (path === '/') return 'overview';
  return managementMenu.find(item => path === item.path || path.startsWith(item.path + '/'))?.code ?? null;
}
