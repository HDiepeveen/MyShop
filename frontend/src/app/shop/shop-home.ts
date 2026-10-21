import { PageBackdrop } from './page-backdrop';
import { DestroyRef } from '@angular/core';
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
      <div class="home-page" [class.with-image]="home.imageUrl && !imageFailed()">
      <section class="home-hero">
        <div>
        <p class="eyebrow">{{ branding.name() }}</p>
        <h1>{{ home.heading }}</h1>
        <div class="hero-actions"><a class="button" routerLink="/assortiment">Bekijk het assortiment <span aria-hidden="true">↗</span></a>
        <a class="home-contact" routerLink="/contact">Neem contact op →</a></div>
        </div>
      </section>
      @if (branding.homePages().length) {
        <nav class="service-links" aria-label="Onze diensten">
          @for (page of branding.homePages(); track page.webAddress) {
            <a [routerLink]="'/' + page.webAddress"><span>{{ page.name }}</span><span aria-hidden="true">↗</span></a>
          }
        </nav>
      }
      @if (home.description) {
        <section class="home-text"><p>{{ home.description }}</p></section>
      }
      @if (home.imageUrl && !imageFailed()) {
        <img class="home-image" [style.object-fit]="home.imageMode === 'stretch' ? 'fill' : 'cover'" aria-hidden="true" [src]="home.imageUrl" alt="" (error)="imageFailed.set(true)" />
      }
      </div>
    }
  `,
  styles: [`
    :host { display: block; }
    .home-hero { position: relative; isolation: isolate; padding: 0 0 clamp(48px, 8vw, 112px); }
    .home-hero > div { position: relative; z-index: 1; max-width: 850px; min-width: 0; }
    .home-page { position: relative; isolation: isolate; }
    .home-page.with-image { width: calc(100% + 84px); margin: -38px -42px; padding: 54px 42px 48px; box-sizing: border-box; min-height: calc(100vh - 78px); color: #fff; }
    .home-page > .home-hero, .home-page > .service-links, .home-page > .home-text { max-width: 1316px; margin-inline: auto; }
    .home-page.with-image::before { content: ''; position: absolute; inset: 0; z-index: -1; background: linear-gradient(90deg, #000c, #0009); }
    .home-image { position: absolute; inset: 0; z-index: -2; width: 100%; height: 100%; object-fit: fill; }
    .with-image .eyebrow, .with-image .home-contact { color: inherit; }
    .with-image .home-hero > div { max-width: 740px; }
    .with-image .home-text { background: none; border: 0; border-radius: 0; padding: 0; }
    .with-image .service-links a { background: transparent; color: inherit; border-color: #ffffff80; }
    @media (max-width: 760px) { .home-page.with-image { width: calc(100% + 40px); margin: -24px -20px; padding: 36px 20px 48px; } }
    h1 { font-size: clamp(2rem, 4vw, 3.5rem); line-height: 1.08; letter-spacing: -.04em; margin: 0 0 28px; overflow-wrap: anywhere; }
    .hero-actions { display: flex; align-items: center; gap: 20px; flex-wrap: wrap; }
    .home-contact { font-weight: 600; }
    .service-links { display: grid; grid-template-columns: repeat(auto-fit, minmax(min(100%, 220px), 1fr)); gap: 16px; margin: 0 0 36px; }
    .service-links a { display: flex; justify-content: space-between; gap: 20px; padding: 24px; border: 1px solid var(--shop-border); background: var(--shop-panel); color: var(--shop-text); border-radius: 12px; font-size: 1.15rem; font-weight: 650; text-decoration: none; overflow-wrap: anywhere; }
    .service-links a:hover { border-color: var(--shop-primary); }
    .service-links a:focus-visible { outline: 3px solid var(--shop-primary); outline-offset: 4px; }
    .home-text { background: var(--shop-panel); border: 1px solid var(--shop-border); border-radius: 20px; padding: clamp(24px, 4vw, 44px); width: 100%; box-sizing: border-box; margin: 0 0 36px; overflow-wrap: anywhere; }
    .home-text p { white-space: pre-wrap; line-height: 1.85; margin: 0; }
  `],
})
export class ShopHome {
  private readonly backdrop = inject(PageBackdrop);
  private readonly backdropOwner = {};
  readonly imageFailed = signal(false);
  readonly branding = inject(ShopBranding);
  private readonly http = inject(HttpClient);
  private readonly seo = inject(StorefrontSeo);
  private readonly refresh = new BehaviorSubject(0);
  readonly state = toSignal(this.refresh.pipe(switchMap(() => loadState(this.http.get<HomePageSettings>('/api/shop/home')))));
  constructor() {
    inject(DestroyRef).onDestroy(()=>this.backdrop.clear(this.backdropOwner));
    effect(()=>{const page=this.state()?.data;this.backdrop.show(this.backdropOwner,this.imageFailed()?null:page?.imageUrl,page?.imageMode);});
    effect(() => {
      const home = this.state()?.data;
      if (home) this.seo.apply(home.seoTitle || home.heading + ' · ' + this.branding.name(), home.seoDescription || home.description?.replace(/\s+/g, ' ').slice(0, 200), '/');
      else if (this.state()?.error) this.seo.apply('Homepagina niet beschikbaar · ' + this.branding.name(), undefined, '/winkel/home');
    });
  }
  retry() { if (!this.state()?.loading) { this.imageFailed.set(false); this.refresh.next(this.refresh.value + 1); } }
}
