import { DatePipe } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { errorMessage } from '../catalog/error-message';
import {
  CustomerOrderApi,
  CustomerOrderPage,
  CustomerOrderPaymentMethod,
  customerOrderStatus,
} from './customer-order.api';

@Component({
  imports: [DatePipe, RouterLink],
  template: `<a routerLink="/winkel/account">← Mijn account</a>
    <div class="eyebrow">Mijn account</div>
    <h1>Mijn bestellingen</h1>
    @if (loading()) {
      <p role="status">Bestellingen ophalen…</p>
    }
    @if (failure()) {
      <p class="error" role="alert">{{ failure() }}</p>
      <button type="button" (click)="load(offset())">Opnieuw proberen</button>
    }
    @if (!loading() && page(); as result) {
      @if (!result.items.length && result.offset === 0) {
        <p>Je hebt met dit account nog geen bestellingen geplaatst.</p>
        <a routerLink="/winkel">Bekijk het assortiment</a>
      }
      @for (order of result.items; track order.id) {
        <article class="panel">
          <h2>
            <a [routerLink]="[order.id]">{{ order.number }}</a>
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
          <p><a [routerLink]="[order.id]">Details bekijken</a></p>
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
  private readonly api = inject(CustomerOrderApi);
  private readonly destroyRef = inject(DestroyRef);
  readonly page = signal<CustomerOrderPage | null>(null);
  readonly loading = signal(false);
  readonly failure = signal('');
  readonly offset = signal(0);
  readonly status = customerOrderStatus;
  constructor() {
    this.load(0);
  }
  load(offset: number) {
    if (this.loading() || !Number.isSafeInteger(offset) || offset < 0 || offset % 20 !== 0) return;
    this.offset.set(offset);
    this.page.set(null);
    this.loading.set(true);
    this.failure.set('');
    this.api
      .list(offset)
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
