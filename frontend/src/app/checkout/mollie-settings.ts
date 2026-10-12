import { Component, DestroyRef, inject, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { errorMessage } from '../catalog/error-message';
interface MollieSettings { returnUrl: string; webhookUrl: string; keyConfigured: boolean; managed: boolean; revision: string; }
@Component({ selector: 'app-mollie-settings', imports: [FormsModule], template: `
  <details class="panel" (toggle)="open($event)">
    <summary>Mollie instellen</summary>
    <p>Alleen iDEAL-testbetalingen. Er wordt geen echt geld afgeschreven.</p>
    @if (loading()) { <p role="status">Instellingen ophalen…</p> }
    @if (error()) { <p role="alert">{{ error() }}</p><button type="button" [disabled]="busy() || loading()" (click)="load()">Opnieuw ophalen</button> }
    @if (settings(); as current) {
      <p>{{ current.keyConfigured ? 'Testsleutel ingesteld.' : 'Nog geen testsleutel ingesteld.' }}</p>
      @if (!current.managed) { <p>Huidige instellingen komen uit de serverconfiguratie. Opslaan maakt deze beheerinstellingen leidend.</p> }
      <form (ngSubmit)="save()">
        <label>Nieuwe test-API-sleutel<input name="mollieKey" type="password" autocomplete="new-password" [(ngModel)]="apiKey" maxlength="69" [disabled]="busy() || loading()" placeholder="Laat leeg om de huidige sleutel te behouden" /></label>
        <label><input name="clearMollieKey" type="checkbox" [(ngModel)]="clearKey" [disabled]="busy() || loading()" /> Opgeslagen sleutel wissen</label>
        <label>Terugkeeradres<input name="mollieReturnUrl" type="url" [(ngModel)]="returnUrl" maxlength="2000" required [disabled]="busy() || loading()" /></label>
        <p>Het adres eindigt op /winkel/betaling. Lokaal mag HTTP; online gebruik je HTTPS.</p>
        <label>Meldingsadres<input name="mollieWebhookUrl" type="url" [(ngModel)]="webhookUrl" maxlength="2000" required [disabled]="busy() || loading()" /></label>
        <p>Publiek bereikbaar HTTPS-adres, eindigend op /api/payments/mollie/webhook. Geen localhost.</p>
        <button [disabled]="busy() || loading() || !returnUrl.trim() || !webhookUrl.trim() || (clearKey && !!apiKey.trim())">{{ busy() ? 'Bezig…' : 'Mollie-instellingen opslaan' }}</button>
        <button type="button" class="secondary" [disabled]="busy() || loading() || !current.keyConfigured" (click)="check()">Opgeslagen koppeling controleren</button>
      </form>
    }
    @if (notice()) { <p role="status">{{ notice() }}</p> }
  </details>
` })
export class MollieSettingsEditor {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  readonly saved = output<void>();
  readonly settings = signal<MollieSettings | null>(null);
  readonly loading = signal(false); readonly busy = signal(false); readonly error = signal(''); readonly notice = signal('');
  apiKey = ''; clearKey = false; returnUrl = ''; webhookUrl = '';
  open(event: Event) { if ((event.target as HTMLDetailsElement).open && !this.settings()) this.load(); }
  load() {
    if (this.busy() || this.loading()) return;
    this.loading.set(true); this.error.set('');
    this.http.get<MollieSettings>('/api/payment-options/mollie').pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: settings => { this.settings.set(settings); this.returnUrl = settings.returnUrl || window.location.origin + '/winkel/betaling';
        this.webhookUrl = settings.webhookUrl; this.apiKey = ''; this.clearKey = false; this.loading.set(false); },
      error: error => { this.loading.set(false); this.error.set(errorMessage(error)); },
    });
  }
  save() {
    const settings = this.settings();
    if (!settings || this.busy() || this.loading() || !this.returnUrl.trim() || !this.webhookUrl.trim() || this.clearKey && !!this.apiKey.trim()) return;
    this.busy.set(true); this.error.set(''); this.notice.set('');
    const request = { returnUrl: this.returnUrl, webhookUrl: this.webhookUrl, apiKey: this.apiKey.trim() || null, clearKey: this.clearKey, revision: settings.revision };
    this.apiKey = '';
    this.http.put<MollieSettings>('/api/payment-options/mollie', request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => { this.busy.set(false); this.notice.set('Mollie-instellingen opgeslagen. Je hoeft de server niet te herstarten.'); this.saved.emit(); this.load(); },
      error: error => { this.busy.set(false); this.error.set(errorMessage(error)); },
    });
  }
  check() {
    const settings = this.settings(); if (!settings || this.busy() || this.loading()) return;
    this.busy.set(true); this.error.set(''); this.notice.set('');
    this.http.post<{ configured: boolean; keyAccepted: boolean; idealAvailable: boolean }>('/api/payment-options/mollie/check', { revision: settings.revision })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => { this.busy.set(false); this.notice.set(!result.configured ? 'De opgeslagen instellingen zijn nog niet compleet.' :
          !result.keyAccepted ? 'De verbinding of testsleutel kon niet worden gecontroleerd.' :
          result.idealAvailable ? 'Testsleutel geaccepteerd: iDEAL is beschikbaar. Het meldingsadres moet nog met een echte testbetaling worden gecontroleerd.' :
          'Testsleutel geaccepteerd, maar iDEAL is nog niet beschikbaar in het testprofiel.'); },
        error: error => { this.busy.set(false); this.error.set(errorMessage(error)); },
      });
  }
}
