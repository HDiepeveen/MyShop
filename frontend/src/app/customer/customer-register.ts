import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Auth } from '../auth/auth';
import { errorMessage } from '../catalog/error-message';

@Component({
  imports: [FormsModule, RouterLink],
  template: `<div class="eyebrow">Mijn account</div><h1>Registreren</h1>
    <form class="panel" (ngSubmit)="submit()">
      <label>E-mailadres<input name="email" type="email" autocomplete="email" maxlength="320" [(ngModel)]="email" required /></label>
      <label>Wachtwoord<input name="password" type="password" autocomplete="new-password" minlength="12" maxlength="128" [(ngModel)]="password" required /></label>
      <p class="muted">Minimaal 12 tekens met een hoofdletter, kleine letter, cijfer en symbool.</p>
      <label>Herhaal wachtwoord<input name="confirm" type="password" autocomplete="new-password" maxlength="128" [(ngModel)]="confirmation" required /></label>
      @if (failure()) { <p class="error" role="alert">{{ failure() }}</p> }
      <button [disabled]="busy()">{{ busy() ? 'Account maken…' : 'Account maken' }}</button>
    </form><p>Al een account? <a routerLink="/winkel/inloggen">Inloggen</a></p>`,
})
export class CustomerRegister {
  private readonly auth = inject(Auth); private readonly router = inject(Router);
  readonly busy = signal(false); readonly failure = signal(''); email = ''; password = ''; confirmation = '';
  submit() {
    this.failure.set('');
    if (this.password !== this.confirmation) { this.failure.set('De wachtwoorden zijn niet gelijk.'); return; }
    if (this.busy()) return; this.busy.set(true);
    this.auth.registerCustomer(this.email, this.password).subscribe({
      next: () => { this.busy.set(false); void this.router.navigate(['/winkel/account']); },
      error: (error) => { this.busy.set(false); this.failure.set(errorMessage(error)); },
    });
  }
}
