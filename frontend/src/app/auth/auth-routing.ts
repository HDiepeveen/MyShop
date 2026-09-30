import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, map, of, switchMap, throwError } from 'rxjs';
import { Auth } from './auth';

export const adminGuard: CanActivateFn = (_, state) => {
  const auth = inject(Auth);
  const router = inject(Router);
  return auth.load().pipe(
    switchMap((session) => {
      if (!session.authenticated)
        return of(router.createUrlTree(['/inloggen'], { queryParams: { returnUrl: state.url } }));
      if (!session.administrator) return of(router.createUrlTree(['/geen-toegang']));
      return auth.prepare().pipe(map(() => true));
    }),
    catchError(() =>
      of(
        router.createUrlTree(['/inloggen'], {
          queryParams: { returnUrl: state.url, reason: 'unavailable' },
        }),
      ),
    ),
  );
};

export const authErrors: HttpInterceptorFn = (request, next) => {
  const router = inject(Router);
  const auth = inject(Auth);
  return next(request).pipe(
    catchError((error) => {
      if (
        request.url.startsWith('/api/') &&
        !request.url.startsWith('/api/auth/') &&
        !request.url.startsWith('/api/shop/') &&
        error instanceof HttpErrorResponse
      ) {
        if (error.status === 401) {
          auth.session.set(null);
          void router.navigate(['/inloggen'], {
            queryParams: { returnUrl: router.url, reason: 'expired' },
          });
        } else if (error.status === 403) {
          void router.navigate(['/geen-toegang']);
        }
      }
      return throwError(() => error);
    }),
  );
};

export function safeReturnUrl(value: string | null): string {
  // Only routes within this administration app; never interpret an external URL.
  return value &&
    /^\/(?:producten|categorieen|producttypen|instellingen\/betalen)(?:[/?]|$)/.test(value) &&
    !/[\\\r\n]/.test(value)
    ? value
    : '/';
}
