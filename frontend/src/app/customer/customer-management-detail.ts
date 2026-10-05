import { DatePipe } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { EMPTY, Subject, catchError, combineLatest, startWith, switchMap } from 'rxjs';
import { errorMessage } from '../catalog/error-message';
import { CustomerManagementApi, ManagedCustomerDetail } from './customer-management.api';

@Component({
  imports: [DatePipe, RouterLink],
  template: `<a routerLink="/klanten">← Klanten</a>
    @if (loading()) {
      <p role="status">Klant ophalen…</p>
    }
    @if (failure()) {
      <p class="error" role="alert">{{ failure() }}</p>
      <button type="button" [disabled]="saving()" (click)="reload()">Opnieuw proberen</button>
    }
    @if (customer(); as item) {
      <div class="eyebrow">{{ item.isLocked ? 'Geblokkeerd' : 'Actief' }}</div>
      <h1>{{ item.name || item.email }}</h1>
      <section class="panel">
        <h2>Account</h2>
        <p>{{ item.email }}</p>
        <p>{{ item.orderCount }} bestelling(en)</p>
        @if (item.lastOrderAt) {
          <p>Laatste bestelling: {{ item.lastOrderAt | date: 'dd-MM-yyyy HH:mm' }}</p>
        }
      </section>
      @if (item.addressLine) {
        <section class="panel">
          <h2>Opgeslagen adres</h2>
          <p>
            {{ item.addressLine }}<br />{{ item.postalCode }} {{ item.city }}<br />{{
              item.countryCode
            }}
          </p>
        </section>
      }
      @if (notice()) {
        <p role="status">{{ notice() }}</p>
      }
      @if (actionFailure()) {
        <p class="error" role="alert">{{ actionFailure() }}</p>
      }
      <button
        [class.secondary]="!item.isLocked"
        [disabled]="saving()"
        (click)="setLocked(item, !item.isLocked)"
      >
        {{ saving() ? 'Opslaan…' : item.isLocked ? 'Account deblokkeren' : 'Account blokkeren' }}
      </button>
    }`,
})
export class CustomerManagementDetail {
  private readonly api = inject(CustomerManagementApi);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly retry = new Subject<void>();
  readonly customer = signal<ManagedCustomerDetail | null>(null);
  readonly loading = signal(true);
  readonly failure = signal('');
  readonly saving = signal(false);
  readonly actionFailure = signal('');
  readonly notice = signal('');
  constructor() {
    combineLatest([this.route.paramMap, this.retry.pipe(startWith(undefined))])
      .pipe(
        switchMap(([params]) => {
          this.customer.set(null);
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
        next: (customer) => {
          this.customer.set(customer);
          this.loading.set(false);
        },
      });
  }
  reload() {
    if (this.loading() || this.saving()) return;
    this.retry.next();
  }
  setLocked(customer: ManagedCustomerDetail, locked: boolean) {
    if (
      this.customer() !== customer ||
      customer.isLocked === locked ||
      this.saving() ||
      !window.confirm(
        locked ? 'Wil je dit klantaccount blokkeren?' : 'Wil je dit klantaccount deblokkeren?',
      )
    )
      return;
    this.saving.set(true);
    this.actionFailure.set('');
    this.notice.set('');
    this.api
      .setLocked(customer.id, customer.revision, locked)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.saving.set(false);
          if (this.customer() !== customer) return;
          this.customer.set({
            ...customer,
            isLocked: result.locked,
            lockedUntil: result.locked ? '9999-12-31T23:59:59Z' : null,
            revision: result.revision,
          });
          this.notice.set(
            result.locked
              ? 'Het klantaccount is geblokkeerd.'
              : 'Het klantaccount is gedeblokkeerd.',
          );
        },
        error: (error) => {
          this.saving.set(false);
          if (this.customer() !== customer) return;
          this.actionFailure.set(errorMessage(error));
        },
      });
  }
}
