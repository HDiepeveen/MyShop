import { Component, DestroyRef, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AdminPaymentOptions, PaymentOptionsApi } from './payment-options.api';
import { errorMessage } from '../catalog/error-message';

@Component({
  selector: 'app-checkout-enabled-settings',
  imports: [FormsModule],
  template: `
    <section class="panel">
      @if (options()) {
        <label><input type="checkbox" [(ngModel)]="enabled" [disabled]="busy()" />Winkelmand en bestellen inschakelen</label>
        <p>Uitgeschakeld: klanten kunnen het assortiment en de prijzen bekijken, maar niet bestellen of betalen. Je betaalopties blijven bewaard.</p>
        <button type="button" [disabled]="busy()" (click)="save()">{{ busy() ? 'Opslaan…' : 'Bestelinstelling opslaan' }}</button>
      } @else if (busy()) { <p role="status">Bestelinstelling ophalen…</p> }
      @if (error()) { <p role="alert">{{ error() }}</p><button type="button" [disabled]="busy()" (click)="load()">Opnieuw ophalen</button> }
      @if (notice()) { <p role="status">{{ notice() }}</p> }
    </section>
  `,
})
export class CheckoutEnabledSettings {
  private readonly api = inject(PaymentOptionsApi);
  private readonly destroyRef = inject(DestroyRef);
  readonly options = signal<AdminPaymentOptions | null>(null);
  readonly busy = signal(false);
  readonly error = signal('');
  readonly notice = signal('');
  enabled = false;
  constructor() { this.load(); }
  load() {
    if (this.busy()) return;
    this.busy.set(true); this.error.set('');
    this.api.adminOptions().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: options => { this.options.set(options); this.enabled = options.checkoutEnabled !== false; this.busy.set(false); },
      error: error => { this.error.set(errorMessage(error)); this.busy.set(false); },
    });
  }
  save() {
    const options = this.options();
    if (!options || this.busy()) return;
    this.notice.set(''); this.error.set('');
    if (this.enabled && !options.payLaterEnabled && !options.onlinePaymentEnabled) {
      this.error.set('Stel eerst minimaal één betaaloptie in bij Betaalopties.'); return;
    }
    this.busy.set(true);
    this.api.update({ checkoutEnabled: this.enabled, payLaterEnabled: options.payLaterEnabled,
      onlinePaymentEnabled: options.onlinePaymentEnabled, payLaterInstructions: options.payLaterInstructions,
      revision: options.revision }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: stored => { this.options.set({ ...options, ...stored }); this.busy.set(false); this.notice.set('Bestelinstelling opgeslagen.'); },
        error: error => { this.busy.set(false); this.error.set(errorMessage(error)); },
      });
  }
}
