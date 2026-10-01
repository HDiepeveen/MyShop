import { Component, DestroyRef, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Auth } from './auth';

@Component({
  imports: [FormsModule],
  template: `
    <h1>Wachtwoord wijzigen</h1>
    <p>
      Gebruik 12–128 tekens met een hoofdletter, kleine letter, cijfer en symbool. Na wijzigen log
      je opnieuw in; ook andere sessies vervallen.
    </p>
    <form class="panel" (ngSubmit)="submit()">
      <label
        >Huidig wachtwoord<input
          name="current"
          type="password"
          autocomplete="current-password"
          [(ngModel)]="current"
          required
          maxlength="128"
          [disabled]="busy()"
      /></label>
      <label
        >Nieuw wachtwoord<input
          name="password"
          type="password"
          autocomplete="new-password"
          [(ngModel)]="password"
          required
          minlength="12"
          maxlength="128"
          [disabled]="busy()"
      /></label>
      <label
        >Herhaal nieuw wachtwoord<input
          name="confirm"
          type="password"
          autocomplete="new-password"
          [(ngModel)]="confirm"
          required
          maxlength="128"
          [disabled]="busy()"
      /></label>
      <button [disabled]="busy() || !current || password.length < 12 || password !== confirm">
        {{ busy() ? 'Opslaan…' : 'Wachtwoord wijzigen' }}
      </button>
      @if (error()) {
        <p role="alert" class="error">{{ error() }}</p>
      }
    </form>
  `,
})
export class Password {
  private readonly auth = inject(Auth);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  readonly busy = signal(false);
  readonly error = signal('');
  current = '';
  password = '';
  confirm = '';
  submit() {
    if (
      this.busy() ||
      !this.current ||
      this.password.length < 12 ||
      this.password.length > 128 ||
      this.password !== this.confirm
    )
      return;
    this.busy.set(true);
    this.error.set('');
    this.auth
      .changePassword(this.current, this.password)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.current = this.password = this.confirm = '';
          const login = this.router.url.startsWith('/winkel/') ? '/winkel/inloggen' : '/inloggen';
          void this.router.navigate([login], { queryParams: { reason: 'changed' } });
        },
        error: () => {
          this.busy.set(false);
          this.error.set(
            'Wijzigen mislukt. Controleer het huidige wachtwoord en de wachtwoordeisen. Log opnieuw in als je sessie is verlopen.',
          );
        },
      });
  }
}
