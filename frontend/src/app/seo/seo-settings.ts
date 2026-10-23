import { currencies, knownCurrency } from '../catalog/currencies';
import { PageBackgroundEditor } from './page-background-editor';
import { HttpClient } from '@angular/common/http';
import { Auth } from '../auth/auth';
import { switchMap } from 'rxjs';
import { ShopBranding } from '../shop/shop-branding';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CatalogApi } from '../catalog/catalog.api';
import { errorMessage } from '../catalog/error-message';
import { CompanyPage, HomePageSettings, ShopSeoSettings } from './seo.models';
@Component({ imports: [FormsModule, RouterLink, PageBackgroundEditor], template: `
  <h1>Website en SEO</h1>
  <p>De koptekst staat boven je producten. De SEO-titel wordt gebruikt voor het browsertabblad en als aanwijzing voor zoekmachines.</p>
  @if (loading()) { <p role="status">Instellingen ophalen…</p> }
  @if (error()) { <p role="alert">{{ error() }}</p><button type="button" [disabled]="busy() || loading()" (click)="load()">Opnieuw ophalen</button> }
  @if (settings()) {
    <form class="panel" #settingsForm="ngForm" (ngSubmit)="save(settingsForm)">
      <h2>Website</h2>
      <label>Plaats website (optioneel)<input name="sitePlace" [(ngModel)]="home.place" maxlength="100" placeholder="Bijvoorbeeld Zwolle" [disabled]="busy()" /></label>
      <label>Naam<input name="shopName" [(ngModel)]="shopName" [class.invalid-required]="submitted() && !shopName.trim()" [attr.aria-invalid]="submitted() && !shopName.trim() ? 'true' : null" maxlength="100" required [disabled]="busy()" /></label>
      <fieldset [disabled]="busy()"><legend>Icoontjes in menubalk</legend>
      <p>Gebruikt het telefoonnummer uit Contact. Zonder telefoonnummer wordt dit onderdeel niet getoond.</p>
      <label><input type="checkbox" name="showHeaderPhone" [(ngModel)]="showHeaderPhone" />Telefoon</label>
      <label><input type="checkbox" name="showHeaderWhatsapp" [(ngModel)]="showHeaderWhatsapp" />WhatsApp</label></fieldset>
      <label>Tekst onderaan de winkel<input name="footerText" [(ngModel)]="footerText" maxlength="200" [disabled]="busy()" placeholder="Ontdek wat bij je past." /></label>
      <label>Valuta<select name="currency" [(ngModel)]="currency" required [disabled]="busy()" [class.invalid-required]="submitted() && !validCurrency()">@if (currency && !knownCurrency(currency)) { <option [value]="currency">{{ currency }} (huidige valuta)</option> }
          @for (item of currencies; track item.code) { <option [value]="item.code">{{ item.name }}</option> }</select></label>
      <p>Bij wijziging krijgen alle bestaande productprijzen deze valuta; de prijsbedragen blijven exact gelijk en worden niet omgerekend. Daarna kun je de valuta per productprijs weer aanpassen.</p>
      <h2>Assortiment</h2>
      <label>Koptekst assortiment<input name="heading" [(ngModel)]="heading" [class.invalid-required]="submitted() && !heading.trim()" [attr.aria-invalid]="submitted() && !heading.trim() ? 'true' : null" maxlength="200" required [disabled]="busy()" /></label>
      <label>Introductietekst<textarea name="introduction" [(ngModel)]="introduction" [class.invalid-required]="submitted() && !introduction.trim()" [attr.aria-invalid]="submitted() && !introduction.trim() ? 'true' : null" maxlength="1000" required [disabled]="busy()"></textarea></label>
      <app-page-background-editor pageId="d2f06ded-68a2-4936-9880-327d616d1a42" [revision]="settings()!.revision" [imageUrl]="settings()?.assortmentImageUrl" [(mode)]="assortmentImageMode" [disabled]="busy()" (working)="busy.set($event)" (stored)="backgroundStored($event)" />
      <label>SEO-titel assortiment<input name="seoTitle" [(ngModel)]="seoTitle" [class.invalid-required]="submitted() && !seoTitle.trim()" [attr.aria-invalid]="submitted() && !seoTitle.trim() ? 'true' : null" maxlength="200" required [disabled]="busy()" /></label>
      <label>SEO-omschrijving assortiment<textarea name="seoDescription" [(ngModel)]="seoDescription" maxlength="500" [disabled]="busy()"></textarea></label>
      <h2>Home</h2>
      <p>SEO-velden zijn optioneel. Leeg gebruikt de automatische titel of omschrijving. Een ingevulde SEO-titel wordt exact gebruikt, zonder automatische toevoegingen.</p>
      @if (settings()?.home?.enabled) { <p><a routerLink="/">Opgeslagen homepagina bekijken</a></p> }
      <p>Home toont de eigen tekst hieronder. Extra pagina’s staan in de menubalk. Hun verplichte titel bepaalt het webadres, zonder plaatsnaam. Na opslaan verandert het webadres mee als je de titel wijzigt. De websiteplaats blijft beschikbaar voor de automatische SEO-titel.</p>
      <label><input type="checkbox" name="homeEnabled" [(ngModel)]="home.enabled" [disabled]="busy()" />Homepagina inschakelen</label>
      <label>Titel<input name="homeHeading" [(ngModel)]="home.heading" [class.invalid-required]="submitted() && !home.heading.trim()" [attr.aria-invalid]="submitted() && !home.heading.trim() ? 'true' : null" maxlength="200" required [disabled]="busy()" /></label>
      <label>Omschrijving<textarea name="homeDescription" [(ngModel)]="home.description" maxlength="4000" rows="7" [disabled]="busy()"></textarea></label>
      <fieldset [disabled]="busy() || loading()">
        <legend>Achtergrondfoto</legend>
        @if (settings()?.homeImageUrl; as image) { <img class="home-image-preview" [src]="image" alt="Huidige afbeelding homepagina" /> }
      <label>Weergave achtergrondfoto<select name="homeImageMode" [ngModel]="home.imageMode || 'cover'" (ngModelChange)="home.imageMode = $event" [disabled]="busy()"><option value="stretch">Uitrekken over het hele vlak</option><option value="cover">Verhoudingen behouden, gecentreerd bijsnijden</option></select></label>
        <label>Afbeelding kiezen<input #homeImageInput type="file" accept="image/jpeg,image/png" (change)="chooseHomeImage(homeImageInput)" /></label>
        <p>JPEG of PNG, maximaal 5 MB en 20 miljoen pixels. De foto vult de Homepagina onder de menubalk. Kies hieronder uitrekken of verhoudingen behouden.</p>
        <button type="button" [disabled]="!homeImageFile" (click)="changeHomeImage(homeImageInput)">Afbeelding uploaden</button>
        @if (settings()?.homeImageUrl) { <button type="button" class="secondary" (click)="changeHomeImage(homeImageInput, true)">Afbeelding verwijderen</button> }
      </fieldset>
      <label>SEO-titel homepagina<input name="homeSeoTitle" [(ngModel)]="home.seoTitle" maxlength="200" [disabled]="busy()" /></label>
      <label>SEO-omschrijving homepagina<textarea name="homeSeoDescription" [(ngModel)]="home.seoDescription" maxlength="500" [disabled]="busy()"></textarea></label>
      <h2>Extra pagina’s</h2>
      @for (item of home.items; track item; let i = $index) {
        <fieldset [disabled]="busy()">
          <legend>Pagina {{ i + 1 }}</legend>
          <label>Titel<input [name]="'homeTitle' + i" [(ngModel)]="item.title" [class.invalid-required]="submitted() && !item.title?.trim()" [attr.aria-invalid]="submitted() && !item.title?.trim() ? 'true' : null" maxlength="200" required /></label>
          @if (item.webAddress) { <p>Huidig webadres (wordt bijgewerkt na opslaan): <a [routerLink]="'/' + item.webAddress">/{{ item.webAddress }}</a></p> }
          <label>Omschrijving<textarea [name]="'homeDescription' + i" [(ngModel)]="item.description" [class.invalid-required]="submitted() && !item.description.trim()" [attr.aria-invalid]="submitted() && !item.description.trim() ? 'true' : null" maxlength="4000" rows="5" required></textarea></label>
          <app-page-background-editor [pageId]="item.pageId" [revision]="settings()!.revision" [imageUrl]="item.imageUrl" [(mode)]="item.imageMode" [disabled]="busy()" (working)="busy.set($event)" (stored)="backgroundStored($event)" />
          <label>SEO-titel (optioneel)<input [name]="'pageSeoTitle' + i" [(ngModel)]="item.seoTitle" maxlength="200" /></label>
          <label>SEO-omschrijving (optioneel)<textarea [name]="'pageSeoDescription' + i" [(ngModel)]="item.seoDescription" maxlength="500"></textarea></label>
          <button type="button" class="secondary" [disabled]="i === 0" (click)="moveHomeItem(i, -1)">Omhoog</button>
          <button type="button" class="secondary" [disabled]="i === home.items.length - 1" (click)="moveHomeItem(i, 1)">Omlaag</button>
          <button type="button" class="secondary" (click)="home.items.splice(i, 1)">Pagina verwijderen</button>
        </fieldset>
      }
      <button type="button" class="secondary" [disabled]="busy() || home.items.length >= 20" (click)="home.items.push({title: '', description: ''})">Pagina toevoegen</button>
      <h2>Contact</h2>
      <p>Deze informatie is openbaar op de bedrijfspagina. De bedrijfsgegevens voor facturen stel je apart in bij Facturatie en btw.</p>
      <label>Titel<input name="companyHeading" [(ngModel)]="company.heading" [class.invalid-required]="submitted() && !company.heading.trim()" [attr.aria-invalid]="submitted() && !company.heading.trim() ? 'true' : null" maxlength="200" required [disabled]="busy()" /></label>
      <label>Omschrijving<textarea name="companyDescription" [(ngModel)]="company.description" maxlength="4000" rows="6" [disabled]="busy()"></textarea></label>
      <label>Adres<textarea name="companyAddress" [(ngModel)]="company.address" maxlength="500" rows="3" [disabled]="busy()"></textarea></label>
      <label>E-mailadres<input name="companyEmail" type="email" [(ngModel)]="company.email" maxlength="254" [disabled]="busy()" /></label>
      <label>Telefoonnummer<input name="companyPhone" type="tel" [(ngModel)]="company.phone" maxlength="100" [disabled]="busy()" /></label>
      <label>Openingstijden<textarea name="companyOpeningHours" [(ngModel)]="company.openingHours" maxlength="1000" rows="4" [disabled]="busy()"></textarea></label>
      <app-page-background-editor pageId="ac8107f2-a136-4b4f-9e85-b70f14860701" [revision]="settings()!.revision" [imageUrl]="company.imageUrl" [(mode)]="company.imageMode" [disabled]="busy()" (working)="busy.set($event)" (stored)="backgroundStored($event)" />
      <label>SEO-titel contactpagina<input name="companySeoTitle" [(ngModel)]="company.seoTitle" maxlength="200" [disabled]="busy()" /></label>
      <label>SEO-omschrijving contactpagina<textarea name="companySeoDescription" [(ngModel)]="company.seoDescription" maxlength="500" [disabled]="busy()"></textarea></label>
      @if (submitted() && invalidPageTitles()) { <p class="validation-error">Vul voor elke extra pagina een titel in. Controleer de rood gemarkeerde velden.</p> }
      <button type="submit" [disabled]="busy() || loading()">{{ busy() ? 'Opslaan…' : 'Instellingen opslaan' }}</button>
    </form>
  }
  @if (notice()) { <p role="status">{{ notice() }}</p> }
`, styles: [`input.invalid-required, textarea.invalid-required, select.invalid-required, input.ng-invalid.ng-touched, textarea.ng-invalid.ng-touched { border: 2px solid #b42318; outline-color: #b42318; }
  .validation-error { color: #b42318; }
  .home-image-preview { display: block; max-width: 300px; max-height: 180px; object-fit: contain; border-radius: 12px; }`] })
