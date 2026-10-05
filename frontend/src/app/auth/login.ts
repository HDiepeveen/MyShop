import { Component, DestroyRef, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Auth } from './auth';
import { safeReturnUrl } from './auth-routing';

@Component({
  imports: [FormsModule],
  template: `
    <h1>Inloggen</h1>
    <p class="muted">Log in met je MyShop-beheerdersaccount.</p>
    @if (reason === 'expired') {
      <p role="status">Je sessie is verlopen. Log opnieuw in.</p>
    }
    @if (reason === 'changed') {
      <p role="status">Je wachtwoord is gewijzigd. Log opnieuw in.</p>
    }
    @if (reason === 'unavailable') {
      <p role="alert">De winkel kon niet worden bereikt. Probeer opnieuw in te loggen.</p>
    }
    <form class="panel" (ngSubmit)="submit()">
      <label
        >Gebruikersnaam<input
          name="userName"
          [(ngModel)]="userName"
          autocomplete="username"
          maxlength="256"
          required
          [disabled]="busy()"
      /></label>
      <label
        >Wachtwoord<input
          name="password"
          [(ngModel)]="password"
          type="password"
          autocomplete="current-password"
          maxlength="128"
          required
          [disabled]="busy()"
      /></label>
      <button
        [disabled]="
          busy() ||
          !userName.trim() ||
          userName.trim().length > 256 ||
          !password ||
          password.length > 128
        "
      >
        {{ busy() ? 'Inloggen…' : 'Inloggen' }}
      </button>
      @if (error()) {
        <p class="error" role="alert">{{ error() }}</p>
      }
    </form>
  `,
})
export class Login {
  private readonly auth = inject(Auth);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  readonly reason = this.route.snapshot.queryParamMap.get('reason');
  readonly busy = signal(false);
  readonly error = signal('');
  userName = '';
  password = '';
  submit() {
    if (
      this.busy() ||
      !this.userName.trim() ||
      this.userName.trim().length > 256 ||
      !this.password ||
      this.password.length > 128
    )
      return;
    this.busy.set(true);
    this.error.set('');
    this.auth
      .login(this.userName.trim(), this.password)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (session) => {
          this.password = '';
          this.busy.set(false);
          void this.router.navigateByUrl(
            session.administrator
              ? safeReturnUrl(this.route.snapshot.queryParamMap.get('returnUrl'))
              : '/geen-toegang',
          );
        },
        error: (error) => {
          this.password = '';
          this.busy.set(false);
          this.error.set(
            error instanceof HttpErrorResponse && error.status === 401
              ? 'Inloggen mislukt. Controleer je gegevens of probeer het over 15 minuten opnieuw.'
              : error instanceof HttpErrorResponse && error.status === 429
                ? 'Te veel pogingen. Wacht een minuut en probeer opnieuw.'
                : 'Inloggen kon niet worden voltooid. Probeer opnieuw.',
          );
        },
      });
  }
}
