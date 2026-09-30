import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { BehaviorSubject, distinctUntilChanged, map, switchMap } from 'rxjs';
import { loadState } from '../catalog/load-state';
import { OrderManagementApi, OrderStatus } from './order-management.api';

@Component({
  imports: [RouterLink, DatePipe, CurrencyPipe, FormsModule],
  template: ` <div class="eyebrow">Verkoop</div>
    <div class="page-head">
      <div>
        <h1>Bestellingen</h1>
        <p class="muted">Geplaatste bestellingen, met de gegevens zoals de klant ze bevestigde.</p>
      </div>
    </div>
    <section class="panel">
      <form class="toolbar" (ngSubmit)="applySearch()">
        <label
          >Zoek bestelling<input
            name="search"
            [(ngModel)]="searchText"
            maxlength="200"
            placeholder="Bestelnummer, klantnaam of e-mailadres"
            type="search" /></label
        ><button type="submit" class="secondary">Zoeken</button>
        @if (listQuery().search) {
          <button type="button" class="secondary" (click)="clearSearch()">Zoekterm wissen</button>
        }
      </form>
      <div class="actions">
        <label
          >Status
          <select [value]="status() ?? ''" (change)="filterStatus($any($event.target).value)">
            <option value="">Alle statussen</option>
            <option value="awaitingPayment">Wacht op betaling</option>
            <option value="paid">Betaald</option>
            <option value="shipped">Verzonden</option>
            <option value="cancelled">Geannuleerd</option>
          </select>
        </label>
        <button type="button" class="secondary" [disabled]="state()?.loading" (click)="retry()">
          Overzicht verversen
        </button>
      </div>
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
                      <a
                        [routerLink]="['/bestellingen', order.id]"
                        queryParamsHandling="preserve"
                        >{{ order.number }}</a
                      >
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
                        queryParamsHandling="preserve"
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
              {{
                offset() > 0
                  ? 'Geen bestellingen op deze pagina'
                  : status() || listQuery().search
                    ? 'Geen bestellingen gevonden'
                    : 'Nog geen bestellingen'
              }}
            </h2>
            <p class="muted">
              {{
                offset() > 0
                  ? 'Ga terug naar de eerste pagina.'
                  : status() || listQuery().search
                    ? 'Pas de zoekterm of status aan om meer bestellingen te bekijken.'
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
  readonly listQuery = signal({ offset: 0, status: null as OrderStatus | null, search: '' });
  readonly offset = computed(() => this.listQuery().offset);
  readonly status = computed(() => this.listQuery().status);
  searchText = '';
  readonly state = toSignal(
    this.route.queryParamMap.pipe(
      map((params) => ({
        offset: this.readOffset(params.get('offset')),
        status: this.readStatus(params.get('status')),
        search: (params.get('search') ?? '').trim(),
      })),
      distinctUntilChanged(
        (previous, current) =>
          previous.offset === current.offset &&
          previous.status === current.status &&
          previous.search === current.search,
      ),
      switchMap((query) => {
        if (query.search !== this.listQuery().search) this.searchText = query.search;
        this.listQuery.set(query);
        return this.refresh.pipe(
          switchMap(() => loadState(this.api.list(query.offset, query.status, query.search))),
        );
      }),
    ),
  );
  applySearch() {
    const search = this.searchText.trim();
    if (search === this.listQuery().search && this.offset() === 0) {
      this.retry();
      return;
    }
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { search: search || null, offset: null },
      queryParamsHandling: 'merge',
    });
  }
  clearSearch() {
    this.searchText = '';
    this.applySearch();
  }
  changePage(delta: number) {
    const offset = Math.max(0, this.offset() + delta);
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { offset: offset || null },
      queryParamsHandling: 'merge',
    });
  }
  filterStatus(value: string) {
    const status = this.readStatus(value);
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { status, offset: null },
      queryParamsHandling: 'merge',
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
        : status === 'shipped'
          ? 'Verzonden'
          : status === 'cancelled'
            ? 'Geannuleerd'
            : status;
  }
  private readOffset(value: string | null) {
    const offset = Number(value);
    return Number.isSafeInteger(offset) && offset >= 0 && offset % 20 === 0 ? offset : 0;
  }
  private readStatus(value: string | null): OrderStatus | null {
    return value === 'awaitingPayment' ||
      value === 'paid' ||
      value === 'shipped' ||
      value === 'cancelled'
      ? value
      : null;
  }
}
