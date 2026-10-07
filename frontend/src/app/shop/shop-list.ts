import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { BehaviorSubject, distinctUntilChanged, map, switchMap } from 'rxjs';
import { ShopApi } from './shop.api';
import { ShopImage } from './shop-image';
import { loadState } from '../catalog/load-state';
import { readShopQuery, shopContextQuery, ShopSort } from './shop-query';

@Component({
  imports: [FormsModule, RouterLink, ShopImage],
  styles: [
    `
      .products {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(min(100%, 260px), 1fr));
        gap: 24px;
      }
      .product {
        text-decoration: none;
        color: inherit;
        display: block;
      }
      .product h2 {
        margin: 18px 0 0;
      }
    `,
  ],
  template: `
    <div class="eyebrow">Welkom bij MyShop</div>
    <h1>Ontdek ons assortiment</h1>
    <p class="muted">Bekijk onze producten en kies de variant die bij je past.</p>
    @if (categories()?.loading) {
      <p role="status">Categorieën ophalen…</p>
    }
    @if (categories()?.error) {
      <p role="alert" class="error">
        Categorieën konden niet worden opgehaald.
        <button
          type="button"
          class="secondary"
          [disabled]="categories()?.loading"
          (click)="retryCategories()"
        >
          Opnieuw proberen
        </button>
      </p>
    }
    <form class="toolbar" (ngSubmit)="search()">
      <label
        >Categorie<select
          name="category"
          [ngModel]="query().categoryId"
          (ngModelChange)="filterCategory($event)"
        >
          <option value="">Alle categorieën</option>
          @for (category of categories()?.data ?? []; track category.id) {
            <option [value]="category.id">{{ category.name }}</option>
          }
        </select></label
      >
      <label
        >Sorteren<select name="sort" [ngModel]="query().sort" (ngModelChange)="changeSort($event)">
          <option value="nameAsc">Naam: A–Z</option>
          <option value="nameDesc">Naam: Z–A</option>
        </select></label
      >
      <label class="check-field"
        ><input
          type="checkbox"
          name="availableOnly"
          [ngModel]="query().availableOnly"
          (ngModelChange)="filterAvailability($event)"
        />Alleen op voorraad</label
      >
      <label
        >Zoek producten<input
          type="search"
          name="search"
          maxlength="200"
          [(ngModel)]="searchText"
          placeholder="Zoeken op productnaam"
      /></label>
      <button type="submit">Zoeken</button>
      @if (query().search) {
        <button type="button" class="secondary" (click)="clearSearch()">Zoekterm wissen</button>
      }
      <label
        >Producten per pagina<select
          name="limit"
          [ngModel]="query().limit"
          (ngModelChange)="changePageSize($event)"
        >
          <option [ngValue]="20">20</option>
          <option [ngValue]="50">50</option>
          <option [ngValue]="100">100</option>
        </select></label
      >
      @if (hasFilters()) {
        <button type="button" class="secondary" (click)="resetFilters()">
          Alle filters herstellen
        </button>
      }
    </form>
    @if (searchError()) {
      <p role="alert">{{ searchError() }}</p>
    }
    @if (hasFilters()) {
      <nav class="toolbar" aria-label="Actieve filters">
        @if (query().search) {
          <button
            type="button"
            class="secondary"
            aria-label="Zoekfilter verwijderen"
            (click)="clearSearch()"
          >
            Zoekterm: {{ query().search }} ×
          </button>
        }
        @if (query().categoryId) {
          <button
            type="button"
            class="secondary"
            aria-label="Categoriefilter verwijderen"
            (click)="filterCategory('')"
          >
            Categorie: {{ categoryName() }} ×
          </button>
        }
        @if (query().availableOnly) {
          <button
            type="button"
            class="secondary"
            aria-label="Voorraadfilter verwijderen"
            (click)="filterAvailability(false)"
          >
            Alleen op voorraad ×
          </button>
        }
        @if (query().sort !== 'nameAsc') {
          <button
            type="button"
            class="secondary"
            aria-label="Standaardsortering herstellen"
            (click)="changeSort('nameAsc')"
          >
            Naam: Z–A ×
          </button>
        }
      </nav>
    }
    @if (state()?.loading) {
      <p role="status">Producten ophalen…</p>
    }
    @if (state()?.error) {
      <div role="alert" class="error">
        {{ state()?.error }} <button class="secondary" (click)="retry()">Opnieuw proberen</button>
      </div>
    }
    @if (state()?.data; as page) {
      @if (page.items.length) {
        <div class="products">
          @for (product of page.items; track product.id) {
            <a
              class="panel product"
              [routerLink]="['/winkel', product.id]"
              [queryParams]="contextQuery()"
            >
              <app-shop-image [url]="product.imageUrl" [alt]="product.imageAlt" />
              <h2>{{ product.name }}</h2>
              @if (product.isAvailable === false) {
                <p><strong>Uitverkocht</strong></p>
              }
              @if (product.prices.length) {
                @for (price of product.prices; track price.currency) {
                  <p>
                    <strong>
                      {{ price.currency }} {{ amount(price.minimumAmount) }}
                      @if (price.minimumAmount !== price.maximumAmount) {
                        – {{ amount(price.maximumAmount) }}
                      }
                    </strong>
                  </p>
                }
              } @else {
                <p class="muted">Prijs niet beschikbaar</p>
              }
              <span>Bekijk product →</span>
            </a>
          }
        </div>
      } @else {
        <div class="panel">
          <h2>
            {{ query().offset ? 'Geen producten op deze pagina' : 'Geen producten gevonden' }}
          </h2>
          <p>
            {{
              query().search
                ? 'Probeer een andere zoekterm.'
                : hasFilters()
                  ? 'Pas je filters aan om meer producten te bekijken.'
                  : 'Er zijn hier nog geen producten te bekijken.'
            }}
          </p>
          @if (query().offset) {
            <button type="button" (click)="goToPage(0)">Terug naar de eerste pagina</button>
          }
          @if (hasFilters()) {
            <button type="button" class="secondary" (click)="resetFilters()">
              Alle filters herstellen
            </button>
          }
        </div>
      }
      <div class="pager">
        <span
          >{{ page.totalCount }} {{ page.totalCount === 1 ? 'product' : 'producten' }} · Pagina
          {{ query().offset / query().limit + 1 }}
          @if (query().offset / query().limit < pageCount()) {
            van {{ pageCount() }}
          }
          @if (page.items.length) {
            · {{ query().offset + 1 }}–{{ query().offset + page.items.length }}
          }
        </span>
        <div class="actions">
          @if (query().offset) {
            <button class="secondary" (click)="goToPage(0)">Eerste pagina</button>
          }
          <button
            class="secondary"
            [disabled]="query().offset === 0"
            (click)="goToPage(query().offset - query().limit)"
          >
            Vorige
          </button>
          <button
            class="secondary"
            [disabled]="query().offset + query().limit >= page.totalCount"
            (click)="goToPage(query().offset + query().limit)"
          >
            Volgende
          </button>
          <button
            type="button"
            class="secondary"
            [disabled]="query().offset >= lastOffset()"
            (click)="goToPage(lastOffset())"
          >
            Laatste pagina
          </button>
        </div>
      </div>
      @if (pageCount() > 1) {
        <form class="toolbar" (ngSubmit)="jumpToPage()">
          <label
            >Ga naar pagina<input
              name="page"
              type="number"
              min="1"
              [max]="pageCount()"
              step="1"
              [(ngModel)]="pageNumber"
          /></label>
          <button type="submit">Ga</button>
        </form>
      }
      @if (pageError()) {
        <p role="alert">{{ pageError() }}</p>
      }
    }
  `,
})
export class ShopList {
  private readonly api = inject(ShopApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly refresh = new BehaviorSubject(0);
  readonly query = signal({
    search: '',
    offset: 0,
    limit: 20,
    categoryId: '',
    sort: 'nameAsc' as ShopSort,
    availableOnly: false,
  });
  searchText = '';
  pageNumber: number | null = 1;
  readonly searchError = signal('');
  readonly pageError = signal('');
  private readonly categoryRefresh = new BehaviorSubject(0);
  readonly categories = toSignal(
    this.categoryRefresh.pipe(switchMap(() => loadState(this.api.categories()))),
  );
  readonly state = toSignal(
    this.route.queryParamMap.pipe(
      map(readShopQuery),
      distinctUntilChanged(
        (a, b) =>
          a.offset === b.offset &&
          a.limit === b.limit &&
          a.search === b.search &&
          a.categoryId === b.categoryId &&
          a.sort === b.sort &&
          a.availableOnly === b.availableOnly,
      ),
      switchMap((query) => {
        if (query.search !== this.query().search) this.searchText = query.search;
        this.query.set(query);
        this.pageNumber = query.offset / query.limit + 1;
        this.pageError.set('');
        this.searchError.set('');
        return this.refresh.pipe(
          switchMap(() =>
            loadState(
              this.api.products(
                query.offset,
                query.search,
                query.categoryId,
                query.sort,
                query.availableOnly,
                query.limit,
              ),
            ),
          ),
        );
      }),
    ),
  );
  search() {
    if (this.searchText.trim().length > 200) {
      this.searchError.set('Gebruik maximaal 200 tekens voor de zoekterm.');
      return;
    }
    this.searchError.set('');
    if (this.query().offset === 0 && this.query().search === this.searchText.trim()) {
      this.retry();
      return;
    }
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        ...this.contextQuery(),
        search: this.searchText.trim() || null,
        categoryId: this.query().categoryId || null,
        offset: null,
      },
    });
  }
  clearSearch() {
    this.searchText = '';
    this.search();
  }
  goToPage(offset: number) {
    if (
      !Number.isSafeInteger(offset) ||
      offset < 0 ||
      offset > 2147483647 ||
      offset % this.query().limit !== 0 ||
      offset === this.query().offset
    )
      return;
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        ...this.contextQuery(),
        search: this.query().search || null,
        categoryId: this.query().categoryId || null,
        offset: Math.max(0, offset) || null,
      },
    });
  }
  filterCategory(categoryId: string) {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        ...this.contextQuery(),
        search: this.query().search || null,
        categoryId: categoryId || null,
        offset: null,
      },
    });
  }
  pageCount() {
    return Math.max(1, Math.ceil((this.state()?.data?.totalCount ?? 0) / this.query().limit));
  }
  lastOffset() {
    return (this.pageCount() - 1) * this.query().limit;
  }
  jumpToPage() {
    if (this.state()?.loading) return;
    const page = this.pageNumber;
    if (page === null || !Number.isSafeInteger(page) || page < 1 || page > this.pageCount()) {
      this.pageError.set('Kies een heel paginanummer van 1 tot en met ' + this.pageCount() + '.');
      return;
    }
    this.pageError.set('');
    this.goToPage((page - 1) * this.query().limit);
  }
  changePageSize(limit: number) {
    if (![20, 50, 100].includes(limit) || limit === this.query().limit) return;
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { ...this.contextQuery(), limit: limit === 20 ? null : limit, offset: null },
    });
  }
  hasFilters() {
    const query = this.query();
    return !!(query.search || query.categoryId || query.availableOnly || query.sort !== 'nameAsc');
  }
  categoryName() {
    return (
      this.categories()?.data?.find((category) => category.id === this.query().categoryId)?.name ??
      'Geselecteerde categorie'
    );
  }
  resetFilters() {
    this.searchText = '';
    this.searchError.set('');
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { ...(this.query().limit !== 20 ? { limit: this.query().limit } : {}) },
    });
  }
  contextQuery() {
    return shopContextQuery(this.query());
  }
  changeSort(sort: ShopSort) {
    if (sort !== 'nameAsc' && sort !== 'nameDesc') return;
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { ...this.contextQuery(), sort: sort === 'nameAsc' ? null : sort, offset: null },
    });
  }
  filterAvailability(availableOnly: boolean) {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        ...this.contextQuery(),
        availableOnly: availableOnly ? true : null,
        offset: null,
      },
    });
  }
  retryCategories() {
    if (this.categories()?.loading) return;
    this.categoryRefresh.next(this.categoryRefresh.value + 1);
  }
  retry() {
    if (this.state()?.loading) return;
    this.refresh.next(this.refresh.value + 1);
  }
  amount(value: string) {
    return value.replace('.', ',');
  }
}
