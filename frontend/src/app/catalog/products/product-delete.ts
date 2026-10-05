import { Component, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CatalogApi } from '../catalog.api';
import { errorMessage } from '../error-message';
import { ProductEditState } from './product-edit-state';

@Component({
  selector: 'app-product-delete',
  template: `
    <section class="remove-section">
      <h2>Product verwijderen</h2>
      @if (confirming()) {
        <p>
          Product “{{ productName() }}” en alle varianten, waarden en kortingsregels definitief
          verwijderen?
        </p>
        <button type="button" [disabled]="busy()" (click)="remove()">
          Ja, product verwijderen
        </button>
        <button type="button" class="secondary" [disabled]="busy()" (click)="confirming.set(false)">
          Annuleren
        </button>
      } @else {
        <button type="button" class="secondary" [disabled]="busy()" (click)="confirming.set(true)">
          Product verwijderen
        </button>
      }
      @if (error()) {
        <p class="error" role="alert">{{ error() }}</p>
      }
    </section>
  `,
})
export class ProductDelete {
  readonly productId = input.required<string>();
  readonly productName = input.required<string>();
  readonly removed = output<void>();
  readonly busy = inject(ProductEditState).busy;
  readonly confirming = signal(false);
  readonly error = signal('');
  private readonly api = inject(CatalogApi);
  private readonly destroyRef = inject(DestroyRef);
  private writing = false;
  constructor() {
    this.destroyRef.onDestroy(() => { if (this.writing) this.busy.set(false); });
    effect(() => {
      this.productId();
      this.productName();
      this.confirming.set(false);
      this.error.set('');
    });
  }
  remove() {
    if (this.busy() || !this.confirming()) return;
    this.busy.set(true);
    this.writing = true;
    const productId = this.productId();
    this.error.set('');
    this.api
      .deleteProduct(this.productId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.writing = false;
          this.busy.set(false);
          if (this.productId() !== productId) return;
          this.removed.emit();
        },
        error: (error) => {
          this.writing = false;
          this.busy.set(false);
          if (this.productId() !== productId) return;
          this.error.set(errorMessage(error));
        },
      });
  }
}
