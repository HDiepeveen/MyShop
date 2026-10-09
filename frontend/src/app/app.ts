import { ShopSettings } from './shop/shop-settings';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Cart } from './shop/cart';
import { Component, inject, signal } from '@angular/core';
import { Auth } from './auth/auth';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map, startWith } from 'rxjs';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  readonly shopSettings = inject(ShopSettings);
  constructor() {
    this.router.events.pipe(filter(event => event instanceof NavigationEnd), takeUntilDestroyed()).subscribe(() => {
      if (this.router.url.startsWith('/winkel')) this.shopSettings.refresh();
    });
    if (this.router.url.startsWith('/winkel')) this.shopSettings.refresh();
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
        (url) => this.router.parseUrl(url).root.children['primary']?.segments[0]?.path === 'winkel',
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
        void this.router.navigate([this.storefront() ? '/winkel' : '/inloggen']);
      },
      error: () => {
        this.signingOut.set(false);
        this.logoutError.set('Uitloggen is niet bevestigd. Probeer opnieuw.');
      },
    });
  }
}
