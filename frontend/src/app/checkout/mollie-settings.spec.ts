import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { MollieSettingsEditor } from './mollie-settings';
describe('Managed Mollie settings', () => {
  let http: HttpTestingController;
  const settings = { returnUrl: 'http://localhost:4200/winkel/betaling', webhookUrl: 'https://example.test/api/payments/mollie/webhook', keyConfigured: true, managed: true, revision: 'r' };
  beforeEach(() => { TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] }); http = TestBed.inject(HttpTestingController); });
  afterEach(() => http.verify());
  function editor() {
    const fixture = TestBed.createComponent(MollieSettingsEditor); fixture.detectChanges();
    http.expectNone('/api/payment-options/mollie');
    fixture.componentInstance.load(); http.expectOne('/api/payment-options/mollie').flush(settings); fixture.detectChanges(); return fixture;
  }
  it('keeps the key hidden and sends a replacement only once, clearing the input immediately', () => {
    const fixture = editor(); const page = fixture.componentInstance;
    expect(fixture.nativeElement.querySelector('input[name="mollieKey"]').type).toBe('password');
    page.apiKey = 'test_abcdefghijklmnopqrstuvwxyz'; page.save(); page.save();
    expect(page.apiKey).toBe(''); const request = http.expectOne('/api/payment-options/mollie');
    expect(request.request.body.apiKey).toBe('test_abcdefghijklmnopqrstuvwxyz');
    request.flush(settings); http.expectOne('/api/payment-options/mollie').flush({ ...settings, revision: 'new' });
    expect(page.settings()?.revision).toBe('new'); expect(page.apiKey).toBe('');
  });
  it('preserves existing keys with null and requires an explicit clear flag', () => {
    const page = editor().componentInstance; page.clearKey = true; page.save();
    const request = http.expectOne('/api/payment-options/mollie');
    expect(request.request.body.apiKey).toBeNull(); expect(request.request.body.clearKey).toBe(true);
    request.flush({}, { status: 409, statusText: 'Conflict' });
    expect(page.error()).toBeTruthy(); expect(page.webhookUrl).toBe(settings.webhookUrl);
  });
  it('checks the stored revision and distinguishes unavailable iDEAL from a successful check', () => {
    const page = editor().componentInstance; page.check();
    const request = http.expectOne('/api/payment-options/mollie/check'); expect(request.request.body).toEqual({ revision: 'r' });
    request.flush({ configured: true, keyAccepted: true, idealAvailable: false });
    expect(page.notice()).toContain('iDEAL is nog niet beschikbaar');
    page.check(); http.expectOne('/api/payment-options/mollie/check').flush({ configured: true, keyAccepted: true, idealAvailable: true });
    expect(page.notice()).toContain('iDEAL is beschikbaar'); expect(page.notice()).toContain('echte testbetaling');
  });
});
