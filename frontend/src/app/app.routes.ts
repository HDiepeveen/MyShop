import { Routes } from '@angular/router';
import { Home } from './home';
export const routes: Routes = [
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
