import { Component, DestroyRef, inject, signal } from '@angular/core';
import { NgStyle } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { switchMap } from 'rxjs';
import { Auth } from '../auth/auth';
import { ShopBranding } from '../shop/shop-branding';
import { errorMessage } from '../catalog/error-message';
import {
  ShopColors,
  defaultColors,
  shopThemes,
  themeStyles,
  validColors,
  contrastWarnings,
} from './shop-theme';

interface AppearanceSettings {
  colors: ShopColors;
  revision: string;
  iconUrl?: string | null;
  showManagementMenuWithoutLogin?: boolean;
}
@Component({
  imports: [FormsModule, NgStyle],
  template: `
    <h1>Vormgeving</h1>
    <p>
      Kies kleuren voor de klantomgeving. Het voorbeeld verandert direct; de winkel verandert na
      opslaan.
    </p>
    @if (loading()) {
      <p role="status">Kleuren ophalen…</p>
    }
    @if (error()) {
      <p class="error" role="alert">{{ error() }}</p>
      <button type="button" [disabled]="loading() || busy()" (click)="load()">
        Opnieuw ophalen
      </button>
    }
    @if (revision) {
      <section class="panel">
        <h2>Beheermenubalk</h2>
        <label><input type="checkbox" name="showManagementMenuWithoutLogin" [(ngModel)]="showManagementMenuWithoutLogin" [disabled]="busy() || loading()" />
          Beheermenu tonen zonder beheerderslogin</label>
        <p>Toont de menulinks op beheerpagina’s aan bezoekers en klanten. Beheerpagina’s blijven beveiligd.
          Bedrijfsbeheerders zien alleen hun toegewezen onderdelen.</p>
        <button type="button" [disabled]="busy() || loading()" (click)="save()">Vormgeving opslaan</button>
      </section>
      <section class="panel">
        <h2>Icoon voor de webshopnaam</h2>
        <p>
          Upload één JPEG- of PNG-afbeelding van maximaal 1 MB en 20 miljoen pixels. Een vierkante
          PNG met transparante achtergrond werkt goed. Uploaden of verwijderen wordt direct
          opgeslagen.
        </p>
        @if (iconUrl) {
          <p class="icon-example"><img [src]="iconUrl" alt="" />{{ branding.name() }}</p>
          @if (removingIcon()) {
            <p>Het webshopicoon verwijderen?</p>
            <button type="button" [disabled]="busy() || loading()" (click)="removeIcon()">
              Ja, verwijderen
            </button>
            <button
              type="button"
              class="secondary"
              [disabled]="busy() || loading()"
              (click)="removingIcon.set(false)"
            >
              Annuleren
            </button>
          } @else {
            <button
              type="button"
              class="secondary"
              [disabled]="busy() || loading()"
              (click)="removingIcon.set(true)"
            >
              Icoon verwijderen
            </button>
          }
        } @else {
          <p>Nog geen icoon ingesteld.</p>
        }
        <label
          >Icoon kiezen<input
            #iconInput
            type="file"
            accept="image/png,image/jpeg"
            [disabled]="busy() || loading()"
            (change)="chooseIcon(iconInput)"
        /></label>
        <button
          type="button"
          [disabled]="busy() || loading() || !iconFile"
          (click)="uploadIcon(iconInput)"
        >
          Icoon uploaden
        </button>
      </section>
      <form class="panel" (ngSubmit)="save()">
        <fieldset [disabled]="busy() || loading()">
          <legend>Kleurenschema</legend>
          <label
            >Thema<select name="theme" [ngModel]="selectedTheme()" (ngModelChange)="choose($event)">
              <option value="">Eigen kleuren</option>
              @for (theme of themes; track theme.name) {
                <option [value]="theme.name">{{ theme.name }}</option>
              }
            </select></label
          >
          <div class="grid">
            @for (field of fields; track field.key) {
              <label
                >{{ field.label
                }}<input type="color" [name]="field.key" [(ngModel)]="colors[field.key]"
              /></label>
            }
          </div>
          @for (warning of warnings(); track warning) {
            <p role="status">
              {{ warning }} Kies duidelijk verschillende kleuren voor een betere leesbaarheid.
            </p>
          }
          <section class="theme-preview" [ngStyle]="styles()" aria-label="Voorbeeld kleurenschema">
            <div class="eyebrow">Jouw webshop</div>
            <h2>Ontdek ons assortiment</h2>
            <p>Dit is een voorbeeld van de kleuren voor jouw klanten.</p>
            <article class="panel">
              <h3>Voorbeeldproduct</h3>
              <p>Productinformatie en prijzen blijven goed leesbaar.</p>
              <a href="#appearance-preview" (click)="$event.preventDefault()">Bekijk product</a>
              <div>
                <button type="button">In winkelmand</button
                ><button type="button" class="secondary">Verder winkelen</button>
              </div>
              <label>Naam<input value="Voorbeeld" readonly /></label>
            </article>
          </section>
          <button [disabled]="!valid()">Kleuren opslaan</button>
          <button type="button" class="secondary" (click)="resetColors()">
            Originele kleuren herstellen
          </button>
        </fieldset>
      </form>
    }
    @if (notice()) {
      <p role="status">{{ notice() }}</p>
    }
  `,
  styles: `
    fieldset {
      border: 0;
      padding: 0;
      min-width: 0;
    }
    legend {
      font-weight: 600;
      margin-bottom: 16px;
    }
    input[type='color'] {
      width: 100%;
      min-height: 48px;
      cursor: pointer;
    }
    .theme-preview {
      padding: 24px;
      border-radius: 12px;
      margin: 20px 0;
    }
    .icon-example {
      display: flex;
      align-items: center;
      gap: 12px;
      font-size: 26px;
      font-weight: 700;
    }
    .icon-example img {
      width: 40px;
      height: 40px;
      object-fit: contain;
    }
  `,
})
export class AppearanceSettingsPage {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(Auth);
  readonly branding = inject(ShopBranding);
  private readonly destroyRef = inject(DestroyRef);
  readonly themes = shopThemes;
  readonly fields: { key: keyof ShopColors; label: string }[] = [
    { key: 'primaryColor', label: 'Hoofdkleur' },
    { key: 'backgroundColor', label: 'Achtergrond' },
    { key: 'panelColor', label: 'Panelen' },
    { key: 'textColor', label: 'Tekst en koppen' },
  ];
  colors = { ...defaultColors };
  showManagementMenuWithoutLogin = false;
  revision = '';
  iconUrl: string | null = null;
  iconFile: File | null = null;
  readonly removingIcon = signal(false);
  readonly loading = signal(false);
  readonly busy = signal(false);
  readonly error = signal('');
  readonly notice = signal('');
  constructor() {
    this.load();
  }
  valid() {
    return validColors(this.colors);
  }
  styles() {
    return themeStyles(this.colors);
  }
  warnings() {
    return contrastWarnings(this.colors);
  }
  selectedTheme() {
    return (
      this.themes.find((theme) =>
        this.fields.every((field) => theme.colors[field.key] === this.colors[field.key]),
      )?.name ?? ''
    );
  }
  choose(name: string) {
    const theme = this.themes.find((item) => item.name === name);
    if (theme) this.colors = { ...theme.colors };
  }
  resetColors() {
    if (this.busy() || this.loading() || !this.revision) return;
    this.colors = { ...defaultColors };
    this.error.set('');
    this.notice.set(
      'Originele kleuren hersteld in het voorbeeld. Klik op Kleuren opslaan om ze toe te passen.',
    );
  }
  chooseIcon(input: HTMLInputElement) {
    this.iconFile = null;
    this.error.set('');
    const file = input.files?.[0];
    if (!file) return;
    if (!file.size || file.size > 1024 * 1024 || !/\.(png|jpe?g)$/i.test(file.name)) {
      this.error.set('Kies een JPEG- of PNG-afbeelding van maximaal 1 MB.');
      input.value = '';
      return;
    }
    this.iconFile = file;
  }
  uploadIcon(input: HTMLInputElement) {
    if (this.busy() || this.loading() || !this.revision || !this.iconFile) return;
    const form = new FormData();
    form.append('revision', this.revision);
    form.append('file', this.iconFile, this.iconFile.name);
    this.changeIcon(
      () => this.http.post('/api/shop-icon', form),
      () => {
        input.value = '';
        this.iconFile = null;
        this.notice.set('Webshopicoon opgeslagen.');
      },
    );
  }
  removeIcon() {
    if (this.busy() || this.loading() || !this.revision || !this.removingIcon()) return;
    this.changeIcon(
      () => this.http.delete('/api/shop-icon', { body: { revision: this.revision } }),
      () => {
        this.removingIcon.set(false);
        this.notice.set('Webshopicoon verwijderd.');
      },
    );
  }
  private changeIcon(action: () => import('rxjs').Observable<unknown>, success: () => void) {
    this.busy.set(true);
    this.error.set('');
    this.notice.set('');
    this.auth
      .prepare()
      .pipe(switchMap(action), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.busy.set(false);
          success();
          this.branding.refresh();
          this.load(true);
        },
        error: (error) => {
          this.busy.set(false);
          this.error.set(errorMessage(error));
        },
      });
  }
  load(keepColors = false) {
    if (this.loading() || this.busy()) return;
    this.loading.set(true);
    this.error.set('');
    this.http
      .get<AppearanceSettings>('/api/shop-appearance')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (settings) => {
          if (!keepColors) {
            this.colors = { ...settings.colors };
            this.showManagementMenuWithoutLogin = settings.showManagementMenuWithoutLogin === true;
          }
          this.iconUrl = settings.iconUrl ?? null;
          this.revision = settings.revision;
          this.loading.set(false);
        },
        error: (error) => {
          this.loading.set(false);
          this.error.set(errorMessage(error));
        },
      });
  }
  save() {
    if (this.busy() || this.loading() || !this.revision) return;
    if (!this.valid()) {
      this.error.set('Kies vier geldige kleuren.');
      return;
    }
    const colors = { ...this.colors };
    this.busy.set(true);
    this.error.set('');
    this.notice.set('');
    this.auth
      .prepare()
      .pipe(
        switchMap(() => this.http.put('/api/shop-appearance', { colors, revision: this.revision, showManagementMenuWithoutLogin: this.showManagementMenuWithoutLogin })),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.busy.set(false);
          this.branding.refresh();
          this.notice.set('Vormgeving opgeslagen.');
          this.load();
        },
        error: (error) => {
          this.busy.set(false);
          this.error.set(errorMessage(error));
        },
      });
  }
}
