import { Component, effect, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { BehaviorSubject, switchMap } from 'rxjs';
import { HomePageSettings } from '../seo/seo.models';
import { loadState } from '../catalog/load-state';
import { StorefrontSeo } from './storefront-seo';
import { ShopBranding } from './shop-branding';

@Component({
  imports: [RouterLink],
  template: `
    @if (state()?.loading) { <p role="status">Homepagina ophalen…</p> }
    @if (state()?.error) {
      <h1>Homepagina niet beschikbaar</h1>
      <p>Bekijk ons assortiment of probeer het opnieuw.</p>
      <a routerLink="/winkel">Naar het assortiment</a>
      <button type="button" class="secondary" (click)="retry()">Opnieuw proberen</button>
    }
    @if (state()?.data; as home) {
      <section class="home-hero">
        <p class="eyebrow">{{ branding.name() }}</p>
        <h1>{{ home.heading }}</h1>
        <a class="button" routerLink="/winkel">Bekijk het assortiment <span aria-hidden="true">↗</span></a>
      </section>
      @if (home.description) {
        <section class="home-text"><p>{{ home.description }}</p></section>
      }
    }
  `,
  styles: [`
    :host { display: block; }
    .home-hero { padding: clamp(32px, 6vw, 80px) 0; max-width: 850px; }
    h1 { font-size: clamp(2.3rem, 5vw, 4.5rem); line-height: 1.08; letter-spacing: -.04em; margin: 0 0 28px; overflow-wrap: anywhere; }
    .home-text { background: var(--shop-panel); border: 1px solid var(--shop-border); border-radius: 20px; padding: clamp(24px, 4vw, 44px); max-width: 900px; margin: 0 auto 36px; overflow-wrap: anywhere; }
    .home-text p { white-space: pre-wrap; line-height: 1.85; margin: 0; }
  `],
})
export class ShopHome {
  readonly branding = inject(ShopBranding);
  private readonly http = inject(HttpClient);
  private readonly seo = inject(StorefrontSeo);
  private readonly refresh = new BehaviorSubject(0);
  readonly state = toSignal(this.refresh.pipe(switchMap(() => loadState(this.http.get<HomePageSettings>('/api/shop/home')))));
  constructor() {
    effect(() => {
      const home = this.state()?.data;
      if (home) this.seo.apply(home.heading + ' · ' + this.branding.name(), home.description?.replace(/\s+/g, ' ').slice(0, 200), '/');
      else if (this.state()?.error) this.seo.apply('Homepagina niet beschikbaar · ' + this.branding.name(), undefined, '/winkel/home');
    });
  }
  retry() { if (!this.state()?.loading) this.refresh.next(this.refresh.value + 1); }
}
