import { Component, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { BehaviorSubject, combineLatest, switchMap } from 'rxjs';
import { CatalogApi } from '../catalog.api';
import { Category, CategorySummary } from '../catalog.models';
import { errorMessage } from '../error-message';
import { loadState } from '../load-state';
import { CategoryEditState } from './category-edit-state';

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
      <button
        type="button"
        class="secondary"
        [disabled]="busy() || usage()?.loading"
        (click)="reloadUsage()"
      >
        Gebruik opnieuw ophalen
      </button>
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
            <select
              name="parent"
              [(ngModel)]="parentId"
              (ngModelChange)="rememberParent()"
              [disabled]="busy() || !!categories()?.loading"
            >
              <option [ngValue]="null">Hoofdniveau</option>
              @if (parentId && !parentOnPage()) {
                <option [ngValue]="parentId">
                  {{ selectedParentName || 'Huidige bovenliggende categorie' }}
                </option>
              }
              @for (option of options(); track option.id) {
                <option [ngValue]="option.id">{{ option.name }}</option>
              }
            </select>
          </label>
          <button
            [disabled]="busy() || categories()?.loading || parentId === category().parentCategoryId"
          >
            Opslaan
          </button>
          <button
            type="button"
            class="secondary"
            [disabled]="busy() || parentId === category().parentCategoryId"
            (click)="resetParent()"
          >
            Keuze terugzetten
          </button>
        </form>
        @if (categories()?.data; as page) {
          @if (!options().length) {
            <p class="muted">
              Geen andere categorieën gevonden. Pas je zoekopdracht aan of blader terug.
            </p>
          }
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
  readonly busy = inject(CategoryEditState).busy;
  readonly confirming = signal(false);
  readonly error = signal('');
  private readonly api = inject(CatalogApi);
  private readonly destroyRef = inject(DestroyRef);
  private writing = false;
  private readonly categoryQuery = new BehaviorSubject({ offset: 0, search: '' });
  private readonly usageRefresh = new BehaviorSubject(0);
  readonly usage = toSignal(
    combineLatest([toObservable(this.category), this.usageRefresh]).pipe(
      switchMap(([category]) => loadState(this.api.categoryUsage(category.id))),
    ),
  );
  readonly categories = toSignal(
    this.categoryQuery.pipe(
      switchMap((query) => loadState(this.api.categories(query.offset, query.search))),
    ),
  );
  readonly categoryOffset = signal(0);
  parentId: string | null = null;
  selectedParentName = '';
  categorySearch = '';
  constructor() {
    this.destroyRef.onDestroy(() => {
      if (this.writing) this.busy.set(false);
    });
    effect(() => {
      this.parentId = this.category().parentCategoryId;
      this.selectedParentName = '';
      this.confirming.set(false);
      this.error.set('');
    });
  }
  options() {
    return (this.categories()?.data ?? []).filter(
      (option: CategorySummary) => option.id !== this.category().id,
    );
  }
  parentOnPage() {
    return this.options().some((option) => option.id === this.parentId);
  }
  rememberParent() {
    this.selectedParentName =
      this.options().find((option) => option.id === this.parentId)?.name ?? '';
  }
  resetParent() {
    if (this.busy()) return;
    this.parentId = this.category().parentCategoryId;
    this.selectedParentName = '';
  }
  searchCategories() {
    if (this.busy()) return;
    this.categoryOffset.set(0);
    this.categoryQuery.next({ offset: 0, search: this.categorySearch });
  }
  changeCategoryPage(delta: number) {
    if (
      this.busy() ||
      this.categories()?.loading ||
      !Number.isSafeInteger(delta) ||
      delta % 20 !== 0
    )
      return;
    const offset = Math.max(0, this.categoryOffset() + delta);
    if (!Number.isSafeInteger(offset) || offset === this.categoryOffset()) return;
    this.categoryOffset.set(offset);
    this.categoryQuery.next({ ...this.categoryQuery.value, offset: this.categoryOffset() });
  }
  retryCategories() {
    if (!this.busy() && !this.categories()?.loading)
      this.categoryQuery.next(this.categoryQuery.value);
  }
  canDelete() {
    const information = this.usage()?.data;
    return (
      !!information &&
      information.categoryId === this.category().id &&
      !information.isInUse &&
      information.directChildCount === 0 &&
      information.productAssignmentCount === 0
    );
  }
  reloadUsage() {
    if (this.busy() || this.usage()?.loading) return;
    this.confirming.set(false);
    this.usageRefresh.next(this.usageRefresh.value + 1);
  }
  move() {
    if (
      this.busy() ||
      this.categories()?.loading ||
      this.parentId === this.category().parentCategoryId
    )
      return;
    if (this.parentId === this.category().id) {
      this.error.set('Een categorie kan niet onder zichzelf worden geplaatst.');
      return;
    }
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
    const category = this.category();
    this.writing = true;
    this.busy.set(true);
    this.error.set('');
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.writing = false;
        this.busy.set(false);
        if (this.category() !== category) return;
        this.confirming.set(false);
        if (message) this.saved.emit(message);
        else this.removed.emit();
      },
      error: (error) => {
        this.writing = false;
        this.busy.set(false);
        if (this.category() !== category) return;
        this.error.set(
          !message && error.status === 409
            ? 'Deze categorie is inmiddels in gebruik en kan niet worden verwijderd.'
            : errorMessage(error),
        );
        if (!message) this.reloadUsage();
      },
    });
  }
}
