import { Component, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { catchError, map, of } from 'rxjs';
import { ShopBranding } from './shop-branding';
import { Meta, Title } from '@angular/platform-browser';

const titles: Record<string, string> = {
  'algemene-voorwaarden': 'Algemene voorwaarden',
  privacyverklaring: 'Privacyverklaring',
  cookiebeleid: 'Cookiebeleid',
};

@Component({
  imports: [RouterLink],
  template: `
    <article class="legal-page">
      <div class="eyebrow">{{ branding.name() }}</div>
      <h1>{{ heading }}</h1>
      <p class="draft-notice">Concept voor Autoservice Ganpat. Gegevens tussen blokhaken worden nog aangevuld.</p>
      @if (document(); as result) {
        @if (result.error) {
          <p role="alert">De tekst kon niet worden geladen. Probeer de pagina opnieuw te openen of download het document hieronder.</p>
        } @else {
          <div class="legal-document" [innerHTML]="result.html"></div>
        }
      } @else { <p role="status">Document laden…</p> }
      <p><a [href]="download" download>Document downloaden (tekstbestand)</a></p>
      <a routerLink="/contact">Contact</a>
    </article>
  `,
  styleUrl: './shop-legal.css',
})
export class ShopLegal {
  readonly branding = inject(ShopBranding);
  private readonly slug = inject(ActivatedRoute).snapshot.data['legalPage'] as string;
  readonly heading = titles[this.slug];
  readonly download = '/legal/' + this.slug + '.md';
  readonly document = toSignal(inject(HttpClient).get('/legal/' + this.slug + '.html', { responseType: 'text' }).pipe(
    map(html => ({ html, error: false })),
    catchError(() => of({ html: '', error: true })),
  ));
  constructor() {
    inject(Title).setTitle(this.heading + ' · Autoservice Ganpat');
    // These drafts contain unfilled company information and should not be indexed yet.
    inject(Meta).updateTag({ name: 'robots', content: 'noindex' });
  }
}
