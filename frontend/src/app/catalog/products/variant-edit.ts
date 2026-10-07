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
    <details (toggle)="loadVatRates($event)">
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
          >Prijs exclusief btw<input
            name="amount"
            inputmode="decimal"
            [(ngModel)]="amount"
            required
            [disabled]="busy()"
            placeholder="Bijvoorbeeld: 29,95"
          />
          <small>Vanaf 0, maximaal twee decimalen. Een komma of punt is toegestaan.</small></label
        >
        @if (variant().price && !variant().price?.isNetPrice) {
          <p>
            De bestaande klantprijs is {{ variant().price?.grossAmount ?? variant().price?.amount }}
            {{ variant().price?.currency }}. Deze prijs is als klantprijs vastgelegd. Vul hieronder
            bewust de netto prijs en btw-keuze in.
          </p>
        }
        <label class="field"
          >Btw<select name="vat" [(ngModel)]="vatChoice" [disabled]="busy()" required>
            <option value="">Kies een btw-behandeling</option>
            @for (rate of taxChoices(); track rate.exempt ? 'exempt' : rate.percentage) {
              <option [value]="rate.exempt ? 'exempt' : rate.percentage.toString()">
                {{ rate.name }}
              </option>
            }
          </select></label
        >
        @if (vatLoading()) {
          <p role="status">Btw-percentages ophalen…</p>
        }
        @if (vatError()) {
          <p role="alert">{{ vatError() }}</p>
          <button type="button" [disabled]="vatLoading() || busy()" (click)="loadVatRates()">
            Opnieuw ophalen
          </button>
        }
        @if (taxPreview(); as split) {
          <p>
            Prijs: {{ split.net }} · Btw: {{ split.vat }} · Totaal voor de klant: {{ split.gross }}
            {{ currency.toUpperCase() }}
          </p>
        }
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
          [disabled]="busy() || parsedAmount() === null || !validCurrency() || !taxPreview()"
        >
          Basisprijs opslaan
        </button>
      </form>

      @if (variant().price) {
        <button type="button" class="secondary" [disabled]="busy()" (click)="clearPrice()">
          Basisprijs wissen
        </button>
      }
      <form (ngSubmit)="saveStock()">
        @if (stock.trim() && parsedStock() === null) {
          <p class="form-errors">Vul een heel aantal van 0 tot en met 2.147.483.647 in.</p>
        }
        <label class="field"
          >Voorraad<input
            name="stock"
            inputmode="numeric"
            [(ngModel)]="stock"
            required
            [disabled]="busy()"
            placeholder="Bijvoorbeeld: 25"
        /></label>
        <button class="secondary" [disabled]="busy() || parsedStock() === null">
          Voorraad opslaan
        </button>
      </form>
      @if (variant().stockQuantity !== null && variant().stockQuantity !== undefined) {
        <button type="button" class="secondary" [disabled]="busy()" (click)="clearStock()">
          Voorraad niet meer volgen
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
  private writing = false;
  name = '';
  sku = '';
  amount = '';
  currency = 'EUR';
  stock = '';
  constructor() {
    this.destroyRef.onDestroy(() => {
      if (this.writing) this.busy.set(false);
    });
    effect(() => {
      this.productId();
      this.error.set('');
      this.confirmingRemove.set(false);
      this.name = this.variant().name;
      this.sku = this.variant().sku ?? '';
      this.amount = this.variant().price?.isNetPrice ? (this.variant().price?.netAmount ?? '') : '';
      this.vatChoice = this.variant().price
        ? this.variant().price?.vatRate == null
          ? ''
          : this.variant().price?.vatExempt
            ? 'exempt'
            : String(this.variant().price?.vatRate)
        : '21';
      this.currency = this.variant().price?.currency ?? 'EUR';
      this.stock = this.variant().stockQuantity?.toString() ?? '';
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
    return !!this.sku.trim() && this.sku.trim().length <= 64 && !/[\s\u0085]/.test(this.sku.trim());
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
  vatChoice = '21';
  readonly vatDefinitions = signal([
    { name: '21%', percentage: 21, exempt: false },
    { name: '9%', percentage: 9, exempt: false },
    { name: '0%', percentage: 0, exempt: false },
    { name: 'Vrijgesteld', percentage: 0, exempt: true },
  ]);
  readonly vatLoading = signal(false);
  readonly vatError = signal('');
  private vatLoaded = false;
  loadVatRates(event?: Event) {
    if (event && (!(event.target as HTMLDetailsElement).open || this.vatLoaded)) return;
    if (this.busy() || this.vatLoading()) return;
    this.vatLoading.set(true);
    this.vatError.set('');
    this.api
      .vatRates()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (values) => {
          this.vatDefinitions.set(values);
          this.vatLoading.set(false);
          this.vatLoaded = true;
        },
        error: () => {
          this.vatDefinitions.set([]);
          this.vatLoading.set(false);
          this.vatError.set('Btw-percentages konden niet worden opgehaald.');
        },
      });
  }
  taxChoices() {
    const current = this.variant().price;
    const values = this.vatDefinitions();
    if (
      current?.vatRate != null &&
      !values.some(
        (rate) => rate.percentage === current.vatRate && rate.exempt === !!current.vatExempt,
      )
    )
      return [
        ...values,
        {
          name: 'Vastgelegd: ' + current.vatRate + '%',
          percentage: current.vatRate,
          exempt: !!current.vatExempt,
        },
      ];
    return values;
  }
  taxPreview(): { net: string; vat: string; gross: string } | null {
    if (
      this.parsedAmount() === null ||
      !this.taxChoices().some(
        (rate) => (rate.exempt ? 'exempt' : rate.percentage.toString()) === this.vatChoice,
      )
    )
      return null;
    const [whole, decimals = ''] = this.amount.trim().replace(',', '.').split('.');
    const cents = BigInt(whole) * 100n + BigInt(decimals.padEnd(2, '0'));
    const [percentWhole, percentDecimals = ''] = (
      this.vatChoice === 'exempt' ? '0' : this.vatChoice
    ).split('.');
    const rate = BigInt(percentWhole) * 100n + BigInt(percentDecimals.padEnd(2, '0'));
    const numerator = cents * rate;
    let vat = numerator / 10000n;
    const remainder = numerator % 10000n;
    if (remainder >= 5000n) vat++;
    const gross = cents + vat;
    const format = (value: bigint) =>
      (value / 100n).toString() + ',' + (value % 100n).toString().padStart(2, '0');
    return { net: format(cents), vat: format(gross - cents), gross: format(gross) };
  }
  validCurrency() {
    return /^[a-zA-Z]{3}$/.test(this.currency.trim());
  }
  savePrice() {
    const amount = this.parsedAmount();
    if (
      this.busy() ||
      this.vatLoading() ||
      amount === null ||
      !this.validCurrency() ||
      !this.taxPreview()
    )
      return;
    this.save(
      this.api.setVariantPrice(
        this.productId(),
        this.variant().id,
        amount,
        this.currency,
        this.vatChoice === 'exempt' ? 0 : Number(this.vatChoice),
        this.vatChoice === 'exempt',
      ),
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
  parsedStock(): number | null {
    const text = this.stock.trim();
    if (!/^\d+$/.test(text)) return null;
    const quantity = Number(text);
    return Number.isInteger(quantity) && quantity <= 2147483647 ? quantity : null;
  }
  saveStock() {
    const quantity = this.parsedStock();
    if (this.busy() || quantity === null || quantity === this.variant().stockQuantity) return;
    this.save(
      this.api.setVariantStock(this.productId(), this.variant().id, quantity),
      'De voorraad is bijgewerkt.',
    );
  }
  clearStock() {
    if (
      this.busy() ||
      this.variant().stockQuantity === null ||
      this.variant().stockQuantity === undefined
    )
      return;
    this.save(
      this.api.clearVariantStock(this.productId(), this.variant().id),
      'De voorraad wordt niet meer gevolgd.',
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
    const productId = this.productId();
    const variant = this.variant();
    this.writing = true;
    this.busy.set(true);
    this.error.set('');
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.writing = false;
        this.busy.set(false);
        if (this.productId() !== productId || this.variant() !== variant) return;
        this.confirmingRemove.set(false);
        this.saved.emit(message);
      },
      error: (error) => {
        this.writing = false;
        this.busy.set(false);
        if (this.productId() !== productId || this.variant() !== variant) return;
        this.error.set(
          error instanceof HttpErrorResponse && error.status === 409 && conflictMessage
            ? conflictMessage
            : errorMessage(error),
        );
      },
    });
  }
}
