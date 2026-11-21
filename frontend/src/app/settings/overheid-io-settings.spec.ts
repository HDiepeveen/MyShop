import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { OverheidIoSettingsPage } from './overheid-io-settings';

describe('Overheid.io settings', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [OverheidIoSettingsPage], providers: [provideHttpClient(), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  it('filters columns and saves the full selection with its revision', () => {
    const page = TestBed.createComponent(OverheidIoSettingsPage).componentInstance;
    const columns = [{ key: 'merk', name: 'Merk', type: 'text' }, { key: 'eerste_kleur', name: 'Eerste kleur', type: 'text' }];
    http.expectOne('/api/overheid-io-settings').flush({ columns, settings: { revision: 'one', columns: ['merk'], hasApiKey: true } });
    page.apiKey = 'new-test-key';
    page.filter = 'kleur';
    expect(page.filtered().map(c => c.key)).toEqual(['eerste_kleur']);
    page.toggle('eerste_kleur', true);
    page.save();
    http.expectOne('/api/auth/csrf').flush(null);
    const save = http.expectOne('/api/overheid-io-settings');
    expect(save.request.body).toEqual({ revision: 'one', columns: ['merk', 'eerste_kleur'], apiKey: 'new-test-key', clearApiKey: false });
    save.flush(null);
    http.expectOne('/api/overheid-io-settings').flush({ columns, settings: { revision: 'two', columns: ['merk', 'eerste_kleur'], hasApiKey: true } });
    expect(page.apiKey).toBe('');
    expect(page.hasApiKey).toBe(true);
    expect(page.revision).toBe('two');
    expect(page.notice()).toContain('opgeslagen');
  });
});
