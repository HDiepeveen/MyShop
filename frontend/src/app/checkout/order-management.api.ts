import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

export interface OrderTotal {
  currency: string;
  amount: string;
}
export interface OrderSummary {
  id: string;
  number: string;
  placedAt: string;
  customerName: string;
  paymentMethod: 'payLater';
  status: 'awaitingPayment' | 'paid';
  paidAt: string | null;
  totals: readonly OrderTotal[];
}
export interface OrderListPage {
  items: readonly OrderSummary[];
  offset: number;
  limit: number;
  totalCount: number;
}
export interface OrderDetail {
  id: string;
  number: string;
  placedAt: string;
  customer: { name: string; email: string };
  deliveryAddress: { addressLine: string; postalCode: string; city: string; countryCode: string };
  paymentMethod: 'payLater';
  status: 'awaitingPayment' | 'paid';
  paidAt: string | null;
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
  totals: readonly OrderTotal[];
}

@Injectable({ providedIn: 'root' })
export class OrderManagementApi {
  private readonly http = inject(HttpClient);
  list(offset: number) {
    return this.http.get<OrderListPage>('/api/orders', {
      params: new HttpParams().set('offset', offset).set('limit', 20),
    });
  }
  get(id: string) {
    return this.http.get<OrderDetail>('/api/orders/' + encodeURIComponent(id));
  }
  markPaid(id: string, revision: string) {
    return this.http.put<{ status: 'paid'; paidAt: string; revision: string }>(
      '/api/orders/' + encodeURIComponent(id) + '/status',
      { status: 'paid', revision },
    );
  }
}
