import { PageBackdrop } from './shop/page-backdrop';
import { NgStyle } from '@angular/common';
import { themeStyles } from './settings/shop-theme';
import { ShopBranding } from './shop/shop-branding';
import { StorefrontSeo } from './shop/storefront-seo';
import webshopVersion from '../../../version.json';
import { ShopSettings } from './shop/shop-settings';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Cart } from './shop/cart';
import { Component, inject, signal } from '@angular/core';
import { Auth } from './auth/auth';
import { managementMenu } from './auth/management-menu';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map, startWith } from 'rxjs';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [NgStyle, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  readonly managementMenu = managementMenu;
  managementGroups() {
    return [managementMenu.slice(0, 3), managementMenu.slice(3, 6), managementMenu.slice(6, 8),
      managementMenu.slice(8, 11), managementMenu.slice(11, 12), managementMenu.slice(12)]
      .map(items => items.filter(item => this.auth.canUse(item.code) || !this.auth.isManagement()))
      .filter(items => items.length > 0);
  }
  showManagementMenu() {
    return !this.storefront() && (this.auth.isManagement() || this.branding.showManagementMenuWithoutLogin());
  }
  readonly themeStyles = themeStyles;
  readonly pageBackdrop = inject(PageBackdrop);
  readonly branding = inject(ShopBranding);
  readonly version = webshopVersion.version;
  private readonly seo = inject(StorefrontSeo);
  readonly shopSettings = inject(ShopSettings);
  constructor() {
    this.branding.refresh();
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe(() => {
        this.seo.reset(this.router.url);
        this.branding.refresh();
        if (this.storefront()) this.shopSettings.refresh();
      });
    if (this.storefront()) this.shopSettings.refresh();
  }
  readonly auth = inject(Auth);
  readonly cart = inject(Cart);
  private readonly router = inject(Router);
  readonly storefront = toSignal(
    this.router.events.pipe(
      filter((event) => event instanceof NavigationEnd),
      map(() => this.router.url),
      startWith(this.router.url),
      map(
        (url) => ['winkel', 'assortiment'].includes(this.router.parseUrl(url).root.children['primary']?.segments[0]?.path ?? '') || this.router.routerState.snapshot.root.firstChild?.data['storefront'] === true,
      ),
    ),
    { initialValue: false },
  );
  readonly signingOut = signal(false);
  readonly logoutError = signal('');
  logout() {
    if (this.signingOut()) return;
    this.signingOut.set(true);
    this.logoutError.set('');
    this.auth.logout().subscribe({
      next: () => {
        this.signingOut.set(false);
        void this.router.navigate([this.storefront() ? '/assortiment' : '/inloggen']);
      },
      error: () => {
        this.signingOut.set(false);
        this.logoutError.set('Uitloggen is niet bevestigd. Probeer opnieuw.');
      },
    });
  }
}
