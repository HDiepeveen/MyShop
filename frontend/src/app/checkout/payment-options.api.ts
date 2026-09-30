import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export interface AdminPaymentOptions {
  payLaterEnabled: boolean;
  onlinePaymentEnabled: boolean;
  payLaterInstructions: string | null;
  onlinePaymentConfigured: boolean;
  revision: string;
}

@Injectable({ providedIn: 'root' })
export class PaymentOptionsApi {
  private readonly http = inject(HttpClient);
  publicOptions() {
    return this.http.get<{ items: { code: string; name: string; instructions: string | null }[] }>(
      '/api/shop/payment-options',
    );
  }
  adminOptions() {
    return this.http.get<AdminPaymentOptions>('/api/payment-options');
  }
  update(
    value: Pick<
      AdminPaymentOptions,
      'payLaterEnabled' | 'onlinePaymentEnabled' | 'payLaterInstructions' | 'revision'
    >,
  ) {
    return this.http.put<
      Pick<
        AdminPaymentOptions,
        'payLaterEnabled' | 'onlinePaymentEnabled' | 'payLaterInstructions' | 'revision'
      >
    >('/api/payment-options', value);
  }
}
