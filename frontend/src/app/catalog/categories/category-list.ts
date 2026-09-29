import { Component, DestroyRef, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { BehaviorSubject, distinctUntilChanged, map, switchMap } from 'rxjs';
import { readListQuery } from '../list-query';
import { CatalogApi } from '../catalog.api';
import { errorMessage } from '../error-message';
import { loadState } from '../load-state';

@Component({
  imports: [FormsModule, RouterLink],
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
        @if (listQuery().search) {
          <button type="button" class="secondary" (click)="clearSearch()">Zoekterm wissen</button>
        }
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
                    <td>
                      <a [routerLink]="['/categorieen', item.id]" [queryParams]="listQuery()">{{
                        item.name
                      }}</a>
                    </td>
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
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly refresh = new BehaviorSubject(0);
  readonly listQuery = signal({ offset: 0, search: '' });
  readonly offset = signal(0);
  searchText = '';
  readonly state = toSignal(
    this.route.queryParamMap.pipe(
      map(readListQuery),
      distinctUntilChanged((a, b) => a.search === b.search && a.offset === b.offset),
      switchMap((query) => {
        if (query.search !== this.listQuery().search) this.searchText = query.search;
        this.listQuery.set(query);
        this.offset.set(query.offset);
        return this.refresh.pipe(
          switchMap(() => loadState(this.api.categories(query.offset, query.search))),
        );
      }),
    ),
  );
  readonly saving = signal(false);
  readonly saveError = signal('');
  readonly saved = signal('');
  name = '';
  search() {
    if (this.listQuery().search === this.searchText.trim() && this.offset() === 0) {
      this.retry();
      return;
    }
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { search: this.searchText.trim() || null, offset: null },
      queryParamsHandling: 'merge',
    });
  }
  clearSearch() {
    this.searchText = '';
    this.search();
  }
  changePage(delta: number) {
    const offset = Math.max(0, this.offset() + delta);
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { offset: offset || null },
      queryParamsHandling: 'merge',
    });
  }
  retry() {
    this.refresh.next(this.refresh.value + 1);
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
