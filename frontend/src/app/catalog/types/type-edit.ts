import { TypeEditState } from './type-edit-state';
import { Component, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CatalogApi } from '../catalog.api';
import { ProductType } from '../catalog.models';
import { errorMessage } from '../error-message';

@Component({
  selector: 'app-type-edit',
  imports: [FormsModule],
  template: `
    <section class="panel form-width">
      <h2>Producttypenaam wijzigen</h2>
      <form (ngSubmit)="rename()">
        <label class="field"
          >Naam<input name="name" [(ngModel)]="name" required [disabled]="busy()"
        /></label>
        <button [disabled]="busy() || !name.trim() || name.trim() === type().name">
          {{ busy() ? 'Opslaan…' : 'Naam opslaan' }}
        </button>
      </form>
      @if (error()) {
        <p class="error" role="alert">{{ error() }}</p>
      }
    </section>
  `,
})
export class TypeEdit {
  readonly type = input.required<ProductType>();
  readonly saved = output<void>();
  readonly busy = inject(TypeEditState).busy;
  readonly error = signal('');
  private readonly api = inject(CatalogApi);
  private readonly destroyRef = inject(DestroyRef);
  private writing = false;
  name = '';
  constructor() {
    this.destroyRef.onDestroy(() => { if (this.writing) this.busy.set(false); });
    effect(() => {
      this.name = this.type().name;
      this.error.set('');
    });
  }
  rename() {
    if (this.busy() || !this.name.trim() || this.name.trim() === this.type().name) return;
    this.busy.set(true);
    this.writing = true;
    const type = this.type();
    this.error.set('');
    this.api
      .renameType(this.type().id, this.name)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.writing = false;
          this.busy.set(false);
          if (this.type() !== type) return;
          this.saved.emit();
        },
        error: (error) => {
          this.writing = false;
          this.busy.set(false);
          if (this.type() !== type) return;
          this.error.set(errorMessage(error));
        },
      });
  }
}
