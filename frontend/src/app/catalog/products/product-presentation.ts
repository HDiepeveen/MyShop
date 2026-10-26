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
        display: block;
        max-width: 100%;
        max-height: 320px;
        object-fit: contain;
      }
      .description {
        margin: 32px 0;
        white-space: pre-wrap;
        overflow-wrap: anywhere;
      }
    `,
  ],
  template: `
    <section class="panel">
      <h2>Productomschrijving</h2>
        <label class="field"
          >Omschrijving<textarea
            name="description"
            [(ngModel)]="description"
            maxlength="10000"
            [disabled]="busy()"
          ></textarea>
        </label>
        <p class="muted">Gewone tekst, maximaal 10.000 tekens. HTML wordt niet opgemaakt.</p>
    </section>
    <ng-content select="[product-details]" />
    <section class="panel">
      <h2>Publiceren</h2>
      <p>
        Status:
        <span class="badge">{{
          product().presentation?.isPublished ? 'Gepubliceerd' : 'Concept'
        }}</span>
      </p>
      <p class="muted">
        Een omschrijving en hoofdafbeelding zijn optioneel.
      </p>
        <p class="muted">Kies je hoofdafbeelding bij Productafbeeldingen. De productnaam wordt automatisch als alternatieve tekst gebruikt.</p>
        <div class="actions">
          <button type="button" class="secondary" [disabled]="busy()" (click)="save(false)">{{ product().presentation?.isPublished ? 'Niet meer publiceren' : 'Opslaan als concept' }}</button>
          <button
            type="button"
            [disabled]="busy()"
            (click)="save(true)"
          >
            Opslaan en publiceren
          </button>
        </div>
      @if (error()) {
        <p class="error" role="alert">{{ error() }}</p>
      }
    </section>
    <section class="panel">
      <h2>Voorbeeld</h2>
      @if (product().presentation; as presentation) {
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
        <p class="description">{{ presentation.description || 'Nog geen beschrijving.' }}</p>
      <ng-content select="[presentation-details]" />
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
      !!imageUrl !== !!imageAlt
    ) {
      this.error.set(
        'Controleer de lengte van de beschrijving en de afbeelding.',
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
              : product.presentation?.isPublished
                ? 'Het product is niet meer gepubliceerd en is als concept opgeslagen.'
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
