import { inject, Injectable, signal } from '@angular/core';
import { PaymentOptionsApi } from '../checkout/payment-options.api';

@Injectable({ providedIn: 'root' })
export class ShopSettings {
  private readonly api = inject(PaymentOptionsApi);
  readonly checkoutEnabled = signal(true);
  private requestVersion = 0;
  refresh() {
    const version = ++this.requestVersion;
    this.checkoutEnabled.set(false);
    this.api.publicOptions().subscribe({
      next: settings => { if (version === this.requestVersion) this.checkoutEnabled.set(settings.checkoutEnabled !== false); },
      error: () => { if (version === this.requestVersion) this.checkoutEnabled.set(false); },
    });
  }
}
