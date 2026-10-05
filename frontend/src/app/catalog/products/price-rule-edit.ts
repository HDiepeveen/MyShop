import { Component, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';
import { CatalogApi } from '../catalog.api';
import { PriceRule, Variant } from '../catalog.models';
import { errorMessage } from '../error-message';
import { ProductEditState } from './product-edit-state';

@Component({
  selector: 'app-price-rule-edit',
  imports: [FormsModule],
  template: `
    <details>
      <summary>Kortingsregels ({{ rules().length }})</summary>
      @if (rules().length) {
        <ul class="issue-list">
          @for (rule of rules(); track rule.id) {
            <li>
              <strong>{{ rule.name }}</strong>
              <span class="badge">{{ typeLabel(rule.adjustmentType) }}</span>
              <p>
                {{ rule.value
                }}{{ rule.adjustmentType === 1 ? '%' : ' ' + (variant().price?.currency ?? '') }} ·
                Prioriteit {{ rule.priority }}
              </p>
              @if (rule.startsAt || rule.endsAt) {
                <small>{{ rule.startsAt || 'Direct' }} tot {{ rule.endsAt || 'onbepaald' }}</small>
              }
              <div class="actions">
                <button type="button" class="secondary" [disabled]="busy()" (click)="edit(rule)">
                  Wijzigen
                </button>
                @if (confirming() === rule.id) {
                  <button type="button" [disabled]="busy()" (click)="remove(rule)">
                    Definitief wissen
                  </button>
                  <button
                    type="button"
                    class="secondary"
                    [disabled]="busy()"
                    (click)="confirming.set(null)"
                  >
                    Annuleren
                  </button>
                } @else {
                  <button
                    type="button"
                    class="secondary"
                    [disabled]="busy()"
                    (click)="confirming.set(rule.id)"
                  >
                    Wissen
                  </button>
                }
              </div>
            </li>
          }
        </ul>
      } @else {
        <p class="muted">Voor deze variant zijn nog geen kortingsregels ingesteld.</p>
      }
      <form (ngSubmit)="save()">
        <h3>{{ editingId ? 'Kortingsregel wijzigen' : 'Kortingsregel toevoegen' }}</h3>
        <label class="field"
          >Naam<input
            name="ruleName"
            [(ngModel)]="name"
            maxlength="200"
            required
            [disabled]="busy()"
        /></label>
        <label class="field"
          >Type<select name="adjustmentType" [(ngModel)]="adjustmentType" [disabled]="busy()">
            <option [ngValue]="1">Percentagekorting</option>
            <option [ngValue]="2">Vaste korting</option>
          </select></label
        >
        <label class="field"
          >Waarde<input
            name="ruleValue"
            inputmode="decimal"
            [(ngModel)]="value"
            required
            [disabled]="busy()"
            placeholder="Bijvoorbeeld: 10"
        /></label>
        <label class="field"
          >Prioriteit<input
            name="priority"
            inputmode="numeric"
            [(ngModel)]="priority"
            required
            [disabled]="busy()"
        /></label>
        <label class="field"
          >Start (optioneel)<input
            name="startsAt"
            type="datetime-local"
            [(ngModel)]="startsAt"
            [disabled]="busy()"
        /></label>
        <label class="field"
          >Einde (optioneel)<input
            name="endsAt"
            type="datetime-local"
            [(ngModel)]="endsAt"
            [disabled]="busy()"
        /></label>
        @if (validationError()) {
          <p class="form-errors">{{ validationError() }}</p>
        }
        <div class="actions">
          <button [disabled]="busy() || !valid()">
            {{ busy() ? 'Opslaan…' : editingId ? 'Wijziging opslaan' : 'Kortingsregel toevoegen' }}
          </button>
          @if (editingId) {
            <button type="button" class="secondary" [disabled]="busy()" (click)="reset()">
              Annuleren
            </button>
          }
        </div>
      </form>
      @if (error()) {
        <p class="error" role="alert">{{ error() }}</p>
      }
    </details>
  `,
})
export class PriceRuleEdit {
  readonly productId = input.required<string>();
  readonly variant = input.required<Variant>();
  readonly saved = output<string>();
  readonly busy = inject(ProductEditState).busy;
  readonly error = signal('');
  readonly confirming = signal<string | null>(null);
  private readonly api = inject(CatalogApi);
  private readonly destroyRef = inject(DestroyRef);
  name = '';
  adjustmentType = 1;
  value = '';
  priority = '0';
  startsAt = '';
  endsAt = '';
  editingId: string | null = null;
  private writing = false;
  constructor() {
    this.destroyRef.onDestroy(() => {
      if (this.writing) this.busy.set(false);
    });
    effect(() => {
      this.productId();
      this.variant();
      this.resetDraft();
    });
  }
  rules() {
    return this.variant().priceRules ?? [];
  }
  typeLabel(type: number) {
    return type === 1 ? 'Percentagekorting' : type === 2 ? 'Vaste korting' : 'Onbekend type';
  }
  validationError() {
    const value = this.parseValue();
    if (this.adjustmentType !== 1 && this.adjustmentType !== 2)
      return 'Kies een geldig kortingstype.';
    if (!this.name.trim()) return 'Geef de kortingsregel een naam.';
    if (this.name.trim().length > 200) return 'De naam mag maximaal 200 tekens bevatten.';
    if (value === null || value <= 0)
      return 'Gebruik een positieve waarde met maximaal twee decimalen.';
    if (this.adjustmentType === 1 && value > 100)
      return 'Een percentagekorting mag maximaal 100 zijn.';
    if (
      !/^\d+$/.test(this.priority.trim()) ||
      Number(this.priority) < 0 ||
      Number(this.priority) > 2147483647
    )
      return 'Prioriteit moet een geheel getal van 0 tot en met 2147483647 zijn.';
    if (this.startsAt && Number.isNaN(Date.parse(this.startsAt)))
      return 'De startdatum is ongeldig.';
    if (this.endsAt && Number.isNaN(Date.parse(this.endsAt))) return 'De einddatum is ongeldig.';
    if (this.startsAt && this.endsAt && new Date(this.endsAt) < new Date(this.startsAt))
      return 'De einddatum mag niet voor de startdatum liggen.';
    return '';
  }
  valid() {
    return !this.validationError();
  }
  parseValue(): number | null {
    const text = this.value.trim().replace(',', '.');
    if (!/^\d+(\.\d{1,2})?$/.test(text)) return null;
    const value = Number(text);
    const [whole, fraction = ''] = text.split('.');
    const canonical = whole.replace(/^0+(?=\d)/, '') + '.' + fraction.padEnd(2, '0');
    return Number.isFinite(value) &&
      Number.isSafeInteger(Math.round(value * 100)) &&
      value.toFixed(2) === canonical &&
      value > 0
      ? value
      : null;
  }
  edit(rule: PriceRule) {
    if (this.busy() || !this.rules().includes(rule)) return;
    this.editingId = rule.id;
    this.name = rule.name;
    this.adjustmentType = rule.adjustmentType;
    this.value = String(rule.value);
    this.priority = String(rule.priority);
    this.startsAt = this.localDate(rule.startsAt);
    this.endsAt = this.localDate(rule.endsAt);
    this.error.set('');
    this.confirming.set(null);
  }
  private localDate(value: string | null) {
    if (!value) return '';
    const date = new Date(value);
    if (Number.isNaN(date.valueOf())) return '';
    const pad = (part: number) => String(part).padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }
  reset() {
    if (this.busy()) return;
    this.resetDraft();
  }
  private resetDraft() {
    this.editingId = null;
    this.name = '';
    this.adjustmentType = 1;
    this.value = '';
    this.priority = '0';
    this.startsAt = '';
    this.endsAt = '';
    this.error.set('');
    this.confirming.set(null);
  }
  save() {
    if (
      this.busy() ||
      !this.valid() ||
      (this.editingId !== null && !this.rules().some((rule) => rule.id === this.editingId))
    )
      return;
    const rule = {
      name: this.name.trim(),
      adjustmentType: this.adjustmentType,
      value: this.parseValue()!,
      priority: Number(this.priority),
      startsAt: this.startsAt ? new Date(this.startsAt).toISOString() : null,
      endsAt: this.endsAt ? new Date(this.endsAt).toISOString() : null,
    };
    const request = this.editingId
      ? this.api.updatePriceRule(this.productId(), this.variant().id, {
          ...rule,
          id: this.editingId,
        })
      : this.api.addPriceRule(this.productId(), this.variant().id, { ...rule });
    this.write(
      request,
      this.editingId ? 'De kortingsregel is bijgewerkt.' : 'De kortingsregel is toegevoegd.',
    );
  }
  remove(rule: PriceRule) {
    if (this.busy() || this.confirming() !== rule.id || !this.rules().includes(rule)) return;
    this.write(
      this.api.removePriceRule(this.productId(), this.variant().id, rule.id),
      'De kortingsregel is gewist.',
    );
  }
  private write(request: Observable<unknown>, message: string) {
    const productId = this.productId(),
      variant = this.variant();
    this.writing = true;
    this.busy.set(true);
    this.error.set('');
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.writing = false;
        this.busy.set(false);
        if (this.productId() !== productId || this.variant() !== variant) return;
        this.reset();
        this.saved.emit(message);
      },
      error: (error) => {
        this.writing = false;
        this.busy.set(false);
        if (this.productId() !== productId || this.variant() !== variant) return;
        this.error.set(errorMessage(error));
      },
    });
  }
}
