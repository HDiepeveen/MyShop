import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, computed, effect, untracked, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CheckoutOrderLine, OnlinePaymentStart, OrderApi, OrderReceipt } from './order.api';
import { errorMessage } from '../catalog/error-message';
import { PendingPayment } from './pending-payment';
import { Auth } from '../auth/auth';
import { CustomerAccountApi, CustomerProfile } from '../customer/customer-account.api';

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
            (ngModelChange)="markEdited('customerName')"
            autocomplete="name"
            maxlength="200"
            [disabled]="busy()"
            required
        /></label>
        <label
          >E-mailadres<input
            name="email"
            [(ngModel)]="email"
            (ngModelChange)="markEdited('email')"
            type="email"
            autocomplete="email"
            maxlength="320"
            [disabled]="busy()"
            required
        /></label>
        @if (savedAddress(); as address) {
          <label class="check-field"><input type="checkbox" name="differentAddress"
            [ngModel]="differentAddress()" (ngModelChange)="differentAddress.set($event)"
            [disabled]="busy() || !!onlinePayment()" />Afwijkend afleveradres</label>
          @if (!differentAddress()) {
            <section aria-label="Opgeslagen afleveradres">
              <h3>Afleveradres uit je account</h3>
              <p>{{ address.addressLine }}<br />{{ address.postalCode }} {{ address.city }}<br />{{ address.countryCode }}</p>
            </section>
          }
        }
        @if (!savedAddress() || differentAddress()) {
        <label
          >Adres<input
            name="address"
            [(ngModel)]="addressLine"
            (ngModelChange)="markEdited('addressLine')"
            autocomplete="street-address"
            maxlength="200"
            [disabled]="busy()"
            required
        /></label>
        <label
          >Postcode<input
            name="postalCode"
            [(ngModel)]="postalCode"
            (ngModelChange)="markEdited('postalCode')"
            autocomplete="postal-code"
            maxlength="32"
            [disabled]="busy()"
            required
        /></label>
        <label
          >Plaats<input
            name="city"
            [(ngModel)]="city"
            (ngModelChange)="markEdited('city')"
            autocomplete="address-level2"
            maxlength="100"
            [disabled]="busy()"
            required
        /></label>
        <label
          >Landcode<input
            name="countryCode"
            [(ngModel)]="countryCode"
            (ngModelChange)="markEdited('countryCode')"
            autocomplete="country"
            minlength="2"
            maxlength="2"
            [disabled]="busy()"
            required
        /></label>
        }
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
            @if (payment.providerName === 'TestPay') {
              <p>Providerbetaling: {{ payment.providerPaymentId }}</p>
            }
            @if (payment.providerName === 'Mollie') {
              <p>Testmodus: er wordt geen echt geld afgeschreven.</p>
            }
            <p><a [href]="payment.checkoutUrl">Betaalpagina openen</a></p>
            <button type="button" [disabled]="busy()" (click)="completeOnlinePayment(payment)">
              {{ busy() ? 'Betaalstatus controleren…' : 'Betaalstatus controleren' }}
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
export class ShopCheckout {
  private readonly api = inject(OrderApi);
  private readonly pending = inject(PendingPayment);
  private readonly destroyRef = inject(DestroyRef);
  private readonly auth = inject(Auth);
  private readonly account = inject(CustomerAccountApi);
  readonly lines = input.required<readonly CheckoutOrderLine[]>();
  readonly paymentMethod = input.required<string>();
  readonly deliveryMethodId = input.required<string>();
  readonly placed = output<OrderReceipt>();
  readonly busyChanged = output<boolean>();
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

  private readonly savedProfile = signal<CustomerProfile | null>(null);
  readonly differentAddress = signal(false);
  private readonly editedFields = new Set<string>();
  readonly savedAddress = computed(() => {
    const profile = this.savedProfile();
    const addressLine = profile?.addressLine?.trim() ?? '';
    const postalCode = profile?.postalCode?.trim() ?? '';
    const city = profile?.city?.trim() ?? '';
    const countryCode = profile?.countryCode?.trim() ?? '';
    return addressLine && addressLine.length <= 200 && postalCode && postalCode.length <= 32 &&
      city && city.length <= 100 && /^[a-zA-Z]{2}$/.test(countryCode)
      ? { addressLine, postalCode, city, countryCode } : null;
  });

