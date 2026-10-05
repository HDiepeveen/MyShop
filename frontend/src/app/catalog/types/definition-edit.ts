import { Component, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';
import { CatalogApi } from '../catalog.api';
import { AttributeDefinition } from '../catalog.models';
import { errorMessage } from '../error-message';
import { TypeEditState } from './type-edit-state';

@Component({
  selector: 'app-definition-edit',
  imports: [FormsModule],
  template: ` <details class="editor">
    <summary>Kenmerk wijzigen: {{ definition().displayName }}</summary>
    <p class="muted">
      Code, soort en niveau staan vast. Een nieuwe naam geldt voor alle producten van dit type.
    </p>
    <form (ngSubmit)="rename()">
      <label class="field"
        >Naam<input name="displayName" [(ngModel)]="displayName" required [disabled]="busy()"
      /></label>
      <button
        [disabled]="
          busy() || !displayName.trim() || displayName.trim() === definition().displayName
        "
      >
        Kenmerknaam opslaan
      </button>
    </form>
    <form (ngSubmit)="configure()">
      <label class="check-field"
        ><input
          type="checkbox"
          name="required"
          [(ngModel)]="required"
          [disabled]="busy()"
        />Verplicht</label
      >
      <label class="check-field"
        ><input
          type="checkbox"
          name="filterable"
          [(ngModel)]="filterable"
          [disabled]="busy()"
        />Filterbaar</label
      >
      <p class="muted">
        Verplicht maken kan ontbrekende waarden bij bestaande producten aan het licht brengen.
      </p>
      <button [disabled]="busy() || !configurationChanged()">Instellingen opslaan</button>
    </form>
    <div class="remove-section">
      @if (confirming()) {
        <p>
          Kenmerk “{{ definition().displayName }}” verwijderen? Het verdwijnt voor alle producten
          van dit type. Ingevulde waarden blijven staan en kunnen per product of variant worden
          gewist.
        </p>
        <button type="button" [disabled]="busy()" (click)="remove()">
          Ja, kenmerk verwijderen
        </button>
        <button type="button" class="secondary" [disabled]="busy()" (click)="confirming.set(false)">
          Annuleren
        </button>
      } @else {
        <button type="button" class="secondary" [disabled]="busy()" (click)="confirming.set(true)">
          Kenmerk verwijderen
        </button>
      }
    </div>
    @if (error()) {
      <p class="error" role="alert">{{ error() }}</p>
    }
  </details>`,
})
export class DefinitionEdit {
  readonly typeId = input.required<string>();
  readonly definition = input.required<AttributeDefinition>();
  readonly saved = output<string>();
  readonly busy = inject(TypeEditState).busy;
  readonly error = signal('');
  readonly confirming = signal(false);
  private readonly api = inject(CatalogApi);
  private readonly destroyRef = inject(DestroyRef);
  private writing = false;
  displayName = '';
  required = false;
  filterable = false;
  constructor() {
    this.destroyRef.onDestroy(() => { if (this.writing) this.busy.set(false); });
    effect(() => {
      this.typeId();
      this.error.set('');
      this.confirming.set(false);
      this.displayName = this.definition().displayName;
      this.required = this.definition().isRequired;
      this.filterable = this.definition().isFilterable;
    });
  }
  rename() {
    if (
      this.busy() ||
      !this.displayName.trim() ||
      this.displayName.trim() === this.definition().displayName
    )
      return;
    this.write(
      this.api.renameDefinition(this.typeId(), this.definition().id, this.displayName),
      'De kenmerknaam is gewijzigd.',
    );
  }
  configurationChanged() {
    return (
      this.required !== this.definition().isRequired ||
      this.filterable !== this.definition().isFilterable
    );
  }
  configure() {
    if (this.busy() || !this.configurationChanged()) return;
    this.write(
      this.api.configureDefinition(
        this.typeId(),
        this.definition().id,
        this.required,
        this.filterable,
      ),
      'De kenmerkinstellingen zijn gewijzigd.',
    );
  }
  remove() {
    if (this.busy() || !this.confirming()) return;
    this.write(
      this.api.removeDefinition(this.typeId(), this.definition().id),
      'Het kenmerk is verwijderd. Bestaande waarden blijven behouden.',
    );
  }
  private write(request: Observable<void>, message: string) {
    const typeId = this.typeId();
    const definition = this.definition();
    this.writing = true;
    this.busy.set(true);
    this.error.set('');
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.writing = false;
        this.busy.set(false);
        if (this.typeId() !== typeId || this.definition() !== definition) return;
        this.saved.emit(message);
      },
      error: (error) => {
        this.writing = false;
        this.busy.set(false);
        if (this.typeId() !== typeId || this.definition() !== definition) return;
        this.error.set(errorMessage(error));
      },
    });
  }
}
