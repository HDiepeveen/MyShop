import { HttpClient } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, map, of } from 'rxjs';
import { Router, Routes } from '@angular/router';
import { adminGuard, customerGuard } from './auth/auth-routing';
import { Home } from './home';
const protectedRoutes: Routes = [
  {
    path: 'instellingen/bedrijfsbeheerders',
    loadComponent: () => import('./auth/company-access').then(m => m.CompanyAccessPage),
    title: 'Bedrijfsbeheerders en rollen · HD Web Framework',
  },
  {
    path: 'instellingen/vormgeving',
    loadComponent: () =>
      import('./settings/appearance-settings').then((m) => m.AppearanceSettingsPage),
    title: 'Vormgeving · HD Web Framework',
  },
  {
    path: 'instellingen/seo',
    loadComponent: () => import('./seo/seo-settings').then((m) => m.SeoSettings),
    title: 'Website en SEO · HD Web Framework',
  },
  {
    path: 'instellingen/facturatie',
    loadComponent: () => import('./billing/billing-settings').then((m) => m.BillingSettings),
    title: 'Facturatie en btw · HD Web Framework',
  },
  {
    path: 'bestellingen/:id/factuur',
    loadComponent: () => import('./billing/invoice-page').then((m) => m.InvoicePage),
    title: 'Factuur · HD Web Framework',
  },
  {
    path: 'instellingen/email',
    loadComponent: () => import('./settings/email-settings').then((m) => m.EmailSettings),
    title: 'E-mailinstellingen · HD Web Framework',
  },
  {
    path: 'klanten/:id',
    loadComponent: () =>
      import('./customer/customer-management-detail').then((m) => m.CustomerManagementDetail),
    title: 'Klant · HD Web Framework',
  },
  {
    path: 'klanten',
    loadComponent: () =>
      import('./customer/customer-management-list').then((m) => m.CustomerManagementList),
    title: 'Klanten · HD Web Framework',
  },
  {
    path: 'bestellingen/:id',
    loadComponent: () => import('./checkout/order-detail').then((m) => m.OrderDetailComponent),
    title: 'Bestelling · HD Web Framework',
  },
  {
    path: 'bestellingen',
    loadComponent: () => import('./checkout/order-list').then((m) => m.OrderList),
    title: 'Bestellingen · HD Web Framework',
  },
  {
    path: 'instellingen/bezorgen',
    loadComponent: () => import('./checkout/delivery-settings').then((m) => m.DeliverySettings),
    title: 'Bezorgopties · HD Web Framework',
  },
  {
    path: 'instellingen/betalen',
    loadComponent: () => import('./checkout/payment-settings').then((m) => m.PaymentSettings),
    title: 'Betaalopties · HD Web Framework',
  },
  {
    path: 'account',
    loadComponent: () => import('./auth/password').then((m) => m.Password),
    title: 'Wachtwoord wijzigen · HD Web Framework',
  },
  { path: 'beheer', component: Home, title: 'Overzicht · HD Web Framework' },
  { path: '', component: Home, title: 'Overzicht · HD Web Framework' },
  {
    path: 'producten/nieuw',
    loadComponent: () => import('./catalog/products/product-create').then((m) => m.ProductCreate),
    title: 'Nieuw product · HD Web Framework',
  },
  {
    path: 'producten/:id',
    loadComponent: () => import('./catalog/products/product-detail').then((m) => m.ProductDetail),
    title: 'Productdetails · HD Web Framework',
  },
  {
    path: 'producten',
    loadComponent: () => import('./catalog/products/product-list').then((m) => m.ProductList),
    title: 'Producten · HD Web Framework',
  },
  {
    path: 'categorieen/:id',
    loadComponent: () =>
      import('./catalog/categories/category-detail').then((m) => m.CategoryDetail),
    title: 'Categoriedetails · HD Web Framework',
  },
  {
    path: 'categorieen',
    loadComponent: () => import('./catalog/categories/category-list').then((m) => m.CategoryList),
    title: 'Categorieën · HD Web Framework',
  },
  {
    path: 'producttypen/:id',
    loadComponent: () => import('./catalog/types/type-detail').then((m) => m.TypeDetail),
    title: 'Producttype · HD Web Framework',
  },
  {
    path: 'producttypen',
    loadComponent: () => import('./catalog/types/type-list').then((m) => m.TypeList),
    title: 'Producttypen · HD Web Framework',
  },
 ];

