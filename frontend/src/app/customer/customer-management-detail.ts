import { DatePipe } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { switchMap } from 'rxjs';
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
  readonly customer = signal<ManagedCustomerDetail | null>(null);
  readonly loading = signal(true);
  readonly failure = signal('');
  readonly saving = signal(false);
  readonly actionFailure = signal('');
  readonly notice = signal('');
  constructor() {
    this.route.paramMap
      .pipe(
        switchMap((params) => this.api.get(params.get('id') ?? '')),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (customer) => {
          this.customer.set(customer);
          this.loading.set(false);
        },
        error: (error) => {
          this.failure.set(errorMessage(error));
          this.loading.set(false);
        },
      });
  }
  setLocked(customer: ManagedCustomerDetail, locked: boolean) {
    if (
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
          this.customer.set({
            ...customer,
            isLocked: result.locked,
            lockedUntil: result.locked ? '9999-12-31T23:59:59Z' : null,
            revision: result.revision,
          });
          this.saving.set(false);
          this.notice.set(
            result.locked
              ? 'Het klantaccount is geblokkeerd.'
              : 'Het klantaccount is gedeblokkeerd.',
          );
        },
        error: (error) => {
          this.saving.set(false);
          this.actionFailure.set(errorMessage(error));
        },
      });
  }
}
