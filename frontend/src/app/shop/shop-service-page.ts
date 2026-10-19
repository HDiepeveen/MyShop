import { Component, effect, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { switchMap } from 'rxjs';
import { loadState } from '../catalog/load-state';
import { HomePageSettings } from '../seo/seo.models';
import { ShopBranding } from './shop-branding';
import { StorefrontSeo } from './storefront-seo';

@Component({
  template: `
    @if (state()?.loading) { <p role="status">Pagina ophalen…</p> }
    @if (state()?.error) { <h1>Pagina niet beschikbaar</h1><p>Deze pagina bestaat niet of is niet gepubliceerd.</p> }
    @if (state()?.data; as page) {
      <article>
        <div class="eyebrow">{{ branding.name() }}</div>
        <h1>{{ page.title || 'Meer informatie' }}</h1>
        <div class="page-content"><p>{{ page.description }}</p></div>
      </article>
    }
  `,
  styleUrls: ['./shop-information-page.css'],
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
      if (page) this.seo.apply(page.seoTitle || (page.title || 'Meer informatie') + (page.place ? ' in ' + page.place : '') + ' · ' + this.branding.name(), page.seoDescription || page.description.replace(/\s+/g, ' ').slice(0, 200), '/' + page.webAddress);
      else if (this.state()?.error) { this.seo.apply('Pagina niet beschikbaar · ' + this.branding.name(), undefined, '/' + this.route.snapshot.paramMap.get('page')); this.seo.reset('/geen-toegang'); }
    });
  }
}
