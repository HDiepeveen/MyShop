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
    @if (loading()) { <p role="status">Verlanglijst ophalen…</p> }
    @if (error()) {
      <div class="panel" role="alert"><p>{{ error() }}</p><button (click)="load()">Opnieuw proberen</button></div>
    }
    @if (!loading() && !error() && !items().length) {
      <div class="panel"><p>Je verlanglijst is nog leeg.</p><a routerLink="/winkel">Bekijk het assortiment</a></div>
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
          <button class="secondary" [disabled]="removing() === item.productId" (click)="remove(item)">
            Verwijderen
          </button>
        </article>
      }
    </div>
    @if (page(); as result) {
      @if (result.items.length && (result.offset > 0 || result.offset + result.items.length < result.totalCount)) {
        <nav class="toolbar" aria-label="Paginering">
          <button class="secondary" [disabled]="loading() || result.offset === 0" (click)="load(Math.max(0, result.offset - 20))">Vorige</button>
          <span>{{ result.offset + 1 }}–{{ result.offset + result.items.length }} van {{ result.totalCount }}</span>
          <button class="secondary" [disabled]="loading() || result.offset + result.items.length >= result.totalCount" (click)="load(result.offset + 20)">Volgende</button>
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
  readonly removing = signal('');
  readonly Math = Math;
  constructor() { this.load(0); }
  load(offset = 0) {
    this.loading.set(true); this.error.set('');
    this.api.list(offset).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (page) => { this.page.set(page); this.items.set(page.items); this.loading.set(false); },
      error: () => { this.error.set('Je verlanglijst kon niet worden opgehaald.'); this.loading.set(false); },
    });
  }
  remove(item: WishlistItem) {
    this.removing.set(item.productId);
    this.api.remove(item.productId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => { this.items.update((items) => items.filter((value) => value.productId !== item.productId)); this.page.update((page) => page ? { ...page, items: page.items.filter((value) => value.productId !== item.productId), totalCount: page.totalCount - 1 } : page); this.removing.set(''); },
      error: () => { this.error.set('Het product kon niet worden verwijderd.'); this.removing.set(''); },
    });
  }
}
