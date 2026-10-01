import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { switchMap } from 'rxjs';
import { Auth } from '../auth/auth';

export type CustomerOrderStatus = 'awaitingPayment' | 'paid' | 'shipped' | 'cancelled' | 'refunded';
export interface CustomerOrderTotal {
  currency: string;
  amount: string;
}
export interface CustomerOrderSummary {
  id: string;
  number: string;
  placedAt: string;
  paymentMethod: 'payLater';
  status: CustomerOrderStatus;
  totals: readonly CustomerOrderTotal[];
}
export interface CustomerOrderPage {
  items: readonly CustomerOrderSummary[];
  offset: number;
  limit: number;
  totalCount: number;
}
export interface CustomerOrderDetail extends CustomerOrderSummary {
  customer: { name: string; email: string };
  deliveryAddress: { addressLine: string; postalCode: string; city: string; countryCode: string };
  paymentInstructions: string | null;
  paidAt: string | null;
  shippedAt: string | null;
  shippingCarrier: string | null;
  trackingCode: string | null;
  cancelledAt: string | null;
  refundedAt: string | null;
  revision: string;
  lines: readonly {
    productId: string;
    variantId: string;
    productName: string;
    variantName: string;
    quantity: number;
    unitAmount: string;
    currency: string;
    totalAmount: string;
  }[];
}

@Injectable({ providedIn: 'root' })
export class CustomerOrderApi {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(Auth);
  list(offset: number) {
    return this.http.get<CustomerOrderPage>('/api/customer/orders', {
      params: new HttpParams().set('offset', offset).set('limit', 20),
    });
  }
  get(id: string) {
    return this.http.get<CustomerOrderDetail>('/api/customer/orders/' + encodeURIComponent(id));
  }
  cancel(id: string, revision: string) {
    return this.auth
      .prepare()
      .pipe(
        switchMap(() =>
          this.http.post<{ status: 'cancelled'; cancelledAt: string; revision: string }>(
            '/api/customer/orders/' + encodeURIComponent(id) + '/cancel',
            { revision },
          ),
        ),
      );
  }
}

export function customerOrderStatus(status: CustomerOrderStatus): string {
  return {
    awaitingPayment: 'Wacht op betaling',
    paid: 'Betaald',
    shipped: 'Verzonden',
    cancelled: 'Geannuleerd',
    refunded: 'Terugbetaald',
  }[status];
}
