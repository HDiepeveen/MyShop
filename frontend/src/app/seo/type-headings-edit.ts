import { Component, DestroyRef, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CatalogApi } from '../catalog/catalog.api';
import { ProductTypeHeadings } from './seo.models';
import { errorMessage } from '../catalog/error-message';


@Component({ selector: 'app-type-headings', imports: [FormsModule], template: `
  <details class="panel" (toggle)="open($event)">
    <summary>Kopteksten voor producten</summary>
    <p>Deze kopteksten gelden voor alle producten van dit type. Lege velden gebruiken de standaardteksten.</p>
    @if (loading()) { <p role="status">Kopteksten ophalen…</p> }
    @if (error()) { <p role="alert">{{ error() }}</p><button type="button" [disabled]="busy() || loading()" (click)="load()">Opnieuw ophalen</button> }
    @if (settings()) {
      <form (ngSubmit)="save()">
        <label>Koptekst productbeschrijving<input name="typeAboutHeading" [(ngModel)]="aboutHeading" maxlength="200" placeholder="Over dit product" [disabled]="busy() || loading()" /></label>
        <label>Koptekst productkenmerken<input name="typeAttributesHeading" [(ngModel)]="attributesHeading" maxlength="200" placeholder="Productkenmerken" [disabled]="busy() || loading()" /></label>
        <button [disabled]="busy() || loading()">{{ busy() ? 'Opslaan…' : 'Kopteksten opslaan' }}</button>
      </form>
    }
    @if (notice()) { <p role="status">{{ notice() }}</p> }
  </details>
` })
export class TypeHeadingsEdit {
  readonly typeId = input.required<string>();
  private readonly api = inject(CatalogApi);
  private readonly destroyRef = inject(DestroyRef);
  readonly settings = signal<ProductTypeHeadings | null>(null);
  readonly loading = signal(false);
  readonly busy = signal(false);
  readonly error = signal('');
  readonly notice = signal('');
  aboutHeading = '';
  attributesHeading = '';
  private generation = 0;
  constructor() {
    effect(() => { this.typeId(); this.generation++; this.settings.set(null); this.loading.set(false);
      this.busy.set(false); this.error.set(''); this.notice.set(''); this.aboutHeading = ''; this.attributesHeading = ''; });
  }
  open(event: Event) { if ((event.target as HTMLDetailsElement).open && !this.settings()) this.load(); }
  load() {
    if (this.loading() || this.busy()) return;
    const id = this.typeId(); const generation = this.generation;
    this.loading.set(true); this.error.set('');
    this.api.typeHeadings(id)
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: settings => { if (this.typeId() !== id || this.generation !== generation) return;
          this.settings.set(settings); this.aboutHeading = settings.aboutHeading ?? ''; this.attributesHeading = settings.attributesHeading ?? ''; this.loading.set(false); },
        error: error => { if (this.typeId() !== id || this.generation !== generation) return; this.loading.set(false); this.error.set(errorMessage(error)); },
      });
  }
  save() {
    const settings = this.settings(); const id = this.typeId(); const generation = this.generation;
    if (!settings || this.loading() || this.busy()) return;
    this.busy.set(true); this.error.set(''); this.notice.set('');
    this.api.updateTypeHeadings(id, {
      aboutHeading: this.aboutHeading.trim() || null, attributesHeading: this.attributesHeading.trim() || null, revision: settings.revision,
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => { if (this.typeId() !== id || this.generation !== generation) return; this.busy.set(false); this.notice.set('Kopteksten opgeslagen.'); this.load(); },
      error: error => { if (this.typeId() !== id || this.generation !== generation) return; this.busy.set(false); this.error.set(errorMessage(error)); },
    });
  }
}
