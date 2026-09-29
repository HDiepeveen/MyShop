import { Component, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CatalogApi } from '../catalog.api';
import { AttributeDefinition, AttributeValue } from '../catalog.models';
import { attributeInput } from '../attribute-input';
import { attributeTypeLabel } from '../attribute-types';
import { errorMessage } from '../error-message';
import { ProductEditState } from './product-edit-state';

@Component({
  selector: 'app-attribute-edit',
  imports: [FormsModule],
  template: `
    <details class="attribute-editor">
      <summary>
        {{ definition().displayName }}
        <span class="badge">{{ definition().isRequired ? 'Verplicht' : 'Optioneel' }}</span>
      </summary>
      <p class="muted">{{ typeLabel(definition().dataType) }}</p>
      <form (ngSubmit)="save()">
        @switch (definition().dataType) {
          @case ('MultiChoice') {
            <p class="muted">Voeg één waarde per keuze toe. De volgorde blijft behouden.</p>
            @for (choice of choices; track $index) {
              <label class="field"
                >Keuze {{ $index + 1
                }}<textarea
                  [name]="'choice' + $index"
                  [(ngModel)]="choices[$index]"
                  [disabled]="busy()"
                  rows="2"
                ></textarea>
              </label>
              <button
                class="secondary"
                type="button"
                [disabled]="busy()"
                (click)="removeChoice($index)"
              >
                Keuze {{ $index + 1 }} verwijderen
              </button>
            }
            <button class="secondary" type="button" [disabled]="busy()" (click)="addChoice()">
              Keuze toevoegen
            </button>
          }
          @case ('Boolean') {
            <label class="field"
              >{{ definition().displayName
              }}<select name="value" [(ngModel)]="text" [disabled]="busy()">
                <option value="">Kies een waarde</option>
                <option value="true">Ja</option>
                <option value="false">Nee</option>
              </select></label
            >
          }
          @case ('Date') {
            <label class="field"
              >{{ definition().displayName
              }}<input
                type="date"
                name="value"
                min="0001-01-01"
                max="9999-12-31"
                [(ngModel)]="text"
                [disabled]="busy()"
            /></label>
          }
          @case ('Integer') {
            <label class="field"
              >{{ definition().displayName
              }}<input name="value" inputmode="numeric" [(ngModel)]="text" [disabled]="busy()"
            /></label>
          }
          @case ('Decimal') {
            <label class="field"
              >{{ definition().displayName
              }}<input name="value" inputmode="decimal" [(ngModel)]="text" [disabled]="busy()"
            /></label>
          }
          @default {
            <label class="field"
              >{{ definition().displayName
              }}<textarea name="value" rows="3" [(ngModel)]="text" [disabled]="busy()"></textarea>
            </label>
          }
        }
        @if (validationError()) {
          <p class="form-errors" role="alert">{{ validationError() }}</p>
        }
        <button class="secondary" [disabled]="busy()">Kenmerk opslaan</button>
      </form>
      @if (current()) {
        @if (definition().isRequired) {
          <p class="muted">
            Dit kenmerk is verplicht. Na wissen meldt de controle dat de waarde ontbreekt.
          </p>
        }
        <button type="button" class="secondary" [disabled]="busy()" (click)="clear()">
          Waarde wissen
        </button>
      }
      @if (error()) {
        <p class="error" role="alert">{{ error() }}</p>
      }
    </details>
  `,
})
export class AttributeEdit {
  readonly variantId = input<string | null>(null);
  readonly productId = input.required<string>();
  readonly definition = input.required<AttributeDefinition>();
  readonly current = input<AttributeValue | undefined>();
  readonly saved = output<string>();
  readonly busy = inject(ProductEditState).busy;
  readonly error = signal('');
  readonly validationError = signal('');
  readonly typeLabel = attributeTypeLabel;
  private readonly api = inject(CatalogApi);
  private readonly destroyRef = inject(DestroyRef);
  text = '';
  choices: string[] = [''];
  constructor() {
    effect(() => {
      this.text = this.current() ? String(this.current()!.value) : '';
      this.choices = Array.isArray(this.current()?.value)
        ? [...(this.current()!.value as string[])]
        : [''];
    });
  }
  addChoice() {
    if (!this.busy()) this.choices.push('');
  }
  removeChoice(index: number) {
    if (!this.busy()) this.choices.splice(index, 1);
  }
  clear() {
    if (this.busy() || !this.current()) return;
    this.busy.set(true);
    this.error.set('');
    this.validationError.set('');
    this.api
      .clearAttribute(this.productId(), this.definition().id, this.variantId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.busy.set(false);
          this.saved.emit('De kenmerkwaarde is gewist.');
        },
        error: (error) => {
          this.busy.set(false);
          this.error.set(errorMessage(error));
        },
      });
  }
  save() {
    if (this.busy()) return;
    if (this.definition().scope !== (this.variantId() === null ? 'Product' : 'Variant')) {
      this.validationError.set('Dit kenmerk hoort bij een ander niveau. Vernieuw de gegevens.');
      return;
    }
    const parsed = attributeInput(
      this.definition().dataType,
      this.definition().dataType === 'MultiChoice' ? JSON.stringify(this.choices) : this.text,
    );
    this.validationError.set(parsed.error);
    if (parsed.body === null) return;
    this.busy.set(true);
    this.error.set('');
    this.api
      .setAttribute(this.productId(), this.definition().id, parsed.body, this.variantId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.busy.set(false);
          this.saved.emit('Het kenmerk is opgeslagen.');
        },
        error: (error) => {
          this.busy.set(false);
          this.error.set(errorMessage(error));
        },
      });
  }
}
