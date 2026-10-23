export const managementMenu = [
  { code: 'categories', name: 'Categorieën', path: '/categorieen' },
  { code: 'productTypes', name: 'Producttypen', path: '/producttypen' },
  { code: 'products', name: 'Producten', path: '/producten' },
  { code: 'overview', name: 'Overzicht', path: '/beheer' },
  { code: 'orders', name: 'Bestellingen', path: '/bestellingen' },
  { code: 'customers', name: 'Klanten', path: '/klanten' },
  { code: 'seo', name: 'Website en SEO', path: '/instellingen/seo' },
  { code: 'appearance', name: 'Vormgeving', path: '/instellingen/vormgeving' },
  { code: 'payments', name: 'Betaalopties', path: '/instellingen/betalen' },
  { code: 'delivery', name: 'Bezorgopties', path: '/instellingen/bezorgen' },
  { code: 'billing', name: 'Facturatie en btw', path: '/instellingen/facturatie' },
  { code: 'companyAccess', name: 'Bedrijfsbeheerders en rollen', path: '/instellingen/bedrijfsbeheerders' },
  { code: 'paymentSettings', name: 'Betaalinstellingen', path: '/instellingen/betaalinstellingen' },
  { code: 'email', name: 'E-mailinstellingen', path: '/instellingen/email' },
] as const;
export function managementPermission(url: string): string | null {
  const path = url.split(/[?#]/)[0];
  if (path === '/') return 'overview';
  return managementMenu.find(item => path === item.path || path.startsWith(item.path + '/'))?.code ?? null;
}
