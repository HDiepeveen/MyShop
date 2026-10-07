import { Component, DestroyRef, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { BehaviorSubject, switchMap } from 'rxjs';
import { PaymentOptionsApi, AdminPaymentOptions } from './payment-options.api';
import { loadState } from '../catalog/load-state';
import { errorMessage } from '../catalog/error-message';

@Component({
  imports: [FormsModule],
  template: `
    <div class="eyebrow">Checkout</div>
    <h1>Betaalopties</h1>
    <p class="muted">Bepaal uit welke opties klanten bij het afrekenen kunnen kiezen.</p>
    @if (state()?.loading) {
      <p role="status">Betaalopties ophalen…</p>
    }
    @if (state()?.error) {
      <p role="alert" class="error">{{ state()?.error }}</p>
      <button class="secondary" type="button" (click)="reload()">Opnieuw proberen</button>
    }
    @if (state()?.data; as options) {
      <form class="panel" (ngSubmit)="save(options)">
        <label
          ><input type="checkbox" name="payLater" [(ngModel)]="payLater" [disabled]="busy()" />
          Later betalen</label
        >
        <label>
          Instructies voor later betalen
          <textarea
            name="payLaterInstructions"
            [(ngModel)]="payLaterInstructions"
            rows="5"
            maxlength="2000"
            [disabled]="busy() || !payLater"
            placeholder="Bijvoorbeeld: maak het bedrag binnen 14 dagen over onder vermelding van het bestelnummer."
          ></textarea>
        </label>
        <p class="muted">Deze tekst wordt bij de bestelling vastgelegd en aan de klant getoond.</p>
        <label
          ><input
            type="checkbox"
            name="online"
            [(ngModel)]="online"
            [disabled]="busy() || !options.onlinePaymentConfigured"
          />
          Direct online betalen</label
        >
        @if (options.onlinePaymentConfigured) {
          @if (options.onlinePaymentProvider === 'Mollie') {
            <p class="muted">Mollie is ingesteld in testmodus. Er wordt geen echt geld afgeschreven.</p>
          } @else {
            <p class="muted">Online betalen is voorbereid via {{ options.onlinePaymentProvider }}.</p>
          }
        } @else {
          <p class="muted">
            Online betalen wordt beschikbaar nadat een betaalprovider is gekoppeld.
          </p>
        }
        @if (validation()) {
          <p role="alert" class="error">{{ validation() }}</p>
        }
        @if (message()) {
          <p role="status">{{ message() }}</p>
        }
        <button [disabled]="busy()">{{ busy() ? 'Opslaan…' : 'Betaalopties opslaan' }}</button>
      </form>
    }
  `,
})
export class PaymentSettings {
  private readonly api = inject(PaymentOptionsApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly refresh = new BehaviorSubject(0);
  readonly busy = signal(false);
  readonly validation = signal('');
  readonly message = signal('');
  payLater = false;
  online = false;
  payLaterInstructions = '';
  readonly state = toSignal(this.refresh.pipe(switchMap(() => loadState(this.api.adminOptions()))));
  constructor() {
    effect(() => {
      const options = this.state()?.data;
      if (options) {
        this.payLater = options.payLaterEnabled;
        this.online = options.onlinePaymentEnabled;
        this.payLaterInstructions = options.payLaterInstructions ?? '';
      }
    });
  }
  reload() {
    if (this.busy() || this.state()?.loading) return;
    this.validation.set('');
    this.message.set('');
    this.refresh.next(this.refresh.value + 1);
  }
  save(options: AdminPaymentOptions) {
    if (this.busy() || this.state()?.data !== options) return;
    this.validation.set('');
    this.message.set('');
    if (!this.payLater && !this.online) {
      this.validation.set('Schakel minimaal één betaaloptie in.');
      return;
    }
    if (this.online && !options.onlinePaymentConfigured) {
      this.validation.set('Koppel eerst een online betaalprovider.');
      return;
    }
    this.busy.set(true);
    this.api
      .update({
        payLaterEnabled: this.payLater,
        onlinePaymentEnabled: this.online,
        payLaterInstructions: this.payLaterInstructions.trim() || null,
        revision: options.revision,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.busy.set(false);
          this.message.set('Betaalopties opgeslagen.');
          this.refresh.next(this.refresh.value + 1);
        },
        error: (error) => {
          this.busy.set(false);
          this.validation.set(errorMessage(error));
        },
      });
  }
}
