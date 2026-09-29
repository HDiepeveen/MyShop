import { Component, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CatalogApi } from '../catalog.api';
import { Product } from '../catalog.models';
import { errorMessage } from '../error-message';

@Component({
  selector: 'app-product-edit',
  imports: [FormsModule],
  template: ` <div class="grid">
      <section class="panel">
        <h2>Productnaam wijzigen</h2>
        <form (ngSubmit)="rename()">
          <label class="field"
            >Naam<input name="name" [(ngModel)]="name" required [disabled]="busy()" /></label
          ><button
            class="secondary"
            [disabled]="busy() || !name.trim() || name.trim() === product().name"
          >
            Naam opslaan
          </button>
        </form>
      </section>
      <section class="panel">
        <h2>Variant toevoegen</h2>
        <form (ngSubmit)="addVariant()">
          <label class="field"
            >Variantnaam<input
              name="variant"
              [(ngModel)]="variantName"
              required
              [disabled]="busy()"
              placeholder="Bijvoorbeeld: naturel / maat L" /></label
          ><button class="secondary" [disabled]="busy() || !variantName.trim()">
            Variant toevoegen
          </button>
        </form>
      </section>
    </div>
    @if (busy()) {
      <p role="status">Wijziging opslaan…</p>
    }
    @if (error()) {
      <p class="error" role="alert">{{ error() }}</p>
    }`,
})
export class ProductEdit {
  readonly product = input.required<Product>();
  readonly saved = output<string>();
  readonly busy = signal(false);
  readonly error = signal('');
  private readonly api = inject(CatalogApi);
  private readonly destroyRef = inject(DestroyRef);
  name = '';
  variantName = '';
  constructor() {
    effect(() => {
      this.name = this.product().name;
    });
  }
  rename() {
    if (this.busy() || !this.name.trim() || this.name.trim() === this.product().name) return;
    this.busy.set(true);
    this.error.set('');
    this.api
      .renameProduct(this.product().id, this.name)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.busy.set(false);
          this.saved.emit('De productnaam is bijgewerkt.');
        },
        error: (error) => {
          this.busy.set(false);
          this.error.set(errorMessage(error));
        },
      });
  }
  addVariant() {
    if (this.busy() || !this.variantName.trim()) return;
    this.busy.set(true);
    this.error.set('');
    this.api
      .addVariant(this.product().id, this.variantName)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.busy.set(false);
          this.variantName = '';
          this.saved.emit('De variant is toegevoegd.');
        },
        error: (error) => {
          this.busy.set(false);
          this.error.set(errorMessage(error));
        },
      });
  }
}
