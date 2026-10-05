import { Component, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CatalogApi } from '../catalog.api';
import { Category } from '../catalog.models';
import { errorMessage } from '../error-message';
import { CategoryEditState } from './category-edit-state';

@Component({
  selector: 'app-category-edit',
  imports: [FormsModule],
  template: `
    <section class="panel form-width">
      <h2>Categorienaam wijzigen</h2>
      <form (ngSubmit)="rename()">
        <label class="field"
          >Naam<input name="name" [(ngModel)]="name" required [disabled]="busy()"
        /></label>
        <button [disabled]="busy() || !name.trim() || name.trim() === category().name">
          {{ busy() ? 'Opslaan…' : 'Naam opslaan' }}
        </button>
      </form>
      @if (error()) {
        <p class="error" role="alert">{{ error() }}</p>
      }
    </section>
  `,
})
export class CategoryEdit {
  readonly category = input.required<Category>();
  readonly saved = output<void>();
  readonly busy = inject(CategoryEditState).busy;
  readonly error = signal('');
  private readonly api = inject(CatalogApi);
  private readonly destroyRef = inject(DestroyRef);
  private writing = false;
  name = '';
  constructor() {
    this.destroyRef.onDestroy(() => { if (this.writing) this.busy.set(false); });
    effect(() => {
      this.name = this.category().name;
      this.error.set('');
    });
  }
  rename() {
    if (this.busy() || !this.name.trim() || this.name.trim() === this.category().name) return;
    this.busy.set(true);
    this.writing = true;
    const category = this.category();
    this.error.set('');
    this.api
      .renameCategory(this.category().id, this.name)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.writing = false;
          this.busy.set(false);
          if (this.category() !== category) return;
          this.saved.emit();
        },
        error: (error) => {
          this.writing = false;
          this.busy.set(false);
          if (this.category() !== category) return;
          this.error.set(errorMessage(error));
        },
      });
  }
}