  constructor() {
    effect((onCleanup) => {
      const session = this.auth.session();
      if (!session?.customer) return;
      const subscription = untracked(() => this.account.profile()
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (profile) => {
            if (this.auth.session() !== session || this.busy() || this.onlinePayment()) return;
            if ((this.addressLine && this.addressLine !== profile.addressLine) ||
                (this.postalCode && this.postalCode !== profile.postalCode) ||
                (this.city && this.city !== profile.city) ||
                (this.countryCode !== 'NL' && this.countryCode !== profile.countryCode))
              this.differentAddress.set(true);
            this.savedProfile.set(profile);
            if (!this.customerName && !this.editedFields.has('customerName')) this.customerName = profile.name ?? '';
            if (!this.email && !this.editedFields.has('email')) this.email = profile.email;
            if (!this.addressLine && !this.editedFields.has('addressLine')) this.addressLine = profile.addressLine ?? '';
            if (!this.postalCode && !this.editedFields.has('postalCode')) this.postalCode = profile.postalCode ?? '';
            if (!this.city && !this.editedFields.has('city')) this.city = profile.city ?? '';
            if (this.countryCode === 'NL' && !this.editedFields.has('countryCode')) this.countryCode = profile.countryCode ?? 'NL';
          },
          error: () => {
            if (this.auth.session() === session && !this.busy() && !this.onlinePayment())
              this.notice.set('Je profiel kon niet worden opgehaald. Vul je gegevens zelf in.');
          },
        }));
      onCleanup(() => subscription.unsubscribe());
    });
  }

  markEdited(field: string) {
    this.editedFields.add(field);
    if (['addressLine', 'postalCode', 'city', 'countryCode'].includes(field)) this.differentAddress.set(true);
  }
  private shippingAddress() {
    const saved = this.savedAddress();
    return saved && !this.differentAddress() ? saved : {
      addressLine: this.addressLine, postalCode: this.postalCode,
      city: this.city, countryCode: this.countryCode,
    };
  }

  validationError() {
    const address = this.shippingAddress();
    if (!this.customerName.trim() || this.customerName.trim().length > 200)
      return 'Vul een naam van maximaal 200 tekens in.';
    if (this.email.trim().length > 320 || !/^[^\s@]+@[^\s@]+$/.test(this.email.trim()))
      return 'Vul een geldig e-mailadres van maximaal 320 tekens in.';
    if (!address.addressLine.trim() || address.addressLine.trim().length > 200)
      return 'Vul een adres van maximaal 200 tekens in.';
    if (!address.postalCode.trim() || address.postalCode.trim().length > 32)
      return 'Vul een postcode van maximaal 32 tekens in.';
    if (!address.city.trim() || address.city.trim().length > 100)
      return 'Vul een plaats van maximaal 100 tekens in.';
    if (!/^[a-zA-Z]{2}$/.test(address.countryCode.trim()))
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
    if (this.onlinePayment()) {
      this.failure.set('Controleer eerst de bestaande betaalstatus voordat je opnieuw afrekent.');
      return;
    }
    this.busy.set(true);
    this.busyChanged.emit(true);
    const address = this.shippingAddress();
    const request = {
      checkoutToken: this.checkoutToken,
      paymentMethod: this.paymentMethod(),
      deliveryMethodId: this.deliveryMethodId(),
      customerName: this.customerName,
      email: this.email,
      addressLine: address.addressLine,
      postalCode: address.postalCode,
      city: address.city,
      countryCode: address.countryCode,
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
            this.busyChanged.emit(false);
            this.notice.set(payment.message);
            this.onlinePayment.set(payment);
            this.pending.save(payment.checkoutToken, request.lines);
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
          this.busyChanged.emit(false);
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
    this.busyChanged.emit(true);
    this.api
      .completeOnlinePayment(payment.checkoutToken, payment.providerPaymentId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (receipt) => {
          this.busy.set(false);
          this.busyChanged.emit(false);
          this.placed.emit(receipt);
        },
        error: (error) => this.showFailure(error),
      });
  }

  private showFailure(error: unknown) {
    if (
      error instanceof HttpErrorResponse &&
      ['paymentFailed', 'paymentCanceled', 'paymentExpired'].includes(error.error?.code)
    ) {
      const payment = this.onlinePayment();
      if (payment) this.pending.clear(payment.checkoutToken);
      this.onlinePayment.set(null);
      this.checkoutToken = crypto.randomUUID();
    }
    this.busy.set(false);
    this.busyChanged.emit(false);
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
