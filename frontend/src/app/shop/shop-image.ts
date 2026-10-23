import { Component, effect, input, signal } from '@angular/core';
@Component({
  selector: 'app-shop-image',
  host: { '[class.large]': 'large()', '[class.padded]': 'padded()' },
  styles: [
    `
      :host {
        display: block;
      }
      img,
      .placeholder {
        width: 100%;
        height: 280px;
        object-fit: contain;
        background: var(--shop-background, #f3f5ed);
        border-radius: 10px;
      }
      :host(.padded) img {
        display: block;
        box-sizing: border-box;
        padding: 24px;
      }
      .placeholder {
        display: grid;
        place-items: center;
        padding: 16px;
        color: var(--shop-text, #62716b);
      }
      :host(.large) img,
      :host(.large) .placeholder {
        height: clamp(360px, 60vw, 560px);
      }
    `,
  ],
  template: `@if (url() && !failed()) {
      <img
        [src]="url()"
        [alt]="alt()"
        loading="lazy"
        referrerpolicy="no-referrer"
        (error)="failed.set(true)"
      />
    } @else {
      <div class="placeholder">Afbeelding niet beschikbaar</div>
    }`,
})
export class ShopImage {
  readonly url = input<string | null>(null);
  readonly alt = input('');
  readonly large = input(false);
  readonly padded = input(false);
  readonly failed = signal(false);
  constructor() {
    effect(() => {
      this.url();
      this.failed.set(false);
    });
  }
}
