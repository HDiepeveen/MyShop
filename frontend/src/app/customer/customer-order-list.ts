import { DatePipe } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { customerOrderStatuses, readCustomerOrderQuery } from './customer-order-query';
import { errorMessage } from '../catalog/error-message';
import {
  CustomerOrderApi,
  CustomerOrderPage,
  CustomerOrderStatus,
  CustomerOrderPaymentMethod,
  customerOrderStatus,
} from './customer-order.api';

@Component({
  imports: [DatePipe, RouterLink, FormsModule],
  template: `<a routerLink="/winkel/account">← Mijn account</a>
    <div class="eyebrow">Mijn account</div>
    <h1>Mijn bestellingen</h1>
    <form class="toolbar" (ngSubmit)="applySearch()">
      <label
        >Zoek op bestelnummer<input
          name="search"
          type="search"
          maxlength="200"
          [(ngModel)]="searchText"
          [disabled]="loading()"
      /></label>
      <button type="submit" [disabled]="loading()">Zoeken</button>
      <label
        >Status<select
          name="status"
          [ngModel]="statusFilter()"
          (ngModelChange)="filterStatus($event)"
          [disabled]="loading()"
        >
          <option [ngValue]="null">Alle statussen</option>
          @for (value of statuses; track value) {
            <option [ngValue]="value">{{ status(value) }}</option>
          }
        </select></label
      >
      @if (search || statusFilter()) {
        <button type="button" class="secondary" [disabled]="loading()" (click)="clearFilters()">
          Filters wissen
        </button>
      }
    </form>
    @if (loading()) {
      <p role="status">Bestellingen ophalen…</p>
    }
    @if (failure()) {
      <p class="error" role="alert">{{ failure() }}</p>
      <button type="button" (click)="load(offset())">Opnieuw proberen</button>
    }
    @if (!loading() && page(); as result) {
      @if (!result.items.length && result.offset === 0) {
        @if (search || statusFilter()) {
          <p>Geen bestellingen gevonden met deze filters.</p>
        } @else {
          <p>Je hebt met dit account nog geen bestellingen geplaatst.</p>
        }
        <a routerLink="/winkel">Bekijk het assortiment</a>
      }
      @for (order of result.items; track order.id) {
        <article class="panel">
          <h2>
            <a [routerLink]="[order.id]" [queryParams]="contextQuery()">{{ order.number }}</a>
          </h2>
          <p>
            {{ order.placedAt | date: 'dd-MM-yyyy HH:mm' }} · {{ status(order.status) }} ·
            {{ paymentMethod(order.paymentMethod) }}
          </p>
          @if (order.shippedAt) {
            <p>
              Verzonden op {{ order.shippedAt | date: 'dd-MM-yyyy HH:mm' }}.
              @if (order.shippingCarrier || order.trackingCode) {
                <br />{{ order.shippingCarrier ?? 'Vervoerder onbekend' }} ·
                {{ order.trackingCode ?? 'tracking niet beschikbaar' }}
              }
            </p>
          }
          @for (total of order.totals; track total.currency) {
            <p>
              <strong>{{ total.currency }} {{ amount(total.amount) }}</strong>
            </p>
          }
          <p><a [routerLink]="[order.id]" [queryParams]="contextQuery()">Details bekijken</a></p>
        </article>
      }
      @if (!result.items.length && result.offset > 0) {
        <p>Deze pagina bevat geen bestellingen meer.</p>
        <button type="button" class="secondary" (click)="previous()">Vorige pagina</button>
      }
      @if (
        result.items.length &&
        (result.offset > 0 || result.offset + result.items.length < result.totalCount)
      ) {
        <nav class="toolbar" aria-label="Paginering">
          <button
            type="button"
            class="secondary"
            [disabled]="loading() || result.offset === 0"
            (click)="previous()"
          >
            Vorige
          </button>
          <span
            >{{ result.offset + 1 }}–{{ result.offset + result.items.length }} van
            {{ result.totalCount }}</span
          >
          <button
            type="button"
            class="secondary"
            [disabled]="loading() || result.offset + result.items.length >= result.totalCount"
            (click)="next()"
          >
            Volgende
          </button>
        </nav>
      }
    }`,
})
export class CustomerOrderList {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(CustomerOrderApi);
  private readonly destroyRef = inject(DestroyRef);
  readonly page = signal<CustomerOrderPage | null>(null);
  readonly loading = signal(false);
  readonly failure = signal('');
  readonly offset = signal(0);
  readonly status = customerOrderStatus;
  readonly statuses = customerOrderStatuses;
  readonly statusFilter = signal<CustomerOrderStatus | null>(null);
  searchText = '';
  search = '';
  constructor() {
    const query = readCustomerOrderQuery(this.route.snapshot.queryParamMap);
    this.statusFilter.set(query.status);
    this.search = this.searchText = query.search;
    this.load(query.offset);
  }
  applySearch() {
    if (this.loading()) return;
    const search = this.searchText.trim();
    if (search.length > 200) {
      this.failure.set('Gebruik maximaal 200 tekens voor het bestelnummer.');
      return;
    }
    this.search = search;
    this.load(0);
  }
  filterStatus(status: CustomerOrderStatus | null) {
    if (this.loading() || (status !== null && !this.statuses.includes(status))) return;
    this.statusFilter.set(status);
    this.load(0);
  }
  clearFilters() {
    if (this.loading()) return;
    this.search = this.searchText = '';
    this.statusFilter.set(null);
    this.load(0);
  }
  contextQuery() {
    return {
      search: this.search || null,
      status: this.statusFilter(),
      offset: this.offset() || null,
    };
  }
  load(offset: number) {
    if (this.loading() || !Number.isSafeInteger(offset) || offset < 0 || offset % 20 !== 0) return;
    this.offset.set(offset);
    this.page.set(null);
    this.loading.set(true);
    this.failure.set('');
    (this.search || this.statusFilter()
      ? this.api.list(offset, this.statusFilter(), this.search)
      : this.api.list(offset)
    )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (page) => {
          this.page.set(page);
          this.offset.set(page.offset);
          this.loading.set(false);
        },
        error: (error) => {
          this.failure.set(errorMessage(error));
          this.loading.set(false);
        },
      });
  }
  previous() {
    if (this.offset() === 0) return;
    this.load(Math.max(0, this.offset() - 20));
  }
  next() {
    const page = this.page();
    if (!page || !page.items.length || page.offset + page.items.length >= page.totalCount) return;
    this.load(this.offset() + 20);
  }
  paymentMethod(method: CustomerOrderPaymentMethod) {
    return method === 'online' ? 'Online betalen' : 'Later betalen';
  }
  amount(value: string) {
    return value.replace('.', ',');
  }
}
