import { ShopColors, defaultColors, validColors } from '../settings/shop-theme';
import { inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
@Injectable({ providedIn: 'root' })
export class ShopBranding {
  private readonly http = inject(HttpClient);
  readonly colors = signal<ShopColors>({ ...defaultColors });
  readonly name = signal('MyShop');
  readonly footerText = signal('Ontdek wat bij je past.');
  private requestVersion = 0;
  refresh() {
    const version = ++this.requestVersion;
    this.http
      .get<{ shopName: string; footerText?: string; appearance?: ShopColors }>('/api/shop/settings')
      .subscribe({
        next: (settings) => {
          if (version === this.requestVersion) {
            this.colors.set(
              validColors(settings.appearance) ? { ...settings.appearance } : { ...defaultColors },
            );
            this.name.set(settings.shopName || 'MyShop');
            this.footerText.set(settings.footerText ?? 'Ontdek wat bij je past.');
          }
        },
        error: () => {},
      });
  }
}
