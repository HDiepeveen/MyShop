import { ProductEditState } from './product-edit-state';
import { Component, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';
import { CatalogApi } from '../catalog.api';
import { Variant } from '../catalog.models';
import { errorMessage } from '../error-message';

@Component({
  selector: 'app-variant-edit',
  imports: [FormsModule],
  template: `
    <details>
      <summary>Variant bewerken: {{ variant().name }}</summary>
      <form (ngSubmit)="rename()">
        <label class="field"
          >Variantnaam<input name="variantName" [(ngModel)]="name" required [disabled]="busy()"
        /></label>
        <button
          class="secondary"
          [disabled]="busy() || !name.trim() || name.trim() === variant().name"
        >
          Variantnaam opslaan
        </button>
      </form>

      <form (ngSubmit)="saveSku()">
        <label class="field"
          >Artikelnummer<input name="sku" [(ngModel)]="sku" maxlength="64" [disabled]="busy()" />
          <small
            >Maximaal 64 tekens, zonder spaties. Wordt in hoofdletters opgeslagen.</small
          ></label
        >
        <button
          class="secondary"
          [disabled]="busy() || !validSku() || sku.trim().toUpperCase() === variant().sku"
        >
          Artikelnummer opslaan
        </button>
      </form>

      @if (variant().sku) {
        <button type="button" class="secondary" [disabled]="busy()" (click)="clearSku()">
          Artikelnummer wissen
        </button>
      }

      <form (ngSubmit)="savePrice()">
        @if (amount.trim() && parsedAmount() === null) {
          <p class="form-errors">
            Vul een geldig bedrag in met maximaal twee decimalen. Dit bedrag kan niet exact worden
            opgeslagen.
          </p>
        }
        @if (currency.trim() && !validCurrency()) {
          <p class="form-errors">Gebruik een valutacode van drie letters, bijvoorbeeld EUR.</p>
        }
        <label class="field"
          >Basisprijs<input
            name="amount"
            inputmode="decimal"
            [(ngModel)]="amount"
            required
            [disabled]="busy()"
            placeholder="Bijvoorbeeld: 29,95"
          />
          <small>Vanaf 0, maximaal twee decimalen. Een komma of punt is toegestaan.</small></label
        >
        <label class="field"
          >Valuta<input
            name="currency"
            [(ngModel)]="currency"
            required
            maxlength="3"
            [disabled]="busy()"
            placeholder="EUR"
        /></label>
        <button
          class="secondary"
          [disabled]="busy() || parsedAmount() === null || !validCurrency()"
        >
          Basisprijs opslaan
        </button>
      </form>

      @if (variant().price) {
        <button type="button" class="secondary" [disabled]="busy()" (click)="clearPrice()">
          Basisprijs wissen
        </button>
      }
      @if (variantCount() > 1) {
        <div class="remove-section">
          @if (confirmingRemove()) {
            <p>
              Variant “{{ variant().name }}” en alle waarden en kortingsregels definitief
              verwijderen?
            </p>
            <button type="button" [disabled]="busy()" (click)="removeVariant()">
              Ja, variant verwijderen
            </button>
            <button
              type="button"
              class="secondary"
              [disabled]="busy()"
              (click)="confirmingRemove.set(false)"
            >
              Annuleren
            </button>
          } @else {
            <button
              type="button"
              class="secondary"
              [disabled]="busy()"
              (click)="confirmingRemove.set(true)"
            >
              Variant verwijderen
            </button>
          }
        </div>
      }
      @if (error()) {
        <p class="error" role="alert">{{ error() }}</p>
      }
    </details>
  `,
})
export class VariantEdit {
  readonly productId = input.required<string>();
  readonly variant = input.required<Variant>();
  readonly variantCount = input(1);
  readonly saved = output<string>();
  readonly busy = inject(ProductEditState).busy;
  readonly error = signal('');
  readonly confirmingRemove = signal(false);
  private readonly api = inject(CatalogApi);
  private readonly destroyRef = inject(DestroyRef);
  name = '';
  sku = '';
  amount = '';
  currency = 'EUR';
  constructor() {
    effect(() => {
      this.name = this.variant().name;
      this.sku = this.variant().sku ?? '';
      this.amount = this.variant().price?.amount.toString() ?? '';
      this.currency = this.variant().price?.currency ?? 'EUR';
    });
  }
  rename() {
    if (this.busy() || !this.name.trim() || this.name.trim() === this.variant().name) return;
    this.save(
      this.api.renameVariant(this.productId(), this.variant().id, this.name),
      'De variantnaam is bijgewerkt.',
    );
  }

  validSku() {
    return !!this.sku.trim() && this.sku.trim().length <= 64 && !/\s/.test(this.sku.trim());
  }
  saveSku() {
    if (this.busy() || !this.validSku() || this.sku.trim().toUpperCase() === this.variant().sku)
      return;
    this.save(
      this.api.setVariantSku(this.productId(), this.variant().id, this.sku),
      'Het artikelnummer is bijgewerkt.',
      'Het artikelnummer is al in gebruik of de variant is ondertussen gewijzigd. Controleer de gegevens en probeer opnieuw.',
    );
  }

  clearSku() {
    if (this.busy() || !this.variant().sku) return;
    this.save(
      this.api.clearVariantSku(this.productId(), this.variant().id),
      'Het artikelnummer is gewist.',
    );
  }

  parsedAmount(): number | null {
    const text = this.amount.trim().replace(',', '.');
    if (!/^\d+(\.\d{1,2})?$/.test(text)) return null;
    const value = Number(text);
    const [whole, decimals = ''] = text.split('.');
    const canonical = whole.replace(/^0+(?=\d)/, '') + '.' + decimals.padEnd(2, '0');
    // Refuse values that JavaScript cannot send without changing the entered cents.
    return Number.isFinite(value) &&
      Number.isSafeInteger(Math.round(value * 100)) &&
      value.toFixed(2) === canonical
      ? value
      : null;
  }
  validCurrency() {
    return /^[a-zA-Z]{3}$/.test(this.currency.trim());
  }
  savePrice() {
    const amount = this.parsedAmount();
    if (this.busy() || amount === null || !this.validCurrency()) return;
    this.save(
      this.api.setVariantPrice(this.productId(), this.variant().id, amount, this.currency),
      'De basisprijs is bijgewerkt.',
    );
  }

  clearPrice() {
    if (this.busy() || !this.variant().price) return;
    this.save(
      this.api.clearVariantPrice(this.productId(), this.variant().id),
      'De basisprijs is gewist.',
    );
  }
  removeVariant() {
    if (this.busy() || !this.confirmingRemove() || this.variantCount() < 2) return;
    this.save(
      this.api.removeVariant(this.productId(), this.variant().id),
      'De variant is verwijderd.',
    );
  }
  private save(request: Observable<void>, message: string, conflictMessage?: string) {
    this.busy.set(true);
    this.error.set('');
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.busy.set(false);
        this.confirmingRemove.set(false);
        this.saved.emit(message);
      },
      error: (error) => {
        this.busy.set(false);
        this.error.set(
          error instanceof HttpErrorResponse && error.status === 409 && conflictMessage
            ? conflictMessage
            : errorMessage(error),
        );
      },
    });
  }
}
