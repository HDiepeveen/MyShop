import { CopyText } from '../copy-text';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { BehaviorSubject, combineLatest, distinctUntilChanged, switchMap, tap } from 'rxjs';
import { loadState } from '../catalog/load-state';
import { errorMessage } from '../catalog/error-message';
import { OrderDetail, OrderManagementApi } from './order-management.api';

@Component({
  imports: [RouterLink, DatePipe, CurrencyPipe, CopyText],
  host: { class: 'printable-order' },
  template: ` <p class="print-only">MyShop · Besteloverzicht</p>
    <a class="back print-hide" routerLink="/bestellingen" queryParamsHandling="preserve"
      >← Terug naar bestellingen</a
    >
    @if (state()?.loading) {
      <p class="loading" role="status">Bestelling ophalen…</p>
    }
    @if (state()?.error) {
      <div class="error" role="alert">
        {{ state()?.error }} <button class="secondary" (click)="retry()">Opnieuw proberen</button>
      </div>
    }
    @if (notice()) {
      <p class="success print-hide" role="status">{{ notice() }}</p>
    }
    @if (actionError()) {
      <p class="error print-hide" role="alert">{{ actionError() }}</p>
    }
    @if (state()?.data; as order) {
      <p class="print-hide">
        <a [routerLink]="['/bestellingen', order.id, 'factuur']">Factuur bekijken / uitgeven</a>
      </p>
      <div class="eyebrow">Bestelling</div>
      <button type="button" class="secondary" [disabled]="saving()" (click)="printOrder(order)">
        Besteloverzicht afdrukken / PDF
      </button>
      <div class="page-head">
        <div>
          <h1>{{ order.number }}</h1>
          <app-copy-text [text]="order.number" label="Bestelnummer kopiëren" />
          <p class="muted">Geplaatst op {{ order.placedAt | date: 'dd-MM-yyyy HH:mm' }}</p>
        </div>
        <span class="badge">{{ statusLabel(order.status) }}</span>
      </div>
      <div class="grid">
        <section class="panel">
          <h2>Klant</h2>
          <dl class="detail-list">
            <dt>Naam</dt>
            <dd>{{ order.customer.name }}</dd>
            <dt>E-mail</dt>
            <dd>
              <a [href]="'mailto:' + order.customer.email">{{ order.customer.email }}</a>
            </dd>
          </dl>
        </section>
        @if (order.deliveryMethod; as delivery) {
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
          <address>
            {{ order.deliveryAddress.addressLine }}<br />{{ order.deliveryAddress.postalCode }}
            {{ order.deliveryAddress.city }}<br />{{ order.deliveryAddress.countryCode }}
          </address>
        </section>
      </div>
      <section class="panel">
        <h2>Tijdlijn</h2>
        <ol class="timeline">
          @for (event of timeline(order); track event.label) {
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
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Product</th>
                <th>Aantal</th>
                <th>Prijs</th>
                <th>Totaal</th>
              </tr>
            </thead>
            <tbody>
              @for (line of order.lines; track line.variantId) {
                <tr>
                  <td>
                    {{ line.productName }}<br /><span class="muted">{{ line.variantName }}</span>
                  </td>
                  <td>{{ line.quantity }}</td>
                  <td class="nowrap">{{ line.unitAmount | currency: line.currency }}</td>
                  <td class="nowrap">{{ line.totalAmount | currency: line.currency }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </section>
      <section class="panel">
        <h2>Betaling</h2>
        <dl class="detail-list">
          <dt>Betaalmethode</dt>
          <dd>{{ paymentMethodLabel(order.paymentMethod) }}</dd>
          @if (order.paymentInstructions) {
            <dt>Betaalinstructies</dt>
            <dd class="preserve-lines">{{ order.paymentInstructions }}</dd>
          }
          <dt>Status</dt>
          <dd>{{ statusLabel(order.status) }}</dd>
          @if (order.paidAt) {
            <dt>Betaald op</dt>
            <dd>{{ order.paidAt | date: 'dd-MM-yyyy HH:mm' }}</dd>
            @if (order.paymentReference) {
              <dt>Betalingskenmerk</dt>
              <dd>
                {{ order.paymentReference }}
                <app-copy-text [text]="order.paymentReference" label="Betalingskenmerk kopiëren" />
              </dd>
            }
          }
          @if (order.shippedAt) {
            <dt>Verzonden op</dt>
            <dd>{{ order.shippedAt | date: 'dd-MM-yyyy HH:mm' }}</dd>
            @if (order.shippingCarrier) {
              <dt>Vervoerder</dt>
              <dd>{{ order.shippingCarrier }}</dd>
            }
            @if (order.trackingCode) {
              <dt>Trackingcode</dt>
              <dd>
                {{ order.trackingCode }}
                <app-copy-text [text]="order.trackingCode" label="Trackingcode kopiëren" />
              </dd>
            }
          }
          @if (order.cancelledAt) {
            <dt>Geannuleerd op</dt>
            <dd>{{ order.cancelledAt | date: 'dd-MM-yyyy HH:mm' }}</dd>
            <dt>Reden</dt>
            <dd class="preserve-lines">{{ order.cancellationReason }}</dd>
          }
          @if (order.refundedAt) {
            <dt>Terugbetaald op</dt>
            <dd>{{ order.refundedAt | date: 'dd-MM-yyyy HH:mm' }}</dd>
            <dt>Terugbetalingskenmerk</dt>
            <dd>
              {{ order.refundReference }}
              @if (order.refundReference) {
                <app-copy-text
                  [text]="order.refundReference"
                  label="Terugbetalingskenmerk kopiëren"
                />
              }
            </dd>
            <dt>Reden</dt>
            <dd class="preserve-lines">{{ order.refundReason }}</dd>
          }
          @for (total of order.totals; track total.currency) {
            <dt>Totaal ({{ total.currency }})</dt>
            <dd>{{ total.amount | currency: total.currency }}</dd>
          }
        </dl>
      </section>
      @if (order.status === 'awaitingPayment') {
        <section class="panel print-hide">
          <h2>Betaling verwerken</h2>
          <p class="muted">Gebruik dit nadat je hebt gecontroleerd dat de betaling is ontvangen.</p>
          <details>
            <summary>Bestelling als betaald markeren</summary>
            <p>De betaalstatus van {{ order.number }} wordt definitief bijgewerkt.</p>
            <label
              >Betalingskenmerk
              <input
                #paymentReference
                [disabled]="saving()"
                maxlength="100"
                required
                placeholder="Bijvoorbeeld: bankafschrift 12345"
            /></label>
            <button
              type="button"
              [disabled]="
                saving() ||
                !paymentReference.value.trim() ||
                paymentReference.value.trim().length > 100
              "
              (click)="markPaid(order, paymentReference.value)"
            >
              {{ saving() ? 'Opslaan…' : 'Bevestigen als betaald' }}
            </button>
          </details>
        </section>
        <section class="panel print-hide">
          <h2>Bestelling annuleren</h2>
          <p class="muted">
            Annuleren is alleen mogelijk zolang er nog geen betaling is vastgelegd.
          </p>
          <details>
            <summary>Onbetaalde bestelling annuleren</summary>
            <label>
              Reden voor annulering
              <textarea #reason [disabled]="saving()" rows="3" maxlength="500" required></textarea>
            </label>
            <button
              type="button"
              [disabled]="saving() || !reason.value.trim() || reason.value.trim().length > 500"
              (click)="cancelOrder(order, reason.value)"
            >
              {{ saving() ? 'Opslaan…' : 'Bestelling annuleren' }}
            </button>
          </details>
        </section>
      }
      @if (order.status === 'paid') {
        <section class="panel print-hide">
          <h2>Verzending verwerken</h2>
          <p class="muted">
            Gebruik dit nadat de volledige bestelling aan de vervoerder is overgedragen.
          </p>
          <details>
            <summary>Bestelling als verzonden markeren</summary>
            <p>De verzendstatus van {{ order.number }} wordt definitief bijgewerkt.</p>
            <label
              >Vervoerder
              <input
                #carrier
                [disabled]="saving()"
                maxlength="100"
                required
                placeholder="Bijvoorbeeld: PostNL"
            /></label>
            <label
              >Trackingcode
              <input
                #trackingCode
                [disabled]="saving()"
                maxlength="100"
                required
                placeholder="Bijvoorbeeld: 3S…"
            /></label>
            <button
              type="button"
              [disabled]="
                saving() ||
                !carrier.value.trim() ||
                carrier.value.trim().length > 100 ||
                !trackingCode.value.trim() ||
                trackingCode.value.trim().length > 100
              "
              (click)="markShipped(order, carrier.value, trackingCode.value)"
            >
              {{ saving() ? 'Opslaan…' : 'Bevestigen als verzonden' }}
            </button>
          </details>
        </section>
        <section class="panel print-hide">
          <h2>Terugbetaling registreren</h2>
          <p class="muted">
            Gebruik dit nadat het volledige bedrag buiten MyShop aan de klant is terugbetaald.
          </p>
          <details>
            <summary>Bestelling als terugbetaald markeren</summary>
            <label
              >Terugbetalingskenmerk
              <input
                #refundReference
                [disabled]="saving()"
                maxlength="100"
                required
                placeholder="Bijvoorbeeld: bankafschrift 67890"
            /></label>
            <label>
              Reden voor terugbetaling
              <textarea
                #refundReason
                [disabled]="saving()"
                rows="3"
                maxlength="500"
                required
              ></textarea>
            </label>
            <button
              type="button"
              [disabled]="
                saving() ||
                !refundReference.value.trim() ||
                refundReference.value.trim().length > 100 ||
                !refundReason.value.trim() ||
                refundReason.value.trim().length > 500
              "
              (click)="refundOrder(order, refundReference.value, refundReason.value)"
            >
              {{ saving() ? 'Opslaan…' : 'Terugbetaling bevestigen' }}
            </button>
          </details>
        </section>
      }
    }`,
})
export class OrderDetailComponent {
  private readonly api = inject(OrderManagementApi);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly refresh = new BehaviorSubject(0);
  readonly saving = signal(false);
  readonly actionError = signal('');
  readonly notice = signal('');
  readonly state = toSignal(
    combineLatest([
      this.route.paramMap.pipe(
        distinctUntilChanged((a, b) => a.get('id') === b.get('id')),
        tap(() => {
          this.notice.set('');
          this.actionError.set('');
        }),
      ),
      this.refresh,
    ]).pipe(switchMap(([params]) => loadState(this.api.get(params.get('id')!)))),
  );
  retry() {
    if (this.saving() || this.state()?.loading) return;
    this.refresh.next(this.refresh.value + 1);
  }
  paymentMethodLabel(method: OrderDetail['paymentMethod']) {
    return method === 'online' ? 'Direct online betalen' : 'Later betalen';
  }
  statusLabel(status: OrderDetail['status']) {
    return status === 'cancelled'
      ? 'Geannuleerd'
      : status === 'refunded'
        ? 'Terugbetaald'
        : status === 'shipped'
          ? 'Verzonden'
          : status === 'paid'
            ? 'Betaald'
            : 'Wacht op betaling';
  }
  timeline(order: OrderDetail) {
    const events: { label: string; at: string; note?: string }[] = [
      { label: 'Bestelling geplaatst', at: order.placedAt },
    ];
    if (order.paidAt)
      events.push({
        label: 'Betaling ontvangen',
        at: order.paidAt,
        note: order.paymentReference ? `Kenmerk: ${order.paymentReference}` : undefined,
      });
    if (order.shippedAt)
      events.push({
        label: 'Bestelling verzonden',
        at: order.shippedAt,
        note: [order.shippingCarrier, order.trackingCode].filter(Boolean).join(' · ') || undefined,
      });
    if (order.cancelledAt)
      events.push({
        label: 'Bestelling geannuleerd',
        at: order.cancelledAt,
        note: order.cancellationReason ?? undefined,
      });
    if (order.refundedAt)
      events.push({
        label: 'Terugbetaling geregistreerd',
        at: order.refundedAt,
        note: order.refundReference ? `Kenmerk: ${order.refundReference}` : undefined,
      });
    return events;
  }
  printOrder(order: OrderDetail) {
    if (this.state()?.data !== order || this.state()?.loading || this.saving()) return;
    window.print();
  }
  markPaid(order: OrderDetail, paymentReference: string) {
    paymentReference = paymentReference.trim();
    if (
      this.state()?.data !== order ||
      order.status !== 'awaitingPayment' ||
      this.saving() ||
      !paymentReference
    )
      return;
    if (paymentReference.length > 100) {
      this.actionError.set('Het betalingskenmerk mag maximaal 100 tekens bevatten.');
      return;
    }
    this.saving.set(true);
    this.actionError.set('');
    this.notice.set('');
    this.api
      .markPaid(order.id, order.revision, paymentReference)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.saving.set(false);
          if (this.state()?.data !== order) return;
          this.notice.set('De bestelling is als betaald gemarkeerd.');
          this.retry();
        },
        error: (error) => {
          this.saving.set(false);
          if (this.state()?.data !== order) return;
          this.actionError.set(errorMessage(error));
        },
      });
  }
  markShipped(order: OrderDetail, carrier: string, trackingCode: string) {
    carrier = carrier.trim();
    trackingCode = trackingCode.trim();
    if (
      this.state()?.data !== order ||
      order.status !== 'paid' ||
      this.saving() ||
      !carrier ||
      !trackingCode
    )
      return;
    if (carrier.length > 100 || trackingCode.length > 100) {
      this.actionError.set('Vervoerder en trackingcode mogen elk maximaal 100 tekens bevatten.');
      return;
    }
    this.saving.set(true);
    this.actionError.set('');
    this.notice.set('');
    this.api
      .markShipped(order.id, order.revision, carrier, trackingCode)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.saving.set(false);
          if (this.state()?.data !== order) return;
          this.notice.set('De bestelling is als verzonden gemarkeerd.');
          this.retry();
        },
        error: (error) => {
          this.saving.set(false);
          if (this.state()?.data !== order) return;
          this.actionError.set(errorMessage(error));
        },
      });
  }
  cancelOrder(order: OrderDetail, reason: string) {
    reason = reason.trim();
    if (
      this.state()?.data !== order ||
      order.status !== 'awaitingPayment' ||
      this.saving() ||
      !reason
    )
      return;
    if (reason.length > 500) {
      this.actionError.set('De reden mag maximaal 500 tekens bevatten.');
      return;
    }
    this.saving.set(true);
    this.actionError.set('');
    this.notice.set('');
    this.api
      .cancel(order.id, order.revision, reason)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.saving.set(false);
          if (this.state()?.data !== order) return;
          this.notice.set('De bestelling is geannuleerd.');
          this.retry();
        },
        error: (error) => {
          this.saving.set(false);
          if (this.state()?.data !== order) return;
          this.actionError.set(errorMessage(error));
        },
      });
  }
  refundOrder(order: OrderDetail, refundReference: string, reason: string) {
    refundReference = refundReference.trim();
    reason = reason.trim();
    if (
      this.state()?.data !== order ||
      order.status !== 'paid' ||
      this.saving() ||
      !refundReference ||
      !reason
    )
      return;
    if (refundReference.length > 100 || reason.length > 500) {
      this.actionError.set(
        'Gebruik maximaal 100 tekens voor het terugbetalingskenmerk en 500 voor de reden.',
      );
      return;
    }
    this.saving.set(true);
    this.actionError.set('');
    this.notice.set('');
    this.api
      .refund(order.id, order.revision, refundReference, reason)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.saving.set(false);
          if (this.state()?.data !== order) return;
          this.notice.set('De terugbetaling is geregistreerd.');
          this.retry();
        },
        error: (error) => {
          this.saving.set(false);
          if (this.state()?.data !== order) return;
          this.actionError.set(errorMessage(error));
        },
      });
  }
}
