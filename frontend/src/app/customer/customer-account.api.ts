import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

export interface CustomerProfile {
  email: string;
  emailConfirmed?: boolean;
  name: string | null;
  addressLine: string | null;
  postalCode: string | null;
  city: string | null;
  countryCode: string | null;
  revision: string | null;
}

@Injectable({ providedIn: 'root' })
export class CustomerAccountApi {
  private readonly http = inject(HttpClient);
  profile() {
    return this.http.get<CustomerProfile>('/api/customer/profile');
  }
  update(profile: Omit<CustomerProfile, 'email'>) {
    return this.http.put<CustomerProfile>('/api/customer/profile', profile);
  }
}