export class SeoSettings {
  readonly currencies = currencies;
  readonly knownCurrency = knownCurrency;
  private readonly http = inject(HttpClient);
  private readonly auth = inject(Auth);
  homeImageFile: File | null = null;
  private readonly branding = inject(ShopBranding);
  private readonly api = inject(CatalogApi);
  private readonly destroyRef = inject(DestroyRef);
  readonly submitted = signal(false);
  readonly settings = signal<ShopSeoSettings | null>(null);
  readonly busy = signal(false); readonly loading = signal(false); readonly error = signal(''); readonly notice = signal('');
  heading = ''; seoTitle = ''; shopName = 'HD Web Framework'; welcomeText = 'Welkom bij HD Web Framework'; introduction = 'Bekijk onze producten en kies de variant die bij je past.';
  currency = 'EUR';
  validCurrency() { return /^[A-Za-z]{3}$/.test(this.currency.trim()); }
  seoDescription = '';
  showHeaderPhone = true; showHeaderWhatsapp = true;
  assortmentImageMode: string | null | undefined;
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
      next: settings => { this.submitted.set(false); this.settings.set(settings); this.currency = settings.currency ?? 'EUR'; this.assortmentImageMode = settings.assortmentImageMode; this.headerText = settings.headerText ?? ''; this.showHeaderPhone = settings.showHeaderPhone !== false; this.showHeaderWhatsapp = settings.showHeaderWhatsapp !== false; this.seoDescription = settings.seoDescription ?? ''; this.home = structuredClone(settings.home ?? { enabled: false, heading: 'Welkom', items: [] }); this.branding.homeEnabled.set(this.home.enabled); this.heading = settings.heading; this.seoTitle = settings.seoTitle; this.shopName = settings.shopName ?? 'HD Web Framework'; this.branding.name.set(this.shopName); this.welcomeText = settings.welcomeText ?? 'Welkom bij HD Web Framework'; this.introduction = settings.introduction ?? 'Bekijk onze producten en kies de variant die bij je past.'; this.footerText = settings.footerText ?? 'Ontdek wat bij je past.'; this.branding.footerText.set(this.footerText); this.company = { ...(settings.company ?? { heading: 'Over ons en contact', name: null, description: null, address: null, email: null, phone: null, openingHours: null }) }; this.loading.set(false); },
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
  backgroundStored(stored: ShopSeoSettings) {
    this.settings.set(stored);
    for(const item of this.home.items) item.imageUrl=stored.home?.items.find(x=>x.pageId===item.pageId)?.imageUrl;
    this.company.imageUrl=stored.company.imageUrl;
  }
  invalidPageTitles() { return this.home.items.some(item => !item.title?.trim()); }
  save(form?: NgForm) {
    const settings = this.settings();
    if (!settings || this.busy() || this.loading()) return;
    this.submitted.set(true); form?.form.markAllAsTouched();
    if (!this.validCurrency() || form?.invalid || !this.heading.trim() || !this.seoTitle.trim() || !this.shopName.trim() || !this.introduction.trim() || !this.company.heading.trim()) { this.error.set('Controleer de rood gemarkeerde velden. Vul alle verplichte velden in, waaronder de titel van elke pagina.'); return; }
    if (!this.home.heading.trim() || this.invalidPageTitles() || this.home.items.some(item => !item.description.trim())) { this.error.set('Controleer de rood gemarkeerde velden. Vul de koptekst en voor elke pagina een titel en omschrijving in.'); return; }
    this.busy.set(true); this.error.set(''); this.notice.set('');
    this.api.updateSeoSettings({ currency: this.currency.trim().toUpperCase(), heading: this.heading, seoTitle: this.seoTitle, seoDescription: this.seoDescription, revision: settings.revision, shopName: this.shopName, welcomeText: this.welcomeText, introduction: this.introduction, footerText: this.footerText, headerText: this.headerText, assortmentImageMode: this.assortmentImageMode, showHeaderPhone: this.showHeaderPhone, showHeaderWhatsapp: this.showHeaderWhatsapp, company: this.company, home: this.home }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => { this.busy.set(false); this.notice.set('Webshopinstellingen opgeslagen.'); this.load(); },
      error: error => { this.busy.set(false); this.error.set(errorMessage(error)); },
    });
  }
}
