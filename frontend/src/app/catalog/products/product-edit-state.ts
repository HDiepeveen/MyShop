import { Injectable, signal } from '@angular/core';

// One write at a time across the editors on a single product detail page.
@Injectable()
export class ProductEditState {
  readonly busy = signal(false);
}
