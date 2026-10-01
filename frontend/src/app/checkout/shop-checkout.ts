import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CheckoutOrderLine, OnlinePaymentStart, OrderApi, OrderReceipt } from './order.api';
import { errorMessage } from '../catalog/error-message';
import { Auth } from '../auth/auth';
import { CustomerAccountApi } from '../customer/customer-account.api';

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
        @if (notice()) {
          <p class="notice">{{ notice() }}</p>
        }
        @if (paymentMethod() === 'online') {
          <p class="muted">Je bestelling wordt gecontroleerd voordat de betaalprovider wordt gestart.</p>
        }
        @if (onlinePayment(); as payment) {
          <section class="notice" aria-label="Online betaalstart">
            <p>Betaalprovider: {{ payment.providerName }}</p>
            <p>Bezorging: {{ payment.deliveryMethod.name }}</p>
            <ul>
              @for (total of payment.totals; track total.currency) {
                <li>Totaal {{ total.currency }} {{ amount(total.amount) }}</li>
              }
            </ul>
          </section>
        }
        <button [disabled]="busy()">
          {{ buttonText() }}
        </button>
      </form>
    </section>
  `,
})
export class ShopCheckout implements OnInit {
  private readonly api = inject(OrderApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly auth = inject(Auth);
  private readonly account = inject(CustomerAccountApi);
  readonly lines = input.required<readonly CheckoutOrderLine[]>();
  readonly paymentMethod = input.required<string>();
  readonly deliveryMethodId = input.required<string>();
  readonly placed = output<OrderReceipt>();
  readonly busy = signal(false);
  readonly failure = signal('');
  readonly notice = signal('');
  readonly onlinePayment = signal<OnlinePaymentStart | null>(null);
  customerName = '';
  email = '';
  addressLine = '';
  postalCode = '';
  city = '';
  countryCode = 'NL';
  private checkoutToken = crypto.randomUUID();

  ngOnInit() {
    if (!this.auth.session()?.customer) return;
    this.account.profile().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (profile) => {
        if (this.customerName || this.addressLine || this.postalCode || this.city) return;
        this.customerName = profile.name ?? '';
        this.email = profile.email;
        this.addressLine = profile.addressLine ?? '';
        this.postalCode = profile.postalCode ?? '';
        this.city = profile.city ?? '';
        this.countryCode = profile.countryCode ?? 'NL';
      },
    });
  }

  submit() {
    if (this.busy()) return;
    this.failure.set('');
    this.notice.set('');
    this.onlinePayment.set(null);
    this.busy.set(true);
    const request = {
      checkoutToken: this.checkoutToken,
      paymentMethod: this.paymentMethod(),
      deliveryMethodId: this.deliveryMethodId(),
      customerName: this.customerName,
      email: this.email,
      addressLine: this.addressLine,
      postalCode: this.postalCode,
      city: this.city,
      countryCode: this.countryCode,
      lines: this.lines(),
    };
    if (this.paymentMethod() === 'online') {
      this.api
        .startOnlinePayment({
          checkoutToken: request.checkoutToken,
          deliveryMethodId: request.deliveryMethodId,
          lines: request.lines,
        })
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (payment) => {
            this.busy.set(false);
            this.notice.set(payment.message);
            this.onlinePayment.set(payment);
          },
          error: (error) => this.showFailure(error),
        });
      return;
    }
    this.api
      .place(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (receipt) => {
          this.busy.set(false);
          this.placed.emit(receipt);
        },
        error: (error) => this.showFailure(error),
      });
  }

  private showFailure(error: unknown) {
    this.busy.set(false);
    this.failure.set(
      error instanceof HttpErrorResponse && typeof error.error?.message === 'string'
        ? error.error.message
        : errorMessage(error),
    );
  }

  amount(value: string) {
    return value.replace('.', ',');
  }

  buttonText() {
    if (this.busy())
      return this.paymentMethod() === 'online' ? 'Betaling voorbereiden…' : 'Bestelling plaatsen…';
    return this.paymentMethod() === 'online' ? 'Online betaling voorbereiden' : 'Bestelling plaatsen';
  }
}
