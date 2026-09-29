import { Component, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { BehaviorSubject, switchMap } from 'rxjs';
import { CatalogApi } from '../catalog.api';
import { Category, CategorySummary } from '../catalog.models';
import { errorMessage } from '../error-message';
import { loadState } from '../load-state';

@Component({
  selector: 'app-category-management',
  imports: [FormsModule],
  template: `
    <section class="panel">
      <h2>Categoriebeheer</h2>
      @if (usage()?.loading) {
        <p role="status">Gebruik ophalen…</p>
      }
      @if (usage()?.error) {
        <p class="error" role="alert">{{ usage()?.error }}</p>
      }
      @if (usage()?.data; as information) {
        <p>
          {{ information.productAssignmentCount }} productkoppelingen en
          {{ information.directChildCount }} directe subcategorieën.
        </p>
        @if (information.isInUse) {
          <p class="muted">
            Deze categorie kan pas worden verwijderd wanneer koppelingen en subcategorieën zijn
            verwijderd.
          </p>
        }
      }
      <details>
        <summary>In hiërarchie plaatsen</summary>
        @if (categories()?.loading) {
          <p role="status">Categorieën ophalen…</p>
        }
        <form class="toolbar" (ngSubmit)="searchCategories()">
          <label class="field"
            >Categorie zoeken<input
              name="categorySearch"
              type="search"
              [(ngModel)]="categorySearch"
              placeholder="Zoeken op naam"
              [disabled]="busy()"
          /></label>
          <button class="secondary" [disabled]="busy()">Zoeken</button>
        </form>
        @if (categories()?.error) {
          <p class="error" role="alert">
            {{ categories()?.error }}
            <button type="button" class="secondary" (click)="retryCategories()" [disabled]="busy()">
              Opnieuw ophalen
            </button>
          </p>
        }
        <form (ngSubmit)="move()">
          <label class="field"
            >Bovenliggende categorie
            <select name="parent" [(ngModel)]="parentId" [disabled]="busy()">
              <option [ngValue]="null">Hoofdniveau</option>
              @for (option of options(); track option.id) {
                <option [ngValue]="option.id">{{ option.name }}</option>
              }
            </select>
          </label>
          <button [disabled]="busy() || parentId === category().parentCategoryId">Opslaan</button>
        </form>
        @if (categories()?.data; as page) {
          <div class="pager">
            <span>Pagina {{ categoryOffset() / 20 + 1 }}</span>
            <div class="actions">
              <button
                type="button"
                class="secondary"
                [disabled]="busy() || categoryOffset() === 0"
                (click)="changeCategoryPage(-20)"
              >
                Vorige
              </button>
              <button
                type="button"
                class="secondary"
                [disabled]="busy() || page.length < 20"
                (click)="changeCategoryPage(20)"
              >
                Volgende
              </button>
            </div>
          </div>
        }
      </details>
      @if (canDelete()) {
        <div class="remove-section">
          @if (confirming()) {
            <p>Categorie “{{ category().name }}” definitief verwijderen?</p>
            <button type="button" [disabled]="busy()" (click)="remove()">
              Ja, categorie verwijderen
            </button>
            <button
              type="button"
              class="secondary"
              [disabled]="busy()"
              (click)="confirming.set(false)"
            >
              Annuleren
            </button>
          } @else {
            <button
              type="button"
              class="secondary"
              [disabled]="busy()"
              (click)="confirming.set(true)"
            >
              Categorie verwijderen
            </button>
          }
        </div>
      }
      @if (error()) {
        <p class="error" role="alert">{{ error() }}</p>
      }
    </section>
  `,
})
export class CategoryManagement {
  readonly category = input.required<Category>();
  readonly saved = output<string>();
  readonly removed = output<void>();
  readonly busy = signal(false);
  readonly confirming = signal(false);
  readonly error = signal('');
  private readonly api = inject(CatalogApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly categoryQuery = new BehaviorSubject({ offset: 0, search: '' });
  readonly usage = toSignal(
    toObservable(this.category).pipe(
      switchMap((category) => loadState(this.api.categoryUsage(category.id))),
    ),
  );
  readonly categories = toSignal(
    this.categoryQuery.pipe(
      switchMap((query) => loadState(this.api.categories(query.offset, query.search))),
    ),
  );
  readonly categoryOffset = signal(0);
  parentId: string | null = null;
  categorySearch = '';
  constructor() {
    effect(() => {
      this.parentId = this.category().parentCategoryId;
      this.confirming.set(false);
      this.error.set('');
    });
  }
  options() {
    return (this.categories()?.data ?? []).filter(
      (option: CategorySummary) => option.id !== this.category().id,
    );
  }
  searchCategories() {
    this.categoryOffset.set(0);
    this.categoryQuery.next({ offset: 0, search: this.categorySearch });
  }
  changeCategoryPage(delta: number) {
    this.categoryOffset.update((value) => Math.max(0, value + delta));
    this.categoryQuery.next({ offset: this.categoryOffset(), search: this.categorySearch });
  }
  retryCategories() {
    if (!this.busy()) this.categoryQuery.next(this.categoryQuery.value);
  }
  canDelete() {
    const information = this.usage()?.data;
    return (
      !!information &&
      !information.isInUse &&
      information.directChildCount === 0 &&
      information.productAssignmentCount === 0
    );
  }
  move() {
    if (this.busy() || this.parentId === this.category().parentCategoryId) return;
    this.write(
      this.api.moveCategory(this.category().id, this.parentId),
      'De categoriehiërarchie is bijgewerkt.',
    );
  }
  remove() {
    if (this.busy() || !this.confirming() || !this.canDelete()) return;
    this.write(this.api.deleteCategory(this.category().id), '');
  }
  private write(request: import('rxjs').Observable<unknown>, message: string) {
    this.busy.set(true);
    this.error.set('');
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.busy.set(false);
        this.confirming.set(false);
        if (message) this.saved.emit(message);
        else this.removed.emit();
      },
      error: (error) => {
        this.busy.set(false);
        this.error.set(errorMessage(error));
      },
    });
  }
}
