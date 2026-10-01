import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { switchMap } from 'rxjs';
import { Auth } from '../auth/auth';

export interface ManagedCustomerSummary {
  id: string;
  email: string;
  name: string | null;
  isLocked: boolean;
  lockedUntil: string | null;
  orderCount: number;
}
export interface ManagedCustomerPage {
  items: readonly ManagedCustomerSummary[];
  offset: number;
  limit: number;
  totalCount: number;
}
export interface ManagedCustomerDetail extends ManagedCustomerSummary {
  addressLine: string | null;
  postalCode: string | null;
  city: string | null;
  countryCode: string | null;
  lastOrderAt: string | null;
  revision: string;
}

@Injectable({ providedIn: 'root' })
export class CustomerManagementApi {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(Auth);
  list(offset: number, search: string) {
    let params = new HttpParams().set('offset', offset).set('limit', 20);
    if (search.trim()) params = params.set('search', search.trim());
    return this.http.get<ManagedCustomerPage>('/api/customers', { params });
  }
  get(id: string) {
    return this.http.get<ManagedCustomerDetail>('/api/customers/' + encodeURIComponent(id));
  }
  setLocked(id: string, revision: string, locked: boolean) {
    return this.auth
      .prepare()
      .pipe(
        switchMap(() =>
          this.http.put<{ locked: boolean; revision: string }>(
            '/api/customers/' + encodeURIComponent(id) + '/access',
            { locked, revision },
          ),
        ),
      );
  }
}
