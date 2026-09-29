import { Component, DestroyRef, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { BehaviorSubject, switchMap } from 'rxjs';
import { CatalogApi } from '../catalog.api';
import { errorMessage } from '../error-message';
import { loadState } from '../load-state';

@Component({
  imports: [FormsModule],
  template: ` <div class="eyebrow">Structuur in je assortiment</div>
    <div class="page-head">
      <div>
        <h1>Categorieën</h1>
        <p class="muted">Een heldere plek voor ieder product.</p>
      </div>
    </div>
    <section class="panel">
      <h2>Nieuwe hoofdcategorie</h2>
      <form class="toolbar" (ngSubmit)="create()">
        <label
          >Naam<input
            name="name"
            [(ngModel)]="name"
            required
            placeholder="Bijvoorbeeld: wonen"
            [disabled]="saving()" /></label
        ><button [disabled]="saving() || !name.trim()">
          {{ saving() ? 'Opslaan…' : 'Toevoegen' }}
        </button>
      </form>
      @if (saveError()) {
        <p class="error" role="alert">{{ saveError() }}</p>
      }
      @if (saved()) {
        <p class="success" role="status">{{ saved() }}</p>
      }
    </section>
    <section class="panel">
      <form class="toolbar" (ngSubmit)="search()">
        <label
          >Zoek categorieën<input
            type="search"
            name="search"
            [(ngModel)]="searchText"
            placeholder="Zoeken op naam" /></label
        ><button class="secondary">Zoeken</button>
      </form>
      @if (state()?.loading) {
        <p class="loading" role="status">Categorieën ophalen…</p>
      }
      @if (state()?.error) {
        <div class="error" role="alert">
          {{ state()?.error }} <button class="secondary" (click)="retry()">Opnieuw proberen</button>
        </div>
      }
      @if (state()?.data; as items) {
        @if (items.length) {
          <div class="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Naam</th>
                  <th>Niveau</th>
                  <th>Directe subcategorieën</th>
                </tr>
              </thead>
              <tbody>
                @for (item of items; track item.id) {
                  <tr>
                    <td>{{ item.name }}</td>
                    <td>
                      <span class="badge">{{
                        item.isRoot ? 'Hoofdcategorie' : 'Subcategorie'
                      }}</span>
                    </td>
                    <td>{{ item.directChildCount }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        } @else {
          <div class="empty">
            <h2>Geen categorieën gevonden</h2>
            <p class="muted">Voeg een hoofdcategorie toe of pas je zoekopdracht aan.</p>
          </div>
        }
        <div class="pager">
          <span>Pagina {{ offset() / 20 + 1 }}</span>
          <div class="actions">
            <button class="secondary" [disabled]="offset() === 0" (click)="changePage(-20)">
              Vorige</button
            ><button class="secondary" [disabled]="items.length < 20" (click)="changePage(20)">
              Volgende
            </button>
          </div>
        </div>
      }
    </section>`,
})
export class CategoryList {
  private readonly api = inject(CatalogApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly query = new BehaviorSubject({ offset: 0, search: '' });
  readonly state = toSignal(
    this.query.pipe(switchMap((q) => loadState(this.api.categories(q.offset, q.search)))),
  );
  readonly offset = signal(0);
  readonly saving = signal(false);
  readonly saveError = signal('');
  readonly saved = signal('');
  name = '';
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
    if (this.saving() || !this.name.trim()) return;
    const name = this.name.trim();
    this.saving.set(true);
    this.saveError.set('');
    this.saved.set('');
    this.api
      .createCategory(name)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.name = '';
          this.saved.set('Hoofdcategorie “' + name + '” is aangemaakt.');
          this.searchText = '';
          this.search();
        },
        error: (error) => {
          this.saving.set(false);
          this.saveError.set(errorMessage(error));
        },
      });
  }
}
