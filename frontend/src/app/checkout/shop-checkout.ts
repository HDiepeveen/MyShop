import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CheckoutOrderLine, OrderApi, OrderReceipt } from './order.api';
import { errorMessage } from '../catalog/error-message';

@Component({
  selector: 'app-shop-checkout',
  imports: [FormsModule],
  template: `
    <section class="panel">
      <h2>Gegevens voor je bestelling</h2>
      <form (ngSubmit)="submit()">
        <label
          >Naam<input
            name="name"
            [(ngModel)]="customerName"
            autocomplete="name"
            maxlength="200"
            required
        /></label>
        <label
          >E-mailadres<input
            name="email"
            [(ngModel)]="email"
            type="email"
            autocomplete="email"
            maxlength="320"
            required
        /></label>
        <label
          >Adres<input
            name="address"
            [(ngModel)]="addressLine"
            autocomplete="street-address"
            maxlength="200"
            required
        /></label>
        <label
          >Postcode<input
            name="postalCode"
            [(ngModel)]="postalCode"
            autocomplete="postal-code"
            maxlength="32"
            required
        /></label>
        <label
          >Plaats<input
            name="city"
            [(ngModel)]="city"
            autocomplete="address-level2"
            maxlength="100"
            required
        /></label>
        <label
          >Landcode<input
            name="countryCode"
            [(ngModel)]="countryCode"
            autocomplete="country"
            minlength="2"
            maxlength="2"
            required
        /></label>
        @if (failure()) {
          <p class="error" role="alert">{{ failure() }}</p>
        }
        <button [disabled]="busy() || paymentMethod() !== 'payLater'">
          {{ busy() ? 'Bestelling plaatsen…' : 'Bestelling plaatsen' }}
        </button>
      </form>
    </section>
  `,
})
export class ShopCheckout {
  private readonly api = inject(OrderApi);
  private readonly destroyRef = inject(DestroyRef);
  readonly lines = input.required<readonly CheckoutOrderLine[]>();
  readonly paymentMethod = input.required<string>();
  readonly placed = output<OrderReceipt>();
  readonly busy = signal(false);
  readonly failure = signal('');
  customerName = '';
  email = '';
  addressLine = '';
  postalCode = '';
  city = '';
  countryCode = 'NL';
  private checkoutToken = crypto.randomUUID();

  submit() {
    if (this.busy()) return;
    this.failure.set('');
    this.busy.set(true);
    this.api
      .place({
        checkoutToken: this.checkoutToken,
        paymentMethod: this.paymentMethod(),
        customerName: this.customerName,
        email: this.email,
        addressLine: this.addressLine,
        postalCode: this.postalCode,
        city: this.city,
        countryCode: this.countryCode,
        lines: this.lines(),
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (receipt) => {
          this.busy.set(false);
          this.placed.emit(receipt);
        },
        error: (error) => {
          this.busy.set(false);
          this.failure.set(
            error instanceof HttpErrorResponse && typeof error.error?.message === 'string'
              ? error.error.message
              : errorMessage(error),
          );
        },
      });
  }
}
