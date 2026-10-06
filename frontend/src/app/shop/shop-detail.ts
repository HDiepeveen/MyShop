import { Cart } from './cart';
import { DatePipe } from '@angular/common';
import { Component, computed, DestroyRef, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { BehaviorSubject, distinctUntilChanged, map, switchMap } from 'rxjs';
import { ShopApi } from './shop.api';
import { ShopImage } from './shop-image';
import { loadState } from '../catalog/load-state';
import { readWishlistReturn } from '../customer/wishlist-query';
import { readShopQuery, shopContextQuery } from './shop-query';
import { Auth } from '../auth/auth';
import { CustomerWishlistApi } from '../customer/customer-wishlist.api';
@Component({
  imports: [FormsModule, RouterLink, ShopImage, DatePipe],
  styles: [
    `
      .description {
        white-space: pre-wrap;
        overflow-wrap: anywhere;
      }
    `,
  ],
  template: `
    @if (wishlistReturn(); as query) {
      <a class="back" routerLink="/winkel/account/verlanglijst" [queryParams]="query"
        >← Terug naar mijn verlanglijst</a
      >
    }
    <a class="back" routerLink="/winkel" [queryParams]="contextQuery()"
      >← Terug naar het assortiment</a
    >
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
          @if (product.categories.length) {
            <nav aria-label="Productcategorieën" class="actions">
              @for (category of product.categories; track category.id) {
                <a routerLink="/winkel" [queryParams]="categoryQuery(category.id)">{{
                  category.name
                }}</a>
              }
            </nav>
          }
          @if (product.variants.length) {
            <label class="field"
              >Kies je variant<select
                name="variant"
                [ngModel]="selectedId()"
                (ngModelChange)="selectedId.set($event)"
              >
                @for (variant of product.variants; track variant.id) {
                  <option [value]="variant.id" [disabled]="variant.isAvailable === false">
                    {{ variant.name }}{{ variant.isAvailable === false ? ' – uitverkocht' : '' }}
                  </option>
                }
              </select></label
            >
            @if (selected(); as variant) {
              <p role="status">Gekozen variant: {{ variant.name }}</p>
              @if (variant.isAvailable === false) {
                <p role="status">Deze variant is uitverkocht.</p>
              }
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
                <p class="price">{{ price.currency }} {{ amount(price.amount) }}</p>
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
          <button
            type="button"
            [disabled]="
              !selected() ||
              prices()?.loading ||
              prices()?.error ||
              selectedPrice()?.amount == null ||
              selected()?.isAvailable === false
            "
            (click)="addToCart()"
          >
            In winkelmand
          </button>
          @if (cartMessage()) {
            <p role="status">
              {{ cartMessage() }} <a routerLink="/winkel/winkelmand">Naar winkelmand</a>
            </p>
          }
          @if (cart.warning()) {
            <p role="status">{{ cart.warning() }}</p>
          }
          @if (auth.session()?.customer) {
            <button
              type="button"
              class="secondary"
              [disabled]="wishlistBusy() || wishlistLoading()"
              (click)="toggleWishlist(product.id)"
            >
              {{ wishlistSaved() ? 'Van verlanglijst verwijderen' : 'Op verlanglijst zetten' }}
            </button>
            @if (wishlistMessage()) {
              <p role="status">{{ wishlistMessage() }}</p>
            }
          }
        </section>
      </div>
    }
  `,
})
export class ShopDetail {
  readonly auth = inject(Auth);
  private readonly wishlistApi = inject(CustomerWishlistApi);
  private readonly destroyRef = inject(DestroyRef);
  readonly wishlistSaved = signal(false);
  readonly wishlistBusy = signal(false);
  readonly wishlistLoading = signal(false);
  private wishlistVersion = 0;
  readonly wishlistMessage = signal('');
  readonly cart = inject(Cart);
  readonly cartMessage = signal('');
  addToCart() {
    const product = this.state()?.data;
    const variant = this.selected();
    if (
      !product ||
      !variant ||
      this.prices()?.loading ||
      this.prices()?.error ||
      this.selectedPrice()?.amount == null
    )
      return;
    this.cartMessage.set(this.cart.add(product.id, variant.id) || 'Toegevoegd aan je winkelmand.');
  }
  private readonly api = inject(ShopApi);
  private readonly route = inject(ActivatedRoute);
  private readonly refresh = new BehaviorSubject(0);
  readonly wishlistReturn = toSignal(this.route.queryParamMap.pipe(map(readWishlistReturn)));
  readonly query = toSignal(this.route.queryParamMap.pipe(map(readShopQuery)));
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
    if (this.prices()?.loading) return;
    this.priceRefresh.next(this.priceRefresh.value + 1);
  }
  amount(value: string) {
    return value.replace('.', ',');
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
      this.wishlistVersion++;
      this.wishlistSaved.set(false);
      this.wishlistMessage.set('');
      this.wishlistBusy.set(false);
      this.wishlistLoading.set(false);
      this.cartMessage.set('');
      this.selectedId.set(
        product?.variants.find((variant) => variant.isAvailable !== false)?.id ??
          product?.variants[0]?.id ??
          '',
      );
      if (product && this.auth.session()?.customer) this.loadWishlistState(product.id);
    });
  }
  private loadWishlistState(productId: string) {
    const version = this.wishlistVersion;
    this.wishlistLoading.set(true);
    this.wishlistApi
      .state(productId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (state) => {
          if (version !== this.wishlistVersion || this.state()?.data?.id !== productId) return;
          this.wishlistLoading.set(false);
          this.wishlistSaved.set(state.saved);
        },
        error: () => {
          if (version !== this.wishlistVersion || this.state()?.data?.id !== productId) return;
          this.wishlistLoading.set(false);
          this.wishlistMessage.set('De verlanglijststatus kon niet worden opgehaald.');
        },
      });
  }
  toggleWishlist(productId: string) {
    if (
      this.wishlistBusy() ||
      this.wishlistLoading() ||
      !this.auth.session()?.customer ||
      this.state()?.data?.id !== productId
    )
      return;
    const version = this.wishlistVersion;
    const saved = !this.wishlistSaved();
    this.wishlistBusy.set(true);
    this.wishlistMessage.set('');
    const request = this.wishlistSaved()
      ? this.wishlistApi.remove(productId)
      : this.wishlistApi.add(productId);
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        if (version !== this.wishlistVersion || this.state()?.data?.id !== productId) return;
        this.wishlistSaved.set(saved);
        this.wishlistBusy.set(false);
        this.wishlistMessage.set(
          saved ? 'Toegevoegd aan je verlanglijst.' : 'Verwijderd van je verlanglijst.',
        );
      },
      error: () => {
        if (version !== this.wishlistVersion || this.state()?.data?.id !== productId) return;
        this.wishlistBusy.set(false);
        this.wishlistMessage.set('Je verlanglijst kon niet worden bijgewerkt.');
      },
    });
  }
  categoryQuery(categoryId: string) {
    return { ...this.contextQuery(), categoryId, offset: null };
  }
  contextQuery() {
    const query = this.query();
    return query ? shopContextQuery(query) : {};
  }
  retry() {
    if (this.state()?.loading) return;
    this.refresh.next(this.refresh.value + 1);
  }
}
