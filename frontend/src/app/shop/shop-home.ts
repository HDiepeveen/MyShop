import { Component, effect, inject, signal } from '@angular/core';
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
      <a routerLink="/assortiment">Naar het assortiment</a>
      <button type="button" class="secondary" (click)="retry()">Opnieuw proberen</button>
    }
    @if (state()?.data; as home) {
      <section class="home-hero" [class.with-image]="home.imageUrl && !imageFailed()">
        <div>
        <p class="eyebrow">{{ branding.name() }}</p>
        <h1>{{ home.welcomeText ?? home.heading }}</h1>
        <a class="button" routerLink="/assortiment">Bekijk het assortiment <span aria-hidden="true">↗</span></a>
        </div>
        @if (home.imageUrl && !imageFailed()) {
          <img class="home-image" aria-hidden="true" [src]="home.imageUrl" alt="" (error)="imageFailed.set(true)" />
        }
      </section>
      @if (home.description) {
        <section class="home-text"><p>{{ home.description }}</p></section>
      }
    }
  `,
  styles: [`
    :host { display: block; }
    .home-hero { position: relative; isolation: isolate; padding: clamp(32px, 6vw, 80px) 0; }
    .home-hero > div { position: relative; z-index: 1; max-width: 850px; min-width: 0; }
    .home-hero.with-image { width: 100vw; margin-inline: calc(50% - 50vw); margin-top: -38px; margin-bottom: 36px; padding-inline: max(42px, calc((100vw - 1316px) / 2)); }
    .home-hero.with-image > div { background: var(--shop-background); color: var(--shop-text); border: 1px solid var(--shop-border); border-radius: 20px; padding: clamp(20px, 3vw, 32px); width: fit-content; max-width: min(740px, 100%); box-shadow: 0 12px 36px #0002; }
    .home-image { position: absolute; inset: 0; z-index: -1; width: 100%; height: 100%; object-fit: cover; }
    @media (max-width: 760px) { .home-hero.with-image { margin-top: -24px; padding-inline: 20px; } }
    h1 { font-size: clamp(2rem, 4vw, 3.5rem); line-height: 1.08; letter-spacing: -.04em; margin: 0 0 28px; overflow-wrap: anywhere; }
    .home-text { background: var(--shop-panel); border: 1px solid var(--shop-border); border-radius: 20px; padding: clamp(24px, 4vw, 44px); max-width: 900px; margin: 0 auto 36px; overflow-wrap: anywhere; }
    .home-text p { white-space: pre-wrap; line-height: 1.85; margin: 0; }
  `],
})
export class ShopHome {
  readonly imageFailed = signal(false);
  readonly branding = inject(ShopBranding);
  private readonly http = inject(HttpClient);
  private readonly seo = inject(StorefrontSeo);
  private readonly refresh = new BehaviorSubject(0);
  readonly state = toSignal(this.refresh.pipe(switchMap(() => loadState(this.http.get<HomePageSettings>('/api/shop/home')))));
  constructor() {
    effect(() => {
      const home = this.state()?.data;
      if (home) this.seo.apply(home.seoTitle || home.heading + ' · ' + this.branding.name(), home.seoDescription || home.description?.replace(/\s+/g, ' ').slice(0, 200), '/');
      else if (this.state()?.error) this.seo.apply('Homepagina niet beschikbaar · ' + this.branding.name(), undefined, '/winkel/home');
    });
  }
  retry() { if (!this.state()?.loading) { this.imageFailed.set(false); this.refresh.next(this.refresh.value + 1); } }
}
