import { CopyText } from '../copy-text';
import { Cart } from '../shop/cart';
import { DatePipe } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { toSignal, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  EMPTY,
  BehaviorSubject,
  catchError,
  combineLatest,
  distinctUntilChanged,
  switchMap,
  map,
} from 'rxjs';
import { readCustomerOrderQuery } from './customer-order-query';
import { errorMessage } from '../catalog/error-message';
import {
  CustomerOrderApi,
  CustomerOrderDetail as Detail,
  customerOrderStatus,
} from './customer-order.api';

@Component({
  imports: [DatePipe, RouterLink, CopyText],
  host: { class: 'printable-order' },
  template: `<p class="print-only">MyShop · Besteloverzicht</p>
    <a class="print-hide" routerLink="/winkel/account/bestellingen" [queryParams]="listQuery()"
      >← Mijn bestellingen</a
    >
    @if (loading()) {
      <p role="status">Bestelling ophalen…</p>
    }
    @if (failure()) {
      <p class="error" role="alert">{{ failure() }}</p>
      <button type="button" [disabled]="loading() || cancelling()" (click)="retry()">
        Opnieuw proberen
      </button>
    }
    @if (order(); as item) {
      <div class="eyebrow">{{ status(item.status) }}</div>
      <h1>Bestelling {{ item.number }}</h1>
      <app-copy-text [text]="item.number" label="Bestelnummer kopiëren" />
      <button type="button" class="secondary" [disabled]="cancelling()" (click)="printOrder(item)">
        Besteloverzicht afdrukken / PDF
      </button>
      <p>Geplaatst op {{ item.placedAt | date: 'dd-MM-yyyy HH:mm' }}.</p>
      @if (notice()) {
        <p class="print-hide" role="status">{{ notice() }}</p>
      }
      @if (actionFailure()) {
        <p class="error print-hide" role="alert">{{ actionFailure() }}</p>
      }
      @if (item.status === 'awaitingPayment') {
        <button type="button" class="secondary" [disabled]="cancelling()" (click)="cancel(item)">
          {{ cancelling() ? 'Annuleren…' : 'Bestelling annuleren' }}
        </button>
      }
      <section class="panel">
        <h2>Tijdlijn</h2>
        <ol class="timeline">
          @for (event of timeline(item); track event.label) {
            <li>
              <strong>{{ event.label }}</strong>
              <span>{{ event.at | date: 'dd-MM-yyyy HH:mm' }}</span>
              @if (event.note) {
                <p>{{ event.note }}</p>
              }
            </li>
          }
        </ol>
      </section>
      <section class="panel">
        <h2>Artikelen</h2>
        @if (item.lines.length) {
          <p class="muted print-hide">
            Opnieuw bestellen zet deze artikelen in je winkelmand. Je controleert daarna de actuele
            prijzen, voorraad en bezorgoptie.
          </p>
          <button
            type="button"
            class="secondary"
            [disabled]="cancelling() || reordered()"
            (click)="reorder(item)"
          >
            Artikelen opnieuw bestellen
          </button>
          @if (reordered()) {
            <p class="print-hide"><a routerLink="/winkel/winkelmand">Winkelmand openen</a></p>
          }
        }
        @for (line of item.lines; track line.productId + line.variantId) {
          <h3>{{ line.productName }} · {{ line.variantName }}</h3>
          <p>
            {{ line.quantity }} × {{ line.currency }} {{ amount(line.unitAmount) }} =
            <strong>{{ line.currency }} {{ amount(line.totalAmount) }}</strong>
          </p>
        }
        @for (total of item.totals; track total.currency) {
          <p>
            <strong>Totaal {{ total.currency }} {{ amount(total.amount) }}</strong>
          </p>
        }
      </section>
      @if (item.deliveryMethod; as delivery) {
        <section class="panel">
          <h2>Bezorgoptie</h2>
          <p>
            <strong>{{ delivery.name }}</strong> · {{ delivery.currency }}
            {{ delivery.amount.replace('.', ',') }}
          </p>
          @if (delivery.description) {
            <p class="preserve-lines">{{ delivery.description }}</p>
          }
        </section>
      }
      <section class="panel">
        <h2>Bezorgadres</h2>
        <p>
          {{ item.customer.name }}<br />
          {{ item.deliveryAddress.addressLine }}<br />{{ item.deliveryAddress.postalCode }}
          {{ item.deliveryAddress.city }}<br />
          {{ item.deliveryAddress.countryCode }}
        </p>
        <p>{{ item.customer.email }}</p>
      </section>
      <section class="panel">
        <h2>Betaling en verzending</h2>
        <p>Betaalmethode: {{ paymentMethod(item.paymentMethod) }}</p>
        @if (item.paymentInstructions) {
          <p class="preserve-lines">{{ item.paymentInstructions }}</p>
        }
        @if (item.paidAt) {
          <p>Betaald op {{ item.paidAt | date: 'dd-MM-yyyy HH:mm' }}.</p>
        }
        @if (item.shippedAt) {
          <p>Verzonden op {{ item.shippedAt | date: 'dd-MM-yyyy HH:mm' }}.</p>
        }
        @if (item.shippingCarrier || item.trackingCode) {
          <p>
            Vervoerder: {{ item.shippingCarrier ?? 'Onbekend' }}<br />Track-en-trace:
            {{ item.trackingCode ?? 'Niet beschikbaar' }}
          </p>
          @if (item.trackingCode) {
            <app-copy-text [text]="item.trackingCode" label="Trackingcode kopiëren" />
          }
        }
        @if (item.cancelledAt) {
          <p>Geannuleerd op {{ item.cancelledAt | date: 'dd-MM-yyyy HH:mm' }}.</p>
        }
        @if (item.refundedAt) {
          <p>Terugbetaald op {{ item.refundedAt | date: 'dd-MM-yyyy HH:mm' }}.</p>
        }
      </section>
    }`,
})
export class CustomerOrderDetail {
  private readonly api = inject(CustomerOrderApi);
  private readonly cart = inject(Cart);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  readonly listQuery = toSignal(this.route.queryParamMap.pipe(map(readCustomerOrderQuery)));
  private readonly destroyRef = inject(DestroyRef);
  readonly order = signal<Detail | null>(null);
  readonly loading = signal(true);
  readonly failure = signal('');
  readonly actionFailure = signal('');
  readonly notice = signal('');
  readonly cancelling = signal(false);
  readonly reordered = signal(false);
  readonly status = customerOrderStatus;
  paymentMethod(method: Detail['paymentMethod']) {
    return method === 'online' ? 'direct online betalen' : 'later betalen';
  }
  private readonly refresh = new BehaviorSubject(0);
  retry() {
    if (this.loading() || this.cancelling()) return;
    this.refresh.next(this.refresh.value + 1);
  }
  constructor() {
    combineLatest([
      this.route.paramMap.pipe(distinctUntilChanged((a, b) => a.get('id') === b.get('id'))),
      this.refresh,
    ])
      .pipe(
        switchMap(([params]) => {
          this.order.set(null);
          this.reordered.set(false);
          this.loading.set(true);
          this.failure.set('');
          this.actionFailure.set('');
          this.notice.set('');
          return this.api.get(params.get('id') ?? '').pipe(
            catchError((error) => {
              this.failure.set(errorMessage(error));
              this.loading.set(false);
              return EMPTY;
            }),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (order) => {
          this.order.set(order);
          this.loading.set(false);
        },
        error: (error) => {
          this.failure.set(errorMessage(error));
          this.loading.set(false);
        },
      });
  }
  amount(value: string) {
    return value.replace('.', ',');
  }
  timeline(order: Detail) {
    const events: { label: string; at: string; note?: string }[] = [
      { label: 'Bestelling geplaatst', at: order.placedAt },
    ];
    if (order.paidAt) events.push({ label: 'Betaling ontvangen', at: order.paidAt });
    if (order.shippedAt)
      events.push({
        label: 'Bestelling verzonden',
        at: order.shippedAt,
        note: [order.shippingCarrier, order.trackingCode].filter(Boolean).join(' · ') || undefined,
      });
    if (order.cancelledAt) events.push({ label: 'Bestelling geannuleerd', at: order.cancelledAt });
    if (order.refundedAt)
      events.push({ label: 'Terugbetaling geregistreerd', at: order.refundedAt });
    return events;
  }
  printOrder(order: Detail) {
    if (this.order() !== order || this.loading() || this.cancelling()) return;
    window.print();
  }
  reorder(order: Detail) {
    if (this.order() !== order || this.loading() || this.cancelling() || this.reordered()) return;
    if (
      this.cart.lines().length &&
      !window.confirm(
        'De artikelen worden samengevoegd met je bestaande winkelmand. Wil je doorgaan?',
      )
    )
      return;
    const error = this.cart.addLines(
      order.lines.map((line) => ({
        productId: line.productId,
        variantId: line.variantId,
        quantity: line.quantity,
      })),
    );
    if (error) {
      this.actionFailure.set(error);
      return;
    }
    this.actionFailure.set('');
    this.reordered.set(true);
    this.notice.set(
      'De artikelen staan in je winkelmand. Controleer de actuele prijzen en voorraad voordat je bestelt.',
    );
    void this.router
      .navigate(['/winkel/winkelmand'])
      .then((opened) => {
        if (!opened && this.order() === order)
          this.actionFailure.set('Je winkelmand is bijgewerkt. Open de winkelmand via de link.');
      })
      .catch(() => {
        if (this.order() === order)
          this.actionFailure.set('Je winkelmand is bijgewerkt. Open de winkelmand via de link.');
      });
  }
  cancel(order: Detail) {
    if (
      this.order() !== order ||
      order.status !== 'awaitingPayment' ||
      this.cancelling() ||
      !window.confirm('Wil je deze bestelling definitief annuleren?')
    )
      return;
    this.cancelling.set(true);
    this.actionFailure.set('');
    this.notice.set('');
    this.api
      .cancel(order.id, order.revision)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.cancelling.set(false);
          if (this.order() !== order) return;
          this.order.set({ ...order, ...result });
          this.notice.set(
            'De bestelling is geannuleerd. De gereserveerde voorraad is vrijgegeven.',
          );
        },
        error: (error) => {
          this.cancelling.set(false);
          if (this.order() !== order) return;
          this.actionFailure.set(errorMessage(error));
        },
      });
  }
}