export const routes: Routes = [
  { path: '', pathMatch: 'full', data: { storefront: true }, canMatch: [() => inject(HttpClient).get<{homeEnabled: boolean}>('/api/shop/settings').pipe(map(s => s.homeEnabled === true), catchError(() => of(false)))], loadComponent: () => import('./shop/shop-home').then(m => m.ShopHome), title: 'Home · HD Web Framework' },
  { path: 'assortiment/:category', data: { storefront: true }, loadComponent: () => import('./shop/shop-category').then(m => m.ShopCategory), title: 'Categorie · HD Web Framework' },
  { path: 'assortiment', data: { storefront: true }, loadComponent: () => import('./shop/shop-list').then(m => m.ShopList), title: 'Assortiment · HD Web Framework' },
  {
    path: 'contact', data: { storefront: true }, loadComponent: () => import('./shop/shop-company').then(m => m.ShopCompany), title: 'Contact · HD Web Framework',
  },
  {
    path: 'winkel',
    children: [
      { path: 'home', redirectTo: '/', pathMatch: 'full' },
      {
        path: 'informatie/bedrijf',
        redirectTo: ({ queryParams }) => inject(Router).createUrlTree(['/contact'], { queryParams }),
      },
      {
        path: 'betaling',
        loadComponent: () => import('./checkout/payment-return').then((m) => m.PaymentReturn),
        title: 'Betaling · HD Web Framework',
      },
      {
        path: '',
        pathMatch: 'full',
        redirectTo: ({ queryParams }) => inject(Router).createUrlTree(['/assortiment'], { queryParams }),
      },
      {
        path: 'winkelmand',
        loadComponent: () => import('./shop/shop-cart').then((m) => m.ShopCart),
        title: 'Winkelmand · HD Web Framework',
      },
      ...(
        [
          'wachtwoord-vergeten',
          'wachtwoord-herstellen',
          'e-mail-bevestigen',
          'bevestiging-aanvragen',
        ] as const
      ).map((path, index) => ({
        path,
        loadComponent: () => import('./customer/customer-email').then((m) => m.CustomerEmail),
        data: { emailMode: ['forgot', 'reset', 'confirm', 'resend'][index] },
        title: 'Account herstellen · HD Web Framework',
      })),
      {
        path: 'inloggen',
        loadComponent: () => import('./customer/customer-login').then((m) => m.CustomerLogin),
        title: 'Inloggen · HD Web Framework',
      },
      {
        path: 'registreren',
        loadComponent: () => import('./customer/customer-register').then((m) => m.CustomerRegister),
        title: 'Registreren · HD Web Framework',
      },
      {
        path: 'account',
        canActivate: [customerGuard],
        loadComponent: () => import('./customer/customer-profile').then((m) => m.CustomerProfile),
        title: 'Mijn account · HD Web Framework',
      },
      {
        path: 'account/verlanglijst',
        canActivate: [customerGuard],
        loadComponent: () => import('./customer/customer-wishlist').then((m) => m.CustomerWishlist),
        title: 'Mijn verlanglijst · HD Web Framework',
      },
      {
        path: 'account/bestellingen/:id',
        canActivate: [customerGuard],
        loadComponent: () =>
          import('./customer/customer-order-detail').then((m) => m.CustomerOrderDetail),
        title: 'Bestelling · HD Web Framework',
      },
      {
        path: 'account/bestellingen/:id/factuur',
        canActivate: [customerGuard],
        data: { customerInvoice: true },
        loadComponent: () => import('./billing/invoice-page').then((m) => m.InvoicePage),
        title: 'Factuur · HD Web Framework',
      },
      {
        path: 'account/bestellingen',
        canActivate: [customerGuard],
        loadComponent: () =>
          import('./customer/customer-order-list').then((m) => m.CustomerOrderList),
        title: 'Mijn bestellingen · HD Web Framework',
      },
      {
        path: 'account/wachtwoord',
        canActivate: [customerGuard],
        loadComponent: () => import('./auth/password').then((m) => m.Password),
        title: 'Wachtwoord wijzigen · HD Web Framework',
      },
      {
        path: ':id',
        loadComponent: () => import('./shop/shop-detail').then((m) => m.ShopDetail),
        title: 'Product · HD Web Framework',
      },
      {
        path: '**',
        loadComponent: () => import('./shop/shop-not-found').then((m) => m.ShopNotFound),
        title: 'Pagina niet gevonden · HD Web Framework',
      },
    ],
  },
  {
    path: 'inloggen',
    loadComponent: () => import('./auth/login').then((m) => m.Login),
    title: 'Inloggen · HD Web Framework',
  },
  {
    path: 'geen-toegang',
    loadComponent: () => import('./auth/forbidden').then((m) => m.Forbidden),
    title: 'Geen toegang · HD Web Framework',
  },
  ...['algemene-voorwaarden', 'privacyverklaring', 'cookiebeleid'].map(page => ({ path: page, data: { storefront: true, legalPage: page }, loadComponent: () => import('./shop/shop-legal').then(m => m.ShopLegal) })),
  ...protectedRoutes.map((route) => ({ ...route, canActivate: [adminGuard] })),
  { path: ':page', data: { storefront: true }, loadComponent: () => import('./shop/shop-service-page').then(m => m.ShopServicePage), title: 'Pagina · HD Web Framework' },
  { path: '**', loadComponent: () => import('./not-found').then(m => m.NotFound), title: 'Pagina niet gevonden · HD Web Framework' },
];
