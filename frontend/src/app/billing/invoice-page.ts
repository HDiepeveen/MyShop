import { Component, DestroyRef, inject, signal } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { BillingApi, Buyer, Invoice } from './billing.api';
import { billingError } from './billing-settings';
import { OrderManagementApi } from '../checkout/order-management.api';
@Component({
  imports: [DatePipe, FormsModule, RouterLink],
  host: { class: 'printable-order' },
  template: `<a
      class="back print-hide"
      [routerLink]="customer ? ['/winkel/account/bestellingen', id] : ['/bestellingen', id]"
      >← Terug naar bestelling</a
    >
    @if (loading()) {
      <p role="status">Factuur ophalen…</p>
    }
    @if (error()) {
      <p role="alert">{{ error() }}</p>
      <button type="button" class="print-hide" [disabled]="busy()" (click)="load()">
        Opnieuw proberen
      </button>
    }
    @if (invoice(); as document) {
      <h1>Factuur {{ document.number }}</h1>
      <button type="button" class="print-hide" (click)="print()">Factuur afdrukken / PDF</button>
      <p>
        Factuurdatum: {{ document.issuedAt | date: 'dd-MM-yyyy' }} · Levering / vooruitbetaling:
        {{ document.supplyDate | date: 'dd-MM-yyyy' }} · Bestelling: {{ document.orderNumber }}
      </p>
      <div class="grid">
        <section class="panel">
          <h2>{{ document.seller.name }}</h2>
          <p>
            {{ document.seller.addressLine }}<br />{{ document.seller.postalCode }}
            {{ document.seller.city }}<br />Nederland
          </p>
          <p>
            Btw-id: {{ document.seller.vatId }}
            @if (document.seller.kvkNumber) {
              · KvK: {{ document.seller.kvkNumber }}
            }
          </p>
        </section>
        <section class="panel">
          <h2>Factuur aan</h2>
          <p>
            {{ document.buyer.name }}<br />{{ document.buyer.addressLine }}<br />{{
              document.buyer.postalCode
            }}
            {{ document.buyer.city }}<br />{{ document.buyer.countryCode }}
          </p>
          @if (document.buyer.vatId) {
            <p>Btw-id: {{ document.buyer.vatId }}</p>
          }
        </section>
      </div>
      <table>
        <thead>
          <tr>
            <th>Omschrijving</th>
            <th>Aantal</th>
            <th>Prijs excl.</th>
            <th>Netto</th>
            <th>Btw</th>
            <th>Totaal</th>
          </tr>
        </thead>
        <tbody>
          @for (line of document.lines; track $index) {
            <tr>
              <td>{{ line.description }}</td>
              <td>{{ line.quantity }}</td>
              <td>{{ amount(line.unitNet) }}</td>
              <td>{{ amount(line.net) }}</td>
              <td>{{ line.exempt ? 'Vrijgesteld' : line.rate + '%' }} · {{ amount(line.vat) }}</td>
              <td>{{ amount(line.gross) }}</td>
            </tr>
          }
        </tbody>
      </table>
      <h2>Btw-overzicht</h2>
      @for (total of document.totals; track $index) {
        <p>
          {{ total.exempt ? 'Vrijgesteld' : total.rate + '%' }} · Netto {{ amount(total.net) }} ·
          Btw {{ amount(total.vat) }} · Totaal {{ amount(total.gross) }} {{ document.currency }}
        </p>
      }
      <p>
        <strong
          >Netto {{ amount(document.net) }} + btw {{ amount(document.vat) }} =
          {{ document.currency }} {{ amount(document.gross) }}</strong
        >
      </p>
      @if (document.taxStatement) {
        <p>{{ document.taxStatement }}</p>
      }
      <p>Btw is per artikel op centen afgerond.</p>
    } @else if (!loading() && !error()) {
      @if (customer) {
        <p>Er is nog geen factuur beschikbaar voor deze bestelling.</p>
      } @else if (revision) {
        <h1>Factuur uitgeven</h1>
        <p>
          Uitgeven legt de gegevens en het nummer definitief vast. Controleer de factuurgegevens,
          ook bij zakelijke en buitenlandse afnemers.
        </p>
        <form class="panel" (ngSubmit)="issue()">
          <label
            >Naam / bedrijfsnaam<input
              name="buyerName"
              [(ngModel)]="buyer.name"
              maxlength="200"
              required
              [disabled]="busy()"
          /></label>
          <label
            >Factuuradres<input
              name="address"
              [(ngModel)]="buyer.addressLine"
              maxlength="200"
              required
              [disabled]="busy()"
          /></label>
          <label
            >Postcode<input
              name="postal"
              [(ngModel)]="buyer.postalCode"
              maxlength="32"
              required
              [disabled]="busy()"
          /></label>
          <label
            >Plaats<input
              name="city"
              [(ngModel)]="buyer.city"
              maxlength="100"
              required
              [disabled]="busy()"
          /></label>
          <label
            >Landcode<input
              name="country"
              [(ngModel)]="buyer.countryCode"
              maxlength="2"
              required
              [disabled]="busy()"
          /></label>
          <label
            >Btw-id afnemer (indien nodig)<input
              name="vat"
              [(ngModel)]="buyer.vatId"
              maxlength="32"
              [disabled]="busy()"
          /></label>
          <label
            >Leveringsdatum / datum vooruitbetaling<input
              name="date"
              type="date"
              [(ngModel)]="supplyDate"
              required
              [disabled]="busy()"
          /></label>
          <label
            >Btw-vermelding / toelichting<textarea
              name="statement"
              [(ngModel)]="statement"
              maxlength="1000"
              [disabled]="busy()"
            ></textarea>
          </label>
          <label
            ><input name="review" type="checkbox" [(ngModel)]="reviewed" [disabled]="busy()" />Ik
            heb de afnemergegevens en btw-behandeling gecontroleerd.</label
          >
          <button [disabled]="busy() || !reviewed">Factuur definitief uitgeven</button>
        </form>
      }
    }`,
})
export class InvoicePage {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(BillingApi);
  private readonly orders = inject(OrderManagementApi);
  private readonly destroy = inject(DestroyRef);
  id = '';
  private readonly cancel = new Subject<void>();
  readonly customer = this.route.snapshot.data['customerInvoice'] === true;
  readonly loading = signal(false);
  readonly busy = signal(false);
  readonly error = signal('');
  readonly invoice = signal<Invoice | null>(null);
  revision = '';
  buyer: Buyer = {
    name: '',
    addressLine: '',
    postalCode: '',
    city: '',
    countryCode: 'NL',
    vatId: '',
  };
  supplyDate = '';
  reviewed = false;
  statement = '';
  constructor() {
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroy)).subscribe((parameters) => {
      this.cancel.next();
      this.id = parameters.get('id') ?? '';
      this.loading.set(false);
      this.busy.set(false);
      this.reviewed = false;
      this.statement = '';
      this.load();
    });
  }
  amount(value: string) {
    return value.replace('.', ',');
  }
  load() {
    if (this.loading() || this.busy()) return;
    this.loading.set(true);
    this.error.set('');
    this.invoice.set(null);
    this.revision = '';
    this.api
      .invoice(this.id, this.customer)
      .pipe(takeUntil(this.cancel), takeUntilDestroyed(this.destroy))
      .subscribe({
        next: (document) => {
          this.invoice.set(document);
          this.loading.set(false);
        },
        error: (error) => {
          if (error instanceof HttpErrorResponse && error.status === 404) {
            if (this.customer) {
              this.loading.set(false);
              return;
            }
            this.orders
              .get(this.id)
              .pipe(takeUntil(this.cancel), takeUntilDestroyed(this.destroy))
              .subscribe({
                next: (order) => {
                  this.revision = order.revision;
                  this.buyer = {
                    name: order.customer.name,
                    addressLine: order.deliveryAddress.addressLine,
                    postalCode: order.deliveryAddress.postalCode,
                    city: order.deliveryAddress.city,
                    countryCode: order.deliveryAddress.countryCode,
                    vatId: '',
                  };
                  this.supplyDate = (order.shippedAt ?? order.paidAt ?? order.placedAt).slice(
                    0,
                    10,
                  );
                  this.reviewed = false;
                  this.loading.set(false);
                },
                error: (value) => {
                  this.loading.set(false);
                  this.error.set(billingError(value));
                },
              });
          } else {
            this.loading.set(false);
            this.error.set(billingError(error));
          }
        },
      });
  }
  issue() {
    if (this.customer || this.busy() || this.loading() || this.invoice() || !this.revision || !this.reviewed) return;
    this.busy.set(true);
    this.error.set('');
    this.api
      .issue(
        this.id,
        this.revision,
        this.supplyDate,
        { ...this.buyer },
        this.reviewed,
        this.statement,
      )
      .pipe(takeUntil(this.cancel), takeUntilDestroyed(this.destroy))
      .subscribe({
        next: (document) => {
          this.invoice.set(document);
          this.busy.set(false);
        },
        error: (error) => {
          this.busy.set(false);
          this.error.set(billingError(error));
        },
      });
  }
  print() {
    if (this.invoice() && !this.loading() && !this.busy()) window.print();
  }
}
