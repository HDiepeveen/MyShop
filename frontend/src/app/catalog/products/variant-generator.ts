import { Component, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CatalogApi } from '../catalog.api';
import { AttributeDefinition, Product } from '../catalog.models';
import { errorMessage } from '../error-message';
import { ProductEditState } from './product-edit-state';
import {
  CombinationPreview,
  combinationPreview,
  combinationRequest,
  variantDimensions,
} from './variant-combinations';

@Component({
  selector: 'app-variant-generator',
  imports: [FormsModule],
  template: `
    <section class="panel">
      <h2>Varianten uit opties aanmaken</h2>
      <p>
        Vul per kenmerk één optie per regel in. Bijvoorbeeld maten S, M en L en kleuren blauw en
        zwart. Bestaande combinaties, prijzen en voorraad blijven behouden.
      </p>
      @if (!dimensions().length) {
        <p>Voeg eerst variantkenmerken toe bij het producttype, bijvoorbeeld Maat en Kleur.</p>
      } @else {
        <form (ngSubmit)="preview()">
          @for (definition of dimensions(); track definition.id) {
            <label class="field"
              >{{ definition.displayName }}
              <textarea
                [name]="definition.id"
                rows="4"
                [ngModel]="lists[definition.id]"
                (ngModelChange)="changeList(definition.id, $event)"
                [disabled]="busy()"
              ></textarea>
              <small>{{ hint(definition.dataType) }}</small>
            </label>
          }
          <button class="secondary" [disabled]="busy()">Combinaties bekijken</button>
        </form>
        @if (hasMultiChoice()) {
          <p>Kenmerken met meerdere keuzes tegelijk vul je na het aanmaken per variant in.</p>
        }
        @if (combinations().length) {
          <p>
            {{ combinations().length }} combinaties. {{ selectedCount() }} nieuwe varianten
            geselecteerd.
          </p>
          @for (combination of combinations(); track $index) {
            <label class="check-field">
              <input
                type="checkbox"
                [(ngModel)]="combination.selected"
                [disabled]="busy() || combination.existing"
              />
              {{ combination.name }} {{ combination.existing ? '(bestaat al)' : '' }}
            </label>
          }
          <form (ngSubmit)="create()">
            <label class="field"
              >Gezamenlijke beginprijs exclusief btw (optioneel)
              <input
                name="netAmount"
                inputmode="decimal"
                [(ngModel)]="netAmount"
                [disabled]="busy()"
              />
            </label>
            @if (netAmount.trim()) {
              <label class="field"
                >Btw
                <select name="vat" [(ngModel)]="vatChoice" [disabled]="busy() || vatLoading()">
                  <option value="">Kies btw</option>
                  @for (rate of vatRates(); track rate.exempt ? 'exempt' : rate.percentage) {
                    <option [value]="rate.exempt ? 'exempt' : rate.percentage.toString()">
                      {{ rate.name }}
                    </option>
                  }
                </select>
              </label>
              @if (vatError()) {
                <p role="alert">{{ vatError() }}</p>
                <button type="button" (click)="loadRates()" [disabled]="busy() || vatLoading()">
                  Opnieuw ophalen
                </button>
              }
              @if (vatLoading()) {
                <p role="status">Btw-percentages ophalen…</p>
              }
              @if (pricePreview(); as price) {
                <p>
                  Prijs: {{ price.net }} · Btw: {{ price.vat }} · Totaal voor de klant:
                  {{ price.gross }} EUR
                </p>
              }
            }
            <label class="field"
              >Beginvoorraad per nieuwe variant (optioneel)
              <input name="stock" inputmode="numeric" [(ngModel)]="stock" [disabled]="busy()" />
              <small>Leeg laten betekent dat de voorraad nog niet wordt gevolgd.</small>
            </label>
            <p>
              De beginprijs is in EUR. Artikelnummer en eventuele afwijkende prijzen stel je daarna
              per variant in.
            </p>
            <button [disabled]="busy() || selectedCount() === 0">
              Geselecteerde varianten aanmaken
            </button>
          </form>
        }
      }
      @if (error()) {
        <p class="error" role="alert">{{ error() }}</p>
      }
    </section>
  `,
})
export class VariantGenerator {
  readonly product = input.required<Product>();
  readonly definitions = input.required<AttributeDefinition[]>();
  readonly saved = output<string>();
  readonly busy = inject(ProductEditState).busy;
  readonly combinations = signal<CombinationPreview[]>([]);
  readonly error = signal('');
  readonly vatRates = signal<{ name: string; percentage: number; exempt: boolean }[]>([]);
  readonly vatLoading = signal(false);
  readonly vatError = signal('');
  lists: Record<string, string> = {};
  netAmount = '';
  stock = '';
  vatChoice = '';
  private readonly api = inject(CatalogApi);
  private readonly destroyRef = inject(DestroyRef);
  private writing = false;
  constructor() {
    this.destroyRef.onDestroy(() => {
      if (this.writing) this.busy.set(false);
    });
    effect(() => {
      const product = this.product();
      this.lists = Object.fromEntries(
        this.dimensions().map((d) => [
          d.id,
          [
            ...new Set(
              product.variants.flatMap((v) => {
                const value = v.attributeValues.find((a) => a.attributeDefinitionId === d.id);
                return value ? [String(value.value)] : [];
              }),
            ),
          ].join('\n'),
        ]),
      );
      this.combinations.set([]);
      this.error.set('');
      this.netAmount = '';
      this.stock = '';
    });
  }
  dimensions() {
    return variantDimensions(this.definitions());
  }
  hasMultiChoice() {
    return this.definitions().some((d) => d.scope === 'Variant' && d.dataType === 'MultiChoice');
  }
  hint(type: string) {
    return type === 'Boolean'
      ? 'Eén optie per regel: true (ja) of false (nee).'
      : type === 'Date'
        ? 'Eén datum per regel: jjjj-mm-dd.'
        : type === 'Integer' || type === 'Decimal'
          ? 'Eén getal per regel, zonder eenheid.'
          : 'Eén optie per regel.';
  }
  changeList(id: string, value: string) {
    if (this.busy()) return;
    this.lists[id] = value;
    this.combinations.set([]);
    this.error.set('');
  }
  selectedCount() {
    return this.combinations().filter((c) => c.selected && !c.existing).length;
  }
  pricePreview() {
    const text = this.netAmount.trim().replace(',', '.');
    const rate = this.vatRates().find(
      (r) => (r.exempt ? 'exempt' : String(r.percentage)) === this.vatChoice,
    );
    if (!/^\d+(\.\d{1,2})?$/.test(text) || !rate) return null;
    const [whole, fraction = ''] = text.split('.');
    const cents = BigInt(whole) * 100n + BigInt(fraction.padEnd(2, '0'));
    const [rateWhole, rateFraction = ''] = String(rate.percentage).split('.');
    const percentage = BigInt(rateWhole) * 100n + BigInt(rateFraction.padEnd(2, '0'));
    const vat = (cents * percentage + 5000n) / 10000n;
    const format = (value: bigint) =>
      (value / 100n).toString() + ',' + (value % 100n).toString().padStart(2, '0');
    return { net: format(cents), vat: format(vat), gross: format(cents + vat) };
  }
  preview() {
    if (this.busy()) return;
    this.error.set('');
    this.combinations.set([]);
    try {
      this.combinations.set(combinationPreview(this.product(), this.definitions(), this.lists));
    } catch (error) {
      this.error.set((error as Error).message);
      return;
    }
    if (!this.vatRates().length) this.loadRates();
  }
  loadRates() {
    if (this.busy() || this.vatLoading()) return;
    this.vatLoading.set(true);
    this.vatError.set('');
    this.api
      .vatRates()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (rates) => {
          this.vatRates.set(rates);
          this.vatLoading.set(false);
        },
        error: () => {
          this.vatRates.set([]);
          this.vatLoading.set(false);
          this.vatError.set('Btw-percentages konden niet worden opgehaald.');
        },
      });
  }
  create() {
    if (this.busy() || !this.selectedCount()) return;
    this.error.set('');
    const text = this.netAmount.trim().replace(',', '.');
    const net = text ? Number(text) : null;
    const quantity = this.stock.trim() ? Number(this.stock.trim()) : null;
    if (
      text &&
      (!/^\d+(\.\d{1,2})?$/.test(text) ||
        !Number.isFinite(net) ||
        !Number.isSafeInteger(Math.round(net! * 100)) ||
        net!.toFixed(2) !==
          text.split('.')[0].replace(/^0+(?=\d)/, '') +
            '.' +
            (text.split('.')[1] ?? '').padEnd(2, '0'))
    ) {
      this.error.set('Vul een prijs vanaf 0 met maximaal twee decimalen in.');
      return;
    }
    if (
      net !== null &&
      (this.vatLoading() ||
        !this.vatRates().some(
          (r) => (r.exempt ? 'exempt' : String(r.percentage)) === this.vatChoice,
        ))
    ) {
      this.error.set('Kies een beschikbaar btw-percentage.');
      return;
    }
    if (
      quantity !== null &&
      (!/^\d+$/.test(this.stock.trim()) || !Number.isInteger(quantity) || quantity > 2147483647)
    ) {
      this.error.set('Vul een hele beginvoorraad van 0 tot en met 2.147.483.647 in.');
      return;
    }
    const product = this.product();
    const body = combinationRequest(product.revision, this.combinations(), {
      netAmount: net,
      vatRate: net === null ? null : this.vatChoice === 'exempt' ? 0 : Number(this.vatChoice),
      vatExempt: net !== null && this.vatChoice === 'exempt',
      stockQuantity: quantity,
    });
    this.writing = true;
    this.busy.set(true);
    this.api
      .generateVariants(product.id, body)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.writing = false;
          this.busy.set(false);
          if (this.product() !== product) return;
          this.combinations.set([]);
          this.saved.emit(
            result.added + ' varianten aangemaakt. Bestaande varianten zijn behouden.',
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
