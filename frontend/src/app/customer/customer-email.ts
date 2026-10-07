import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { switchMap, Subscription } from 'rxjs';
import { Auth } from '../auth/auth';
import { errorMessage } from '../catalog/error-message';

@Component({
  imports: [FormsModule, RouterLink],
  template: `<h1>{{ title }}</h1>
    @if (!done()) {
      <form class="panel" (ngSubmit)="submit()">
        @if (requestMode) {
          <label
            >E-mailadres<input
              name="email"
              type="email"
              autocomplete="email"
              maxlength="320"
              required
              [(ngModel)]="email"
              [disabled]="busy()"
          /></label>
        }
        @if (mode === 'reset') {
          <label
            >Nieuw wachtwoord<input
              name="password"
              type="password"
              autocomplete="new-password"
              minlength="12"
              maxlength="128"
              required
              [(ngModel)]="password"
              [disabled]="busy()"
          /></label>
          <p>Minimaal 12 tekens met hoofdletter, kleine letter, cijfer en symbool.</p>
          <label
            >Herhaal wachtwoord<input
              name="confirmation"
              type="password"
              autocomplete="new-password"
              maxlength="128"
              required
              [(ngModel)]="confirmation"
              [disabled]="busy()"
          /></label>
        }
        @if (mode === 'confirm') {
          <p>Klik hieronder om je e-mailadres te bevestigen.</p>
        }
        <button [disabled]="busy()">
          {{
            busy()
              ? 'Verwerken…'
              : requestMode
                ? 'E-mail aanvragen'
                : mode === 'confirm'
                  ? 'E-mailadres bevestigen'
                  : 'Wachtwoord herstellen'
          }}
        </button>
      </form>
    }
    @if (message()) {
      <p role="status">{{ message() }}</p>
    }
    @if (failure()) {
      <p role="alert">{{ failure() }}</p>
    }
    @if (!requestMode) {
      <p>
        <a
          [routerLink]="
            mode === 'confirm' ? '/winkel/bevestiging-aanvragen' : '/winkel/wachtwoord-vergeten'
          "
          >Nieuwe link aanvragen</a
        >
      </p>
    }
    <p>
      <a routerLink="/winkel/inloggen">Naar inloggen</a> ·
      <a routerLink="/winkel/account">Mijn account</a>
    </p>`,
})
export class CustomerEmail {
  private readonly route = inject(ActivatedRoute);
  private readonly http = inject(HttpClient);
  private readonly auth = inject(Auth);
  private readonly destroyRef = inject(DestroyRef);
  readonly mode = this.route.snapshot.data['emailMode'] as
    'forgot' | 'reset' | 'confirm' | 'resend';
  readonly requestMode = this.mode === 'forgot' || this.mode === 'resend';
  readonly title = {
    forgot: 'Wachtwoord vergeten',
    reset: 'Wachtwoord herstellen',
    confirm: 'E-mailadres bevestigen',
    resend: 'Bevestigingsmail aanvragen',
  }[this.mode];
  readonly busy = signal(false);
  readonly done = signal(false);
  readonly message = signal('');
  readonly failure = signal('');
  email = '';
  password = '';
  confirmation = '';
  private operation?: Subscription;
  constructor() {
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.operation?.unsubscribe();
      this.busy.set(false);
      this.done.set(false);
      this.message.set('');
      this.failure.set('');
      this.password = this.confirmation = '';
    });
  }
  submit() {
    if (this.busy() || this.done()) return;
    this.failure.set('');
    if (this.requestMode && (!this.email.trim() || this.email.trim().length > 320)) {
      this.failure.set('Vul een geldig e-mailadres van maximaal 320 tekens in.');
      return;
    }
    if (
      this.mode === 'reset' &&
      (this.password.length < 12 ||
        this.password.length > 128 ||
        this.password !== this.confirmation)
    ) {
      this.failure.set('Gebruik gelijke wachtwoorden van 12 tot en met 128 tekens.');
      return;
    }
    const params = this.route.snapshot.queryParamMap;
    const userId = params.get('userId') ?? '';
    const token = params.get('token') ?? '';
    if (!this.requestMode && (!userId || userId.length > 450 || !token || token.length > 4096)) {
      this.failure.set('De link is ongeldig. Vraag een nieuwe link aan.');
      return;
    }
    const endpoint = {
      forgot: 'request-password-reset',
      reset: 'reset-password',
      confirm: 'confirm-email',
      resend: 'request-confirmation',
    }[this.mode];
    const body = this.requestMode
      ? { email: this.email.trim() }
      : this.mode === 'reset'
        ? { userId, token, password: this.password }
        : { userId, token };
    this.busy.set(true);
    this.operation = this.auth
      .prepare()
      .pipe(
        switchMap(() => this.http.post<void>('/api/customer/auth/' + endpoint, body)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.password = this.confirmation = '';
          this.busy.set(false);
          this.done.set(true);
          this.message.set(
            this.requestMode
              ? 'Als dit e-mailadres in aanmerking komt, ontvang je een e-mail. Controleer ook je spammap.'
              : this.mode === 'confirm'
                ? 'Je e-mailadres is bevestigd.'
                : 'Je wachtwoord is hersteld. Log opnieuw in.',
          );
          if (this.mode === 'reset') this.auth.session.set(null);
        },
        error: (error) => {
          this.busy.set(false);
          this.failure.set(
            error instanceof HttpErrorResponse &&
              error.status === 400 &&
              error.error?.code === 'invalidLink'
              ? 'De link of het wachtwoord is ongeldig. Vraag een nieuwe link aan en gebruik een sterk wachtwoord.'
              : errorMessage(error),
          );
        },
      });
  }
}
