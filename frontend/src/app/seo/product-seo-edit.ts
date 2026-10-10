import { Component, DestroyRef, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CatalogApi } from '../catalog/catalog.api';
import { errorMessage } from '../catalog/error-message';
import { ProductSeoInfo } from './seo.models';
@Component({ selector: 'app-product-seo', imports: [FormsModule], template: `
  <details class="panel" (toggle)="open($event)">
    <summary>SEO voor dit product</summary>
    <p>Deze velden zijn optioneel. Lege velden gebruiken de productnaam, beschrijving, een automatisch webadres.</p>
    @if (loading()) { <p role="status">Productinstellingen ophalen…</p> }
    @if (error()) { <p role="alert">{{ error() }}</p><button type="button" [disabled]="busy() || loading()" (click)="load()">Opnieuw ophalen</button> }
    @if (info(); as product) {
      <form (ngSubmit)="save()">
        <label>SEO-titel<input name="productSeoTitle" [(ngModel)]="seoTitle" maxlength="200" [disabled]="busy() || loading()" [placeholder]="product.resolvedTitle" /></label>
        <label>SEO-omschrijving<textarea name="productSeoDescription" [(ngModel)]="description" maxlength="500" rows="3" [disabled]="busy() || loading()" [placeholder]="product.resolvedDescription"></textarea></label>
        <label>Webadres<input name="productWebAddress" [(ngModel)]="webAddress" maxlength="160" [disabled]="busy() || loading()" placeholder="Bijvoorbeeld opel-corsa-2014" /></label>
        <p class="muted">Vul alleen het laatste deel in, zonder /winkel/. Eerdere adressen blijven doorverwijzen.</p>
        <p>Huidig adres: /winkel/{{ product.resolvedAddress }}</p>
        <button [disabled]="busy() || loading()">{{ busy() ? 'Opslaan…' : 'Productinstellingen opslaan' }}</button>
      </form>
    }
    @if (notice()) { <p role="status">{{ notice() }}</p> }
  </details>
` })
export class ProductSeoEdit {
  readonly productId = input.required<string>();
  private readonly api = inject(CatalogApi); private readonly destroyRef = inject(DestroyRef);
  readonly info = signal<ProductSeoInfo | null>(null);
  readonly loading = signal(false); readonly busy = signal(false); readonly error = signal(''); readonly notice = signal('');
  private generation = 0;
  seoTitle = ''; description = ''; webAddress = '';
  constructor() { effect(() => { this.productId(); this.generation++; this.loading.set(false); this.busy.set(false); this.info.set(null); this.error.set(''); this.notice.set(''); }); }
  open(event: Event) { if ((event.target as HTMLDetailsElement).open && !this.info()) this.load(); }
  load() {
    if (this.busy() || this.loading()) return;
    const id = this.productId(); const generation = this.generation; this.loading.set(true); this.error.set('');
    this.api.productSeo(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: info => { if (this.productId() !== id || this.generation !== generation) return; this.info.set(info); this.seoTitle = info.values.seoTitle ?? ''; this.description = info.values.seoDescription ?? ''; this.webAddress = info.values.webAddress ?? ''; this.loading.set(false); },
      error: error => { if (this.productId() !== id || this.generation !== generation) return; this.loading.set(false); this.error.set(errorMessage(error)); },
    });
  }
  save() {
    const info = this.info(); const id = this.productId(); const generation = this.generation;
    if (!info || this.busy() || this.loading()) return;
    this.busy.set(true); this.error.set(''); this.notice.set('');
    this.api.updateProductSeo(id, { seoTitle: this.seoTitle || null, seoDescription: this.description || null, webAddress: this.webAddress || null, revision: info.revision }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => { if (this.productId() !== id || this.generation !== generation) return; this.busy.set(false); this.notice.set('Productinstellingen opgeslagen.'); this.load(); },
      error: error => { if (this.productId() !== id || this.generation !== generation) return; this.busy.set(false); this.error.set(errorMessage(error)); },
    });
  }
}
