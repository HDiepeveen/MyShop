import { CurrencyPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import {
  BehaviorSubject,
  catchError,
  combineLatest,
  forkJoin,
  map,
  of,
  switchMap,
  throwError,
} from 'rxjs';
import { Cart, CartLine } from './cart';
import { ShopApi } from './shop.api';
import { loadState } from '../catalog/load-state';

@Component({
  imports: [CurrencyPipe, FormsModule, RouterLink],
  template: `
    <a routerLink="/winkel">← Verder winkelen</a>
    <h1>Winkelmand</h1>
    @if (cart.warning()) {
      <p role="status">{{ cart.warning() }}</p>
    }
    @if (error()) {
      <p role="alert">{{ error() }}</p>
    }
    @if (!cart.lines().length) {
      <p>Je winkelmand is leeg.</p>
    } @else {
      @if (state()?.loading) {
        <p role="status">Producten en prijzen controleren…</p>
      }
      @if (state()?.error) {
        <p role="alert">
          De winkelmand kon niet worden gecontroleerd. Probeer opnieuw. Er wordt geen oud totaal
          getoond.
        </p>
      }
      @for (line of cart.lines(); track line.productId + line.variantId; let i = $index) {
        <section class="panel">
          @if (rows()[i]; as row) {
            <h2>
              <a [routerLink]="['/winkel', line.productId]">{{ row.name }}</a>
            </h2>
            <p>{{ row.variant }}</p>
            @if (row.amount !== null) {
              <p>Per stuk: {{ row.amount | currency: row.currency : 'code' : '1.2-2' }}</p>
              <p>Regelbedrag: {{ row.total | currency: row.currency : 'code' : '1.2-2' }}</p>
            } @else {
              <p role="status">{{ row.message }}</p>
            }
          } @else {
            <h2>Artikel {{ i + 1 }}</h2>
          }
          <form class="toolbar" (ngSubmit)="update(line, quantity.value)">
            <label
              >Aantal<input
                #quantity
                type="number"
                min="1"
                max="99"
                step="1"
                required
                [value]="line.quantity"
            /></label>
            <button type="submit">Aantal bijwerken</button>
            <button type="button" class="secondary" (click)="remove(line)">Verwijderen</button>
          </form>
        </section>
      }
      @if (complete()) {
        @for (total of totals(); track total.currency) {
          <p>
            <strong
              >Subtotaal {{ total.currency }}:
              {{ total.amount | currency: total.currency : 'code' : '1.2-2' }}</strong
            >
          </p>
        }
        <p class="muted">
          Zonder eventuele verzendkosten. Prijzen worden opnieuw gecontroleerd bij wijzigingen.
        </p>
      } @else if (state()?.data) {
        <p>Geen subtotaal beschikbaar: controleer de artikelen hierboven.</p>
      }
      <button type="button" class="secondary" [disabled]="state()?.loading" (click)="refresh()">
        Winkelmand vernieuwen
      </button>
      <p>Bestellen is nog niet beschikbaar. Er is geen voorraad gereserveerd.</p>
    }
  `,
})
export class ShopCart {
  readonly cart = inject(Cart);
  private readonly api = inject(ShopApi);
  private readonly reload = new BehaviorSubject(0);
  readonly error = signal('');
  readonly state = toSignal(
    combineLatest([toObservable(this.cart.lines), this.reload]).pipe(
      switchMap(([lines]) => {
        const ids = [...new Set(lines.map((line) => line.productId))];
        if (!ids.length) return of({ data: { lines, products: [] }, loading: false, error: '' });
        return loadState(
          forkJoin(
            ids.map((id) =>
              forkJoin({ product: this.api.product(id), prices: this.api.prices(id) }).pipe(
                map((data) => ({ id, ...data })),
                catchError((error) =>
                  error instanceof HttpErrorResponse && error.status === 404
                    ? of({ id, product: null, prices: null })
                    : throwError(() => error),
                ),
              ),
            ),
          ).pipe(map((products) => ({ lines, products }))),
        );
      }),
    ),
  );
  readonly rows = computed(() => {
    if (!this.state()?.data || this.state()!.data!.lines !== this.cart.lines()) return [];
    return this.cart.lines().map((line) => {
      const data = this.state()!.data!.products.find((data) => data.id === line.productId);
      const variant = data?.product?.variants.find((variant) => variant.id === line.variantId);
      const price = data?.prices?.variants.find((price) => price.variantId === line.variantId);
      const cents = price?.amount == null ? NaN : Math.round(price.amount * 100);
      const total = cents * line.quantity;
      const available =
        !!variant &&
        !!price?.currency &&
        Number.isSafeInteger(cents) &&
        cents >= 0 &&
        Number.isSafeInteger(total);
      return {
        name: data?.product?.name ?? 'Artikel niet beschikbaar',
        variant: variant?.name ?? '',
        amount: available ? price!.amount : null,
        currency: price?.currency ?? '',
        total: available ? total / 100 : 0,
        totalCents: available ? total : 0,
        message: !variant
          ? 'Dit artikel is niet meer beschikbaar. Verwijder het uit je winkelmand.'
          : 'Voor deze variant is geen bruikbare prijs beschikbaar.',
      };
    });
  });
  readonly totals = computed(() => {
    const sums = new Map<string, number>();
    for (const row of this.rows()) {
      if (row.amount === null) return [];
      const cents = (sums.get(row.currency) ?? 0) + row.totalCents;
      if (!Number.isSafeInteger(cents)) return [];
      sums.set(row.currency, cents);
    }
    return [...sums].map(([currency, cents]) => ({ currency, amount: cents / 100 }));
  });
  readonly complete = computed(
    () =>
      !this.state()?.loading &&
      !this.state()?.error &&
      this.rows().length === this.cart.lines().length &&
      this.totals().length > 0,
  );
  update(line: CartLine, value: string) {
    this.error.set(this.cart.setQuantity(line, value.trim() ? Number(value) : NaN));
  }
  remove(line: CartLine) {
    this.error.set('');
    this.cart.remove(line);
  }
  refresh() {
    this.reload.next(this.reload.value + 1);
  }
}
