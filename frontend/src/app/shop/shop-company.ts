import { Component, effect, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { BehaviorSubject, switchMap } from 'rxjs';
import { ShopApi } from './shop.api';
import { ShopBranding } from './shop-branding';
import { StorefrontSeo } from './storefront-seo';
import { loadState } from '../catalog/load-state';

@Component({ imports: [RouterLink], styles: ['.company-text { white-space: pre-wrap; }'], template: `
  <a routerLink="/winkel">← Terug naar het assortiment</a>
  @if (state()?.loading) { <p role="status">Bedrijfsinformatie ophalen…</p> }
  @if (state()?.error) { <p role="alert">{{ state()?.error }}</p><button type="button" (click)="retry()">Opnieuw proberen</button> }
  @if (state()?.data; as company) {
    <h1>{{ company.heading }}</h1>
    <div class="panel">
      <h2>{{ company.name }}</h2>
      @if (company.description) { <p class="company-text">{{ company.description }}</p> }
      @if (!company.description && !company.address && !company.email && !company.phone && !company.openingHours) {
        <p>Bedrijfsinformatie wordt binnenkort toegevoegd.</p>
      }
      <dl class="detail-list">
        @if (company.address) { <dt>Adres</dt><dd class="company-text">{{ company.address }}</dd> }
        @if (company.email) { <dt>E-mailadres</dt><dd><a [href]="emailLink(company.email)">{{ company.email }}</a></dd> }
        @if (company.phone) { <dt>Telefoonnummer</dt><dd>{{ company.phone }}</dd> }
        @if (company.openingHours) { <dt>Openingstijden</dt><dd class="company-text">{{ company.openingHours }}</dd> }
      </dl>
    </div>
  }
` })
export class ShopCompany {
  private readonly api = inject(ShopApi);
  private readonly branding = inject(ShopBranding);
  private readonly seo = inject(StorefrontSeo);
  private readonly refresh = new BehaviorSubject(0);
  readonly state = toSignal(this.refresh.pipe(switchMap(() => loadState(this.api.company()))));
  constructor() {
    effect(() => { const company = this.state()?.data;
      if (company) this.seo.apply(company.heading + ' · ' + this.branding.name(),
        company.description?.replace(/\s+/g, ' ').slice(0, 200), '/winkel/informatie/bedrijf');
    });
  }
  retry() { if (!this.state()?.loading) this.refresh.next(this.refresh.value + 1); }
  emailLink(email: string) { return 'mailto:' + encodeURIComponent(email); }
}
