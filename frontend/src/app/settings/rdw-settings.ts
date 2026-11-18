import { Component, DestroyRef, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { switchMap } from 'rxjs';
import { Auth } from '../auth/auth';
import { errorMessage } from '../catalog/error-message';

interface Column { key: string; name: string; type: string; }
interface Settings { revision: string; columns: string[]; }
@Component({
  imports: [FormsModule],
  template: `
    <h1>RDW-instellingen</h1>
    <p>Kies welke gegevens uit RDW Open Data ‘Gekentekende voertuigen’ bij producten worden getoond.</p>
    @if (error()) { <p class="error" role="alert">{{ error() }}</p> }
    @if (loading()) { <p role="status">RDW-instellingen ophalen…</p> }
    @if (!loading() && !revision) {
      <button type="button" (click)="load()">Opnieuw ophalen</button>
    }
    @if (revision) {
      <form class="panel" (ngSubmit)="save()">
        <button type="button" class="secondary" [disabled]="busy() || loading()" (click)="load()">Gegevens vernieuwen</button>
        <fieldset [disabled]="busy() || loading()">
          <legend>Zichtbare voertuiggegevens</legend>
          <label>Zoek kolom<input name="filter" [(ngModel)]="filter" type="search" /></label>
          <p>{{ selection.size }} van {{ columns.length }} kolommen geselecteerd</p>
          <div class="actions">
            <button type="button" class="secondary" (click)="selectAll()">Alle kolommen selecteren</button>
            <button type="button" class="secondary" (click)="selection.clear()">Selectie wissen</button>
          </div>
          <div class="columns">
            @for (column of filtered(); track column.key) {
              <label class="column">
                <input type="checkbox" [name]="column.key"
                  [ngModel]="selection.has(column.key)" (ngModelChange)="toggle(column.key, $event)" />
                <span>{{ column.name }}<small>{{ column.key }}</small></span>
              </label>
            }
          </div>
          <button type="submit">RDW-instellingen opslaan</button>
        </fieldset>
      </form>
    }
    @if (notice()) { <p role="status">{{ notice() }}</p> }
  `,
  styles: `
    fieldset { border: 0; padding: 0; min-width: 0; }
    legend { font-size: 1.3rem; font-weight: 600; margin-bottom: 20px; }
    .columns { display: grid; grid-template-columns: repeat(auto-fit, minmax(min(100%, 300px), 1fr)); gap: 16px; margin: 20px 0; }
    .column { display: flex; gap: 12px; align-items: start; margin: 0; }
    .column input { width: auto; flex: none; margin-top: 4px; }
    small { display: block; overflow-wrap: anywhere; font-weight: normal; }
  `,
})
export class RdwSettingsPage {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(Auth);
  private readonly destroyRef = inject(DestroyRef);
  readonly error = signal('');
  readonly notice = signal('');
  readonly loading = signal(false);
  readonly busy = signal(false);
  columns: Column[] = [];
  selection = new Set<string>();
  revision = '';
  filter = '';
  constructor() { this.load(); }
  filtered() {
    const term = this.filter.trim().toLocaleLowerCase();
    return this.columns.filter(c => (c.name + ' ' + c.key).toLocaleLowerCase().includes(term));
  }
  toggle(key: string, selected: boolean) {
    if (selected) this.selection.add(key); else this.selection.delete(key);
  }
  selectAll() { this.selection = new Set(this.columns.map(c => c.key)); }
  load() {
    if (this.loading() || this.busy()) return;
    this.loading.set(true);
    this.error.set('');
    this.http.get<{columns: Column[]; settings: Settings}>('/api/rdw-settings')
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          this.columns = result.columns;
          this.selection = new Set(result.settings.columns);
          this.revision = result.settings.revision;
          this.loading.set(false);
        },
        error: error => { this.loading.set(false); this.error.set(errorMessage(error)); },
      });
  }
  save() {
    if (this.busy() || this.loading() || !this.revision) return;
    this.busy.set(true);
    this.error.set('');
    this.notice.set('');
    const body = { revision: this.revision, columns: [...this.selection] };
    this.auth.prepare().pipe(
      switchMap(() => this.http.put('/api/rdw-settings', body)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => { this.busy.set(false); this.notice.set('RDW-instellingen opgeslagen.'); this.load(); },
      error: error => { this.busy.set(false); this.error.set(errorMessage(error)); },
    });
  }
}
