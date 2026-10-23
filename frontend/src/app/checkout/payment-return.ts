import { CurrencySymbolPipe } from '../shared/money';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, takeUntil } from 'rxjs';
import { OrderApi, OrderReceipt } from './order.api';
import { Cart } from '../shop/cart';
import { PendingPayment } from './pending-payment';

@Component({
  selector: 'app-payment-return',
  imports: [CurrencySymbolPipe, RouterLink],
  template: `<section class="panel">
    <h1>Betaling</h1>
    @if (busy()) {
      <p role="status">Je betaalstatus wordt gecontroleerd…</p>
    }
    @if (receipt(); as order) {
      <p role="status">Je betaling is bevestigd. Bestelling {{ order.number }} is geplaatst.</p>
      @for (total of order.totals; track total.currency) {
        <p>Totaal {{ total.currency | currencySymbol }} {{ total.amount.replace('.', ',') }}</p>
      }
      <p>Je ontvangt een bestelbevestiging per e-mail.</p>
      @if (cartNotice()) {
        <p>{{ cartNotice() }}</p>
      }
      <a routerLink="/assortiment">Verder winkelen</a>
    } @else {
      @if (message()) {
        <p role="status">{{ message() }}</p>
      }
      @if (resumeUrl()) { <p><a [href]="resumeUrl()">Eerdere betaling openen en afronden</a></p> }
      @if (validToken && !terminal()) {
        <button type="button" [disabled]="busy()" (click)="check()">
          Betaalstatus opnieuw controleren
        </button>
      }
      @if (terminal()) {
        <p>Je winkelmand is bewaard.</p>
        <a routerLink="/winkel/winkelmand">Opnieuw afrekenen</a>
      }
      @if (!validToken) {
        <a routerLink="/winkel/winkelmand">Naar je winkelmand</a>
      }
    }
  </section>`,
})
export class PaymentReturn implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(OrderApi);
  private readonly cart = inject(Cart);
  private readonly pending = inject(PendingPayment);
  private readonly destroyRef = inject(DestroyRef);
  private readonly cancel = new Subject<void>();
  private token = '';
  validToken = false;
  readonly resumeUrl = signal<string | null>(null);
  readonly busy = signal(false);
  readonly terminal = signal(false);
  readonly message = signal('');
  readonly cartNotice = signal('');
  readonly receipt = signal<OrderReceipt | null>(null);
  ngOnInit() {
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      this.cancel.next();
      this.resumeUrl.set(null);
      this.busy.set(false);
      this.receipt.set(null);
      this.terminal.set(false);
      this.cartNotice.set('');
      this.token = params.get('checkoutToken') ?? '';
      this.validToken =
        /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(this.token) &&
        this.token !== '00000000-0000-0000-0000-000000000000';
      this.message.set(this.validToken ? '' : 'Deze betaallink is ongeldig.');
      if (this.validToken) this.check();
    });
  }
  check() {
    if (!this.validToken || this.busy() || this.receipt() || this.terminal()) return;
    this.busy.set(true);
    this.message.set('');
    this.resumeUrl.set(null);
    this.api
      .completeOnlinePayment(this.token)
      .pipe(takeUntil(this.cancel), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (receipt) => {
          this.busy.set(false);
          this.receipt.set(receipt);
          if (this.pending.matches(this.token, this.cart.lines())) this.cart.clear();
          else if (this.cart.lines().length)
            this.cartNotice.set(
              'Je huidige winkelmand is behouden omdat deze na de betaalstart is gewijzigd.',
            );
          this.pending.clear(this.token);
        },
        error: (error: unknown) => {
          this.busy.set(false);
          const code = error instanceof HttpErrorResponse ? error.error?.code : undefined;
          if (code === 'paymentPending' && error instanceof HttpErrorResponse) {
            try {
              const url = new URL(error.error?.checkoutUrl);
              if (url.protocol === 'https:' && !url.username && !url.password) this.resumeUrl.set(url.href);
            } catch {}
          }
          this.terminal.set(['paymentFailed', 'paymentCanceled', 'paymentExpired'].includes(code));
          if (this.terminal()) this.pending.clear(this.token);
          const messages: Record<string, string> = {
            paymentPending:
              'De betaling is nog niet bevestigd. Controleer straks opnieuw; betaal niet nogmaals.',
            paymentFailed: 'De betaling is mislukt.',
            paymentCanceled: 'De betaling is geannuleerd.',
            paymentExpired: 'De betaling is verlopen.',
            paymentNotFound: 'Deze betaling is niet gevonden. Neem contact op met de webshop.',
            paymentMismatch:
              'De betaalgegevens komen niet overeen. Neem contact op met de webshop.',
            cartUnavailable:
              'Je betaling kon nog niet worden verwerkt in een bestelling. Neem contact op met de webshop en betaal niet nogmaals.',
          };
          this.message.set(
            (this.resumeUrl() ? 'Deze betaling staat nog open. Open de eerdere betaalpagina om deze af te ronden.' : messages[code]) ??
              'De betaalstatus kon niet worden opgehaald. Controleer opnieuw; betaal niet nogmaals.',
          );
        },
      });
  }
}
