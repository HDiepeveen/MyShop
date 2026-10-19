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
      <div class="home-items">
        @for (item of home.items; track $index) {
          <section class="home-item" [class.featured]="$first">
            @if (item.title) { <h2>{{ item.title }}</h2> }
            <p>{{ summary(item.description) }}</p>
          </section>
        }
      </div>
    }
  `,
  styles: [`
    :host { display: block; }
    .home-hero { padding: clamp(32px, 6vw, 80px) 0; max-width: 850px; }
    h1 { font-size: clamp(2.3rem, 5vw, 4.5rem); line-height: 1.08; letter-spacing: -.04em; margin: 0 0 28px; overflow-wrap: anywhere; }
    .home-items { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 24px; padding-bottom: 36px; }
    .home-item { background: var(--shop-panel); border: 1px solid var(--shop-border); border-radius: 20px; padding: clamp(24px, 4vw, 42px); overflow-wrap: anywhere; }
    .featured { grid-column: 1 / -1; border-top: 4px solid var(--shop-primary); }
    h2 { font-size: clamp(1.35rem, 2.5vw, 1.85rem); margin: 0 0 18px; }
    .home-item p { white-space: pre-wrap; line-height: 1.8; margin: 0; max-width: 78ch; }
    @media (max-width: 760px) { .home-items { grid-template-columns: 1fr; gap: 16px; } }
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
      if (home) this.seo.apply(home.heading + ' · ' + this.branding.name(), home.items[0]?.description.replace(/\s+/g, ' ').slice(0, 200), '/');
      else if (this.state()?.error) this.seo.apply('Homepagina niet beschikbaar · ' + this.branding.name(), undefined, '/winkel/home');
    });
  }
  summary(value: string) { return value.length > 200 ? value.slice(0, 200).trimEnd() + '…' : value; }
  retry() { if (!this.state()?.loading) this.refresh.next(this.refresh.value + 1); }
}
