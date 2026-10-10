import { inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
@Injectable({ providedIn: 'root' })
export class ShopBranding {
  private readonly http = inject(HttpClient);
  readonly name = signal('MyShop');
  private requestVersion = 0;
  refresh() {
    const version = ++this.requestVersion;
    this.http.get<{ shopName: string }>('/api/shop/settings').subscribe({
      next: settings => { if (version === this.requestVersion) this.name.set(settings.shopName || 'MyShop'); },
      error: () => {},
    });
  }
}
