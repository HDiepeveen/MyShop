import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { switchMap } from 'rxjs';
import { Auth } from '../auth/auth';
export interface Company {
  name: string;
  addressLine: string;
  postalCode: string;
  city: string;
  vatId: string;
  kvkNumber: string;
  invoicePrefix: string;
  revision: string;
}
export interface VatRate {
  id: string;
  name: string;
  percentage: number;
  exempt: boolean;
  enabled: boolean;
  revision: string;
}
export interface Buyer {
  name: string;
  addressLine: string;
  postalCode: string;
  city: string;
  countryCode: string;
  vatId: string;
}
export interface Invoice {
  id: string;
  orderId: string;
  number: string;
  orderNumber: string;
  issuedAt: string;
  supplyDate: string;
  seller: Company;
  buyer: Buyer;
  lines: {
    description: string;
    quantity: number;
    unitNet: string;
    net: string;
    vat: string;
    gross: string;
    rate: number;
    exempt: boolean;
  }[];
  totals: { rate: number; exempt: boolean; net: string; vat: string; gross: string }[];
  net: string;
  vat: string;
  gross: string;
  currency: string;
  taxStatement: string;
}
@Injectable({ providedIn: 'root' })
export class BillingApi {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(Auth);
  company() {
    return this.http.get<Company>('/api/billing/company');
  }
  saveCompany(value: Company) {
    return this.auth
      .prepare()
      .pipe(switchMap(() => this.http.put<Company>('/api/billing/company', value)));
  }
  rates() {
    return this.http.get<VatRate[]>('/api/billing/vat-rates');
  }
  saveRate(
    value: Omit<VatRate, 'id' | 'revision'> & { id: string | null; revision: string | null },
  ) {
    return this.auth
      .prepare()
      .pipe(switchMap(() => this.http.post<VatRate>('/api/billing/vat-rates', value)));
  }
  invoice(id: string, customer = false) {
    return this.http.get<Invoice>(
      (customer ? '/api/customer/orders/' : '/api/orders/') + encodeURIComponent(id) + '/invoice',
    );
  }
  issue(
    id: string,
    orderRevision: string,
    supplyDate: string,
    buyer: Buyer,
    taxReviewed: boolean,
    taxStatement: string,
  ) {
    return this.auth
      .prepare()
      .pipe(
        switchMap(() =>
          this.http.post<Invoice>('/api/orders/' + encodeURIComponent(id) + '/invoice', {
            orderRevision,
            supplyDate,
            buyer,
            taxReviewed,
            taxStatement,
          }),
        ),
      );
  }
}
