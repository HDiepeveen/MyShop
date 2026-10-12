import { DOCUMENT } from '@angular/common';
import { inject, Injectable } from '@angular/core';
@Injectable({ providedIn: 'root' })
export class MollieNavigation {
  private readonly document = inject(DOCUMENT);
  open(url: string) {
    const destination = new URL(url);
    if (destination.protocol !== 'https:' || destination.hostname !== 'www.mollie.com' || destination.username || destination.password)
      throw new Error('Ongeldig Mollie-betaaladres.');
    this.document.location.assign(destination.href);
  }
}
