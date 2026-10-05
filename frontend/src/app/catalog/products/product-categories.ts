import { ProductEditState } from './product-edit-state';
import { Component, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { BehaviorSubject, catchError, combineLatest, forkJoin, map, of, switchMap } from 'rxjs';
import { CatalogApi } from '../catalog.api';
import { Product } from '../catalog.models';
import { errorMessage } from '../error-message';
import { loadState } from '../load-state';

@Component({
  selector: 'app-product-categories',
  imports: [FormsModule, RouterLink],
  template: `
    <section class="panel">
      <h2>Categorieën</h2>
      @if (assigned()?.loading) {
        <p role="status">Gekoppelde categorieën ophalen…</p>
      }
      @if (assigned()?.data; as items) {
        @if (!items.length) {
          <p class="muted">Dit product is nog niet aan een categorie gekoppeld.</p>
        }
        <ul class="category-links">
          @for (item of items; track item.id) {
            <li>
              @if (item.name) {
                <a [routerLink]="['/categorieen', item.id]">{{ item.name }}</a>
              } @else {
                <span>{{ item.error }}</span>
                <button
                  class="secondary"
                  [disabled]="busy() || assigned()?.loading"
                  (click)="retryAssigned()"
                >
                  Opnieuw ophalen
                </button>
              }
              <button class="secondary" [disabled]="busy() || !item.name" (click)="remove(item.id)">
                Ontkoppelen<span class="sr-only"
                  >: {{ item.name || 'categorie waarvan de naam niet beschikbaar is' }}</span
                >
              </button>
            </li>
          }
        </ul>
      }
      <button
        class="secondary"
        type="button"
        [disabled]="busy()"
        [attr.aria-expanded]="choosing()"
        (click)="togglePicker()"
      >
        {{ choosing() ? 'Kiezer sluiten' : 'Categorie koppelen' }}
      </button>
      @if (choosing()) {
        <form class="toolbar" (ngSubmit)="search()">
          <label
            >Zoek categorieën<input
              name="categorySearch"
              type="search"
              [(ngModel)]="searchText"
              [disabled]="busy()"
          /></label>
          <button class="secondary" [disabled]="busy()">Zoeken</button>
        </form>
        @if (options()?.loading) {
          <p role="status">Categorieën ophalen…</p>
        }
        @if (options()?.error) {
          <p class="error" role="alert">
            {{ options()?.error }}
            <button
              class="secondary"
              (click)="retryOptions()"
              [disabled]="busy() || options()?.loading"
            >
              Opnieuw proberen
            </button>
          </p>
        }
        @if (options()?.data; as items) {
          @if (!items.length) {
            <p class="muted">Geen categorieën gevonden. Pas je zoekopdracht aan of blader terug.</p>
          }
          <ul class="category-links">
            @for (item of items; track item.id) {
              <li>
                <span
                  >{{ item.name }}
                  <small>{{ item.isRoot ? 'Hoofdcategorie' : 'Subcategorie' }}</small></span
                >
                <button
                  class="secondary"
                  [disabled]="busy() || isAssigned(item.id)"
                  (click)="assign(item.id)"
                >
                  {{ isAssigned(item.id) ? 'Gekoppeld' : 'Koppelen'
                  }}<span class="sr-only">: {{ item.name }}</span>
                </button>
              </li>
            }
          </ul>
          <div class="pager">
            <span>Pagina {{ offset() / 20 + 1 }}</span>
            <div class="actions">
              <button
                class="secondary"
                [disabled]="busy() || options()?.loading || offset() === 0"
                (click)="changePage(-20)"
              >
                Vorige
              </button>
              <button
                class="secondary"
                [disabled]="busy() || options()?.loading || items.length < 20"
                (click)="changePage(20)"
              >
                Volgende
              </button>
            </div>
          </div>
        }
      }
      @if (error()) {
        <p class="error" role="alert">{{ error() }}</p>
      }
    </section>
  `,
})
export class ProductCategories {
  readonly product = input.required<Product>();
  readonly saved = output<string>();
  readonly busy = inject(ProductEditState).busy;
  readonly error = signal('');
  readonly choosing = signal(false);
  readonly offset = signal(0);
  searchText = '';
  private readonly api = inject(CatalogApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly refresh = new BehaviorSubject(0);
  private readonly query = new BehaviorSubject<{ offset: number; search: string } | null>(null);
  readonly assigned = toSignal(
    combineLatest([toObservable(this.product), this.refresh]).pipe(
      switchMap(([product]) =>
        loadState(
          product.categoryIds.length
            ? forkJoin(
                product.categoryIds.map((id) =>
                  this.api.category(id).pipe(
                    map((category) => ({ id, name: category.name as string | null, error: '' })),
                    catchError(() =>
                      of({
                        id,
                        name: null,
                        error: 'De naam van deze gekoppelde categorie kon niet worden opgehaald.',
                      }),
                    ),
                  ),
                ),
              )
            : of([]),
        ),
      ),
    ),
  );
  readonly options = toSignal(
    this.query.pipe(
      switchMap((q) => (q ? loadState(this.api.categories(q.offset, q.search)) : of(null))),
    ),
  );
  private writing = false;
  constructor() {
    this.destroyRef.onDestroy(() => {
      if (this.writing) this.busy.set(false);
    });
    effect(() => {
      this.product();
      this.error.set('');
      this.choosing.set(false);
      this.offset.set(0);
      this.searchText = '';
      this.query.next(null);
    });
  }
  isAssigned(id: string) {
    return this.product().categoryIds.includes(id);
  }
  togglePicker() {
    if (this.busy()) return;
    this.choosing.update((value) => !value);
    this.offset.set(0);
    this.query.next(this.choosing() ? { offset: 0, search: this.searchText } : null);
  }
  search() {
    if (this.busy()) return;
    this.offset.set(0);
    this.query.next({ offset: 0, search: this.searchText });
  }
  changePage(delta: number) {
    const options = this.options();
    if (this.busy() || options?.loading || !options?.data || !this.query.value) return;
    if (delta > 0 && options.data.length < 20) return;
    this.offset.update((value) => Math.max(0, value + delta));
    this.query.next({ ...this.query.value, offset: this.offset() });
  }
  retryOptions() {
    if (!this.busy() && !this.options()?.loading && this.query.value)
      this.query.next(this.query.value);
  }
  retryAssigned() {
    if (this.busy() || this.assigned()?.loading) return;
    this.refresh.next(this.refresh.value + 1);
  }
  remove(id: string) {
    if (this.busy() || !this.isAssigned(id)) return;
    const product = this.product();
    this.writing = true;
    this.busy.set(true);
    this.error.set('');
    this.api
      .removeCategory(this.product().id, id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.writing = false;
          this.busy.set(false);
          if (this.product() !== product) return;
          this.saved.emit('De categorie is ontkoppeld.');
        },
        error: (error) => {
          this.writing = false;
          this.busy.set(false);
          if (this.product() !== product) return;
          this.error.set(errorMessage(error));
        },
      });
  }
  assign(id: string) {
    if (this.busy() || this.isAssigned(id)) return;
    const product = this.product();
    this.writing = true;
    this.busy.set(true);
    this.error.set('');
    this.api
      .assignCategory(this.product().id, id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.writing = false;
          this.busy.set(false);
          if (this.product() !== product) return;
          this.saved.emit('De categorie is gekoppeld.');
        },
        error: (error) => {
          this.writing = false;
          this.busy.set(false);
          if (this.product() !== product) return;
          this.error.set(errorMessage(error));
        },
      });
  }
}
