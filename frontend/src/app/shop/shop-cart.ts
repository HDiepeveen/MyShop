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
import { ShopCheckout } from '../checkout/shop-checkout';
import { OrderReceipt } from '../checkout/order.api';
import { Auth } from '../auth/auth';
import { DeliveryMethodsApi } from '../checkout/delivery-methods.api';

@Component({
  imports: [DatePipe, FormsModule, RouterLink, ShopCheckout],
  template: `
    <a routerLink="/winkel">← Verder winkelen</a>
    @if (paymentOptions()?.data?.checkoutEnabled === false) {
      <h1>Assortiment</h1>
      <p>Bestellen is momenteel uitgeschakeld. Je kunt het assortiment bekijken.</p>
    } @else {
    <h1>Winkelmand</h1>
    @if (cart.warning()) {
      <p role="status">{{ cart.warning() }}</p>
    }
    @if (error()) {
      <p role="alert">{{ error() }}</p>
    }
    @if (notice()) {
      <p role="status">{{ notice() }}</p>
    }
    @if (undoItems().length && !orderReceipt()) {
      <button type="button" class="secondary" [disabled]="checkoutBusy()" (click)="undoRemoval()">
        Verwijdering ongedaan maken
      </button>
    }
    @if (orderReceipt(); as receipt) {
      <section class="panel" role="status">
        <h2>Bedankt voor je bestelling</h2>
        <p>
          Je bestelnummer is <strong>{{ receipt.number }}</strong
          >.
        </p>
        <dl class="detail-list">
          <dt>Geplaatst op</dt>
          <dd>{{ receipt.placedAt | date: 'dd-MM-yyyy HH:mm' }}</dd>
          @if (receipt.deliveryMethod; as delivery) {
            <dt>Ontvangst</dt>
            <dd>
              {{ delivery.name }} · {{ delivery.currency }} {{ amount(delivery.amount) }}
              @if (delivery.description) {
                <br /><span class="muted preserve-lines">{{ delivery.description }}</span>
              }
            </dd>
          }
          @for (total of receipt.totals; track total.currency) {
            <dt>Totaal {{ total.currency }}</dt>
            <dd>{{ total.currency }} {{ amount(total.amount) }}</dd>
          }
        </dl>
        @if (receipt.paymentInstructions) {
          <h3>Betaalinstructies</h3>
          <p class="preserve-lines">{{ receipt.paymentInstructions }}</p>
        } @else {
          <p>Je betaling is verwerkt. Bewaar dit nummer voor je administratie.</p>
        }
        @if (auth.session()?.customer) {
          <a [routerLink]="['/winkel/account/bestellingen', receipt.id]">Bestelling bekijken</a>
        } @else {
          <a routerLink="/winkel">Verder winkelen</a>
        }
      </section>
    } @else if (!cart.lines().length) {
      <p>Je winkelmand is leeg.</p>
    } @else {
      <button type="button" class="secondary" [disabled]="checkoutBusy()" (click)="clearCart()">
        Winkelmand leegmaken
      </button>
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
                [disabled]="checkoutBusy()"
                type="number"
                min="1"
                max="99"
                step="1"
                required
                [value]="line.quantity"
            /></label>
            <button type="submit" [disabled]="checkoutBusy()">Aantal bijwerken</button>
            <button
              type="button"
              class="secondary"
              [disabled]="checkoutBusy()"
              (click)="remove(line)"
            >
              Verwijderen
            </button>
          </form>
        </section>
      }
      @if (complete()) {
        @for (total of totals(); track total.currency) {
          <p>
            <strong>Subtotaal {{ total.currency }}: {{ amount(total.amount) }}</strong>
          </p>
        }
        <section class="panel">
          <h2>Hoe wil je je bestelling ontvangen?</h2>
          @if (deliveryMethods()?.loading) {
            <p role="status">Bezorgopties ophalen…</p>
          }
          @if (deliveryMethods()?.error) {
            <p role="alert">Bezorgopties konden niet worden opgehaald.</p>
            <button type="button" (click)="retryDelivery()">Bezorgopties opnieuw proberen</button>
          }
          @for (method of deliveryMethods()?.data ?? []; track method.id) {
            <label
              ><input
                type="radio"
                name="delivery"
                [value]="method.id"
                [disabled]="checkoutBusy()"
                [ngModel]="selectedDelivery()"
                (ngModelChange)="!checkoutBusy() && selectedDelivery.set($event)"
              />
              {{ method.name }} · {{ method.currency }} {{ amount(method.amount) }}</label
            >
            @if (method.id === selectedDelivery() && method.description) {
              <p class="muted preserve-lines">{{ method.description }}</p>
            }
          }
          @if (deliveryMethods()?.data && !deliveryMethods()?.data?.length) {
            <p role="alert">Er is momenteel geen bezorgoptie beschikbaar.</p>
          }
        </section>
        <section class="panel">
          <h2>Hoe wil je betalen?</h2>
          @if (paymentOptions()?.loading) {
            <p role="status">Betaalopties ophalen…</p>
          }
          @if (paymentOptions()?.error) {
            <p role="alert">Betaalopties konden niet worden opgehaald.</p>
            <button type="button" (click)="retryPayment()">Betaalopties opnieuw proberen</button>
          }
          @for (option of paymentOptions()?.data?.items ?? []; track option.code) {
            <label
              ><input
                type="radio"
                name="payment"
                [disabled]="checkoutBusy()"
                [value]="option.code"
                [ngModel]="selectedPayment()"
                (ngModelChange)="!checkoutBusy() && selectedPayment.set($event)"
              />
              {{ option.name }}</label
            >
            @if (option.code === selectedPayment() && option.instructions) {
              <p class="muted preserve-lines">{{ option.instructions }}</p>
            }
          }
          @if (paymentOptions()?.data && !paymentOptions()?.data?.items?.length) {
            <p role="alert">Er is momenteel geen betaaloptie beschikbaar.</p>
          }
        </section>
        @if (selectedPayment() && selectedDelivery()) {
          <app-shop-checkout
            [lines]="checkoutLines()"
            [paymentMethod]="selectedPayment()"
            [deliveryMethodId]="selectedDelivery()"
            (placed)="orderPlaced($event)"
            (busyChanged)="checkoutBusy.set($event)"
          />
        }
      } @else if (state()?.data) {
        <p>Geen subtotaal beschikbaar: controleer de artikelen hierboven.</p>
      }
      @if (quote()?.at; as at) {
        <p class="muted">Gecontroleerd op {{ at | date: 'dd-MM-yyyy HH:mm:ss' }}.</p>
      }
      <button
        type="button"
        class="secondary"
        [disabled]="state()?.loading || checkoutBusy()"
        (click)="refresh()"
      >
        Winkelmand vernieuwen
      </button>
      <p class="muted">Er wordt nog geen voorraad gereserveerd.</p>
    }
    }
  `,
})
export class ShopCart {
  readonly cart = inject(Cart);
  readonly auth = inject(Auth);
  private readonly api = inject(ShopApi);
  private readonly paymentApi = inject(PaymentOptionsApi);
  private readonly deliveryApi = inject(DeliveryMethodsApi);
  private readonly reload = new BehaviorSubject(0);
  private readonly paymentRefresh = new BehaviorSubject(0);
  private readonly deliveryRefresh = new BehaviorSubject(0);
  readonly error = signal('');
  readonly notice = signal('');
  readonly undoItems = signal<readonly CartLine[]>([]);
  readonly checkoutBusy = signal(false);
  readonly selectedPayment = signal('');
  readonly selectedDelivery = signal('');
  readonly orderReceipt = signal<OrderReceipt | null>(null);
  readonly paymentOptions = toSignal(
    this.paymentRefresh.pipe(switchMap(() => loadState(this.paymentApi.publicOptions()))),
  );
  readonly deliveryMethods = toSignal(
    this.deliveryRefresh.pipe(switchMap(() => loadState(this.deliveryApi.publicMethods()))),
  );
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
            : line.failure === 'outOfStock'
              ? 'Er is onvoldoende voorraad voor dit aantal. Verlaag het aantal of verwijder de regel.'
              : 'Voor deze variant is geen prijs beschikbaar.',
      })) ?? [],
  );
  readonly totals = computed(() => this.quote()?.totals ?? []);
  readonly checkoutLines = computed(
    () =>
      this.quote()?.lines.map((line) => ({
        productId: line.productId,
        variantId: line.variantId,
        quantity: line.quantity,
        expectedAmount: line.amount!,
        expectedCurrency: line.currency!,
      })) ?? [],
  );
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
    effect(() => {
      const methods = this.deliveryMethods()?.data ?? [];
      if (!methods.some((method) => method.id === this.selectedDelivery()))
        this.selectedDelivery.set(methods[0]?.id ?? '');
    });
  }
  amount(value: string | null) {
    return value?.replace('.', ',') ?? '';
  }
  update(line: CartLine, value: string) {
    if (this.checkoutBusy()) return;
    this.error.set(this.cart.setQuantity(line, value.trim() ? Number(value) : NaN));
  }
  remove(line: CartLine) {
    if (this.checkoutBusy()) return;
    const current = this.cart
      .lines()
      .find((item) => item.productId === line.productId && item.variantId === line.variantId);
    if (!current) return;
    this.error.set('');
    this.undoItems.set([{ ...current }]);
    this.cart.remove(current);
    this.notice.set('Artikel verwijderd.');
  }
  clearCart() {
    if (
      this.checkoutBusy() ||
      this.orderReceipt() ||
      !this.cart.lines().length ||
      !window.confirm('Wil je de hele winkelmand leegmaken?')
    )
      return;
    this.undoItems.set(this.cart.lines().map((line) => ({ ...line })));
    this.cart.clear();
    this.error.set('');
    this.notice.set('De winkelmand is leeggemaakt.');
  }
  undoRemoval() {
    if (this.checkoutBusy() || this.orderReceipt() || !this.undoItems().length) return;
    const error = this.cart.addLines(this.undoItems());
    this.error.set(error);
    if (error) return;
    this.undoItems.set([]);
    this.notice.set(
      'De verwijderde artikelen zijn teruggezet. Prijzen en voorraad worden opnieuw gecontroleerd.',
    );
  }
  refresh() {
    if (this.checkoutBusy() || this.state()?.loading) return;
    this.reload.next(this.reload.value + 1);
  }
  retryPayment() {
    if (this.checkoutBusy() || this.paymentOptions()?.loading) return;
    this.paymentRefresh.next(this.paymentRefresh.value + 1);
  }
  retryDelivery() {
    if (this.checkoutBusy() || this.deliveryMethods()?.loading) return;
    this.deliveryRefresh.next(this.deliveryRefresh.value + 1);
  }
  orderPlaced(receipt: OrderReceipt) {
    this.undoItems.set([]);
    this.checkoutBusy.set(false);
    this.notice.set('');
    this.error.set('');
    this.orderReceipt.set(receipt);
    this.cart.clear();
  }
}
