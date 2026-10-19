import { Component, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CatalogApi } from '../catalog.api';
import { Product } from '../catalog.models';
import { errorMessage } from '../error-message';
import { ProductEditState } from './product-edit-state';

@Component({
  selector: 'app-product-presentation',
  imports: [FormsModule],
  styles: [
    `
      textarea {
        width: 100%;
        min-height: 180px;
        font: inherit;
        padding: 10px;
      }
      img {
        max-width: 100%;
        max-height: 320px;
        object-fit: contain;
      }
      .description {
        white-space: pre-wrap;
        overflow-wrap: anywhere;
      }
    `,
  ],
  template: `
    <section class="panel">
      <h2>Productpresentatie</h2>
      <p>
        Status:
        <span class="badge">{{
          product().presentation?.isPublished ? 'Gepubliceerd' : 'Concept'
        }}</span>
      </p>
      <p class="muted">
        Publiceren vereist een beschrijving en een hoofdafbeelding. Dit
        bepaalt de selectie voor de toekomstige klantwinkel; het is geen controle van prijs of
        voorraad.
      </p>
      <form (ngSubmit)="save(false)">
        <label class="field"
          >Beschrijving<textarea
            name="description"
            [(ngModel)]="description"
            maxlength="10000"
            [disabled]="busy()"
          ></textarea>
        </label>
        <p class="muted">Gewone tekst, maximaal 10.000 tekens. HTML wordt niet opgemaakt.</p>
        <p class="muted">Kies je hoofdafbeelding bij Productafbeeldingen. De productnaam wordt automatisch als alternatieve tekst gebruikt.</p>
        <div class="actions">
          <button type="submit" class="secondary" [disabled]="busy()">Opslaan als concept</button>
          <button
            type="button"
            [disabled]="busy() || !description.trim() || !imageUrl.trim()"
            (click)="save(true)"
          >
            Opslaan en publiceren
          </button>
        </div>
      </form>
      @if (error()) {
        <p class="error" role="alert">{{ error() }}</p>
      }
    </section>
    <section class="panel">
      <h2>Voorbeeld opgeslagen presentatie</h2>
      @if (product().presentation; as presentation) {
        <p class="description">{{ presentation.description || 'Nog geen beschrijving.' }}</p>
        @if (presentation.imageUrl) {
          @if (!imageFailed()) {
            <img
              [src]="presentation.imageUrl"
              [alt]="product().name"
              referrerpolicy="no-referrer"
              loading="lazy"
              (error)="imageFailed.set(true)"
            />
          } @else {
            <p role="status">De afbeelding kon niet worden geladen. Controleer de link.</p>
          }
        } @else {
          <p class="muted">Nog geen hoofdafbeelding.</p>
        }
      } @else {
        <p class="muted">Nog geen presentatie ingevuld.</p>
      }
    </section>
  `,
})
export class ProductPresentationEdit {
  readonly product = input.required<Product>();
  readonly saved = output<string>();
  readonly busy = inject(ProductEditState).busy;
  readonly error = signal('');
  readonly imageFailed = signal(false);
  private readonly api = inject(CatalogApi);
  private readonly destroyRef = inject(DestroyRef);
  description = '';
  imageUrl = '';
  private writing = false;
  constructor() {
    this.destroyRef.onDestroy(() => {
      if (this.writing) this.busy.set(false);
    });
    effect(() => {
      this.error.set('');
      const presentation = this.product().presentation;
      this.description = presentation?.description ?? '';
      this.imageUrl = presentation?.imageUrl ?? '';
      this.imageFailed.set(false);
    });
  }
  save(isPublished: boolean) {
    if (this.busy()) return;
    this.error.set('');
    const description = this.description.trim(),
      imageUrl = this.imageUrl.trim(),
      imageAlt = imageUrl ? this.product().name.trim().slice(0, 250) : '';
    if (
      description.length > 10000 ||
      imageAlt.length > 250 ||
      imageUrl.length > 2048 ||
      !!imageUrl !== !!imageAlt ||
      (isPublished && (!description || !imageUrl))
    ) {
      this.error.set(
        'Controleer de lengte van de beschrijving en de afbeelding. Publiceren vereist een beschrijving en afbeelding.',
      );
      return;
    }
    if (imageUrl) {
      try {
        const uploaded = /^\/api\/shop\/product-images\/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(imageUrl);
        const url = uploaded ? new URL(imageUrl, window.location.origin) : new URL(imageUrl);
        if (
          (!uploaded && url.protocol !== 'https:') ||
          url.username ||
          url.password ||
          /[\\\u0000-\u001f\u007f]/.test(imageUrl)
        )
          throw new Error();
      } catch {
        this.error.set(
          'Gebruik een volledige HTTPS-afbeeldingslink zonder gebruikersnaam of wachtwoord.',
        );
        return;
      }
    }
    const product = this.product();
    this.writing = true;
    this.busy.set(true);
    this.api
      .setPresentation(this.product().id, {
        description,
        imageUrl: imageUrl || null,
        imageAlt,
        isPublished,
        revision: this.product().revision,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.writing = false;
          this.busy.set(false);
          if (this.product() !== product) return;
          this.saved.emit(
            isPublished
              ? 'De productpresentatie is gepubliceerd.'
              : 'De productpresentatie is als concept opgeslagen.',
          );
        },
        error: (error) => {
          this.writing = false;
          this.busy.set(false);
          if (this.product() !== product) return;
          this.error.set(errorMessage(error));
        },
      });
  }
}
