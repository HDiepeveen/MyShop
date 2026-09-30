import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { BehaviorSubject, distinctUntilChanged, map, switchMap } from 'rxjs';
import { ShopApi } from './shop.api';
import { ShopImage } from './shop-image';
import { loadState } from '../catalog/load-state';
import { readListQuery } from '../catalog/list-query';
@Component({
  imports: [FormsModule, RouterLink, ShopImage, CurrencyPipe, DatePipe],
  styles: [
    `
      .description {
        white-space: pre-wrap;
        overflow-wrap: anywhere;
      }
    `,
  ],
  template: `
    <a class="back" routerLink="/winkel" [queryParams]="query()">← Terug naar het assortiment</a>
    @if (state()?.loading) {
      <p role="status">Product ophalen…</p>
    }
    @if (state()?.error) {
      <div class="panel" role="alert">
        <h1>Product niet beschikbaar</h1>
        <p>{{ state()?.error }}</p>
        <button class="secondary" (click)="retry()">Opnieuw proberen</button>
      </div>
    }
    @if (state()?.data; as product) {
      <h1>{{ product.name }}</h1>
      <div class="grid">
        <section class="panel">
          <app-shop-image [url]="product.imageUrl" [alt]="product.imageAlt" />
        </section>
        <section class="panel">
          <h2>Over dit product</h2>
          <p class="description">{{ product.description }}</p>
          @if (product.variants.length) {
            <label class="field"
              >Kies je variant<select
                name="variant"
                [ngModel]="selectedId()"
                (ngModelChange)="selectedId.set($event)"
              >
                @for (variant of product.variants; track variant.id) {
                  <option [value]="variant.id">{{ variant.name }}</option>
                }
              </select></label
            >
            @if (selected(); as variant) {
              <p role="status">Gekozen variant: {{ variant.name }}</p>
            }
          } @else {
            <p>Er zijn geen varianten beschikbaar.</p>
          }
          <section aria-label="Actuele prijs" aria-live="polite">
            @if (prices()?.loading) {
              <p>Prijs ophalen…</p>
            } @else if (prices()?.error) {
              <p role="alert">Prijs niet beschikbaar. Probeer de prijs opnieuw op te halen.</p>
            } @else if (selectedPrice(); as price) {
              @if (price.amount !== null && price.currency) {
                <p class="price">
                  {{ price.amount | currency: price.currency : 'code' : '1.2-2' }}
                </p>
              } @else {
                <p>Voor deze variant is nog geen prijs beschikbaar.</p>
              }
            }
            @if (prices()?.data && !selectedPrice()) {
              <p>Voor deze variant is nog geen prijs beschikbaar.</p>
            }
            @if (prices()?.data; as quote) {
              <p class="muted">Prijs opgehaald op {{ quote.at | date: 'dd-MM-yyyy HH:mm:ss' }}.</p>
            }
            <button
              type="button"
              class="secondary"
              [disabled]="prices()?.loading"
              (click)="refreshPrices()"
            >
              Prijs vernieuwen
            </button>
          </section>
          <p class="muted">Bestellen is nog niet beschikbaar.</p>
        </section>
      </div>
    }
  `,
})
export class ShopDetail {
  private readonly api = inject(ShopApi);
  private readonly route = inject(ActivatedRoute);
  private readonly refresh = new BehaviorSubject(0);
  readonly query = toSignal(this.route.queryParamMap.pipe(map(readListQuery)));
  private readonly priceRefresh = new BehaviorSubject(0);
  readonly prices = toSignal(
    this.route.paramMap.pipe(
      map((params) => params.get('id')!),
      distinctUntilChanged(),
      switchMap((id) => this.priceRefresh.pipe(switchMap(() => loadState(this.api.prices(id))))),
    ),
  );
  readonly selectedPrice = computed(() =>
    this.prices()?.data?.variants.find((price) => price.variantId === this.selectedId()),
  );
  refreshPrices() {
    this.priceRefresh.next(this.priceRefresh.value + 1);
  }
  readonly selectedId = signal('');
  readonly state = toSignal(
    this.route.paramMap.pipe(
      map((params) => params.get('id')!),
      distinctUntilChanged(),
      switchMap((id) => this.refresh.pipe(switchMap(() => loadState(this.api.product(id))))),
    ),
  );
  readonly selected = computed(() =>
    this.state()?.data?.variants.find((variant) => variant.id === this.selectedId()),
  );
  constructor() {
    effect(() => {
      const product = this.state()?.data;
      this.selectedId.set(product?.variants[0]?.id ?? '');
    });
  }
  retry() {
    this.refresh.next(this.refresh.value + 1);
  }
}
