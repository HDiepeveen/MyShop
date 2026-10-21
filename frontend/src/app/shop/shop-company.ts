import { PageBackdrop } from './page-backdrop';
import { DestroyRef } from '@angular/core';
import { Component, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { BehaviorSubject, switchMap } from 'rxjs';
import { ShopApi } from './shop.api';
import { ShopBranding } from './shop-branding';
import { StorefrontSeo } from './storefront-seo';
import { loadState } from '../catalog/load-state';

@Component({ styleUrls: ['./shop-information-page.css'], styles: ['.company-text { white-space: pre-wrap; }'], template: `
  @if (state()?.loading) { <p role="status">Bedrijfsinformatie ophalen…</p> }
  @if (state()?.error) { <p role="alert">{{ state()?.error }}</p><button type="button" (click)="retry()">Opnieuw proberen</button> }
  @if (state()?.data; as company) {
    <article class="photo-page" [class.with-image]="company.imageUrl && !imageFailed()">
    <div class="photo-content">
    <div class="eyebrow">{{ branding.name() }}</div>
    <h1>{{ company.heading }}</h1>
    <div class="company-layout">
    @if (company.description || (!company.address && !company.email && !company.phone && !company.openingHours)) {
    <div class="page-content">
      @if (company.description) { <p class="company-text">{{ company.description }}</p> }
      @if (!company.description && !company.address && !company.email && !company.phone && !company.openingHours) {
        <p>Bedrijfsinformatie wordt binnenkort toegevoegd.</p>
      }
    </div>
    }
      <dl class="contact-details">
        @if (company.address) { <div class="contact-detail"><dt><svg class="contact-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M20 10c0 6-8 12-8 12S4 16 4 10a8 8 0 1 1 16 0Z"/><circle cx="12" cy="10" r="2.5"/></svg>Bezoek ons</dt><dd class="company-text">{{ company.address }}</dd></div> }
        @if (company.email) { <div class="contact-detail"><dt><svg class="contact-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="3" y="5" width="18" height="14" rx="2"/><path d="m3 6 9 7 9-7"/></svg>E-mailadres</dt><dd><a [href]="emailLink(company.email)">{{ company.email }}</a></dd></div> }
        @if (company.phone) { <div class="contact-detail"><dt><svg class="contact-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m5 3 4 1 1 4-2 2a16 16 0 0 0 6 6l2-2 4 1 1 4c-1 3-5 2-8 0S5 13 3 7C2 4 3 3 5 3Z"/></svg>Telefoonnummer</dt><dd>{{ company.phone }}</dd></div> }
        @if (company.openingHours) { <div class="contact-detail"><dt><svg class="contact-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/></svg>Openingstijden</dt><dd class="company-text">{{ company.openingHours }}</dd></div> }
      </dl>
    </div>
    </div>
    @if (company.imageUrl && !imageFailed()) { <img class="page-background" [src]="company.imageUrl" [style.object-fit]="company.imageMode === 'stretch' ? 'fill' : 'cover'" alt="" aria-hidden="true" (error)="imageFailed.set(true)" /> }
    </article>
  }
` })
export class ShopCompany {
  private readonly api = inject(ShopApi);
  private readonly backdrop = inject(PageBackdrop);
  private readonly backdropOwner = {};
  readonly imageFailed = signal(false);
  readonly branding = inject(ShopBranding);
  private readonly seo = inject(StorefrontSeo);
  private readonly refresh = new BehaviorSubject(0);
  readonly state = toSignal(this.refresh.pipe(switchMap(() => loadState(this.api.company()))));
  constructor() {
    inject(DestroyRef).onDestroy(()=>this.backdrop.clear(this.backdropOwner));
    effect(()=>{const page=this.state()?.data;this.backdrop.show(this.backdropOwner,this.imageFailed()?null:page?.imageUrl,page?.imageMode);});
    effect(() => { const company = this.state()?.data;
      if (company) this.seo.apply(company.seoTitle || company.heading + ' · ' + this.branding.name(),
        company.seoDescription || company.description?.replace(/\s+/g, ' ').slice(0, 200), '/contact');
    });
  }
  retry() { if (!this.state()?.loading) this.refresh.next(this.refresh.value + 1); }
  emailLink(email: string) { return 'mailto:' + encodeURIComponent(email); }
}
