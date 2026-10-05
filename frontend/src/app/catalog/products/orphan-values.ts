import {
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CatalogApi } from '../catalog.api';
import { AttributeDefinition, AttributeValue } from '../catalog.models';
import { errorMessage } from '../error-message';
import { ProductEditState } from './product-edit-state';
@Component({
  selector: 'app-orphan-values',
  template: ` @if (orphans().length) {
    <section class="remove-section">
      <h3>Waarden van verwijderde kenmerken</h3>
      <p class="muted">
        Deze waarden horen niet meer bij een kenmerk van dit producttype. Wis alleen wat je niet
        meer nodig hebt.
      </p>
      <ul class="issue-list">
        @for (value of orphans(); track value.attributeDefinitionId) {
          <li>
            <p class="preserve-lines">{{ display(value) }}</p>
            <small>Kenmerk-ID: {{ value.attributeDefinitionId }}</small>
            @if (confirming() === value.attributeDefinitionId) {
              <p>Deze opgeslagen waarde definitief wissen?</p>
              <button
                type="button"
                [disabled]="busy()"
                (click)="clear(value.attributeDefinitionId)"
              >
                Ja, waarde wissen
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
                (click)="confirming.set(value.attributeDefinitionId)"
              >
                Waarde wissen
              </button>
            }
          </li>
        }
      </ul>
      @if (error()) {
        <p class="error" role="alert">{{ error() }}</p>
      }
    </section>
  }`,
})
export class OrphanValues {
  readonly productId = input.required<string>();
  readonly variantId = input<string | null>(null);
  readonly values = input.required<AttributeValue[]>();
  readonly definitions = input.required<AttributeDefinition[]>();
  readonly saved = output<string>();
  readonly busy = inject(ProductEditState).busy;
  readonly confirming = signal<string | null>(null);
  readonly error = signal('');
  readonly orphans = computed(() =>
    this.values().filter((v) => !this.definitions().some((d) => d.id === v.attributeDefinitionId)),
  );
  private readonly api = inject(CatalogApi);
  private readonly destroyRef = inject(DestroyRef);
  private writing = false;
  constructor() {
    this.destroyRef.onDestroy(() => {
      if (this.writing) this.busy.set(false);
    });
    effect(() => {
      this.productId();
      this.variantId();
      this.values();
      this.definitions();
      this.error.set('');
      this.confirming.set(null);
    });
  }
  display(value: AttributeValue) {
    return Array.isArray(value.value)
      ? value.value.join(', ')
      : typeof value.value === 'boolean'
        ? value.value
          ? 'Ja'
          : 'Nee'
        : String(value.value);
  }
  clear(id: string) {
    if (
      this.busy() ||
      this.confirming() !== id ||
      !this.orphans().some((v) => v.attributeDefinitionId === id)
    )
      return;
    const productId = this.productId(),
      variantId = this.variantId(),
      values = this.values(),
      definitions = this.definitions();
    this.writing = true;
    this.busy.set(true);
    this.error.set('');
    this.api
      .clearAttribute(this.productId(), id, this.variantId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.writing = false;
          this.busy.set(false);
          if (
            this.productId() !== productId ||
            this.variantId() !== variantId ||
            this.values() !== values ||
            this.definitions() !== definitions
          )
            return;
          this.confirming.set(null);
          this.saved.emit('De waarde van het verwijderde kenmerk is gewist.');
        },
        error: (error) => {
          this.writing = false;
          this.busy.set(false);
          if (
            this.productId() !== productId ||
            this.variantId() !== variantId ||
            this.values() !== values ||
            this.definitions() !== definitions
          )
            return;
          this.error.set(errorMessage(error));
        },
      });
  }
}
