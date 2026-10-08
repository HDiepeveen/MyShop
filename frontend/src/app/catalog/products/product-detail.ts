import { ProductPresentationEdit } from './product-presentation';
import { ProductImagesEdit } from './product-images';
import { OrphanValues } from './orphan-values';
import { PriceRuleEdit } from './price-rule-edit';
import { ProductDelete } from './product-delete';
import { AttributeEdit } from './attribute-edit';
import { ProductEditState } from './product-edit-state';
import { ProductValidation } from './product-validation';
import { VariantEdit } from './variant-edit';
import { ProductCategories } from './product-categories';
import { ProductEdit } from './product-edit';
import { Component, effect, inject, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { BehaviorSubject, combineLatest, tap, distinctUntilChanged, map, switchMap } from 'rxjs';
import { CatalogApi } from '../catalog.api';
import { AttributeDefinition, AttributeValue, Variant } from '../catalog.models';
import { loadState } from '../load-state';
import { readProductListQuery, ProductStockFilter } from './product-list-query';

@Component({
  providers: [ProductEditState],
  imports: [
    RouterLink,
    CurrencyPipe,
    FormsModule,
    ProductEdit,
    ProductPresentationEdit,
    ProductImagesEdit,
    ProductValidation,
    VariantEdit,
    ProductCategories,
    AttributeEdit,
    OrphanValues,
    PriceRuleEdit,
    ProductDelete,
  ],
  template: ` <a class="back" routerLink="/producten" [queryParams]="listQuery()"
      >← Terug naar producten</a
    >
    @if (state()?.loading) {
      <p class="loading" role="status">Product ophalen…</p>
    }
    @if (state()?.error) {
      <div class="error" role="alert">
        {{ state()?.error }} <button class="secondary" (click)="reload()">Opnieuw proberen</button>
      </div>
    }
    @if (editState.busy()) {
      <p role="status">Wijziging opslaan…</p>
    }
    @if (notice()) {
      <p class="success" role="status">{{ notice() }}</p>
    }
    @if (state()?.data; as detail) {
      <div class="eyebrow">{{ detail.type.name }}</div>
      <p>
        <a [routerLink]="['/producttypen', detail.type.id]">Producttype en kenmerken beheren</a>
      </p>
      <div class="page-head">
        <div>
          <h1>{{ detail.product.name }}</h1>
          <p class="muted">
            {{ detail.product.variants.length }}
            {{ detail.product.variants.length === 1 ? 'variant' : 'varianten' }} ·
            {{ detail.product.categoryIds.length }}
            {{ detail.product.categoryIds.length === 1 ? 'categorie' : 'categorieën' }}
          </p>
        </div>
      </div>
      <app-product-presentation [product]="detail.product" (saved)="onSaved($event)" />
      <app-product-edit [product]="detail.product" (saved)="onSaved($event)" />
      <app-product-delete
        [productId]="detail.product.id"
        [productName]="detail.product.name"
        (removed)="onRemoved()"
      />
      <section class="panel">
        <h2>Productgegevens</h2>
        @if (!detail.product.attributeValues.length) {
          <p class="muted">Dit product heeft nog geen ingevulde kenmerken.</p>
        }
        @for (definition of detail.type.attributeDefinitions; track definition.id) {
          @if (definition.scope === 'Product') {
            <app-attribute-edit
              [productId]="detail.product.id"
              [definition]="definition"
              [current]="attributeValue(definition.id, detail.product.attributeValues)"
              (saved)="onSaved($event)"
            />
          }
        }
        <dl class="detail-list">
          @for (value of detail.product.attributeValues; track value.attributeDefinitionId) {
            <dt>
              {{ attributeName(value.attributeDefinitionId, detail.type.attributeDefinitions) }}
            </dt>
            <dd>{{ displayValue(value) }}</dd>
          }
        </dl>
      </section>
      <app-orphan-values
        [productId]="detail.product.id"
        [values]="detail.product.attributeValues"
        [definitions]="detail.type.attributeDefinitions"
        (saved)="onSaved($event)"
      />
      <app-product-categories [product]="detail.product" (saved)="onSaved($event)" />
      <app-product-images [product]="detail.product" (saved)="onSaved($event)" />
      <app-product-validation [product]="detail.product" [type]="detail.type" />
      <h2>Varianten</h2>
      <form class="toolbar" (ngSubmit)="searchVariants()">
        <label
          >Zoek variant<input
            name="variantSearch"
            type="search"
            maxlength="200"
            placeholder="Naam of artikelnummer"
            [(ngModel)]="variantSearchText"
            [disabled]="editState.busy()"
        /></label>
        <button class="secondary" [disabled]="editState.busy()">Zoeken</button>
        <label
          >Voorraad<select
            name="variantStock"
            [ngModel]="variantStock()"
            (ngModelChange)="filterVariants($event)"
            [disabled]="editState.busy()"
          >
            <option value="all">Alle varianten</option>
            <option value="low">Lage voorraad (0–5)</option>
            <option value="out">Uitverkocht</option>
            <option value="untracked">Voorraad niet gevolgd</option>
          </select></label
        >
        @if (variantSearch() || variantStock() !== 'all') {
          <button
            type="button"
            class="secondary"
            [disabled]="editState.busy()"
            (click)="clearVariantFilters()"
          >
            Alle varianten tonen
          </button>
        }
      </form>
      @if (variantFilterError()) {
        <p class="error" role="alert">{{ variantFilterError() }}</p>
      }
      <p role="status">
        {{ visibleVariants(detail.product.variants).length }} van
        {{ detail.product.variants.length }} varianten
      </p>
      @if (!visibleVariants(detail.product.variants).length) {
        <p>Geen varianten gevonden. Pas de zoekterm of voorraadselectie aan.</p>
      }
      <div class="grid">
        @for (variant of visibleVariants(detail.product.variants); track variant.id) {
          <section class="panel">
            <h3>{{ variant.name }}</h3>
            <app-variant-edit
              [productId]="detail.product.id"
              [variant]="variant"
              [variantCount]="detail.product.variants.length"
              (saved)="onSaved($event)"
            />
            <app-price-rule-edit
              [productId]="detail.product.id"
              [variant]="variant"
              (saved)="onSaved($event)"
            />
            <dl class="detail-list">
              <dt>Artikelnummer</dt>
              <dd>{{ variant.sku || 'Nog niet ingevuld' }}</dd>
              <dt>Basisprijs</dt>
              <dd>
                @if (variant.price; as price) {
                  @if (price.netAmount != null) {
                    <p>
                      Prijs exclusief btw: {{ price.netAmount.replace('.', ',') }}
                      {{ price.currency }}
                    </p>
                    <p>
                      Btw {{ price.vatExempt ? '(vrijgesteld)' : '(' + price.vatRate + '%)' }}:
                      {{ price.vatAmount?.replace('.', ',') }} {{ price.currency }}
                    </p>
                    <p>
                      Totaal klantprijs: {{ price.grossAmount?.replace('.', ',') }}
                      {{ price.currency }}
                    </p>
                  } @else {
                    @if (price.grossAmount) {
                      {{ price.grossAmount.replace('.', ',') }} {{ price.currency }}
                    } @else {
                      {{ price.amount | currency: price.currency }}
                    }
                    <span class="muted"> · Btw nog niet vastgelegd</span>
                  }
                } @else {
                  Nog niet ingesteld
                }
              </dd>
              <dt>Voorraad</dt>
              <dd>
                {{
                  variant.stockQuantity === null || variant.stockQuantity === undefined
                    ? 'Niet gevolgd'
                    : variant.stockQuantity === 0
                      ? 'Uitverkocht'
                      : variant.stockQuantity
                }}
              </dd>
            </dl>
            @for (definition of detail.type.attributeDefinitions; track definition.id) {
              @if (definition.scope === 'Variant') {
                <app-attribute-edit
                  [productId]="detail.product.id"
                  [variantId]="variant.id"
                  [definition]="definition"
                  [current]="attributeValue(definition.id, variant.attributeValues)"
                  (saved)="onSaved($event)"
                />
              }
            }
            <app-orphan-values
              [productId]="detail.product.id"
              [variantId]="variant.id"
              [values]="variant.attributeValues"
              [definitions]="detail.type.attributeDefinitions"
              (saved)="onSaved($event)"
            />
            @if (variant.attributeValues.length) {
              <dl class="detail-list">
                @for (value of variant.attributeValues; track value.attributeDefinitionId) {
                  <dt>
                    {{
                      attributeName(value.attributeDefinitionId, detail.type.attributeDefinitions)
                    }}
                  </dt>
                  <dd>{{ displayValue(value) }}</dd>
                }
              </dl>
            }
          </section>
        }
      </div>
    }`,
})
export class ProductDetail {
  readonly editState = inject(ProductEditState);
  readonly variantSearch = signal('');
  readonly variantStock = signal<ProductStockFilter | 'all'>('all');
  readonly variantFilterError = signal('');
  variantSearchText = '';
  private variantProductId = '';
  private readonly api = inject(CatalogApi);
  private readonly route = inject(ActivatedRoute);
  readonly listQuery = toSignal(this.route.queryParamMap.pipe(map(readProductListQuery)));
  private readonly router = inject(Router);
  private readonly refresh = new BehaviorSubject(0);
  readonly notice = signal('');
  readonly state = toSignal(
    combineLatest([
      this.route.paramMap.pipe(
        distinctUntilChanged((a, b) => a.get('id') === b.get('id')),
        tap(() => {
          this.notice.set('');
          this.editState.busy.set(false);
        }),
      ),
      this.refresh,
    ]).pipe(
      switchMap(([params]) =>
        loadState(
          this.api
            .product(params.get('id')!)
            .pipe(
              switchMap((product) =>
                this.api.type(product.productTypeId).pipe(map((type) => ({ product, type }))),
              ),
            ),
        ),
      ),
    ),
  );
  constructor() {
    effect(() => {
      this.variantStock.set(this.listQuery()?.stock ?? 'all');
    });
    effect(() => {
      const id = this.state()?.data?.product.id;
      if (!id || id === this.variantProductId) return;
      this.variantProductId = id;
      this.variantSearchText = '';
      this.variantSearch.set('');
      this.variantFilterError.set('');
      this.variantStock.set(this.listQuery()?.stock ?? 'all');
    });
    effect(() => {
      if (this.editState.busy()) this.notice.set('');
    });
  }
  searchVariants() {
    if (this.editState.busy()) return;
    const search = this.variantSearchText.trim();
    if (search.length > 200) {
      this.variantFilterError.set('Gebruik maximaal 200 tekens voor de variantzoekterm.');
      return;
    }
    this.variantSearch.set(search);
    this.variantFilterError.set('');
  }
  filterVariants(stock: ProductStockFilter | 'all') {
    if (this.editState.busy() || !['all', 'low', 'out', 'untracked'].includes(stock)) return;
    this.variantStock.set(stock);
    this.variantFilterError.set('');
  }
  clearVariantFilters() {
    if (this.editState.busy()) return;
    this.variantSearchText = '';
    this.variantSearch.set('');
    this.variantStock.set('all');
    this.variantFilterError.set('');
  }
  visibleVariants(variants: readonly Variant[]) {
    const search = this.variantSearch().toLowerCase(),
      stock = this.variantStock();
    return variants.filter((variant) => {
      const quantity = variant.stockQuantity;
      const matches =
        stock === 'all' ||
        (stock === 'out' && quantity === 0) ||
        (stock === 'untracked' && quantity == null) ||
        (stock === 'low' && quantity != null && quantity >= 0 && quantity <= 5);
      return (
        matches &&
        (!search ||
          variant.name.toLowerCase().includes(search) ||
          (variant.sku ?? '').toLowerCase().includes(search))
      );
    });
  }
  onSaved(message: string) {
    this.notice.set(message);
    this.reload();
  }
  onRemoved() {
    void this.router.navigate(['/producten'], {
      queryParams: { ...this.listQuery(), offset: null },
    });
  }
  reload() {
    if (this.editState.busy() || this.state()?.loading) return;
    this.refresh.next(this.refresh.value + 1);
  }
  attributeValue(id: string, values: AttributeValue[]) {
    return values.find((value) => value.attributeDefinitionId === id);
  }
  attributeName(id: string, definitions: AttributeDefinition[]) {
    return definitions.find((value) => value.id === id)?.displayName ?? 'Verwijderd kenmerk';
  }
  displayValue(value: AttributeValue) {
    return Array.isArray(value.value)
      ? value.value.join(', ')
      : typeof value.value === 'boolean'
        ? value.value
          ? 'Ja'
          : 'Nee'
        : String(value.value);
  }
}
