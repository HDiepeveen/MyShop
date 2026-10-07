import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { switchMap } from 'rxjs';
import { Auth } from '../auth/auth';

export interface EmailSettingsData {
  enabled: boolean;
  host: string;
  port: number;
  userName: string;
  fromAddress: string;
  fromName: string;
  publicBaseUrl: string;
  passwordConfigured: boolean;
  revision: string;
}
export type SaveEmailSettings = Omit<EmailSettingsData, 'passwordConfigured'> & {
  password: string | null;
  clearPassword: boolean;
};
@Injectable({ providedIn: 'root' })
export class EmailSettingsApi {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(Auth);
  get() {
    return this.http.get<EmailSettingsData>('/api/email-settings');
  }
  save(value: SaveEmailSettings) {
    return this.auth
      .prepare()
      .pipe(switchMap(() => this.http.put<EmailSettingsData>('/api/email-settings', value)));
  }
  test(recipient: string, revision: string) {
    return this.auth
      .prepare()
      .pipe(switchMap(() => this.http.post('/api/email-settings/test', { recipient, revision })));
  }
}
