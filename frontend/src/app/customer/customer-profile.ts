import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CustomerAccountApi, CustomerProfile as Profile } from './customer-account.api';
import { errorMessage } from '../catalog/error-message';

@Component({
  imports: [FormsModule, RouterLink],
  template: `<div class="eyebrow">Mijn account</div>
    <h1>Profiel en afleveradres</h1>
    @if (loading()) {
      <p role="status">Profiel ophalen…</p>
    }
    @if (failure()) {
      <p class="error" role="alert">{{ failure() }}</p>
    }
    @if (!loading() && !loaded()) {
      <button type="button" (click)="load()">Opnieuw proberen</button>
    }
    @if (loaded()) {
      <form class="panel" (ngSubmit)="save()">
        <label>E-mailadres<input [value]="email" disabled /></label>
        <label
          >Naam<input name="name" [disabled]="busy()" [(ngModel)]="name" maxlength="200" required
        /></label>
        <label
          >Adres<input
            name="address"
            [disabled]="busy()"
            [(ngModel)]="addressLine"
            maxlength="200"
            required
        /></label>
        <label
          >Postcode<input
            name="postal"
            [disabled]="busy()"
            [(ngModel)]="postalCode"
            maxlength="32"
            required
        /></label>
        <label
          >Plaats<input name="city" [disabled]="busy()" [(ngModel)]="city" maxlength="100" required
        /></label>
        <label
          >Landcode<input
            name="country"
            [disabled]="busy()"
            [(ngModel)]="countryCode"
            minlength="2"
            maxlength="2"
            required
        /></label>
        @if (message()) {
          <p role="status">{{ message() }}</p>
        }
        <button [disabled]="busy()">{{ busy() ? 'Opslaan…' : 'Profiel opslaan' }}</button>
      </form>
    }
    <p><a routerLink="/winkel/account/bestellingen">Mijn bestellingen bekijken</a></p>
    <p><a routerLink="/winkel/account/verlanglijst">Mijn verlanglijst bekijken</a></p>
    <p><a routerLink="/winkel/account/wachtwoord">Wachtwoord wijzigen</a></p>`,
})
export class CustomerProfile {
  private readonly api = inject(CustomerAccountApi);
  private readonly destroyRef = inject(DestroyRef);
  readonly loaded = signal(false);
  readonly loading = signal(false);
  readonly busy = signal(false);
  readonly failure = signal('');
  readonly message = signal('');
  email = '';
  name = '';
  addressLine = '';
  postalCode = '';
  city = '';
  countryCode = 'NL';
  revision: string | null = null;
  constructor() {
    this.load();
  }
  private assign(profile: Profile) {
    this.email = profile.email;
    this.name = profile.name ?? '';
    this.addressLine = profile.addressLine ?? '';
    this.postalCode = profile.postalCode ?? '';
    this.city = profile.city ?? '';
    this.countryCode = profile.countryCode ?? 'NL';
    this.revision = profile.revision;
  }
  load() {
    if (this.loaded() || this.loading()) return;
    this.loading.set(true);
    this.failure.set('');
    this.api
      .profile()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (profile) => {
          this.assign(profile);
          this.loaded.set(true);
          this.loading.set(false);
        },
        error: (error) => {
          this.failure.set(errorMessage(error));
          this.loading.set(false);
        },
      });
  }
  validationError() {
    if (!this.name.trim() || this.name.trim().length > 200)
      return 'Vul een naam van maximaal 200 tekens in.';
    if (!this.addressLine.trim() || this.addressLine.trim().length > 200)
      return 'Vul een adres van maximaal 200 tekens in.';
    if (!this.postalCode.trim() || this.postalCode.trim().length > 32)
      return 'Vul een postcode van maximaal 32 tekens in.';
    if (!this.city.trim() || this.city.trim().length > 100)
      return 'Vul een plaats van maximaal 100 tekens in.';
    if (!/^[a-zA-Z]{2}$/.test(this.countryCode.trim()))
      return 'Gebruik een landcode van twee letters.';
    return '';
  }
  save() {
    if (this.busy() || !this.loaded()) return;
    const validation = this.validationError();
    if (validation) {
      this.message.set('');
      this.failure.set(validation);
      return;
    }
    this.busy.set(true);
    this.failure.set('');
    this.message.set('');
    this.api
      .update({
        name: this.name.trim(),
        addressLine: this.addressLine.trim(),
        postalCode: this.postalCode.trim(),
        city: this.city.trim(),
        countryCode: this.countryCode.trim().toUpperCase(),
        revision: this.revision,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (profile) => {
          this.assign(profile);
          this.busy.set(false);
          this.message.set('Je profiel is opgeslagen.');
        },
        error: (error) => {
          this.busy.set(false);
          this.failure.set(errorMessage(error));
        },
      });
  }
}
