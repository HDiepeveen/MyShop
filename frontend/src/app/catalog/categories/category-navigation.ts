import { Component, DestroyRef, effect, inject, input, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Category } from '../catalog.models';
import { CategoryEditState } from './category-edit-state';
import { ShopBranding } from '../../shop/shop-branding';
import { errorMessage } from '../error-message';
type Settings = {
  categoryId: string;
  showInMenu: boolean;
  revision: string;
  webAddress?: string | null;
  seoTitle?: string | null;
  seoDescription?: string | null;
};
@Component({
  selector: 'app-category-navigation',
  imports: [FormsModule],
  template: `
    <section class="panel form-width">
      <h2>Categorie in menubalk</h2>
      <button type="button" class="secondary" (click)="load()" [disabled]="loading() || busy()">
        Instelling ophalen
      </button>
      @if (settings()) {
        <form (ngSubmit)="save()">
          <label
            ><input
              type="checkbox"
              name="showInMenu"
              [(ngModel)]="showInMenu"
              [disabled]="busy() || loading()"
            />
            Categorie in menubalk tonen</label
          >
          <p>De link opent het assortiment met deze categorie geselecteerd.</p>
          <label class="field"
            >Webadres<input
              name="webAddress"
              [(ngModel)]="webAddress"
              maxlength="160"
              [disabled]="busy() || loading()"
              placeholder="bijvoorbeeld occasions"
          /></label>
          <p>Adres: /assortiment/{{ webAddress.trim() || category().id }}</p>
          <label class="field"
            >SEO-titel<input
              name="seoTitle"
              [(ngModel)]="seoTitle"
              maxlength="200"
              [disabled]="busy() || loading()"
          /></label>
          <label class="field"
            >SEO-omschrijving<textarea
              name="seoDescription"
              [(ngModel)]="seoDescription"
              maxlength="500"
              [disabled]="busy() || loading()"
            ></textarea>
          </label>
          <p>Laat de SEO-velden leeg om de categorienaam en standaardomschrijving te gebruiken.</p>
          <button [disabled]="busy() || loading()">Instelling opslaan</button>
        </form>
      }
      @if (error()) {
        <p class="error" role="alert">{{ error() }}</p>
      }
      @if (saved()) {
        <p class="success" role="status">Instelling opgeslagen.</p>
      }
    </section>
  `,
})
export class CategoryNavigation {
  readonly category = input.required<Category>();
  readonly busy = inject(CategoryEditState).busy;
  readonly loading = signal(false);
  readonly settings = signal<Settings | null>(null);
  readonly error = signal('');
  readonly saved = signal(false);
  showInMenu = false;
  webAddress = '';
  seoTitle = '';
  seoDescription = '';
  private readonly http = inject(HttpClient);
  private readonly branding = inject(ShopBranding);
  private readonly destroyRef = inject(DestroyRef);
  private writing = false;
  constructor() {
    effect(() => {
      this.category();
      this.settings.set(null);
      this.loading.set(false);
      this.error.set('');
      this.saved.set(false);
    });
    this.destroyRef.onDestroy(() => {
      if (this.writing) this.busy.set(false);
    });
  }
  load() {
    if (this.loading() || this.busy()) return;
    const id = this.category().id;
    this.loading.set(true);
    this.error.set('');
    this.saved.set(false);
    this.http
      .get<Settings>('/api/categories/' + id + '/navigation')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (value) => {
          if (this.category().id !== id) return;
          this.loading.set(false);
          this.settings.set(value);
          this.showInMenu = value.showInMenu;
          this.webAddress = value.webAddress ?? '';
          this.seoTitle = value.seoTitle ?? '';
          this.seoDescription = value.seoDescription ?? '';
        },
        error: (error) => {
          if (this.category().id !== id) return;
          this.loading.set(false);
          this.error.set(errorMessage(error));
        },
      });
  }
  save() {
    const settings = this.settings();
    if (!settings || this.busy() || this.loading()) return;
    const id = this.category().id;
    this.busy.set(true);
    this.writing = true;
    this.error.set('');
    this.saved.set(false);
    this.http
      .put('/api/categories/' + id + '/navigation', {
        showInMenu: this.showInMenu,
        revision: settings.revision,
        webAddress: this.webAddress.trim() || null,
        seoTitle: this.seoTitle.trim() || null,
        seoDescription: this.seoDescription.trim() || null,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.busy.set(false);
          this.writing = false;
          this.branding.refresh();
          if (this.category().id !== id) return;
          this.settings.set(null);
          this.load();
          this.saved.set(true);
        },
        error: (error) => {
          this.busy.set(false);
          this.writing = false;
          if (this.category().id !== id) return;
          this.error.set(errorMessage(error));
        },
      });
  }
}
