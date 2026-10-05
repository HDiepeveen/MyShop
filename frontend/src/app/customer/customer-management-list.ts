import { Component, DestroyRef, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { errorMessage } from '../catalog/error-message';
import { CustomerManagementApi, ManagedCustomerPage } from './customer-management.api';

@Component({
  imports: [FormsModule, RouterLink],
  template: `<div class="eyebrow">Accounts</div>
    <h1>Klanten</h1>
    <form class="toolbar" (ngSubmit)="applySearch()">
      <label
        >Zoeken<input
          type="search"
          name="search"
          [(ngModel)]="searchText"
          placeholder="E-mailadres of naam" /></label
      ><button [disabled]="loading()">Zoeken</button>
      @if (search) {
        <button type="button" class="secondary" [disabled]="loading()" (click)="clearSearch()">Zoekterm wissen</button>
      }
    </form>
    @if (loading()) {
      <p role="status">Klanten ophalen…</p>
    }
    @if (failure()) {
      <p class="error" role="alert">{{ failure() }}</p>
      <button (click)="load()">Opnieuw proberen</button>
    }
    @if (page(); as result) {
      @if (!result.items.length && result.offset > 0) {
        <p>Deze pagina bevat geen klanten meer.</p>
        <button type="button" class="secondary" (click)="previous()">Vorige pagina</button>
      } @else if (!result.items.length) {
        <p>Geen klanten gevonden.</p>
      }
      @for (customer of result.items; track customer.id) {
        <article class="panel">
          <h2>
            <a [routerLink]="[customer.id]">{{ customer.name || customer.email }}</a>
          </h2>
          <p>{{ customer.email }}</p>
          <p>
            {{ customer.orderCount }} bestelling(en) ·
            {{ customer.isLocked ? 'Geblokkeerd' : 'Actief' }}
          </p>
        </article>
      }
      @if (result.items.length && (result.offset > 0 || result.offset + result.items.length < result.totalCount)) {
        <nav class="toolbar" aria-label="Paginering">
          <button class="secondary" [disabled]="result.offset === 0" (click)="previous()">
            Vorige</button
          ><span
            >{{ result.offset + 1 }}–{{ result.offset + result.items.length }} van
            {{ result.totalCount }}</span
          ><button
            class="secondary"
            [disabled]="result.offset + result.items.length >= result.totalCount"
            (click)="next()"
          >
            Volgende
          </button>
        </nav>
      }
    }`,
})
export class CustomerManagementList {
  private readonly api = inject(CustomerManagementApi);
  private readonly destroyRef = inject(DestroyRef);
  readonly page = signal<ManagedCustomerPage | null>(null);
  readonly loading = signal(false);
  readonly failure = signal('');
  readonly offset = signal(0);
  searchText = '';
  search = '';
  constructor() {
    this.load();
  }
  load() {
    if (this.loading()) return;
    this.page.set(null);
    this.loading.set(true);
    this.failure.set('');
    this.api
      .list(this.offset(), this.search)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (page) => {
          this.page.set(page);
          this.offset.set(page.offset);
          this.loading.set(false);
        },
        error: (error) => {
          this.failure.set(errorMessage(error));
          this.loading.set(false);
        },
      });
  }
  applySearch() {
    if (this.loading()) return;
    this.search = this.searchText.trim();
    this.offset.set(0);
    this.load();
  }
  clearSearch() {
    if (this.loading()) return;
    this.searchText = '';
    this.applySearch();
  }
  previous() {
    if (this.loading()) return;
    this.offset.set(Math.max(0, this.offset() - 20));
    this.load();
  }
  next() {
    if (this.loading()) return;
    this.offset.set(this.offset() + 20);
    this.load();
  }
}
