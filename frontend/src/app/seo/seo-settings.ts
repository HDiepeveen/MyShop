import { Component, DestroyRef, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CatalogApi } from '../catalog/catalog.api';
import { errorMessage } from '../catalog/error-message';
import { ShopSeoSettings } from './seo.models';
@Component({ imports: [FormsModule], template: `
  <h1>SEO voor het assortiment</h1>
  <p>De koptekst staat boven je producten. De SEO-titel wordt gebruikt voor het browsertabblad en als aanwijzing voor zoekmachines.</p>
  @if (loading()) { <p role="status">Instellingen ophalen…</p> }
  @if (error()) { <p role="alert">{{ error() }}</p><button type="button" [disabled]="busy() || loading()" (click)="load()">Opnieuw ophalen</button> }
  @if (settings()) {
    <form class="panel" (ngSubmit)="save()">
      <label>Koptekst assortiment<input name="heading" [(ngModel)]="heading" maxlength="200" required [disabled]="busy()" /></label>
      <label>SEO-titel assortiment<input name="seoTitle" [(ngModel)]="seoTitle" maxlength="200" required [disabled]="busy()" /></label>
      <button [disabled]="busy() || loading() || !heading.trim() || !seoTitle.trim()">{{ busy() ? 'Opslaan…' : 'SEO-instellingen opslaan' }}</button>
    </form>
  }
  @if (notice()) { <p role="status">{{ notice() }}</p> }
` })
export class SeoSettings {
  private readonly api = inject(CatalogApi);
  private readonly destroyRef = inject(DestroyRef);
  readonly settings = signal<ShopSeoSettings | null>(null);
  readonly busy = signal(false); readonly loading = signal(false); readonly error = signal(''); readonly notice = signal('');
  heading = ''; seoTitle = '';
  constructor() { this.load(); }
  load() {
    if (this.loading() || this.busy()) return;
    this.loading.set(true); this.error.set('');
    this.api.seoSettings().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: settings => { this.settings.set(settings); this.heading = settings.heading; this.seoTitle = settings.seoTitle; this.loading.set(false); },
      error: error => { this.loading.set(false); this.error.set(errorMessage(error)); },
    });
  }
  save() {
    const settings = this.settings();
    if (!settings || this.busy() || this.loading() || !this.heading.trim() || !this.seoTitle.trim()) return;
    this.busy.set(true); this.error.set(''); this.notice.set('');
    this.api.updateSeoSettings({ heading: this.heading, seoTitle: this.seoTitle, revision: settings.revision }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => { this.busy.set(false); this.notice.set('SEO-instellingen opgeslagen.'); this.load(); },
      error: error => { this.busy.set(false); this.error.set(errorMessage(error)); },
    });
  }
}
