import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { switchMap } from 'rxjs';
import { Auth } from '../auth/auth';

export interface DeliveryMethod {
  id: string;
  name: string;
  description: string | null;
  amount: string;
  currency: string;
  enabled: boolean;
  revision: string;
}

export type PublicDeliveryMethod = Omit<DeliveryMethod, 'enabled' | 'revision'>;

export interface SaveDeliveryMethod {
  name: string;
  description: string | null;
  amount: string;
  currency: string;
  enabled: boolean;
  revision?: string;
}

@Injectable({ providedIn: 'root' })
export class DeliveryMethodsApi {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(Auth);
  publicMethods() {
    return this.http.get<readonly PublicDeliveryMethod[]>('/api/shop/delivery-methods');
  }
  adminMethods() {
    return this.http.get<readonly DeliveryMethod[]>('/api/delivery-methods');
  }
  create(request: SaveDeliveryMethod) {
    return this.auth.prepare().pipe(
      switchMap(() => this.http.post<DeliveryMethod>('/api/delivery-methods', request)),
    );
  }
  update(id: string, request: SaveDeliveryMethod) {
    return this.auth.prepare().pipe(
      switchMap(() =>
        this.http.put<DeliveryMethod>('/api/delivery-methods/' + encodeURIComponent(id), request),
      ),
    );
  }
  delete(id: string, revision: string) {
    const params = new HttpParams().set('revision', revision);
    return this.auth.prepare().pipe(
      switchMap(() =>
        this.http.delete<void>('/api/delivery-methods/' + encodeURIComponent(id), { params }),
      ),
    );
  }
}
