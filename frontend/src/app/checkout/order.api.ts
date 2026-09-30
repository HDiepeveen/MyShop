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
}

export interface PlaceOrderRequest {
  checkoutToken: string;
  paymentMethod: string;
  customerName: string;
  email: string;
  addressLine: string;
  postalCode: string;
  city: string;
  countryCode: string;
  lines: readonly CheckoutOrderLine[];
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
}
