import { Routes } from '@angular/router';
import { adminGuard, customerGuard } from './auth/auth-routing';
import { Home } from './home';
const protectedRoutes: Routes = [
  {
    path: 'bestellingen/:id',
    loadComponent: () => import('./checkout/order-detail').then((m) => m.OrderDetailComponent),
    title: 'Bestelling · MyShop',
  },
  {
    path: 'bestellingen',
    loadComponent: () => import('./checkout/order-list').then((m) => m.OrderList),
    title: 'Bestellingen · MyShop',
  },
  {
    path: 'instellingen/betalen',
    loadComponent: () => import('./checkout/payment-settings').then((m) => m.PaymentSettings),
    title: 'Betaalopties · MyShop',
  },
  {
    path: 'account',
    loadComponent: () => import('./auth/password').then((m) => m.Password),
    title: 'Wachtwoord wijzigen · MyShop',
  },
  { path: '', component: Home, title: 'Overzicht · MyShop' },
  {
    path: 'producten/nieuw',
    loadComponent: () => import('./catalog/products/product-create').then((m) => m.ProductCreate),
    title: 'Nieuw product · MyShop',
  },
  {
    path: 'producten/:id',
    loadComponent: () => import('./catalog/products/product-detail').then((m) => m.ProductDetail),
    title: 'Productdetails · MyShop',
  },
  {
    path: 'producten',
    loadComponent: () => import('./catalog/products/product-list').then((m) => m.ProductList),
    title: 'Producten · MyShop',
  },
  {
    path: 'categorieen/:id',
    loadComponent: () =>
      import('./catalog/categories/category-detail').then((m) => m.CategoryDetail),
    title: 'Categoriedetails · MyShop',
  },
  {
    path: 'categorieen',
    loadComponent: () => import('./catalog/categories/category-list').then((m) => m.CategoryList),
    title: 'Categorieën · MyShop',
  },
  {
    path: 'producttypen/:id',
    loadComponent: () => import('./catalog/types/type-detail').then((m) => m.TypeDetail),
    title: 'Producttype · MyShop',
  },
  {
    path: 'producttypen',
    loadComponent: () => import('./catalog/types/type-list').then((m) => m.TypeList),
    title: 'Producttypen · MyShop',
  },
  {
    path: '**',
    loadComponent: () => import('./not-found').then((m) => m.NotFound),
    title: 'Pagina niet gevonden · MyShop',
  },
];

export const routes: Routes = [
  {
    path: 'winkel',
    children: [
      {
        path: '',
        pathMatch: 'full',
        loadComponent: () => import('./shop/shop-list').then((m) => m.ShopList),
        title: 'Assortiment · MyShop',
      },
      {
        path: 'winkelmand',
        loadComponent: () => import('./shop/shop-cart').then((m) => m.ShopCart),
        title: 'Winkelmand · MyShop',
      },
      {
        path: 'inloggen',
        loadComponent: () => import('./customer/customer-login').then((m) => m.CustomerLogin),
        title: 'Inloggen · MyShop',
      },
      {
        path: 'registreren',
        loadComponent: () => import('./customer/customer-register').then((m) => m.CustomerRegister),
        title: 'Registreren · MyShop',
      },
      {
        path: 'account', canActivate: [customerGuard],
        loadComponent: () => import('./customer/customer-profile').then((m) => m.CustomerProfile),
        title: 'Mijn account · MyShop',
      },
      {
        path: 'account/wachtwoord', canActivate: [customerGuard],
        loadComponent: () => import('./auth/password').then((m) => m.Password),
        title: 'Wachtwoord wijzigen · MyShop',
      },
      {
        path: ':id',
        loadComponent: () => import('./shop/shop-detail').then((m) => m.ShopDetail),
        title: 'Product · MyShop',
      },
      {
        path: '**',
        loadComponent: () => import('./shop/shop-not-found').then((m) => m.ShopNotFound),
        title: 'Pagina niet gevonden · MyShop',
      },
    ],
  },
  {
    path: 'inloggen',
    loadComponent: () => import('./auth/login').then((m) => m.Login),
    title: 'Inloggen · MyShop',
  },
  {
    path: 'geen-toegang',
    loadComponent: () => import('./auth/forbidden').then((m) => m.Forbidden),
    title: 'Geen toegang · MyShop',
  },
  ...protectedRoutes.map((route) => ({ ...route, canActivate: [adminGuard] })),
];
