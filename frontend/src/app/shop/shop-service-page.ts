import { PageBackdrop } from './page-backdrop';
import { DestroyRef } from '@angular/core';
import { Component, effect, inject, signal } from '@angular/core';
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
    @if (state()?.loading) { <p role="status">Pagina ophalen…</p> }
    @if (state()?.error) { <h1>Pagina niet beschikbaar</h1><p>Deze pagina bestaat niet of is niet gepubliceerd.</p> }
    @if (state()?.data; as page) {
      <article class="photo-page" [class.with-image]="page.imageUrl && !imageFailed()">
    <div class="photo-content">
        <div class="eyebrow">{{ branding.name() }}</div>
        <h1>{{ page.title || 'Meer informatie' }}</h1>
        <div class="information-layout">
          <div class="page-content"><p>{{ page.description }}</p></div>
          <aside class="contact-card">
            <span class="eyebrow">Persoonlijk contact</span>
            <h2>Hoe kunnen we u helpen?</h2>
            <p>Heeft u een vraag of wilt u een afspraak bespreken? Neem gerust contact met ons op.</p>
            <a class="button" routerLink="/contact">Neem contact op <span aria-hidden="true">↗</span></a>
          </aside>
        </div>
      </div>
    @if (page.imageUrl && !imageFailed()) { <img class="page-background" [src]="page.imageUrl" [style.object-fit]="page.imageMode === 'stretch' ? 'fill' : 'cover'" alt="" aria-hidden="true" (error)="imageFailed.set(true)" /> }
    </article>
    }
  `,
  styleUrls: ['./shop-information-page.css'],
})
export class ShopServicePage {
  private readonly backdrop = inject(PageBackdrop);
  private readonly backdropOwner = {};
  readonly imageFailed = signal(false);
  readonly branding = inject(ShopBranding);
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly seo = inject(StorefrontSeo);
  readonly state = toSignal(this.route.paramMap.pipe(switchMap(params => loadState(
    this.http.get<HomePageSettings['items'][number]>('/api/shop/pages/' + encodeURIComponent(params.get('page') ?? '')),
  ))));
  constructor() {
    inject(DestroyRef).onDestroy(()=>this.backdrop.clear(this.backdropOwner));
    effect(()=>{const page=this.state()?.data;this.backdrop.show(this.backdropOwner,this.imageFailed()?null:page?.imageUrl,page?.imageMode);});
    effect(() => {
      const page = this.state()?.data;
      this.imageFailed.set(false);
      if (page) this.seo.apply(page.seoTitle || (page.title || 'Meer informatie') + (page.place ? ' in ' + page.place : '') + ' · ' + this.branding.name(), page.seoDescription || page.description.replace(/\s+/g, ' ').slice(0, 200), '/' + page.webAddress);
      else if (this.state()?.error) { this.seo.apply('Pagina niet beschikbaar · ' + this.branding.name(), undefined, '/' + this.route.snapshot.paramMap.get('page')); this.seo.reset('/geen-toegang'); }
    });
  }
}
