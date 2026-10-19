import { ShopColors, defaultColors, validColors } from '../settings/shop-theme';
import { computed, inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
@Injectable({ providedIn: 'root' })
export class ShopBranding {
  private readonly http = inject(HttpClient);
  readonly colors = signal<ShopColors>({ ...defaultColors });
  readonly showManagementMenuWithoutLogin = signal(false);
  readonly iconUrl = signal<string | null>(null);
  readonly iconFailed = signal(false);
  readonly icon = computed(() => (this.iconFailed() ? null : this.iconUrl()));
  readonly homeEnabled = signal(false);
  readonly homePages = signal<readonly { name: string; webAddress: string }[]>([]);
  readonly name = signal('MyShop');
  readonly footerText = signal('Ontdek wat bij je past.');
  private requestVersion = 0;
  refresh() {
    const version = ++this.requestVersion;
    this.http
      .get<{
        shopName: string;
        homeEnabled?: boolean;
        homePages?: { name: string; webAddress: string }[];
        footerText?: string;
        appearance?: ShopColors;
        iconUrl?: string | null;
        showManagementMenuWithoutLogin?: boolean;
      }>('/api/shop/settings')
      .subscribe({
        next: (settings) => {
          if (version === this.requestVersion) {
            this.homeEnabled.set(settings.homeEnabled === true);
            this.homePages.set(settings.homeEnabled === true ? (settings.homePages ?? []) : []);
            this.showManagementMenuWithoutLogin.set(settings.showManagementMenuWithoutLogin === true);
            const icon =
              typeof settings.iconUrl === 'string' &&
              /^\/api\/shop\/icon\?v=[0-9a-f-]{36}$/i.test(settings.iconUrl)
                ? settings.iconUrl
                : null;
            if (icon !== this.iconUrl()) this.iconFailed.set(false);
            this.iconUrl.set(icon);
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
