import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { BehaviorSubject, combineLatest, distinctUntilChanged, switchMap } from 'rxjs';
import { loadState } from '../catalog/load-state';
import { errorMessage } from '../catalog/error-message';
import { OrderDetail, OrderManagementApi } from './order-management.api';

@Component({
  imports: [RouterLink, DatePipe, CurrencyPipe],
  template: ` <a class="back" routerLink="/bestellingen" queryParamsHandling="preserve"
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
      <p class="success" role="status">{{ notice() }}</p>
    }
    @if (actionError()) {
      <p class="error" role="alert">{{ actionError() }}</p>
    }
    @if (state()?.data; as order) {
      <div class="eyebrow">Bestelling</div>
      <div class="page-head">
        <div>
          <h1>{{ order.number }}</h1>
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
        <section class="panel">
          <h2>Bezorgadres</h2>
          <address>
            {{ order.deliveryAddress.addressLine }}<br />{{ order.deliveryAddress.postalCode }}
            {{ order.deliveryAddress.city }}<br />{{ order.deliveryAddress.countryCode }}
          </address>
        </section>
      </div>
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
          <dd>Later betalen</dd>
          <dt>Status</dt>
          <dd>{{ statusLabel(order.status) }}</dd>
          @if (order.paidAt) {
            <dt>Betaald op</dt>
            <dd>{{ order.paidAt | date: 'dd-MM-yyyy HH:mm' }}</dd>
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
              <dd>{{ order.trackingCode }}</dd>
            }
          }
          @if (order.cancelledAt) {
            <dt>Geannuleerd op</dt>
            <dd>{{ order.cancelledAt | date: 'dd-MM-yyyy HH:mm' }}</dd>
            <dt>Reden</dt>
            <dd class="preserve-lines">{{ order.cancellationReason }}</dd>
          }
          @for (total of order.totals; track total.currency) {
            <dt>Totaal ({{ total.currency }})</dt>
            <dd>{{ total.amount | currency: total.currency }}</dd>
          }
        </dl>
      </section>
      @if (order.status === 'awaitingPayment') {
        <section class="panel">
          <h2>Betaling verwerken</h2>
          <p class="muted">Gebruik dit nadat je hebt gecontroleerd dat de betaling is ontvangen.</p>
          <details>
            <summary>Bestelling als betaald markeren</summary>
            <p>De betaalstatus van {{ order.number }} wordt definitief bijgewerkt.</p>
            <button type="button" [disabled]="saving()" (click)="markPaid(order)">
              {{ saving() ? 'Opslaan…' : 'Bevestigen als betaald' }}
            </button>
          </details>
        </section>
        <section class="panel">
          <h2>Bestelling annuleren</h2>
          <p class="muted">
            Annuleren is alleen mogelijk zolang er nog geen betaling is vastgelegd.
          </p>
          <details>
            <summary>Onbetaalde bestelling annuleren</summary>
            <label>
              Reden voor annulering
              <textarea #reason rows="3" maxlength="500" required></textarea>
            </label>
            <button
              type="button"
              [disabled]="saving() || !reason.value.trim()"
              (click)="cancelOrder(order, reason.value)"
            >
              {{ saving() ? 'Opslaan…' : 'Bestelling annuleren' }}
            </button>
          </details>
        </section>
      }
      @if (order.status === 'paid') {
        <section class="panel">
          <h2>Verzending verwerken</h2>
          <p class="muted">
            Gebruik dit nadat de volledige bestelling aan de vervoerder is overgedragen.
          </p>
          <details>
            <summary>Bestelling als verzonden markeren</summary>
            <p>De verzendstatus van {{ order.number }} wordt definitief bijgewerkt.</p>
            <label
              >Vervoerder
              <input #carrier maxlength="100" required placeholder="Bijvoorbeeld: PostNL"
            /></label>
            <label
              >Trackingcode
              <input #trackingCode maxlength="100" required placeholder="Bijvoorbeeld: 3S…"
            /></label>
            <button
              type="button"
              [disabled]="saving() || !carrier.value.trim() || !trackingCode.value.trim()"
              (click)="markShipped(order, carrier.value, trackingCode.value)"
            >
              {{ saving() ? 'Opslaan…' : 'Bevestigen als verzonden' }}
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
      this.route.paramMap.pipe(distinctUntilChanged((a, b) => a.get('id') === b.get('id'))),
      this.refresh,
    ]).pipe(switchMap(([params]) => loadState(this.api.get(params.get('id')!)))),
  );
  retry() {
    this.refresh.next(this.refresh.value + 1);
  }
  statusLabel(status: OrderDetail['status']) {
    return status === 'cancelled'
      ? 'Geannuleerd'
      : status === 'shipped'
        ? 'Verzonden'
        : status === 'paid'
          ? 'Betaald'
          : 'Wacht op betaling';
  }
  markPaid(order: OrderDetail) {
    if (this.saving()) return;
    this.saving.set(true);
    this.actionError.set('');
    this.notice.set('');
    this.api
      .markPaid(order.id, order.revision)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.notice.set('De bestelling is als betaald gemarkeerd.');
          this.retry();
        },
        error: (error) => {
          this.saving.set(false);
          this.actionError.set(errorMessage(error));
        },
      });
  }
  markShipped(order: OrderDetail, carrier: string, trackingCode: string) {
    carrier = carrier.trim();
    trackingCode = trackingCode.trim();
    if (this.saving() || !carrier || !trackingCode) return;
    this.saving.set(true);
    this.actionError.set('');
    this.notice.set('');
    this.api
      .markShipped(order.id, order.revision, carrier, trackingCode)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.notice.set('De bestelling is als verzonden gemarkeerd.');
          this.retry();
        },
        error: (error) => {
          this.saving.set(false);
          this.actionError.set(errorMessage(error));
        },
      });
  }
  cancelOrder(order: OrderDetail, reason: string) {
    reason = reason.trim();
    if (this.saving() || !reason) return;
    this.saving.set(true);
    this.actionError.set('');
    this.notice.set('');
    this.api
      .cancel(order.id, order.revision, reason)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.notice.set('De bestelling is geannuleerd.');
          this.retry();
        },
        error: (error) => {
          this.saving.set(false);
          this.actionError.set(errorMessage(error));
        },
      });
  }
}
