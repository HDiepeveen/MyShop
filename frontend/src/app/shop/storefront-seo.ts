import { inject, Injectable } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';
import { DOCUMENT } from '@angular/common';
@Injectable({ providedIn: 'root' })
export class StorefrontSeo {
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);
  private readonly document = inject(DOCUMENT);
  reset(path: string) {
    this.meta.removeTag('name="description"');
    this.document.querySelector('link[rel="canonical"]')?.remove();
    const segment = path.split('?')[0].split('/')[2];
    const pathname = path.split('?')[0];
    if (!(pathname === '/winkel' || pathname.startsWith('/winkel/')) || ['betaling', 'account', 'inloggen', 'registreren', 'winkelmand', 'e-mail-bevestigen', 'wachtwoord-herstellen', 'wachtwoord-vergeten'].includes(segment))
      this.meta.updateTag({ name: 'robots', content: 'noindex' });
    else this.meta.removeTag('name="robots"');
  }
  apply(title: string, description: string | undefined, path: string) {
    this.title.setTitle(title);
    if (description) this.meta.updateTag({ name: 'description', content: description });
    else this.meta.removeTag('name="description"');
    this.meta.removeTag('name="robots"');
    let canonical = this.document.querySelector<HTMLLinkElement>('link[rel="canonical"]');
    if (!canonical) { canonical = this.document.createElement('link'); canonical.rel = 'canonical'; this.document.head.appendChild(canonical); }
    canonical.href = new URL(path, this.document.location.origin).href;
  }
}
