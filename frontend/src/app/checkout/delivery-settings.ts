import { Component, DestroyRef, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { errorMessage } from '../catalog/error-message';
import { DeliveryMethod, DeliveryMethodsApi } from './delivery-methods.api';

@Component({
  imports: [FormsModule],
  template: `<div class="eyebrow">Checkout</div><h1>Bezorgopties</h1>
    <p>Alle ingeschakelde opties worden bij het afrekenen aangeboden.</p>
    @if (failure()) { <p class="error" role="alert">{{ failure() }}</p> }
    @if (notice()) { <p class="success" role="status">{{ notice() }}</p> }
    @if (loading()) { <p role="status">Bezorgopties ophalen…</p> }
    @for (method of methods(); track method.id) {
      <section class="panel">
        <h2>{{ method.name }} <span class="badge">{{ method.enabled ? 'Beschikbaar' : 'Uitgeschakeld' }}</span></h2>
        <p>{{ method.currency }} {{ amount(method.amount) }}</p>
        @if (method.description) { <p class="preserve-lines">{{ method.description }}</p> }
        <button class="secondary" type="button" (click)="edit(method)">Bewerken</button>
        <button class="secondary" type="button" [disabled]="busy()" (click)="remove(method)">Verwijderen</button>
      </section>
    }
    <section class="panel form-width">
      <h2>{{ editingId ? 'Bezorgoptie bewerken' : 'Bezorgoptie toevoegen' }}</h2>
      <form (ngSubmit)="save()">
        <label>Naam<input name="name" [(ngModel)]="name" maxlength="100" required /></label>
        <label>Toelichting<textarea name="description" [(ngModel)]="description" maxlength="500" rows="3"></textarea></label>
        <div class="grid">
          <label>Bedrag<input name="amount" [(ngModel)]="price" inputmode="decimal" pattern="[0-9]+([.,][0-9]{1,2})?" required /></label>
          <label>Valuta<input name="currency" [(ngModel)]="currency" minlength="3" maxlength="3" required /></label>
        </div>
        <label class="check-field"><input type="checkbox" name="enabled" [(ngModel)]="enabled" />Beschikbaar voor klanten</label>
        <div class="actions"><button [disabled]="busy()">{{ busy() ? 'Opslaan…' : 'Opslaan' }}</button>
          @if (editingId) { <button class="secondary" type="button" (click)="clear()">Annuleren</button> }
        </div>
      </form>
    </section>`,
})
export class DeliverySettings {
  private readonly api = inject(DeliveryMethodsApi);
  private readonly destroyRef = inject(DestroyRef);
  readonly methods = signal<readonly DeliveryMethod[]>([]);
  readonly loading = signal(false);
  readonly busy = signal(false);
  readonly failure = signal('');
  readonly notice = signal('');
  editingId = '';
  revision = '';
  name = '';
  description = '';
  price = '0.00';
  currency = 'EUR';
  enabled = true;
  constructor() { this.load(); }
  load() {
    this.loading.set(true); this.failure.set('');
    this.api.adminMethods().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (methods) => { this.methods.set(methods); this.loading.set(false); },
      error: (error) => { this.failure.set(errorMessage(error)); this.loading.set(false); },
    });
  }
  edit(method: DeliveryMethod) {
    this.editingId = method.id; this.revision = method.revision; this.name = method.name;
    this.description = method.description ?? ''; this.price = method.amount;
    this.currency = method.currency; this.enabled = method.enabled; this.notice.set('');
  }
  clear() {
    this.editingId = ''; this.revision = ''; this.name = ''; this.description = '';
    this.price = '0.00'; this.currency = 'EUR'; this.enabled = true;
  }
  save() {
    if (this.busy()) return;
    this.busy.set(true); this.failure.set(''); this.notice.set('');
    const request = { name: this.name, description: this.description.trim() || null,
      amount: this.price.replace(',', '.'), currency: this.currency.toUpperCase(),
      enabled: this.enabled, ...(this.revision ? { revision: this.revision } : {}) };
    const operation = this.editingId ? this.api.update(this.editingId, request) : this.api.create(request);
    operation.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => { this.busy.set(false); this.clear(); this.notice.set('De bezorgoptie is opgeslagen.'); this.load(); },
      error: (error) => { this.busy.set(false); this.failure.set(errorMessage(error)); },
    });
  }
  remove(method: DeliveryMethod) {
    if (this.busy() || !window.confirm(`Wil je ${method.name} verwijderen?`)) return;
    this.busy.set(true); this.failure.set(''); this.notice.set('');
    this.api.delete(method.id, method.revision).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => { this.busy.set(false); if (this.editingId === method.id) this.clear(); this.notice.set('De bezorgoptie is verwijderd.'); this.load(); },
      error: (error) => { this.busy.set(false); this.failure.set(errorMessage(error)); },
    });
  }
  amount(value: string) { return value.replace('.', ','); }
}
