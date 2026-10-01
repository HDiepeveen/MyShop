import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { switchMap } from 'rxjs';
import { Auth } from '../auth/auth';

export interface WishlistItem {
  productId: string;
  name: string;
  imageUrl: string | null;
  imageAlt: string;
  isAvailable: boolean;
  addedAt: string;
}
export interface WishlistPage {
  items: WishlistItem[];
  offset: number;
  limit: number;
  totalCount: number;
}

@Injectable({ providedIn: 'root' })
export class CustomerWishlistApi {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(Auth);
  list(offset = 0) {
    return this.http.get<WishlistPage>('/api/customer/wishlist', {
      params: new HttpParams().set('offset', offset).set('limit', 20),
    });
  }
  state(productId: string) {
    return this.http.get<{ saved: boolean }>(
      '/api/customer/wishlist/' + encodeURIComponent(productId),
    );
  }
  add(productId: string) {
    return this.auth.prepare().pipe(
      switchMap(() =>
        this.http.post<void>('/api/customer/wishlist/' + encodeURIComponent(productId), {}),
      ),
    );
  }
  remove(productId: string) {
    return this.auth.prepare().pipe(
      switchMap(() =>
        this.http.delete<void>('/api/customer/wishlist/' + encodeURIComponent(productId)),
      ),
    );
  }
}
