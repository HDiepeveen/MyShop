import { managementMenu, managementMenuGroups } from './management-menu';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, switchMap } from 'rxjs';
import { Auth } from './auth';
import { errorMessage } from '../catalog/error-message';

export interface CompanyRole { id: string; name: string; revision: string; permissions: string[]; }
export interface CompanyAccount { id: string; userName: string; email: string; revision: string; enabled: boolean; roles: string[]; }
export interface CompanyAccessData { permissions: { code: string; name: string }[]; roles: CompanyRole[]; accounts: CompanyAccount[]; }

@Component({
  imports: [FormsModule],
  template: `
    <h1>Bedrijfsbeheerders en rollen</h1>
    <p>Maak rollen met toegang tot geselecteerde beheeronderdelen en wijs ze toe aan bedrijfsbeheerders.
      Toegang geeft ook de bewerkacties van dat onderdeel. Meerdere rollen combineren hun rechten.
      Alleen de volledige beheerder kan accounts en rollen beheren.</p>
    @if (failure()) { <p class="error" role="alert">{{ failure() }}</p> }
    @if (notice()) { <p role="status">{{ notice() }}</p> }
    @if (loading()) { <p role="status">Gegevens ophalen…</p> }
    <button type="button" class="secondary refresh-overview" [disabled]="busy() || loading()" (click)="load()">Gegevens vernieuwen</button>
    @if (data(); as items) {
      <section class="panel">
        <h2>Bedrijfsrollen</h2>
        <h3>Bestaande rollen</h3>
        @for (role of items.roles; track role.id) {
          <div class="access-row">
          <p><strong>{{ role.name }}</strong> · {{ permissionNames(role) || 'Geen beheeronderdelen' }}</p>
          <button type="button" class="secondary" [disabled]="busy()" (click)="editRole(role)">Rol aanpassen</button>
          </div>
        } @empty { <p>Er zijn nog geen bedrijfsrollen.</p> }
        <h3 class="access-form-heading">{{ editingRole ? 'Rol aanpassen' : 'Nieuwe rol' }}</h3>
        <form (ngSubmit)="saveRole()">
          <label>Rolnaam <input name="roleName" [(ngModel)]="roleName" required maxlength="80" [disabled]="busy()" /></label>
          <fieldset [disabled]="busy()">
            <legend>Toegang tot beheeronderdelen</legend>
            @for (group of permissionGroups(); track $index) {
              <div class="permission-group">
                @for (permission of group; track permission.code) {
                  @if (permission.code === 'companyAccess') {
                    <p class="admin-only">{{ permission.name }} — alleen de volledige beheerder</p>
                  } @else {
                    <label><input type="checkbox" [checked]="rolePermissions.includes(permission.code)"
                      (change)="togglePermission(permission.code, $event)" />{{ permission.name }}</label>
                  }
                }
              </div>
            }
          </fieldset>
          <button type="submit" [disabled]="busy() || !roleName.trim()">Rol opslaan</button>
          @if (editingRole) { <button type="button" class="secondary" [disabled]="busy()" (click)="resetRole()">Nieuwe rol</button> }
        </form>
      </section>
      <section class="panel">
        <h2>Bedrijfsaccounts</h2>
        <h3>Bestaande accounts</h3>
        @for (account of items.accounts; track account.id) {
          <div class="access-row">
          <p><strong>{{ account.userName }}</strong> · {{ account.email }} · {{ account.enabled ? 'Actief' : 'Geblokkeerd' }}<br />
            Rollen: {{ roleNames(account) || 'Geen rollen' }}</p>
          <button type="button" class="secondary" [disabled]="busy()" (click)="editAccount(account)">Account en rollen aanpassen</button>
          </div>
        } @empty { <p>Er zijn nog geen bedrijfsaccounts.</p> }
        <h3 class="access-form-heading account-form-divider">{{ editingAccount ? 'Account aanpassen: ' + editingAccount.userName : 'Nieuwe Account' }}</h3>
        <form (ngSubmit)="saveAccount()">
          @if (!editingAccount) {
            <label>Gebruikersnaam <input name="userName" [(ngModel)]="userName" required maxlength="100" autocomplete="off" [disabled]="busy()" /></label>
            <label>E-mailadres <input name="email" type="email" [(ngModel)]="email" required maxlength="320" [disabled]="busy()" /></label>
            <label>Initieel wachtwoord <input name="password" type="password" [(ngModel)]="password" required minlength="12" maxlength="128" autocomplete="new-password" [disabled]="busy()" /></label>
            <p>Minimaal 12 tekens, met hoofdletter, kleine letter, cijfer en speciaal teken. De bedrijfsbeheerder kan dit zelf wijzigen na het inloggen.</p>
          } @else {
            <label><input type="checkbox" name="enabled" [(ngModel)]="enabled" [disabled]="busy()" />Account actief</label>
          }
          <fieldset [disabled]="busy()">
            <legend>Bedrijfsrollen</legend>
            @for (role of items.roles; track role.id) {
              <label><input type="checkbox" [checked]="accountRoles.includes(role.id)" (change)="toggleRole(role.id, $event)" />{{ role.name }}</label>
            } @empty { <p>Maak eerst een bedrijfsrol aan.</p> }
          </fieldset>
          <button type="submit" [disabled]="busy() || !accountRoles.length || (!editingAccount && (!userName.trim() || !email.trim() || password.length < 12))">Account opslaan</button>
          @if (editingAccount) { <button type="button" class="secondary" [disabled]="busy()" (click)="resetAccount()">Nieuw account</button> }
        </form>
      </section>
    }
  `,
  styles: [`.account-form-divider { border-top: 1px solid #9eb7af; padding-top: 16px; } .access-form-heading { margin-top: 32px; } .access-row { display: flex; align-items: center; gap: 16px; padding-block: 8px; } .access-row p { margin: 0; min-width: 0; overflow-wrap: anywhere; } .access-row button { margin: 0 0 0 auto; flex-shrink: 0; } @media (max-width: 600px) { .access-row { flex-wrap: wrap; } } .permission-group + .permission-group { border-top: 1px solid #9eb7af; margin-top: 16px; padding-top: 16px; } .admin-only { margin: 0; }`],
})
export class CompanyAccessPage {
  permissionGroups() {
    const available = new Set(this.data()?.permissions.map(item => item.code) ?? []);
    return managementMenuGroups.map(group => group.filter(item => item.code === 'companyAccess' || available.has(item.code)))
      .filter(group => group.length > 0);
  }
  private readonly http = inject(HttpClient);
  private readonly auth = inject(Auth);
  private readonly destroyRef = inject(DestroyRef);
  readonly data = signal<CompanyAccessData | null>(null);
  readonly loading = signal(false);
  readonly busy = signal(false);
  readonly failure = signal('');
  readonly notice = signal('');
  editingRole: CompanyRole | null = null;
  roleName = '';
  rolePermissions: string[] = [];
  editingAccount: CompanyAccount | null = null;
  userName = ''; email = ''; password = ''; enabled = true;
  accountRoles: string[] = [];
  constructor() { this.load(); }
  load() {
    if (this.busy() || this.loading()) return;
    this.loading.set(true); this.failure.set('');
    this.http.get<CompanyAccessData>('/api/company-access').pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: value => { this.data.set(value); this.loading.set(false); this.resetRole(); this.resetAccount(); },
      error: error => { this.loading.set(false); this.failure.set(errorMessage(error)); },
    });
  }
  editRole(role: CompanyRole) {
    if (this.busy() || !this.data()?.roles.includes(role)) return;
    this.editingRole = role; this.roleName = role.name; this.rolePermissions = [...role.permissions];
  }
  resetRole() { this.editingRole = null; this.roleName = ''; this.rolePermissions = []; }
  editAccount(account: CompanyAccount) {
    if (this.busy() || !this.data()?.accounts.includes(account)) return;
    this.resetAccount(); this.editingAccount = account; this.accountRoles = [...account.roles]; this.enabled = account.enabled;
  }
  resetAccount() { this.editingAccount = null; this.userName = ''; this.email = ''; this.password = ''; this.accountRoles = []; this.enabled = true; }
  togglePermission(code: string, event: Event) {
    if (this.busy()) return;
    this.rolePermissions = this.toggle(this.rolePermissions, code, event);
  }
  toggleRole(id: string, event: Event) {
    if (this.busy()) return;
    this.accountRoles = this.toggle(this.accountRoles, id, event);
  }
  private toggle(items: string[], item: string, event: Event) {
    return (event.target as HTMLInputElement).checked ? [...new Set([...items, item])] : items.filter(value => value !== item);
  }
  permissionNames(role: CompanyRole) { return managementMenu.filter(item => role.permissions.includes(item.code)).map(item => item.name).join(', '); }
  roleNames(account: CompanyAccount) { return this.data()?.roles.filter(role => account.roles.includes(role.id)).map(role => role.name).join(', '); }
  saveRole() {
    if (this.busy() || this.loading() || !this.data() || !this.roleName.trim() || this.roleName.trim().length > 80) return;
    const body = { name: this.roleName.trim(), permissions: [...this.rolePermissions], revision: this.editingRole?.revision };
    this.save(() => this.editingRole
      ? this.http.put('/api/company-access/roles/' + encodeURIComponent(this.editingRole.id), body)
      : this.http.post('/api/company-access/roles', body));
  }
  saveAccount() {
    if (this.busy() || this.loading() || !this.data() || !this.accountRoles.length) return;
    const account = this.editingAccount;
    if (!account && (!this.userName.trim() || !this.email.trim() || this.password.length < 12 || this.password.length > 128)) return;
    const body = account
      ? { revision: account.revision, enabled: this.enabled, roles: [...this.accountRoles] }
      : { userName: this.userName.trim(), email: this.email.trim(), password: this.password, roles: [...this.accountRoles] };
    this.save(() => account
      ? this.http.put('/api/company-access/accounts/' + encodeURIComponent(account.id), body)
      : this.http.post('/api/company-access/accounts', body));
  }
  private save(action: () => Observable<unknown>) {
    this.busy.set(true); this.failure.set(''); this.notice.set('');
    this.auth.prepare().pipe(switchMap(action), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => { this.busy.set(false); this.password = ''; this.notice.set('De gegevens zijn opgeslagen.'); this.load(); },
      error: error => { this.busy.set(false); this.password = ''; this.failure.set(error instanceof HttpErrorResponse && typeof error.error?.message === 'string' ? error.error.message : errorMessage(error)); },
    });
  }
}
