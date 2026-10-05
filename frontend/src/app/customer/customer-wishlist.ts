import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { CustomerWishlistApi, WishlistItem, WishlistPage } from './customer-wishlist.api';
import { ShopImage } from '../shop/shop-image';

@Component({
  imports: [RouterLink, ShopImage],
  template: `
    <a class="back" routerLink="/winkel/account">← Mijn account</a>
    <h1>Mijn verlanglijst</h1>
    @if (loading()) {
      <p role="status">Verlanglijst ophalen…</p>
    }
    @if (notice()) {
      <p role="status">{{ notice() }}</p>
    }
    @if (error()) {
      <div class="panel" role="alert">
        <p>{{ error() }}</p>
        <button (click)="load(offset())">Opnieuw proberen</button>
      </div>
    }
    @if (removeError()) {
      <p role="alert">{{ removeError() }}</p>
    }
    @if (!loading() && !error() && !items().length) {
      @if (offset() > 0) {
        <div class="panel">
          <p>Deze pagina bevat geen producten meer.</p>
          <button type="button" (click)="load(Math.max(0, offset() - 20))">Vorige pagina</button>
        </div>
      } @else {
        <div class="panel">
          <p>Je verlanglijst is nog leeg.</p>
          <a routerLink="/winkel">Bekijk het assortiment</a>
        </div>
      }
    }
    <div class="grid">
      @for (item of items(); track item.productId) {
        <article class="panel">
          <app-shop-image [url]="item.imageUrl" [alt]="item.imageAlt" />
          <h2>{{ item.name }}</h2>
          @if (item.isAvailable) {
            <p><a [routerLink]="['/winkel', item.productId]">Bekijk product</a></p>
          } @else {
            <p class="muted">Dit product is momenteel niet beschikbaar.</p>
          }
          <button class="secondary" [disabled]="loading() || !!removing()" (click)="remove(item)">
            Verwijderen
          </button>
        </article>
      }
    </div>
    @if (page(); as result) {
      @if (
        result.items.length &&
        (result.offset > 0 || result.offset + result.items.length < result.totalCount)
      ) {
        <nav class="toolbar" aria-label="Paginering">
          <button
            class="secondary"
            [disabled]="loading() || !!removing() || result.offset === 0"
            (click)="load(Math.max(0, result.offset - 20))"
          >
            Vorige
          </button>
          <span
            >{{ result.offset + 1 }}–{{ result.offset + result.items.length }} van
            {{ result.totalCount }}</span
          >
          <button
            class="secondary"
            [disabled]="
              loading() || !!removing() || result.offset + result.items.length >= result.totalCount
            "
            (click)="load(result.offset + 20)"
          >
            Volgende
          </button>
        </nav>
      }
    }
  `,
})
export class CustomerWishlist {
  private readonly api = inject(CustomerWishlistApi);
  private readonly destroyRef = inject(DestroyRef);
  readonly items = signal<WishlistItem[]>([]);
  readonly page = signal<WishlistPage | null>(null);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly notice = signal('');
  readonly removing = signal('');
  readonly removeError = signal('');
  readonly offset = signal(0);
  readonly Math = Math;
  constructor() {
    this.load(0);
  }
  load(offset = 0) {
    if (
      this.loading() ||
      this.removing() ||
      !Number.isSafeInteger(offset) ||
      offset < 0 ||
      offset % 20 !== 0
    )
      return;
    this.offset.set(offset);
    this.items.set([]);
    this.page.set(null);
    this.removeError.set('');
    this.loading.set(true);
    this.error.set('');
    this.notice.set('');
    this.api
      .list(offset)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (page) => {
          this.page.set(page);
          this.offset.set(page.offset);
          this.items.set(page.items);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Je verlanglijst kon niet worden opgehaald.');
          this.loading.set(false);
        },
      });
  }
  remove(item: WishlistItem) {
    if (
      this.loading() ||
      this.removing() ||
      !this.items().includes(item) ||
      !window.confirm('Wil je dit product van je verlanglijst verwijderen?')
    )
      return;
    this.removeError.set('');
    this.removing.set(item.productId);
    this.notice.set('');
    this.api
      .remove(item.productId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.items.update((items) => items.filter((value) => value.productId !== item.productId));
          this.page.update((page) =>
            page
              ? {
                  ...page,
                  items: page.items.filter((value) => value.productId !== item.productId),
                  totalCount: page.totalCount - 1,
                }
              : page,
          );
          this.notice.set('Verwijderd van je verlanglijst.');
          this.removing.set('');
        },
        error: () => {
          this.removeError.set('Het product kon niet worden verwijderd.');
          this.removing.set('');
        },
      });
  }
}
