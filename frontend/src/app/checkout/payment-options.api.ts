import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export interface AdminPaymentOptions {
  checkoutEnabled?: boolean;
  payLaterEnabled: boolean;
  onlinePaymentEnabled: boolean;
  payLaterInstructions: string | null;
  onlinePaymentConfigured: boolean;
  onlinePaymentProvider: string | null;
  revision: string;
}

@Injectable({ providedIn: 'root' })
export class PaymentOptionsApi {
  private readonly http = inject(HttpClient);
  publicOptions() {
    return this.http.get<{ checkoutEnabled?: boolean; items: { code: string; name: string; instructions: string | null }[] }>(
      '/api/shop/payment-options',
    );
  }
  adminOptions() {
    return this.http.get<AdminPaymentOptions>('/api/payment-options');
  }
  update(
    value: Pick<
      AdminPaymentOptions,
      'checkoutEnabled' | 'payLaterEnabled' | 'onlinePaymentEnabled' | 'payLaterInstructions' | 'revision'
    >,
  ) {
    return this.http.put<
      Pick<
        AdminPaymentOptions,
        'checkoutEnabled' | 'payLaterEnabled' | 'onlinePaymentEnabled' | 'payLaterInstructions' | 'revision'
      >
    >('/api/payment-options', value);
  }
}
