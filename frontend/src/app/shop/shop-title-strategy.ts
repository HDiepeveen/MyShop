import { effect, inject, Injectable } from '@angular/core';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { ShopBranding } from './shop-branding';

@Injectable()
export class ShopTitleStrategy extends TitleStrategy {
  private readonly title = inject(Title);
  private readonly branding = inject(ShopBranding);
  private routeTitle: string | undefined;
  private lastTitle: string | undefined;
  constructor() {
    super();
    effect(() => {
      const name = this.branding.name();
      // Keep explicit SEO metadata applied by assortment and product pages.
      if (this.routeTitle && this.title.getTitle() === this.lastTitle) this.setTitle(name);
    });
  }
  override updateTitle(snapshot: RouterStateSnapshot) {
    this.routeTitle = this.buildTitle(snapshot);
    this.setTitle(this.branding.name());
  }
  private setTitle(name: string) {
    if (!this.routeTitle) return;
    this.lastTitle = this.routeTitle.replace(/ · MyShop$/, () => ' · ' + name);
    this.title.setTitle(this.lastTitle);
  }
}
