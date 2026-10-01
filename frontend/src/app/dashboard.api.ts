import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

export type DashboardOrderStatus = 'awaitingPayment' | 'paid' | 'shipped' | 'cancelled' | 'refunded';
export interface DashboardAmount { currency: string; amount: string; }
export interface DashboardData {
  productCount: number; publishedProductCount: number; draftProductCount: number; customerCount: number;
  orders: { status: DashboardOrderStatus; count: number }[];
  activeRevenue: DashboardAmount[];
  lowStock: { productId: string; variantId: string; productName: string; variantName: string; sku: string | null; quantity: number }[];
  recentOrders: { id: string; number: string; placedAt: string; customerName: string; status: DashboardOrderStatus; totals: DashboardAmount[] }[];
}

@Injectable({ providedIn: 'root' })
export class DashboardApi {
  private readonly http = inject(HttpClient);
  get() { return this.http.get<DashboardData>('/api/dashboard'); }
}
