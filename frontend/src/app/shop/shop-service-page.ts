import { Component, effect, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { switchMap } from 'rxjs';
import { loadState } from '../catalog/load-state';
import { HomePageSettings } from '../seo/seo.models';
import { ShopBranding } from './shop-branding';
import { StorefrontSeo } from './storefront-seo';

@Component({
  imports: [RouterLink],
  template: `
    <a routerLink="/">← Home</a>
    @if (state()?.loading) { <p role="status">Pagina ophalen…</p> }
    @if (state()?.error) { <h1>Pagina niet beschikbaar</h1><p>Deze pagina bestaat niet of is niet gepubliceerd.</p> }
    @if (state()?.data; as page) {
      <article>
        <p class="eyebrow">{{ branding.name() }}</p>
        <h1>{{ page.title || 'Meer informatie' }}@if (page.place) { <span> in {{ page.place }}</span> }</h1>
        <div class="page-content"><p>{{ page.description }}</p></div>
      </article>
    }
  `,
  styles: [`
    article { max-width: 900px; margin: 40px auto; }
    h1 { font-size: clamp(2rem, 5vw, 3.8rem); line-height: 1.12; letter-spacing: -.035em; overflow-wrap: anywhere; }
    .page-content { padding: clamp(24px, 4vw, 44px); border-radius: 20px; border: 1px solid var(--shop-border); background: var(--shop-panel); }
    .page-content p { margin: 0; white-space: pre-wrap; line-height: 1.85; overflow-wrap: anywhere; }
  `],
})
export class ShopServicePage {
  readonly branding = inject(ShopBranding);
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly seo = inject(StorefrontSeo);
  readonly state = toSignal(this.route.paramMap.pipe(switchMap(params => loadState(
    this.http.get<HomePageSettings['items'][number]>('/api/shop/pages/' + encodeURIComponent(params.get('page') ?? '')),
  ))));
  constructor() {
    effect(() => {
      const page = this.state()?.data;
      if (page) this.seo.apply((page.title || 'Meer informatie') + (page.place ? ' in ' + page.place : '') + ' · ' + this.branding.name(), page.description.replace(/\s+/g, ' ').slice(0, 200), '/' + page.webAddress);
      else if (this.state()?.error) { this.seo.apply('Pagina niet beschikbaar · ' + this.branding.name(), undefined, '/' + this.route.snapshot.paramMap.get('page')); this.seo.reset('/geen-toegang'); }
    });
  }
}
