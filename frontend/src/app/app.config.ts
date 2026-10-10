import { ApplicationConfig, LOCALE_ID, provideBrowserGlobalErrorListeners } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localeNl from '@angular/common/locales/nl';
import { authErrors } from './auth/auth-routing';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ShopTitleStrategy } from './shop/shop-title-strategy';
import { TitleStrategy, provideRouter, withInMemoryScrolling } from '@angular/router';
import { routes } from './app.routes';
registerLocaleData(localeNl);
export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    { provide: TitleStrategy, useClass: ShopTitleStrategy },
    provideHttpClient(withInterceptors([authErrors])),
    provideRouter(
      routes,
      withInMemoryScrolling({ scrollPositionRestoration: 'enabled', anchorScrolling: 'enabled' }),
    ),
    { provide: LOCALE_ID, useValue: 'nl-NL' },
  ],
};
