import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { BillingApi, Company, VatRate } from './billing.api';
import { errorMessage } from '../catalog/error-message';
export function billingError(error: unknown) {
  return error instanceof HttpErrorResponse &&
    error.error?.code === 'billing' &&
    typeof error.error.message === 'string'
    ? error.error.message
    : errorMessage(error);
}
@Component({
  imports: [FormsModule],
  template: `<h1>Facturatie en btw</h1>
    @if (loading()) {
      <p role="status">Instellingen ophalen…</p>
    }
    @if (error()) {
      <p role="alert">{{ error() }}</p>
    }
    @if (!loading() && !company()) {
      <button type="button" (click)="load()">Opnieuw proberen</button>
    }
    @if (company(); as saved) {
      <form class="panel" (ngSubmit)="saveCompany()">
        <h2>Bedrijfsgegevens</h2>
        <p>Nieuwe facturen bewaren deze gegevens. Uitgegeven facturen veranderen niet.</p>
        <label
          >Volledige bedrijfsnaam<input
            name="companyName"
            maxlength="200"
            [(ngModel)]="draft.name"
            [disabled]="busy()"
        /></label>
        <label
          >Straat en huisnummer<input
            name="address"
            maxlength="200"
            [(ngModel)]="draft.addressLine"
            [disabled]="busy()"
        /></label>
        <label
          >Postcode<input
            name="postal"
            maxlength="32"
            [(ngModel)]="draft.postalCode"
            [disabled]="busy()"
        /></label>
        <label
          >Plaats<input name="city" maxlength="100" [(ngModel)]="draft.city" [disabled]="busy()"
        /></label>
        <p>Vestigingsland: Nederland.</p>
        <label
          >Btw-id<input
            name="vatId"
            maxlength="32"
            [(ngModel)]="draft.vatId"
            [disabled]="busy()"
            placeholder="NL123456789B01"
        /></label>
        <label
          >KvK-nummer<input
            name="kvk"
            maxlength="8"
            [(ngModel)]="draft.kvkNumber"
            [disabled]="busy()"
        /></label>
        <label
          >Factuurprefix<input
            name="prefix"
            maxlength="20"
            [(ngModel)]="draft.invoicePrefix"
            [disabled]="busy()"
        /></label>
        <button [disabled]="busy()">Bedrijfsgegevens opslaan</button>
      </form>
      <section class="panel">
        <h2>Btw-percentages</h2>
        <p>
          Percentages worden niet automatisch aan verkooplanden gekoppeld. Controleer zelf welk
          percentage van toepassing is.
        </p>
        @for (rate of rates(); track rate.id) {
          <p>
            {{ rate.name }} · {{ rate.percentage }}% {{ rate.exempt ? '(vrijgesteld)' : '' }} ·
            {{ rate.enabled ? 'Actief' : 'Uitgeschakeld' }}
            <button type="button" class="secondary" [disabled]="busy()" (click)="editRate(rate)">
              Bewerken
            </button>
          </p>
        }
        <form (ngSubmit)="saveRate()">
          <h3>{{ rateId ? 'Percentage bewerken' : 'Percentage toevoegen' }}</h3>
          <label
            >Naam<input
              name="rateName"
              maxlength="100"
              [(ngModel)]="rateName"
              [disabled]="busy()"
              required
          /></label>
          <label
            >Percentage<input
              name="percentage"
              inputmode="decimal"
              [(ngModel)]="percentage"
              [disabled]="busy()"
              required
          /></label>
          <label
            ><input
              name="exempt"
              type="checkbox"
              [(ngModel)]="exempt"
              [disabled]="busy()"
            />Vrijgesteld (0%)</label
          >
          <label
            ><input
              name="enabled"
              type="checkbox"
              [(ngModel)]="enabled"
              [disabled]="busy()"
            />Beschikbaar bij prijsinvoer</label
          >
          <button [disabled]="busy()">Percentage opslaan</button
          ><button type="button" class="secondary" [disabled]="busy()" (click)="newRate()">
            Nieuwe invoer
          </button>
        </form>
      </section>
      <button type="button" class="secondary" [disabled]="busy()" (click)="load()">
        Opgeslagen instellingen opnieuw ophalen
      </button>
    }
    @if (message()) {
      <p role="status">{{ message() }}</p>
    }`,
})
export class BillingSettings {
  private readonly api = inject(BillingApi);
  private readonly destroy = inject(DestroyRef);
  readonly company = signal<Company | null>(null);
  readonly rates = signal<VatRate[]>([]);
  readonly loading = signal(false);
  readonly busy = signal(false);
  readonly error = signal('');
  readonly message = signal('');
  draft: Company = {
    name: '',
    addressLine: '',
    postalCode: '',
    city: '',
    vatId: '',
    kvkNumber: '',
    invoicePrefix: 'INV-',
    revision: '',
  };
  rateId: string | null = null;
  rateRevision: string | null = null;
  rateName = '';
  percentage = '';
  exempt = false;
  enabled = true;
  constructor() {
    this.load();
  }
  load() {
    if (this.busy() || this.loading()) return;
    this.loading.set(true);
    this.company.set(null);
    this.error.set('');
    this.message.set('');
    forkJoin({ company: this.api.company(), rates: this.api.rates() })
      .pipe(takeUntilDestroyed(this.destroy))
      .subscribe({
        next: (data) => {
          this.company.set(data.company);
          this.draft = { ...data.company };
          this.rates.set(data.rates);
          this.loading.set(false);
          this.newRate();
        },
        error: (error) => {
          this.loading.set(false);
          this.error.set(billingError(error));
        },
      });
  }
  newRate() {
    if (this.busy()) return;
    this.rateId = this.rateRevision = null;
    this.rateName = '';
    this.percentage = '';
    this.exempt = false;
    this.enabled = true;
  }
  editRate(rate: VatRate) {
    if (this.busy() || !this.rates().includes(rate)) return;
    this.rateId = rate.id;
    this.rateRevision = rate.revision;
    this.rateName = rate.name;
    this.percentage = rate.percentage.toString();
    this.exempt = rate.exempt;
    this.enabled = rate.enabled;
  }
  saveCompany() {
    if (this.busy() || !this.company()) return;
    this.busy.set(true);
    this.error.set('');
    this.message.set('');
    this.api
      .saveCompany({ ...this.draft, revision: this.company()!.revision })
      .pipe(takeUntilDestroyed(this.destroy))
      .subscribe({
        next: (saved) => {
          this.company.set(saved);
          this.draft = { ...saved };
          this.busy.set(false);
          this.message.set('Bedrijfsgegevens opgeslagen.');
        },
        error: (error) => {
          this.busy.set(false);
          this.error.set(billingError(error));
        },
      });
  }
  saveRate() {
    if (this.busy() || !this.company()) return;
    const text = this.percentage.trim().replace(',', '.');
    const value = Number(text);
    if (
      !this.rateName.trim() ||
      this.rateName.trim().length > 100 ||
      !/^\d{1,3}(\.\d{1,2})?$/.test(text) ||
      value > 100 ||
      (this.exempt && value !== 0)
    ) {
      this.error.set(
        'Gebruik een naam en een percentage van 0 tot 100 met maximaal twee decimalen.',
      );
      return;
    }
    this.busy.set(true);
    this.error.set('');
    this.message.set('');
    this.api
      .saveRate({
        id: this.rateId,
        revision: this.rateRevision,
        name: this.rateName.trim(),
        percentage: value,
        exempt: this.exempt,
        enabled: this.enabled,
      })
      .pipe(takeUntilDestroyed(this.destroy))
      .subscribe({
        next: (saved) => {
          this.rates.update((values) => [
            ...values.filter((value) => value.id !== saved.id),
            saved,
          ]);
          this.busy.set(false);
          this.newRate();
          this.message.set(
            'Btw-percentage opgeslagen. Bestaande prijzen en facturen blijven behouden.',
          );
        },
        error: (error) => {
          this.busy.set(false);
          this.error.set(billingError(error));
        },
      });
  }
}
