import { Component, DestroyRef, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { BehaviorSubject, switchMap } from 'rxjs';
import { CatalogApi } from '../catalog.api';
import { TypeSummary } from '../catalog.models';
import { errorMessage } from '../error-message';
import { loadState } from '../load-state';

@Component({
  imports: [FormsModule, RouterLink],
  template: ` <a class="back" routerLink="/producten">← Alle producten</a>
    <div class="eyebrow">Assortiment uitbreiden</div>
    <h1>Nieuw product</h1>
    <p class="muted">Kies een producttype en geef je product een naam.</p>
    <section class="panel">
      <h2>1. Kies een producttype</h2>
      @if (selected(); as type) {
        <p class="success" role="status">
          Gekozen: <strong>{{ type.name }}</strong>
        </p>
      }
      <form class="toolbar" (ngSubmit)="search()">
        <label
          >Zoek producttypen<input
            name="search"
            type="search"
            [(ngModel)]="searchText"
            [disabled]="saving()"
            placeholder="Zoeken op naam" /></label
        ><button class="secondary" [disabled]="saving()">Zoeken</button>
      </form>
      @if (types()?.loading) {
        <p role="status">Producttypen ophalen…</p>
      }
      @if (types()?.error) {
        <div class="error" role="alert">
          {{ types()?.error }} <button class="secondary" (click)="retry()">Opnieuw proberen</button>
        </div>
      }
      @if (types()?.data; as items) {
        @if (!items.length) {
          <p class="muted">
            Geen producttypen gevonden.
            <a routerLink="/producttypen">Maak een producttype aan</a> of pas je zoekopdracht aan.
          </p>
        }
        <div class="table-wrap">
          <table>
            <tbody>
              @for (type of items; track type.id) {
                <tr>
                  <td>{{ type.name }}</td>
                  <td>
                    <button
                      type="button"
                      class="secondary"
                      [attr.aria-pressed]="selected()?.id === type.id"
                      [disabled]="saving()"
                      (click)="selected.set(type)"
                    >
                      {{ selected()?.id === type.id ? 'Gekozen' : 'Kiezen'
                      }}<span class="sr-only">: {{ type.name }}</span>
                    </button>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
        <div class="pager">
          <span>Pagina {{ offset() / 20 + 1 }}</span>
          <div class="actions">
            <button
              class="secondary"
              [disabled]="saving() || offset() === 0"
              (click)="changePage(-20)"
            >
              Vorige</button
            ><button
              class="secondary"
              [disabled]="saving() || items.length < 20"
              (click)="changePage(20)"
            >
              Volgende
            </button>
          </div>
        </div>
      }
    </section>
    <section class="panel form-width">
      <h2>2. Productgegevens</h2>
      <form (ngSubmit)="create()">
        <label class="field"
          >Productnaam<input
            name="name"
            [(ngModel)]="name"
            required
            [disabled]="saving()"
            placeholder="Bijvoorbeeld: linnen overhemd"
        /></label>
        <label class="field"
          >Naam van de eerste variant<input
            name="variant"
            [(ngModel)]="variantName"
            required
            [disabled]="saving()"
            placeholder="Bijvoorbeeld: naturel / maat M"
        /></label>
        <p class="muted">Je kunt later meer varianten toevoegen.</p>
        @if (error()) {
          <p class="error" role="alert">{{ error() }}</p>
        }
        <button [disabled]="saving() || !selected() || !name.trim() || !variantName.trim()">
          {{ saving() ? 'Product aanmaken…' : 'Product aanmaken' }}
        </button>
      </form>
    </section>`,
})
export class ProductCreate {
  private readonly api = inject(CatalogApi);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly query = new BehaviorSubject({ offset: 0, search: '' });
  readonly types = toSignal(
    this.query.pipe(switchMap((q) => loadState(this.api.types(q.offset, q.search)))),
  );
  readonly selected = signal<TypeSummary | null>(null);
  readonly offset = signal(0);
  readonly saving = signal(false);
  readonly error = signal('');
  name = '';
  variantName = '';
  searchText = '';
  search() {
    this.offset.set(0);
    this.query.next({ offset: 0, search: this.searchText });
  }
  changePage(delta: number) {
    this.offset.update((value) => Math.max(0, value + delta));
    this.query.next({ ...this.query.value, offset: this.offset() });
  }
  retry() {
    this.query.next(this.query.value);
  }
  create() {
    const type = this.selected();
    if (this.saving() || !type || !this.name.trim() || !this.variantName.trim()) return;
    this.saving.set(true);
    this.error.set('');
    this.api
      .createProduct(type.id, this.name, this.variantName)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (product) => {
          void this.router.navigate(['/producten', product.id]);
        },
        error: (error) => {
          this.saving.set(false);
          this.error.set(errorMessage(error));
        },
      });
  }
}
