import { Component, effect, input, signal } from '@angular/core';
@Component({
  selector: 'app-shop-image',
  host: { '[class.large]': 'large()' },
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
        background: #f3f5ed;
        border-radius: 10px;
      }
      .placeholder {
        display: grid;
        place-items: center;
        padding: 16px;
        color: #62716b;
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
  readonly failed = signal(false);
  constructor() {
    effect(() => {
      this.url();
      this.failed.set(false);
    });
  }
}
