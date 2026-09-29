import { Component, inject, signal } from '@angular/core';
import { Auth } from './auth/auth';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  readonly auth = inject(Auth);
  private readonly router = inject(Router);
  readonly signingOut = signal(false);
  readonly logoutError = signal('');
  logout() {
    if (this.signingOut()) return;
    this.signingOut.set(true);
    this.logoutError.set('');
    this.auth.logout().subscribe({
      next: () => {
        this.signingOut.set(false);
        void this.router.navigate(['/inloggen']);
      },
      error: () => {
        this.signingOut.set(false);
        this.logoutError.set('Uitloggen is niet bevestigd. Probeer opnieuw.');
      },
    });
  }
}
