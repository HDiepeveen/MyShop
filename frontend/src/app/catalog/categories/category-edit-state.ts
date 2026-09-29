import { Injectable, signal } from '@angular/core';

@Injectable()
export class CategoryEditState {
  readonly busy = signal(false);
}
