import { Cart } from './cart';
import { DatePipe } from '@angular/common';
import { Component, computed, DestroyRef, effect, inject, signal, untracked } from '@angular/core';
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
import { chooseOption, optionAvailable, optionValue, variantOptions } from './variant-options';
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
          <app-shop-image [url]="product.imageUrl" [alt]="product.imageAlt" [large]="true" />
          <div class="grid" aria-label="Extra productfoto’s">
            @for (image of extraImages(); track image.id) {
              <app-shop-image [url]="image.url" [alt]="image.alternativeText" />
            }
          </div>
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
            @if (options(); as choices) {
              @for (dimension of choices.dimensions; track dimension.id) {
                <label class="field"
                  >Kies {{ dimension.name }}
                  <select
                    [attr.name]="'option-' + dimension.id"
                    [ngModel]="selectedOption(dimension.id)"
                    (ngModelChange)="selectOption(dimension.id, $event)"
                  >
                    <option value="" disabled>Kies {{ dimension.name }}</option>
                    @for (value of dimension.values; track value) {
                      <option [value]="value" [disabled]="!availableOption(dimension.id, value)">
                        {{ value
                        }}{{ availableOption(dimension.id, value) ? '' : ' – niet beschikbaar' }}
                      </option>
                    }
                  </select>
                </label>
              }
              @if (choices.otherVariants.length) {
                <details>
                  <summary>Andere uitvoeringen</summary>
                  <label class="field"
                    >Kies een andere uitvoering
                    <select
                      name="otherVariant"
                      [ngModel]="selectedId()"
                      (ngModelChange)="selectVariant($event)"
                    >
                      <option value="" disabled>Kies een uitvoering</option>
                      @for (variant of choices.otherVariants; track variant.id) {
                        <option [value]="variant.id" [disabled]="variant.isAvailable === false">
                          {{ variant.name
                          }}{{ variant.isAvailable === false ? ' – uitverkocht' : '' }}
                        </option>
                      }
                    </select>
                  </label>
                </details>
              }
            } @else {
              <label class="field"
                >Kies je variant<select
                  name="variant"
                  [ngModel]="selectedId()"
                  (ngModelChange)="selectVariant($event)"
                >
                  @for (variant of product.variants; track variant.id) {
                    <option [value]="variant.id" [disabled]="variant.isAvailable === false">
                      {{ variant.name }}{{ variant.isAvailable === false ? ' – uitverkocht' : '' }}
                    </option>
                  }
                </select></label
              >
            }
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
          @if (selected()) {
            <div class="toolbar">
              <button
                type="button"
                class="secondary"
                aria-label="Aantal verlagen"
                [disabled]="!validQuantity() || (quantity ?? 0) <= 1"
                (click)="changeQuantity(-1)"
              >
                −
              </button>
              <label
                >Aantal<input
                  name="quantity"
                  type="number"
                  min="1"
                  max="99"
                  step="1"
                  [(ngModel)]="quantity"
                  (ngModelChange)="clearCartFeedback()"
              /></label>
              <button
                type="button"
                class="secondary"
                aria-label="Aantal verhogen"
                [disabled]="!validQuantity() || (quantity ?? 0) >= 99"
                (click)="changeQuantity(1)"
              >
                +
              </button>
            </div>
            @if (cartQuantity()) {
              <p>
                {{ cartQuantity() }} {{ cartQuantity() === 1 ? 'stuk' : 'stuks' }} van deze variant
                in je winkelmand.
              </p>
            }
          }
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
          @if (cartError()) {
            <p role="alert">{{ cartError() }}</p>
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
  readonly cartError = signal('');
  quantity: number | null = 1;
  private selectionProductId = '';
  readonly cartQuantity = computed(() => {
    const product = this.state()?.data;
    const variant = this.selected();
    if (!product || !variant) return 0;
    return (
      this.cart
        .lines()
        .find(
          (line) =>
            line.productId === product.id.toLowerCase() &&
            line.variantId === variant.id.toLowerCase(),
        )?.quantity ?? 0
    );
  });
  validQuantity() {
    return (
      this.quantity !== null &&
      Number.isInteger(this.quantity) &&
      this.quantity >= 1 &&
      this.quantity <= 99
    );
  }
  clearCartFeedback() {
    this.cartMessage.set('');
    this.cartError.set('');
  }
  changeQuantity(delta: number) {
    if (!this.validQuantity() || (delta !== -1 && delta !== 1)) return;
    const quantity = this.quantity! + delta;
    if (quantity < 1 || quantity > 99) return;
    this.quantity = quantity;
    this.clearCartFeedback();
  }
  selectVariant(id: string) {
    if (
      !this.state()?.data?.variants.some(
        (variant) => variant.id === id && variant.isAvailable !== false,
      )
    )
      return;
    this.selectedId.set(id);
    this.clearCartFeedback();
  }
  readonly options = computed(() => variantOptions(this.state()?.data));
  selectedOption(definitionId: string) {
    return optionValue(this.selected(), definitionId) ?? '';
  }
  availableOption(definitionId: string, value: string) {
    const model = this.options();
    return !!model && optionAvailable(model, this.selected(), definitionId, value);
  }
  selectOption(definitionId: string, value: string) {
    const model = this.options();
    if (!model) return;
    const variant = chooseOption(model, this.selected(), definitionId, value);
    if (variant) this.selectVariant(variant.id);
  }
  addToCart() {
    this.clearCartFeedback();
    const product = this.state()?.data;
    const variant = this.selected();
    if (
      !product ||
      !variant ||
      variant.isAvailable === false ||
      this.prices()?.loading ||
      this.prices()?.error ||
      this.selectedPrice()?.amount == null
    )
      return;
    if (!this.validQuantity()) {
      this.cartError.set('Kies een heel aantal van 1 tot en met 99.');
      return;
    }
    const error = this.cart.add(product.id, variant.id, this.quantity!);
    if (error) this.cartError.set(error);
    else this.cartMessage.set('Toegevoegd aan je winkelmand.');
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
  readonly extraImages = computed(() => {
    const product = this.state()?.data;
    const main = product?.imageUrl;
    const mainId = main
      ?.match(/^\/api\/shop\/product-images\/([0-9a-f-]{36})$/i)?.[1]
      .toLowerCase();
    return (product?.images ?? []).filter(
      (image) => image.url !== main && (!mainId || image.id.toLowerCase() !== mainId),
    );
  });
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
      this.clearCartFeedback();
      if (product) {
        const candidates = variantOptions(product)?.variants ?? product.variants;
        const sameProduct = this.selectionProductId === product.id;
        if (!sameProduct) this.quantity = 1;
        const selected = untracked(() => this.selectedId());
        this.selectedId.set(
          (sameProduct
            ? product.variants.find(
                (variant) => variant.id === selected && variant.isAvailable !== false,
              )?.id
            : undefined) ??
            candidates.find((variant) => variant.isAvailable !== false)?.id ??
            candidates[0]?.id ??
            '',
        );
        this.selectionProductId = product.id;
      }
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
