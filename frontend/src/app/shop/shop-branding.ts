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
  readonly showAssortmentInMenu = signal(true);
  readonly categoryLinks = signal<readonly { id: string; name: string; webAddress?: string }[]>([]);
  readonly homePages = signal<readonly { name: string; webAddress: string }[]>([]);
  readonly assortmentImageUrl = signal<string | null>(null);
  readonly assortmentImageMode = signal<string | null>(null);
  readonly contactPhone = signal<string | null>(null);
  readonly showHeaderPhone = signal(true);
  readonly showHeaderWhatsapp = signal(true);
  readonly headerText = signal<string | null>(null);
  readonly name = signal('HD Web Framework');
  readonly footerText = signal('Ontdek wat bij je past.');
  private requestVersion = 0;
  refresh() {
    const version = ++this.requestVersion;
    this.http
      .get<{
        showAssortmentInMenu?: boolean;
        categoryLinks?: { id: string; name: string; webAddress?: string }[];
        shopName: string;
        assortmentImageUrl?: string | null; assortmentImageMode?: string | null;
        contactPhone?: string | null; showHeaderPhone?: boolean; showHeaderWhatsapp?: boolean;
        headerText?: string | null;
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
            this.showAssortmentInMenu.set(settings.showAssortmentInMenu !== false);
            this.categoryLinks.set(settings.categoryLinks ?? []);
            this.headerText.set(settings.headerText?.trim() || null);
            this.assortmentImageUrl.set(settings.assortmentImageUrl ?? null);
            this.assortmentImageMode.set(settings.assortmentImageMode ?? null);
            this.contactPhone.set(settings.contactPhone?.trim() || null);
            this.showHeaderPhone.set(settings.showHeaderPhone !== false);
            this.showHeaderWhatsapp.set(settings.showHeaderWhatsapp !== false);
            this.homeEnabled.set(settings.homeEnabled === true);
            this.homePages.set(settings.homeEnabled === true ? (settings.homePages ?? []) : []);
            this.showManagementMenuWithoutLogin.set(
              settings.showManagementMenuWithoutLogin === true,
            );
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
            this.name.set(settings.shopName || 'HD Web Framework');
            this.footerText.set(settings.footerText ?? 'Ontdek wat bij je past.');
          }
        },
        error: () => {},
      });
  }
}
