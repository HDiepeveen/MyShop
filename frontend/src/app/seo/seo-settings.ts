import { ShopBranding } from '../shop/shop-branding';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CatalogApi } from '../catalog/catalog.api';
import { errorMessage } from '../catalog/error-message';
import { CompanyPage, ShopSeoSettings } from './seo.models';
@Component({ imports: [FormsModule], template: `
  <h1>Webshop en SEO</h1>
  <p>De koptekst staat boven je producten. De SEO-titel wordt gebruikt voor het browsertabblad en als aanwijzing voor zoekmachines.</p>
  @if (loading()) { <p role="status">Instellingen ophalen…</p> }
  @if (error()) { <p role="alert">{{ error() }}</p><button type="button" [disabled]="busy() || loading()" (click)="load()">Opnieuw ophalen</button> }
  @if (settings()) {
    <form class="panel" (ngSubmit)="save()">
      <label>Webshopnaam<input name="shopName" [(ngModel)]="shopName" maxlength="100" required [disabled]="busy()" /></label>
      <label>Welkomsttekst<input name="welcomeText" [(ngModel)]="welcomeText" maxlength="200" required [disabled]="busy()" /></label>
      <label>Koptekst assortiment<input name="heading" [(ngModel)]="heading" maxlength="200" required [disabled]="busy()" /></label>
      <label>Introductietekst<textarea name="introduction" [(ngModel)]="introduction" maxlength="1000" required [disabled]="busy()"></textarea></label>
      <label>SEO-titel assortiment<input name="seoTitle" [(ngModel)]="seoTitle" maxlength="200" required [disabled]="busy()" /></label>
      <label>Tekst onderaan de winkel<input name="footerText" [(ngModel)]="footerText" maxlength="200" [disabled]="busy()" placeholder="Ontdek wat bij je past." /></label>
      <h2>Bedrijfsinformatie</h2>
      <p>Deze informatie is openbaar op de bedrijfspagina. De bedrijfsgegevens voor facturen stel je apart in bij Facturatie en btw.</p>
      <label>Koptekst bedrijfspagina<input name="companyHeading" [(ngModel)]="company.heading" maxlength="200" required [disabled]="busy()" /></label>
      <label>Bedrijfsnaam<input name="companyName" [(ngModel)]="company.name" maxlength="200" [disabled]="busy()" placeholder="Gebruik de webshopnaam als dit leeg blijft" /></label>
      <label>Over het bedrijf<textarea name="companyDescription" [(ngModel)]="company.description" maxlength="4000" rows="6" [disabled]="busy()"></textarea></label>
      <label>Adres<textarea name="companyAddress" [(ngModel)]="company.address" maxlength="500" rows="3" [disabled]="busy()"></textarea></label>
      <label>Contact e-mailadres<input name="companyEmail" type="email" [(ngModel)]="company.email" maxlength="254" [disabled]="busy()" /></label>
      <label>Telefoonnummer<input name="companyPhone" type="tel" [(ngModel)]="company.phone" maxlength="100" [disabled]="busy()" /></label>
      <label>Openingstijden<textarea name="companyOpeningHours" [(ngModel)]="company.openingHours" maxlength="1000" rows="4" [disabled]="busy()"></textarea></label>
      <button [disabled]="busy() || loading() || !heading.trim() || !seoTitle.trim() || !shopName.trim() || !welcomeText.trim() || !introduction.trim() || !company.heading.trim()">{{ busy() ? 'Opslaan…' : 'Instellingen opslaan' }}</button>
    </form>
  }
  @if (notice()) { <p role="status">{{ notice() }}</p> }
` })
export class SeoSettings {
  private readonly branding = inject(ShopBranding);
  private readonly api = inject(CatalogApi);
  private readonly destroyRef = inject(DestroyRef);
  readonly settings = signal<ShopSeoSettings | null>(null);
  readonly busy = signal(false); readonly loading = signal(false); readonly error = signal(''); readonly notice = signal('');
  heading = ''; seoTitle = ''; shopName = 'MyShop'; welcomeText = 'Welkom bij MyShop'; introduction = 'Bekijk onze producten en kies de variant die bij je past.';
  footerText = 'Ontdek wat bij je past.';
  company: CompanyPage = { heading: 'Over ons en contact', name: null, description: null, address: null, email: null, phone: null, openingHours: null };
  constructor() { this.load(); }
  load() {
    if (this.loading() || this.busy()) return;
    this.loading.set(true); this.error.set('');
    this.api.seoSettings().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: settings => { this.settings.set(settings); this.heading = settings.heading; this.seoTitle = settings.seoTitle; this.shopName = settings.shopName ?? 'MyShop'; this.branding.name.set(this.shopName); this.welcomeText = settings.welcomeText ?? 'Welkom bij MyShop'; this.introduction = settings.introduction ?? 'Bekijk onze producten en kies de variant die bij je past.'; this.footerText = settings.footerText ?? 'Ontdek wat bij je past.'; this.branding.footerText.set(this.footerText); this.company = { ...(settings.company ?? { heading: 'Over ons en contact', name: null, description: null, address: null, email: null, phone: null, openingHours: null }) }; this.loading.set(false); },
      error: error => { this.loading.set(false); this.error.set(errorMessage(error)); },
    });
  }
  save() {
    const settings = this.settings();
    if (!settings || this.busy() || this.loading() || !this.heading.trim() || !this.seoTitle.trim() || !this.shopName.trim() || !this.welcomeText.trim() || !this.introduction.trim() || !this.company.heading.trim()) return;
    this.busy.set(true); this.error.set(''); this.notice.set('');
    this.api.updateSeoSettings({ heading: this.heading, seoTitle: this.seoTitle, revision: settings.revision, shopName: this.shopName, welcomeText: this.welcomeText, introduction: this.introduction, footerText: this.footerText, company: this.company }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => { this.busy.set(false); this.notice.set('Webshopinstellingen opgeslagen.'); this.load(); },
      error: error => { this.busy.set(false); this.error.set(errorMessage(error)); },
    });
  }
}
