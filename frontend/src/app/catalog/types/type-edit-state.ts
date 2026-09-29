import { Injectable, signal } from '@angular/core';
// Serialize writes to one product type across all editors on its detail page.
@Injectable()
export class TypeEditState {
  readonly busy = signal(false);
}
