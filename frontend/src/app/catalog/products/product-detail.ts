import { AttributeEdit } from './attribute-edit';
import { ProductEditState } from './product-edit-state';
import { ProductValidation } from './product-validation';
import { VariantEdit } from './variant-edit';
import { ProductCategories } from './product-categories';
import { ProductEdit } from './product-edit';
import { Component, effect, inject, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { BehaviorSubject, combineLatest, tap, distinctUntilChanged, map, switchMap } from 'rxjs';
import { CatalogApi } from '../catalog.api';
import { AttributeDefinition, AttributeValue } from '../catalog.models';
import { loadState } from '../load-state';

@Component({
  providers: [ProductEditState],
  imports: [
    RouterLink,
    CurrencyPipe,
    ProductEdit,
    ProductValidation,
    VariantEdit,
    ProductCategories,
    AttributeEdit,
  ],
  template: ` <a class="back" routerLink="/producten">← Alle producten</a>
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
      <app-product-edit [product]="detail.product" (saved)="onSaved($event)" />
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
      <app-product-categories [product]="detail.product" (saved)="onSaved($event)" />
      <app-product-validation [product]="detail.product" [type]="detail.type" />
      <h2>Varianten</h2>
      <div class="grid">
        @for (variant of detail.product.variants; track variant.id) {
          <section class="panel">
            <h3>{{ variant.name }}</h3>
            <app-variant-edit
              [productId]="detail.product.id"
              [variant]="variant"
              (saved)="onSaved($event)"
            />
            <dl class="detail-list">
              <dt>Artikelnummer</dt>
              <dd>{{ variant.sku || 'Nog niet ingevuld' }}</dd>
              <dt>Basisprijs</dt>
              <dd>
                {{
                  variant.price
                    ? (variant.price.amount | currency: variant.price.currency)
                    : 'Nog niet ingesteld'
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
  private readonly api = inject(CatalogApi);
  private readonly route = inject(ActivatedRoute);
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
      if (this.editState.busy()) this.notice.set('');
    });
  }
  onSaved(message: string) {
    this.notice.set(message);
    this.reload();
  }
  reload() {
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
