import { HttpClient } from '@angular/common/http';
import { Auth } from '../auth/auth';
import { switchMap } from 'rxjs';
import { ShopBranding } from '../shop/shop-branding';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CatalogApi } from '../catalog/catalog.api';
import { errorMessage } from '../catalog/error-message';
import { CompanyPage, HomePageSettings, ShopSeoSettings } from './seo.models';
@Component({ imports: [FormsModule, RouterLink], template: `
  <h1>Webshop en SEO</h1>
  <p>De koptekst staat boven je producten. De SEO-titel wordt gebruikt voor het browsertabblad en als aanwijzing voor zoekmachines.</p>
  @if (loading()) { <p role="status">Instellingen ophalen…</p> }
  @if (error()) { <p role="alert">{{ error() }}</p><button type="button" [disabled]="busy() || loading()" (click)="load()">Opnieuw ophalen</button> }
  @if (settings()) {
    <form class="panel" (ngSubmit)="save()">
      <label>Plaats website (optioneel)<input name="sitePlace" [(ngModel)]="home.place" maxlength="100" placeholder="Bijvoorbeeld Zwolle" [disabled]="busy()" /></label>
      <label>Webshopnaam<input name="shopName" [(ngModel)]="shopName" maxlength="100" required [disabled]="busy()" /></label>
      <label>Extra tekst in titelbalk<input name="headerText" [(ngModel)]="headerText" maxlength="200" [disabled]="busy()" /></label>
      <p>Laat leeg om geen extra tekst in de titelbalk te tonen.</p>
      <label>Welkomsttekst<input name="welcomeText" [(ngModel)]="welcomeText" maxlength="200" required [disabled]="busy()" /></label>
      <label>Koptekst assortiment<input name="heading" [(ngModel)]="heading" maxlength="200" required [disabled]="busy()" /></label>
      <label>Introductietekst<textarea name="introduction" [(ngModel)]="introduction" maxlength="1000" required [disabled]="busy()"></textarea></label>
      <label>SEO-titel assortiment<input name="seoTitle" [(ngModel)]="seoTitle" maxlength="200" required [disabled]="busy()" /></label>
      <label>Tekst onderaan de winkel<input name="footerText" [(ngModel)]="footerText" maxlength="200" [disabled]="busy()" placeholder="Ontdek wat bij je past." /></label>
      <h2>Homepagina</h2>
      @if (settings()?.home?.enabled) { <p><a routerLink="/">Opgeslagen homepagina bekijken</a></p> }
      <p>Home toont de eigen tekst hieronder. Extra pagina’s staan in de titelbalk. Hun titel en de algemene websiteplaats bepalen het webadres bij de eerste opslag; daarna blijft het adres behouden.</p>
      <label><input type="checkbox" name="homeEnabled" [(ngModel)]="home.enabled" [disabled]="busy()" />Homepagina inschakelen</label>
      <label>Koptekst homepagina<input name="homeHeading" [(ngModel)]="home.heading" maxlength="200" required [disabled]="busy()" /></label>
      <label>Tekst homepagina<textarea name="homeDescription" [(ngModel)]="home.description" maxlength="4000" rows="7" [disabled]="busy()"></textarea></label>
      <fieldset [disabled]="busy() || loading()">
        <legend>Achtergrondafbeelding homepagina</legend>
        @if (settings()?.homeImageUrl; as image) { <img class="home-image-preview" [src]="image" alt="Huidige afbeelding homepagina" /> }
        <label>Afbeelding kiezen<input #homeImageInput type="file" accept="image/jpeg,image/png" (change)="chooseHomeImage(homeImageInput)" /></label>
        <p>JPEG of PNG, maximaal 5 MB en 20 miljoen pixels. De afbeelding vult de breedte achter de welkomsttekst en kan daarbij worden bijgesneden.</p>
        <button type="button" [disabled]="!homeImageFile" (click)="changeHomeImage(homeImageInput)">Afbeelding uploaden</button>
        @if (settings()?.homeImageUrl) { <button type="button" class="secondary" (click)="changeHomeImage(homeImageInput, true)">Afbeelding verwijderen</button> }
      </fieldset>
      <h2>Extra pagina’s</h2>
      @for (item of home.items; track item; let i = $index) {
        <fieldset [disabled]="busy()">
          <legend>Pagina {{ i + 1 }}</legend>
          <label>Titel (optioneel)<input [name]="'homeTitle' + i" [(ngModel)]="item.title" maxlength="200" /></label>
          @if (item.webAddress) { <p>Webadres: <a [routerLink]="'/' + item.webAddress">/{{ item.webAddress }}</a></p> }
          <label>Omschrijving<textarea [name]="'homeDescription' + i" [(ngModel)]="item.description" maxlength="4000" rows="5" required></textarea></label>
          <button type="button" class="secondary" [disabled]="i === 0" (click)="moveHomeItem(i, -1)">Omhoog</button>
          <button type="button" class="secondary" [disabled]="i === home.items.length - 1" (click)="moveHomeItem(i, 1)">Omlaag</button>
          <button type="button" class="secondary" (click)="home.items.splice(i, 1)">Pagina verwijderen</button>
        </fieldset>
      }
      <button type="button" class="secondary" [disabled]="busy() || home.items.length >= 20" (click)="home.items.push({title: '', description: ''})">Pagina toevoegen</button>
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
`, styles: [`.home-image-preview { display: block; max-width: 300px; max-height: 180px; object-fit: contain; border-radius: 12px; }`] })
export class SeoSettings {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(Auth);
  homeImageFile: File | null = null;
  private readonly branding = inject(ShopBranding);
  private readonly api = inject(CatalogApi);
  private readonly destroyRef = inject(DestroyRef);
  readonly settings = signal<ShopSeoSettings | null>(null);
  readonly busy = signal(false); readonly loading = signal(false); readonly error = signal(''); readonly notice = signal('');
  heading = ''; seoTitle = ''; shopName = 'MyShop'; welcomeText = 'Welkom bij MyShop'; introduction = 'Bekijk onze producten en kies de variant die bij je past.';
  headerText = '';
  footerText = 'Ontdek wat bij je past.';
  home: HomePageSettings = { enabled: false, heading: 'Welkom', items: [] };
  moveHomeItem(index: number, direction: number) { const target = index + direction; if (this.busy() || target < 0 || target >= this.home.items.length) return; [this.home.items[index], this.home.items[target]] = [this.home.items[target], this.home.items[index]]; }
  company: CompanyPage = { heading: 'Over ons en contact', name: null, description: null, address: null, email: null, phone: null, openingHours: null };
  constructor() { this.load(); }
  load() {
    if (this.loading() || this.busy()) return;
    this.loading.set(true); this.error.set('');
    this.api.seoSettings().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: settings => { this.settings.set(settings); this.headerText = settings.headerText ?? ''; this.home = structuredClone(settings.home ?? { enabled: false, heading: 'Welkom', items: [] }); this.branding.homeEnabled.set(this.home.enabled); this.heading = settings.heading; this.seoTitle = settings.seoTitle; this.shopName = settings.shopName ?? 'MyShop'; this.branding.name.set(this.shopName); this.welcomeText = settings.welcomeText ?? 'Welkom bij MyShop'; this.introduction = settings.introduction ?? 'Bekijk onze producten en kies de variant die bij je past.'; this.footerText = settings.footerText ?? 'Ontdek wat bij je past.'; this.branding.footerText.set(this.footerText); this.company = { ...(settings.company ?? { heading: 'Over ons en contact', name: null, description: null, address: null, email: null, phone: null, openingHours: null }) }; this.loading.set(false); },
      error: error => { this.loading.set(false); this.error.set(errorMessage(error)); },
    });
  }
  chooseHomeImage(input: HTMLInputElement) {
    this.homeImageFile = null; this.error.set('');
    const file = input.files?.[0];
    if (!file) return;
    if (!['image/jpeg', 'image/png'].includes(file.type) || file.size > 5 * 1024 * 1024) { this.error.set('Kies een JPEG- of PNG-afbeelding van maximaal 5 MB.'); input.value = ''; return; }
    this.homeImageFile = file;
  }
  changeHomeImage(input: HTMLInputElement, remove = false) {
    const settings = this.settings();
    if (!settings || this.busy() || this.loading() || (!remove && !this.homeImageFile)) return;
    const form = new FormData(); form.append('revision', settings.revision);
    if (this.homeImageFile) form.append('file', this.homeImageFile, this.homeImageFile.name);
    this.busy.set(true); this.error.set(''); this.notice.set('');
    this.auth.prepare().pipe(
      switchMap(() => remove ? this.http.delete('/api/home-image', { body: { revision: settings.revision } }) : this.http.post('/api/home-image', form)),
      switchMap(() => this.api.seoSettings()), takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: stored => { this.settings.set(stored); this.homeImageFile = null; input.value = ''; this.busy.set(false); this.notice.set(remove ? 'Afbeelding verwijderd.' : 'Afbeelding opgeslagen.'); },
      error: error => { this.busy.set(false); this.error.set(errorMessage(error)); },
    });
  }
  save() {
    const settings = this.settings();
    if (!settings || this.busy() || this.loading() || !this.heading.trim() || !this.seoTitle.trim() || !this.shopName.trim() || !this.welcomeText.trim() || !this.introduction.trim() || !this.company.heading.trim()) return;
    if (!this.home.heading.trim() || this.home.items.some(item => !item.description.trim())) { this.error.set('Vul de koptekst en omschrijvingen in.'); return; }
    this.busy.set(true); this.error.set(''); this.notice.set('');
    this.api.updateSeoSettings({ heading: this.heading, seoTitle: this.seoTitle, revision: settings.revision, shopName: this.shopName, welcomeText: this.welcomeText, introduction: this.introduction, footerText: this.footerText, headerText: this.headerText, company: this.company, home: this.home }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => { this.busy.set(false); this.notice.set('Webshopinstellingen opgeslagen.'); this.load(); },
      error: error => { this.busy.set(false); this.error.set(errorMessage(error)); },
    });
  }
}
