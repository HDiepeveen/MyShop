import { DatePipe } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { EMPTY, catchError, switchMap } from 'rxjs';
import { errorMessage } from '../catalog/error-message';
import {
  CustomerOrderApi,
  CustomerOrderDetail as Detail,
  customerOrderStatus,
} from './customer-order.api';

@Component({
  imports: [DatePipe, RouterLink],
  template: `<a routerLink="/winkel/account/bestellingen">← Mijn bestellingen</a>
    @if (loading()) {
      <p role="status">Bestelling ophalen…</p>
    }
    @if (failure()) {
      <p class="error" role="alert">{{ failure() }}</p>
    }
    @if (order(); as item) {
      <div class="eyebrow">{{ status(item.status) }}</div>
      <h1>Bestelling {{ item.number }}</h1>
      <p>Geplaatst op {{ item.placedAt | date: 'dd-MM-yyyy HH:mm' }}.</p>
      @if (notice()) {
        <p role="status">{{ notice() }}</p>
      }
      @if (actionFailure()) {
        <p class="error" role="alert">{{ actionFailure() }}</p>
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
              @if (event.note) { <p>{{ event.note }}</p> }
            </li>
          }
        </ol>
      </section>
      <section class="panel">
        <h2>Artikelen</h2>
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
        <section class="panel"><h2>Bezorgoptie</h2>
          <p><strong>{{ delivery.name }}</strong> · {{ delivery.currency }} {{ delivery.amount.replace('.', ',') }}</p>
          @if (delivery.description) { <p class="preserve-lines">{{ delivery.description }}</p> }
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
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  readonly order = signal<Detail | null>(null);
  readonly loading = signal(true);
  readonly failure = signal('');
  readonly actionFailure = signal('');
  readonly notice = signal('');
  readonly cancelling = signal(false);
  readonly status = customerOrderStatus;
  paymentMethod(method: Detail['paymentMethod']) {
    return method === 'online' ? 'direct online betalen' : 'later betalen';
  }
  constructor() {
    this.route.paramMap
      .pipe(
        switchMap((params) => {
          this.order.set(null);
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
    if (order.shippedAt) events.push({
      label: 'Bestelling verzonden',
      at: order.shippedAt,
      note: [order.shippingCarrier, order.trackingCode].filter(Boolean).join(' · ') || undefined,
    });
    if (order.cancelledAt) events.push({ label: 'Bestelling geannuleerd', at: order.cancelledAt });
    if (order.refundedAt) events.push({ label: 'Terugbetaling geregistreerd', at: order.refundedAt });
    return events;
  }
  cancel(order: Detail) {
    if (this.order() !== order || order.status !== 'awaitingPayment' || this.cancelling() || !window.confirm('Wil je deze bestelling definitief annuleren?'))
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
