import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Auth } from '../auth/auth';
import { errorMessage } from '../catalog/error-message';

@Component({
  imports: [FormsModule, RouterLink],
  template: `<div class="eyebrow">Mijn account</div>
    <h1>Inloggen</h1>
    @if (reason === 'changed') {
      <p role="status">Je wachtwoord is gewijzigd. Log opnieuw in.</p>
    }
    @if (reason === 'expired') {
      <p role="status">Je sessie is verlopen. Log opnieuw in.</p>
    }
    <form class="panel" (ngSubmit)="submit()">
      <label
        >E-mailadres<input
          name="email"
          type="email"
          autocomplete="email"
          maxlength="320"
          [disabled]="busy()"
          [(ngModel)]="email"
          required
      /></label>
      <label
        >Wachtwoord<input
          name="password"
          type="password"
          autocomplete="current-password"
          maxlength="128"
          [disabled]="busy()"
          [(ngModel)]="password"
          required
      /></label>
      @if (failure()) {
        <p class="error" role="alert">{{ failure() }}</p>
      }
      <button
        [disabled]="
          busy() || !email.trim() || email.trim().length > 320 || !password || password.length > 128
        "
      >
        {{ busy() ? 'Inloggen…' : 'Inloggen' }}
      </button>
    </form>
    <p><a routerLink="/winkel/wachtwoord-vergeten">Wachtwoord vergeten?</a></p>
    <p>Nog geen account? <a routerLink="/winkel/registreren">Registreren</a></p>`,
})
export class CustomerLogin {
  readonly reason = inject(ActivatedRoute).snapshot.queryParamMap.get('reason');
  private readonly destroyRef = inject(DestroyRef);
  private readonly auth = inject(Auth);
  private readonly router = inject(Router);
  readonly busy = signal(false);
  readonly failure = signal('');
  email = '';
  password = '';
  submit() {
    if (
      this.busy() ||
      !this.email.trim() ||
      this.email.trim().length > 320 ||
      !this.password ||
      this.password.length > 128
    )
      return;
    this.busy.set(true);
    this.failure.set('');
    this.auth
      .customerLogin(this.email.trim(), this.password)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.password = '';
          this.busy.set(false);
          void this.router.navigate(['/winkel/account']);
        },
        error: (error) => {
          this.busy.set(false);
          this.failure.set(errorMessage(error));
        },
      });
  }
}
