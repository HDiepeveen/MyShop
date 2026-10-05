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
            [disabled]="busy()"
            required
        /></label>
        <label
          >E-mailadres<input
            name="email"
            [(ngModel)]="email"
            type="email"
            autocomplete="email"
            maxlength="320"
            [disabled]="busy()"
            required
        /></label>
        <label
          >Adres<input
            name="address"
            [(ngModel)]="addressLine"
            autocomplete="street-address"
            maxlength="200"
            [disabled]="busy()"
            required
        /></label>
        <label
          >Postcode<input
            name="postalCode"
            [(ngModel)]="postalCode"
            autocomplete="postal-code"
            maxlength="32"
            [disabled]="busy()"
            required
        /></label>
        <label
          >Plaats<input
            name="city"
            [(ngModel)]="city"
            autocomplete="address-level2"
            maxlength="100"
            [disabled]="busy()"
            required
        /></label>
        <label
          >Landcode<input
            name="countryCode"
            [(ngModel)]="countryCode"
            autocomplete="country"
            minlength="2"
            maxlength="2"
            [disabled]="busy()"
            required
        /></label>
        @if (failure()) {
          <p class="error" role="alert">{{ failure() }}</p>
        }
        @if (notice()) {
          <p class="notice">{{ notice() }}</p>
        }
        @if (paymentMethod() === 'online') {
          <p class="muted">
            Je bestelling wordt gecontroleerd voordat de betaalprovider wordt gestart.
          </p>
        }
        @if (onlinePayment(); as payment) {
          <section class="notice" aria-label="Online betaalstart">
            <p>Betaalprovider: {{ payment.providerName }}</p>
            <p>Betalingskenmerk: {{ payment.paymentReference }}</p>
            <p>Providerbetaling: {{ payment.providerPaymentId }}</p>
            <p><a [href]="payment.checkoutUrl">Testbetaling openen</a></p>
            <button type="button" [disabled]="busy()" (click)="completeOnlinePayment(payment)">
              {{ busy() ? 'Betaling afronden…' : 'Testbetaling afronden' }}
            </button>
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
    this.account
      .profile()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (profile) => {
          if (
            this.busy() ||
            this.customerName ||
            this.email ||
            this.addressLine ||
            this.postalCode ||
            this.city ||
            this.countryCode !== 'NL'
          )
            return;
          this.customerName = profile.name ?? '';
          this.email = profile.email;
          this.addressLine = profile.addressLine ?? '';
          this.postalCode = profile.postalCode ?? '';
          this.city = profile.city ?? '';
          this.countryCode = profile.countryCode ?? 'NL';
        },
        error: () => {
          if (!this.busy() && !this.onlinePayment())
            this.notice.set('Je profiel kon niet worden opgehaald. Vul je gegevens zelf in.');
        },
      });
  }

  validationError() {
    if (!this.customerName.trim() || this.customerName.trim().length > 200)
      return 'Vul een naam van maximaal 200 tekens in.';
    if (this.email.trim().length > 320 || !/^[^\s@]+@[^\s@]+$/.test(this.email.trim()))
      return 'Vul een geldig e-mailadres van maximaal 320 tekens in.';
    if (!this.addressLine.trim() || this.addressLine.trim().length > 200)
      return 'Vul een adres van maximaal 200 tekens in.';
    if (!this.postalCode.trim() || this.postalCode.trim().length > 32)
      return 'Vul een postcode van maximaal 32 tekens in.';
    if (!this.city.trim() || this.city.trim().length > 100)
      return 'Vul een plaats van maximaal 100 tekens in.';
    if (!/^[a-zA-Z]{2}$/.test(this.countryCode.trim()))
      return 'Gebruik een landcode van twee letters.';
    const lines = this.lines();
    if (
      !lines.length ||
      lines.length > 20 ||
      lines.some(
        (line) =>
          !line.productId ||
          !line.variantId ||
          !Number.isInteger(line.quantity) ||
          line.quantity < 1 ||
          line.quantity > 99,
      )
    )
      return 'Controleer de producten en aantallen in je winkelmand.';
    if (this.paymentMethod() !== 'payLater' && this.paymentMethod() !== 'online')
      return 'Kies een beschikbare betaalwijze.';
    if (!this.deliveryMethodId().trim()) return 'Kies een beschikbare bezorgoptie.';
    return '';
  }
  submit() {
    if (this.busy()) return;
    const validation = this.validationError();
    if (validation) {
      this.failure.set(validation);
      return;
    }
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
          customerName: request.customerName,
          email: request.email,
          addressLine: request.addressLine,
          postalCode: request.postalCode,
          city: request.city,
          countryCode: request.countryCode,
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

  completeOnlinePayment(payment: OnlinePaymentStart) {
    if (this.busy() || this.onlinePayment() !== payment) return;
    this.failure.set('');
    this.notice.set('');
    this.busy.set(true);
    this.api
      .completeOnlinePayment(payment.checkoutToken, payment.providerPaymentId)
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
    return this.paymentMethod() === 'online'
      ? 'Online betaling voorbereiden'
      : 'Bestelling plaatsen';
  }
}
