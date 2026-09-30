import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { BehaviorSubject, distinctUntilChanged, map, switchMap } from 'rxjs';
import { loadState } from '../catalog/load-state';
import { OrderManagementApi } from './order-management.api';

@Component({
  imports: [RouterLink, DatePipe, CurrencyPipe],
  template: ` <div class="eyebrow">Verkoop</div>
    <div class="page-head">
      <div>
        <h1>Bestellingen</h1>
        <p class="muted">Geplaatste bestellingen, met de gegevens zoals de klant ze bevestigde.</p>
      </div>
    </div>
    <section class="panel">
      <button type="button" class="secondary" [disabled]="state()?.loading" (click)="retry()">
        Overzicht verversen
      </button>
      @if (state()?.loading) {
        <p class="loading" role="status">Bestellingen ophalen…</p>
      }
      @if (state()?.error) {
        <div class="error" role="alert">
          {{ state()?.error }} <button class="secondary" (click)="retry()">Opnieuw proberen</button>
        </div>
      }
      @if (state()?.data; as page) {
        @if (page.items.length) {
          <div class="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Bestelling</th>
                  <th>Geplaatst</th>
                  <th>Klant</th>
                  <th>Status</th>
                  <th>Totaal</th>
                  <th>Bekijken</th>
                </tr>
              </thead>
              <tbody>
                @for (order of page.items; track order.id) {
                  <tr>
                    <td>
                      <a [routerLink]="['/bestellingen', order.id]">{{ order.number }}</a>
                    </td>
                    <td class="nowrap">{{ order.placedAt | date: 'dd-MM-yyyy HH:mm' }}</td>
                    <td>{{ order.customerName }}</td>
                    <td>
                      <span class="badge">{{ statusLabel(order.status) }}</span>
                    </td>
                    <td>
                      @for (total of order.totals; track total.currency) {
                        <div class="nowrap">{{ total.amount | currency: total.currency }}</div>
                      }
                    </td>
                    <td>
                      <a
                        [routerLink]="['/bestellingen', order.id]"
                        [attr.aria-label]="order.number + ' bekijken'"
                        >Details →</a
                      >
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        } @else {
          <div class="empty">
            <h2>
              {{ offset() > 0 ? 'Geen bestellingen op deze pagina' : 'Nog geen bestellingen' }}
            </h2>
            <p class="muted">
              {{
                offset() > 0
                  ? 'Ga terug naar de eerste pagina.'
                  : 'Nieuwe bestellingen verschijnen hier automatisch.'
              }}
            </p>
          </div>
        }
        <div class="pager">
          <span
            >{{ page.totalCount }} {{ page.totalCount === 1 ? 'bestelling' : 'bestellingen' }} ·
            Pagina {{ offset() / 20 + 1 }}</span
          >
          <div class="actions">
            @if (offset() > 0) {
              <button class="secondary" (click)="changePage(-offset())">Eerste pagina</button>
            }
            <button class="secondary" [disabled]="offset() === 0" (click)="changePage(-20)">
              Vorige</button
            ><button
              class="secondary"
              [disabled]="offset() + 20 >= page.totalCount"
              (click)="changePage(20)"
            >
              Volgende
            </button>
          </div>
        </div>
      }
    </section>`,
})
export class OrderList {
  private readonly api = inject(OrderManagementApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly refresh = new BehaviorSubject(0);
  readonly offset = toSignal(
    this.route.queryParamMap.pipe(
      map((params) => this.readOffset(params.get('offset'))),
      distinctUntilChanged(),
    ),
    { initialValue: 0 },
  );
  readonly state = toSignal(
    this.route.queryParamMap.pipe(
      map((params) => this.readOffset(params.get('offset'))),
      distinctUntilChanged(),
      switchMap((offset) => this.refresh.pipe(switchMap(() => loadState(this.api.list(offset))))),
    ),
  );
  changePage(delta: number) {
    const offset = Math.max(0, this.offset() + delta);
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { offset: offset || null },
    });
  }
  retry() {
    this.refresh.next(this.refresh.value + 1);
  }
  statusLabel(status: string) {
    return status === 'awaitingPayment'
      ? 'Wacht op betaling'
      : status === 'paid'
        ? 'Betaald'
        : status;
  }
  private readOffset(value: string | null) {
    const offset = Number(value);
    return Number.isSafeInteger(offset) && offset >= 0 && offset % 20 === 0 ? offset : 0;
  }
}
