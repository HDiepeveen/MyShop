import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { switchMap } from 'rxjs';
import { Auth } from '../auth/auth';

export interface CheckoutOrderLine {
  productId: string;
  variantId: string;
  quantity: number;
  expectedAmount: string;
  expectedCurrency: string;
}

export interface OrderReceipt {
  id: string;
  number: string;
  placedAt: string;
  paymentInstructions: string | null;
  totals: readonly OrderReceiptTotal[];
  deliveryMethod: OrderReceiptDeliveryMethod | null;
}

export interface OrderReceiptTotal {
  currency: string;
  amount: string;
}

export interface OrderReceiptDeliveryMethod {
  id: string;
  name: string;
  description: string | null;
  amount: string;
  currency: string;
}

export interface PlaceOrderRequest {
  checkoutToken: string;
  paymentMethod: string;
  deliveryMethodId: string;
  customerName: string;
  email: string;
  addressLine: string;
  postalCode: string;
  city: string;
  countryCode: string;
  lines: readonly CheckoutOrderLine[];
}

export interface StartOnlinePaymentRequest {
  checkoutToken: string;
  deliveryMethodId: string;
  customerName: string;
  email: string;
  addressLine: string;
  postalCode: string;
  city: string;
  countryCode: string;
  lines: readonly CheckoutOrderLine[];
}

export interface OnlinePaymentStart {
  checkoutToken: string;
  providerName: string;
  paymentReference: string;
  providerPaymentId: string;
  checkoutUrl: string;
  message: string;
  totals: readonly OrderReceiptTotal[];
  deliveryMethod: OrderReceiptDeliveryMethod;
}

@Injectable({ providedIn: 'root' })
export class OrderApi {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(Auth);

  place(request: PlaceOrderRequest) {
    return this.auth
      .prepare()
      .pipe(switchMap(() => this.http.post<OrderReceipt>('/api/shop/orders', request)));
  }

  startOnlinePayment(request: StartOnlinePaymentRequest) {
    return this.auth
      .prepare()
      .pipe(switchMap(() => this.http.post<OnlinePaymentStart>('/api/shop/online-payments', request)));
  }

  completeOnlinePayment(checkoutToken: string, providerPaymentId: string) {
    const token = encodeURIComponent(checkoutToken);
    const provider = encodeURIComponent(providerPaymentId);
    return this.auth
      .prepare()
      .pipe(switchMap(() => this.http.get<OrderReceipt>(`/api/shop/online-payments/${token}/complete?providerPaymentId=${provider}`)));
  }
}
