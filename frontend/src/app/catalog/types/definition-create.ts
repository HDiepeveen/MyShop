import { TypeEditState } from './type-edit-state';
import { Component, DestroyRef, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CatalogApi } from '../catalog.api';
import { attributeTypes } from '../attribute-types';
import { errorMessage } from '../error-message';

@Component({
  selector: 'app-definition-create',
  imports: [FormsModule],
  template: `
    <section class="panel form-width">
      <h2>Kenmerk toevoegen</h2>
      <p class="muted">
        Bepaal welke gegevens bij dit producttype horen. Soort en niveau staan na aanmaken vast.
      </p>
      <form (ngSubmit)="create()">
        <label class="field"
          >Naam<input name="displayName" [(ngModel)]="displayName" required [disabled]="busy()"
        /></label>
        <label class="field"
          >Code<input name="code" [(ngModel)]="code" required maxlength="64" [disabled]="busy()" />
          <small
            >Begin met een kleine letter. Gebruik alleen a–z, 0–9 en underscores, maximaal 64
            tekens.</small
          ></label
        >
        <label class="field"
          >Soort<select name="dataType" [(ngModel)]="dataType" [disabled]="busy()">
            @for (type of types; track type.code) {
              <option [ngValue]="type.code">{{ type.label }}</option>
            }
          </select></label
        >
        <label class="field"
          >Niveau<select name="scope" [(ngModel)]="scope" [disabled]="busy()">
            <option [ngValue]="0">Product</option>
            <option [ngValue]="1">Variant</option>
          </select></label
        >
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
        <button [disabled]="busy() || !valid()">
          {{ busy() ? 'Opslaan…' : 'Kenmerk toevoegen' }}
        </button>
      </form>
      @if (error()) {
        <p class="error" role="alert">{{ error() }}</p>
      }
    </section>
  `,
})
export class DefinitionCreate {
  readonly typeId = input.required<string>();
  readonly started = output<void>();
  readonly saved = output<void>();
  readonly busy = inject(TypeEditState).busy;
  readonly error = signal('');
  readonly types = attributeTypes;
  private readonly api = inject(CatalogApi);
  private readonly destroyRef = inject(DestroyRef);
  displayName = '';
  code = '';
  dataType = 0;
  scope = 0;
  required = false;
  filterable = false;
  valid() {
    return (
      !!this.displayName.trim() &&
      /^[a-z][a-z0-9_]{0,63}$/.test(this.code.trim()) &&
      this.types.some((type) => type.code === this.dataType) &&
      (this.scope === 0 || this.scope === 1)
    );
  }
  create() {
    if (this.busy() || !this.valid()) return;
    this.started.emit();
    this.busy.set(true);
    this.error.set('');
    this.api
      .addDefinition(this.typeId(), {
        code: this.code.trim(),
        displayName: this.displayName.trim(),
        dataType: this.dataType,
        scope: this.scope,
        isRequired: this.required,
        isFilterable: this.filterable,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.busy.set(false);
          this.saved.emit();
        },
        error: (error) => {
          this.busy.set(false);
          this.error.set(errorMessage(error));
        },
      });
  }
}
