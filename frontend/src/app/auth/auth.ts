import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { catchError, of, switchMap, tap, throwError } from 'rxjs';

export interface AdminSession {
  authenticated: boolean;
  administrator: boolean;
  customer: boolean;
  name: string | null;
}
@Injectable({ providedIn: 'root' })
export class Auth {
  private readonly http = inject(HttpClient);
  readonly session = signal<AdminSession | null>(null);
  load() {
    return this.http
      .get<AdminSession>('/api/auth/session')
      .pipe(tap((session) => this.session.set(session)));
  }
  private csrf() {
    return this.http.get<void>('/api/auth/csrf');
  }
  login(userName: string, password: string) {
    return this.csrf().pipe(
      switchMap(() => this.http.post<void>('/api/auth/login', { userName, password })),
      switchMap(() => this.csrf()),
      switchMap(() => this.load()),
    );
  }
  customerLogin(email: string, password: string) {
    return this.csrf().pipe(
      switchMap(() => this.http.post<void>('/api/customer/auth/login', { email, password })),
      switchMap(() => this.csrf()),
      switchMap(() => this.load()),
    );
  }
  registerCustomer(email: string, password: string) {
    return this.csrf().pipe(
      switchMap(() => this.http.post<void>('/api/customer/auth/register', { email, password })),
      switchMap(() => this.csrf()),
      switchMap(() => this.load()),
    );
  }
  logout() {
    return this.csrf().pipe(
      switchMap(() => this.http.post<void>('/api/auth/logout', {})),
      catchError((error) =>
        error instanceof HttpErrorResponse && error.status === 401
          ? of(null)
          : throwError(() => error),
      ),
      tap(() => this.session.set(null)),
    );
  }
  changePassword(currentPassword: string, newPassword: string) {
    return this.csrf().pipe(
      switchMap(() => this.http.post<void>('/api/auth/password', { currentPassword, newPassword })),
      tap(() => this.session.set(null)),
    );
  }
  prepare() {
    return this.csrf();
  }
}
