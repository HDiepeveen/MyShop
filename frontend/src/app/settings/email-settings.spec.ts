import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { EmailSettings } from './email-settings';
const saved = {
  enabled: false,
  host: 'smtp.example.test',
  port: 587,
  userName: 'user',
  fromAddress: 'shop@example.test',
  fromName: 'Shop',
  publicBaseUrl: 'https://shop.example.test',
  passwordConfigured: true,
  revision: 'revision',
};
describe('Email settings administration', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [EmailSettings],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  function setup(enabled = false) {
    const fixture = TestBed.createComponent(EmailSettings);
    http.expectOne('/api/email-settings').flush({ ...saved, enabled });
    fixture.detectChanges();
    return { fixture, page: fixture.componentInstance };
  }
  it('never fills the password field with a stored credential', () => {
    const { fixture, page } = setup();
    expect(fixture.nativeElement.querySelector('input[name="password"]').value).toBe('');
    expect(page.password).toBe('');
    expect(fixture.nativeElement.textContent).toContain('Een wachtwoord is opgeslagen.');
  });
  it('preserves the password and prevents duplicate saves or reloads while saving', () => {
    const { page } = setup();
    page.fromName = 'New name';
    page.save();
    page.save();
    page.load();
    http.expectOne('/api/auth/csrf').flush(null);
    const request = http.expectOne('/api/email-settings');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body.password).toBeNull();
    expect(request.request.body.clearPassword).toBe(false);
    expect(request.request.body.revision).toBe('revision');
    request.flush({ ...saved, fromName: 'New name', revision: 'new' });
    expect(page.settings()!.revision).toBe('new');
    expect(page.message()).toContain('opgeslagen');
  });
  it('supports replacing and explicitly clearing passwords', () => {
    const { page } = setup();
    page.password = ' secret ';
    page.save();
    http.expectOne('/api/auth/csrf').flush(null);
    let request = http.expectOne('/api/email-settings');
    expect(request.request.body.password).toBe(' secret ');
    request.flush({ ...saved, revision: 'new' });
    expect(page.password).toBe('');
    page.password = 'discard';
    page.changeClearPassword(true);
    expect(page.password).toBe('');
    page.save();
    http.expectOne('/api/auth/csrf').flush(null);
    request = http.expectOne('/api/email-settings');
    expect(request.request.body.clearPassword).toBe(true);
    expect(request.request.body.password).toBeNull();
    request.flush({ ...saved, revision: 'cleared', passwordConfigured: false });
    expect(page.clearPassword).toBe(false);
  });
  it('retains drafts on conflict and reloads only on an explicit action', () => {
    const { page } = setup();
    page.host = 'draft.example.test';
    page.password = 'secret';
    page.save();
    http.expectOne('/api/auth/csrf').flush(null);
    http.expectOne('/api/email-settings').flush({}, { status: 409, statusText: 'Conflict' });
    expect(page.host).toBe('draft.example.test');
    expect(page.password).toBe('secret');
    expect(page.busy()).toBe(false);
    page.load();
    http.expectOne('/api/email-settings').flush(saved);
    expect(page.password).toBe('');
  });
  it.each([null, 0, 465, 65536, 1.5, NaN])(
    'rejects unsupported port %s without posting',
    (port) => {
      const { page } = setup();
      page.port = port;
      page.save();
      expect(page.failure()).not.toBe('');
      http.expectNone('/api/auth/csrf');
    },
  );
  it('uses saved settings for a test instead of unsubmitted form changes', () => {
    const { page } = setup(true);
    page.enabled = false;
    page.host = 'unsaved';
    page.recipient = ' test@example.test ';
    page.sendTest();
    page.sendTest();
    http.expectOne('/api/auth/csrf').flush(null);
    const request = http.expectOne('/api/email-settings/test');
    expect(request.request.body).toEqual({ recipient: 'test@example.test', revision: 'revision' });
    request.flush({});
    expect(page.message()).toContain('klaargezet');
  });
  it('does not queue a test when stored settings are disabled', () => {
    const { page } = setup();
    page.enabled = true;
    page.recipient = 'test@example.test';
    page.sendTest();
    http.expectNone('/api/auth/csrf');
  });
  it('cancels a pending settings write on leaving the page', () => {
    const { page, fixture } = setup();
    page.save();
    http.expectOne('/api/auth/csrf').flush(null);
    const request = http.expectOne('/api/email-settings');
    fixture.destroy();
    expect(request.cancelled).toBe(true);
  });
});
