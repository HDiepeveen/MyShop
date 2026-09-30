import { DatePipe } from '@angular/common';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';

import { BehaviorSubject, combineLatest, map, of, switchMap } from 'rxjs';
import { Cart, CartLine } from './cart';
import { ShopApi } from './shop.api';
import { loadState } from '../catalog/load-state';
import { PaymentOptionsApi } from '../checkout/payment-options.api';

@Component({
  imports: [DatePipe, FormsModule, RouterLink],
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
              <p>Per stuk: {{ row.currency }} {{ amount(row.amount) }}</p>
              <p>Regelbedrag: {{ row.currency }} {{ amount(row.total) }}</p>
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
            <strong>Subtotaal {{ total.currency }}: {{ amount(total.amount) }}</strong>
          </p>
        }
        <p class="muted">
          Zonder eventuele verzendkosten. Prijzen worden opnieuw gecontroleerd bij wijzigingen.
        </p>
        <section class="panel">
          <h2>Hoe wil je betalen?</h2>
          @if (paymentOptions()?.loading) {
            <p role="status">Betaalopties ophalen…</p>
          }
          @if (paymentOptions()?.error) {
            <p role="alert">Betaalopties konden niet worden opgehaald.</p>
          }
          @for (option of paymentOptions()?.data?.items ?? []; track option.code) {
            <label
              ><input
                type="radio"
                name="payment"
                [value]="option.code"
                [ngModel]="selectedPayment()"
                (ngModelChange)="selectedPayment.set($event)"
              />
              {{ option.name }}</label
            >
          }
          @if (paymentOptions()?.data && !paymentOptions()?.data?.items?.length) {
            <p role="alert">Er is momenteel geen betaaloptie beschikbaar.</p>
          }
        </section>
      } @else if (state()?.data) {
        <p>Geen subtotaal beschikbaar: controleer de artikelen hierboven.</p>
      }
      @if (quote()?.at; as at) {
        <p class="muted">Gecontroleerd op {{ at | date: 'dd-MM-yyyy HH:mm:ss' }}.</p>
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
  private readonly paymentApi = inject(PaymentOptionsApi);
  private readonly reload = new BehaviorSubject(0);
  readonly error = signal('');
  readonly selectedPayment = signal('');
  readonly paymentOptions = toSignal(loadState(this.paymentApi.publicOptions()));
  readonly state = toSignal(
    combineLatest([toObservable(this.cart.lines), this.reload]).pipe(
      switchMap(([lines]) => {
        if (!lines.length)
          return of({
            data: { lines, quote: { at: '', lines: [], totals: [] } },
            loading: false,
            error: '',
          });
        return loadState(this.api.quote(lines).pipe(map((quote) => ({ lines, quote }))));
      }),
    ),
  );
  readonly quote = computed(() =>
    this.state()?.data?.lines === this.cart.lines() ? this.state()?.data?.quote : null,
  );
  readonly rows = computed(
    () =>
      this.quote()?.lines.map((line) => ({
        ...line,
        name: line.name ?? 'Artikel niet beschikbaar',
        message:
          line.failure === 'unavailable'
            ? 'Dit artikel is niet meer beschikbaar. Verwijder het uit je winkelmand.'
            : 'Voor deze variant is geen prijs beschikbaar.',
      })) ?? [],
  );
  readonly totals = computed(() => this.quote()?.totals ?? []);
  readonly complete = computed(
    () =>
      !this.state()?.loading &&
      !this.state()?.error &&
      this.rows().length === this.cart.lines().length &&
      this.totals().length > 0,
  );
  constructor() {
    effect(() => {
      const items = this.paymentOptions()?.data?.items ?? [];
      if (!items.some((option) => option.code === this.selectedPayment()))
        this.selectedPayment.set(items[0]?.code ?? '');
    });
  }
  amount(value: string | null) {
    return value?.replace('.', ',') ?? '';
  }
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
