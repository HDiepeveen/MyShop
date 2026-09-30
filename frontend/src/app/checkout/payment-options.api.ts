import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export interface AdminPaymentOptions {
  payLaterEnabled: boolean;
  onlinePaymentEnabled: boolean;
  onlinePaymentConfigured: boolean;
  revision: string;
}

@Injectable({ providedIn: 'root' })
export class PaymentOptionsApi {
  private readonly http = inject(HttpClient);
  publicOptions() {
    return this.http.get<{ items: { code: string; name: string }[] }>('/api/shop/payment-options');
  }
  adminOptions() {
    return this.http.get<AdminPaymentOptions>('/api/payment-options');
  }
  update(
    value: Pick<AdminPaymentOptions, 'payLaterEnabled' | 'onlinePaymentEnabled' | 'revision'>,
  ) {
    return this.http.put<
      Pick<AdminPaymentOptions, 'payLaterEnabled' | 'onlinePaymentEnabled' | 'revision'>
    >('/api/payment-options', value);
  }
}
