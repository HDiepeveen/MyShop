import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Auth } from '../auth/auth';
import { errorMessage } from '../catalog/error-message';

@Component({
  imports: [FormsModule, RouterLink],
  template: `<div class="eyebrow">Mijn account</div><h1>Inloggen</h1>
    <form class="panel" (ngSubmit)="submit()">
      <label>E-mailadres<input name="email" type="email" autocomplete="email" maxlength="320" [(ngModel)]="email" required /></label>
      <label>Wachtwoord<input name="password" type="password" autocomplete="current-password" maxlength="128" [(ngModel)]="password" required /></label>
      @if (failure()) { <p class="error" role="alert">{{ failure() }}</p> }
      <button [disabled]="busy()">{{ busy() ? 'Inloggen…' : 'Inloggen' }}</button>
    </form><p>Nog geen account? <a routerLink="/winkel/registreren">Registreren</a></p>`,
})
export class CustomerLogin {
  private readonly auth = inject(Auth); private readonly router = inject(Router);
  readonly busy = signal(false); readonly failure = signal(''); email = ''; password = '';
  submit() {
    if (this.busy()) return; this.busy.set(true); this.failure.set('');
    this.auth.customerLogin(this.email, this.password).subscribe({
      next: () => { this.busy.set(false); void this.router.navigate(['/winkel/account']); },
      error: (error) => { this.busy.set(false); this.failure.set(errorMessage(error)); },
    });
  }
}
