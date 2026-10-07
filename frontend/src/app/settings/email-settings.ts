import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { EmailSettingsApi, EmailSettingsData } from './email-settings.api';
import { errorMessage } from '../catalog/error-message';

@Component({
  imports: [FormsModule],
  template: `<h1>E-mailinstellingen</h1>
    @if (loading()) {
      <p role="status">Instellingen ophalen…</p>
    }
    @if (failure()) {
      <p role="alert">{{ failure() }}</p>
    }
    @if (!loading() && !settings()) {
      <button type="button" (click)="load()">Opnieuw proberen</button>
    }
    @if (settings(); as saved) {
      <form class="panel" (ngSubmit)="save()">
        <label
          ><input
            type="checkbox"
            name="enabled"
            [(ngModel)]="enabled"
            [disabled]="busy()"
          />E-mailverzending inschakelen</label
        >
        <p>Bij inschakelen worden ook eerder klaargezette e-mails verstuurd.</p>
        <label
          >SMTP-server<input name="host" maxlength="253" [(ngModel)]="host" [disabled]="busy()"
        /></label>
        <label
          >Poort<input
            name="port"
            type="number"
            min="1"
            max="65535"
            step="1"
            [(ngModel)]="port"
            [disabled]="busy()"
            required
        /></label>
        <p>
          Beveiligde verbinding met STARTTLS, meestal poort 587. Poort 465 wordt niet ondersteund.
        </p>
        <label
          >Gebruikersnaam<input
            name="userName"
            maxlength="320"
            autocomplete="off"
            [(ngModel)]="userName"
            [disabled]="busy()"
        /></label>
        <label
          >Nieuw SMTP-wachtwoord<input
            name="password"
            type="password"
            maxlength="1024"
            autocomplete="new-password"
            [(ngModel)]="password"
            [disabled]="busy() || clearPassword"
        /></label>
        <p>
          {{
            saved.passwordConfigured
              ? 'Een wachtwoord is opgeslagen.'
              : 'Er is geen wachtwoord opgeslagen.'
          }}
          Leeg laten behoudt het bestaande wachtwoord.
        </p>
        <label
          ><input
            type="checkbox"
            name="clearPassword"
            [ngModel]="clearPassword"
            (ngModelChange)="changeClearPassword($event)"
            [disabled]="busy()"
          />Opgeslagen wachtwoord wissen</label
        >
        <label
          >Afzenderadres<input
            name="fromAddress"
            type="email"
            maxlength="320"
            [(ngModel)]="fromAddress"
            [disabled]="busy()"
        /></label>
        <label
          >Afzendernaam<input
            name="fromName"
            maxlength="200"
            [(ngModel)]="fromName"
            [disabled]="busy()"
        /></label>
        <label
          >Winkel-URL<input
            name="publicBaseUrl"
            type="url"
            maxlength="2000"
            required
            [(ngModel)]="publicBaseUrl"
            [disabled]="busy()"
        /></label>
        <p>Deze URL wordt gebruikt in bevestigings- en herstellinks.</p>
        <div class="actions">
          <button [disabled]="busy()">{{ busy() ? 'Verwerken…' : 'Instellingen opslaan' }}</button>
          <button type="button" class="secondary" [disabled]="busy()" (click)="load()">
            Opgeslagen instellingen opnieuw ophalen
          </button>
        </div>
      </form>
      <form class="panel" (ngSubmit)="sendTest()">
        <h2>Testmail</h2>
        <p>
          De test gebruikt de laatst opgeslagen instellingen. Schakel verzending in en sla
          wijzigingen eerst op.
        </p>
        <label
          >Testontvanger<input
            name="recipient"
            type="email"
            maxlength="320"
            required
            [(ngModel)]="recipient"
            [disabled]="busy()"
        /></label>
        <button [disabled]="busy() || !saved.enabled">Testmail versturen</button>
      </form>
    }
    @if (message()) {
      <p role="status">{{ message() }}</p>
    }`,
})
export class EmailSettings {
  private readonly api = inject(EmailSettingsApi);
  private readonly destroyRef = inject(DestroyRef);
  readonly settings = signal<EmailSettingsData | null>(null);
  readonly loading = signal(false);
  readonly busy = signal(false);
  readonly failure = signal('');
  readonly message = signal('');
  enabled = false;
  host = '';
  port: number | null = 587;
  userName = '';
  password = '';
  clearPassword = false;
  fromAddress = '';
  fromName = '';
  publicBaseUrl = '';
  recipient = '';
  constructor() {
    this.load();
  }
  private assign(value: EmailSettingsData) {
    this.settings.set(value);
    this.enabled = value.enabled;
    this.host = value.host;
    this.port = value.port;
    this.userName = value.userName;
    this.fromAddress = value.fromAddress;
    this.fromName = value.fromName;
    this.publicBaseUrl = value.publicBaseUrl;
    this.password = '';
    this.clearPassword = false;
  }
  changeClearPassword(value: boolean) {
    if (this.busy()) return;
    this.clearPassword = value;
    if (value) this.password = '';
  }
  load() {
    if (this.loading() || this.busy()) return;
    this.loading.set(true);
    this.settings.set(null);
    this.failure.set('');
    this.message.set('');
    this.password = '';
    this.api
      .get()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (value) => {
          this.assign(value);
          this.loading.set(false);
        },
        error: (error) => {
          this.loading.set(false);
          this.failure.set(errorMessage(error));
        },
      });
  }
  save() {
    const saved = this.settings();
    if (!saved || this.busy() || this.loading()) return;
    this.failure.set('');
    this.message.set('');
    if (
      this.port === null ||
      !Number.isInteger(this.port) ||
      this.port < 1 ||
      this.port > 65535 ||
      this.port === 465
    ) {
      this.failure.set('Gebruik een hele SMTP-poort met STARTTLS, meestal 587.');
      return;
    }
    if (
      !this.publicBaseUrl.trim() ||
      this.publicBaseUrl.trim().length > 2000 ||
      this.host.trim().length > 253 ||
      this.userName.trim().length > 320 ||
      this.fromAddress.trim().length > 320 ||
      this.fromName.trim().length > 200 ||
      this.password.length > 1024 ||
      (this.enabled && (!this.host.trim() || !this.fromAddress.trim()))
    ) {
      this.failure.set('Controleer de SMTP-server, afzender en winkel-URL.');
      return;
    }
    this.busy.set(true);
    this.api
      .save({
        enabled: this.enabled,
        host: this.host.trim(),
        port: this.port,
        userName: this.userName.trim(),
        fromAddress: this.fromAddress.trim(),
        fromName: this.fromName.trim(),
        publicBaseUrl: this.publicBaseUrl.trim(),
        password: this.password || null,
        clearPassword: this.clearPassword,
        revision: saved.revision,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (value) => {
          this.assign(value);
          this.busy.set(false);
          this.message.set('E-mailinstellingen opgeslagen.');
        },
        error: (error) => {
          this.busy.set(false);
          this.failure.set(errorMessage(error));
        },
      });
  }
  sendTest() {
    const saved = this.settings();
    if (!saved || !saved.enabled || this.busy() || this.loading()) return;
    this.message.set('');
    this.failure.set('');
    const recipient = this.recipient.trim();
    if (!recipient || recipient.length > 320) {
      this.failure.set('Vul een geldig testadres in.');
      return;
    }
    this.busy.set(true);
    this.api
      .test(recipient, saved.revision)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.busy.set(false);
          this.message.set('Testmail klaargezet. Controleer je mailbox, ook de spammap.');
        },
        error: (error) => {
          this.busy.set(false);
          this.failure.set(errorMessage(error));
        },
      });
  }
}
