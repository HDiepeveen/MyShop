import { Component, DestroyRef, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { BehaviorSubject, distinctUntilChanged, map, switchMap } from 'rxjs';
import { CatalogApi } from '../catalog.api';
import { errorMessage } from '../error-message';
import { loadState } from '../load-state';
import { readProductListQuery } from './product-list-query';

@Component({
  imports: [FormsModule, RouterLink],
  template: ` <div class="eyebrow">Assortiment</div>
    <div class="page-head">
      <div>
        <h1>Producten</h1>
        <p class="muted">Alles wat jouw winkel bijzonder maakt.</p>
      </div>
      <a class="button" routerLink="/producten/nieuw" [queryParams]="listQuery()"
        >+ Nieuw product</a
      >
    </div>
    <section class="panel">
      @if (filters().categoryId; as categoryId) {
        <p>
          Filter: <a [routerLink]="['/categorieen', categoryId]">Geselecteerde categorie</a>
          <button type="button" class="secondary" (click)="clearFilter('categoryId')">
            Categoriefilter verwijderen
          </button>
        </p>
        <p class="muted">
          Alleen rechtstreeks gekoppelde producten; subcategorieën worden niet meegenomen.
        </p>
      }
      @if (filters().productTypeId; as typeId) {
        <p>
          Filter: <a [routerLink]="['/producttypen', typeId]">Geselecteerd producttype</a>
          <button type="button" class="secondary" (click)="clearFilter('productTypeId')">
            Producttypefilter verwijderen
          </button>
        </p>
      }
      <form class="toolbar" (ngSubmit)="search()">
        <label
          >Zoek op productnaam<input
            name="search"
            [(ngModel)]="searchText"
            placeholder="Bijvoorbeeld: linnen overhemd"
            type="search" /></label
        ><button type="submit" class="secondary">Zoeken</button>
        @if (listQuery().search) {
          <button type="button" class="secondary" (click)="clearSearch()">Zoekterm wissen</button>
        }
      </form>
      <form class="toolbar" (ngSubmit)="lookupSku()">
        <label
          >Zoek op artikelnummer<input
            name="sku"
            [(ngModel)]="skuText"
            placeholder="Bijvoorbeeld: SHIRT-001"
            maxlength="64"
            type="search"
            [disabled]="skuBusy()" /></label
        ><button class="secondary" [disabled]="skuBusy() || !skuText.trim()">
          {{ skuBusy() ? 'Zoeken…' : 'Artikelnummer zoeken' }}
        </button>
      </form>
      @if (filters().categoryId || filters().productTypeId) {
        <p class="muted">Zoeken op artikelnummer doorzoekt het hele assortiment.</p>
      }
      @if (skuError()) {
        <p class="error" role="alert">{{ skuError() }}</p>
      }
      <button type="button" class="secondary" [disabled]="state()?.loading" (click)="retry()">
        Overzicht verversen
      </button>
      @if (state()?.loading) {
        <p class="loading" role="status">Producten ophalen…</p>
      }
      @if (state()?.error) {
        <div class="error" role="alert">
          {{ state()?.error }} <button class="secondary" (click)="retry()">Opnieuw proberen</button>
        </div>
      }
      @if (state()?.data; as page) {
        @if (page.items.length) {
          <div class="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Product</th>
                  <th>Status</th>
                  <th>Varianten</th>
                  <th>Bekijken</th>
                </tr>
              </thead>
              <tbody>
                @for (product of page.items; track product.id) {
                  <tr>
                    <td>
                      <a [routerLink]="['/producten', product.id]" [queryParams]="listQuery()">{{
                        product.name
                      }}</a>
                    </td>
                    <td>
                      <span class="badge">{{
                        product.isPublished ? 'Gepubliceerd' : 'Concept'
                      }}</span>
                    </td>
                    <td>
                      <span class="badge">{{ product.variantCount }} varianten</span>
                    </td>
                    <td>
                      <a
                        [routerLink]="['/producten', product.id]"
                        [queryParams]="listQuery()"
                        [attr.aria-label]="product.name + ' bekijken'"
                        >Details →</a
                      >
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        } @else {
          <div class="empty">
            @if (offset() > 0) {
              <h2>Geen producten op deze pagina</h2>
              <p class="muted">Ga terug naar de eerste pagina om het overzicht te bekijken.</p>
            } @else {
              <h2>Geen producten gevonden</h2>
              <p class="muted">Pas je zoekopdracht of filters aan, of voeg een product toe.</p>
            }
          </div>
        }
        <div class="pager">
          <span
            >{{ page.totalCount }} {{ page.totalCount === 1 ? 'product' : 'producten' }} · Pagina
            {{ offset() / 20 + 1 }}</span
          >
          <div class="actions">
            @if (offset() > 0) {
              <button type="button" class="secondary" (click)="changePage(-offset())">
                Eerste pagina
              </button>
            }
            <button class="secondary" [disabled]="offset() === 0" (click)="changePage(-20)">
              Vorige</button
            ><button
              class="secondary"
              [disabled]="offset() + 20 >= page.totalCount"
              (click)="changePage(20)"
            >
              Volgende
            </button>
          </div>
        </div>
      }
    </section>`,
})
export class ProductList {
  private readonly api = inject(CatalogApi);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly route = inject(ActivatedRoute);
  private readonly refresh = new BehaviorSubject(0);
  searchText = '';
  readonly listQuery = signal({
    categoryId: null as string | null,
    productTypeId: null as string | null,
    search: '',
    offset: 0,
  });
  readonly filters = signal<{ categoryId: string | null; productTypeId: string | null }>({
    categoryId: null,
    productTypeId: null,
  });
  readonly offset = signal(0);
  readonly state = toSignal(
    this.route.queryParamMap.pipe(
      map(readProductListQuery),
      distinctUntilChanged(
        (a, b) =>
          a.categoryId === b.categoryId &&
          a.productTypeId === b.productTypeId &&
          a.search === b.search &&
          a.offset === b.offset,
      ),
      switchMap((query) => {
        if (query.search !== this.listQuery().search) this.searchText = query.search;
        this.listQuery.set(query);
        this.filters.set(query);
        this.offset.set(query.offset);
        return this.refresh.pipe(
          switchMap(() => loadState(this.api.products(query.offset, query.search, query))),
        );
      }),
    ),
  );
  readonly skuBusy = signal(false);
  readonly skuError = signal('');
  skuText = '';
  clearFilter(key: 'categoryId' | 'productTypeId') {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { [key]: null, offset: null },
      queryParamsHandling: 'merge',
    });
  }
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
  lookupSku() {
    const sku = this.skuText.trim();
    if (this.skuBusy() || !sku) return;
    if (sku.length > 64 || /[\s\u0085]/.test(sku)) {
      this.skuError.set('Gebruik een artikelnummer van maximaal 64 tekens zonder spaties.');
      return;
    }
    this.skuBusy.set(true);
    this.skuError.set('');
    this.api
      .productBySku(sku)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (owner) => {
          this.skuBusy.set(false);
          void this.router.navigate(['/producten', owner.productId], {
            queryParams: this.listQuery(),
          });
        },
        error: (error) => {
          this.skuBusy.set(false);
          this.skuError.set(
            error instanceof HttpErrorResponse && error.status === 404
              ? 'Geen product gevonden met dit artikelnummer. Controleer het artikelnummer en probeer opnieuw.'
              : errorMessage(error),
          );
        },
      });
  }
}
