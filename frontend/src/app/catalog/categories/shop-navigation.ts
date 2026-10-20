import { Component, DestroyRef, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ShopBranding } from '../../shop/shop-branding';
import { errorMessage } from '../error-message';
type Settings = { showAssortmentInMenu: boolean; revision: string };
@Component({
  selector: 'app-shop-navigation',
  imports: [FormsModule],
  template: `<section class="panel form-width">
    <h2>Assortiment in menubalk</h2>
    <button type="button" class="secondary" (click)="load()" [disabled]="busy()">
      Instelling ophalen
    </button>
    @if (settings()) {
      <form (ngSubmit)="save()">
        <label
          ><input
            type="checkbox"
            name="showAssortmentInMenu"
            [(ngModel)]="showAssortmentInMenu"
            [disabled]="busy()"
          />
          Assortiment in menubalk tonen</label
        >
        <p>De afzonderlijke categorielinks blijven zichtbaar wanneer je Assortiment verbergt.</p>
        <button [disabled]="busy()">Instelling opslaan</button>
      </form>
    }
    @if (error()) {
      <p class="error" role="alert">{{ error() }}</p>
    }
    @if (saved()) {
      <p class="success" role="status">Instelling opgeslagen.</p>
    }
  </section>`,
})
export class ShopNavigation {
  readonly settings = signal<Settings | null>(null);
  readonly busy = signal(false);
  readonly error = signal('');
  readonly saved = signal(false);
  showAssortmentInMenu = true;
  private readonly http = inject(HttpClient);
  private readonly branding = inject(ShopBranding);
  private readonly destroyRef = inject(DestroyRef);
  load() {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set('');
    this.saved.set(false);
    this.http
      .get<Settings>('/api/categories/navigation-settings')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (value) => {
          this.busy.set(false);
          this.settings.set(value);
          this.showAssortmentInMenu = value.showAssortmentInMenu;
        },
        error: (error) => {
          this.busy.set(false);
          this.error.set(errorMessage(error));
        },
      });
  }
  save() {
    const settings = this.settings();
    if (!settings || this.busy()) return;
    this.busy.set(true);
    this.error.set('');
    this.saved.set(false);
    this.http
      .put('/api/categories/navigation-settings', {
        showAssortmentInMenu: this.showAssortmentInMenu,
        revision: settings.revision,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.busy.set(false);
          this.branding.refresh();
          this.settings.set(null);
          this.load();
          this.saved.set(true);
        },
        error: (error) => {
          this.busy.set(false);
          this.error.set(errorMessage(error));
        },
      });
  }
}
