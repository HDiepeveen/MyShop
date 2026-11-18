import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { RdwSettingsPage } from './rdw-settings';

describe('RDW settings', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [RdwSettingsPage], providers: [provideHttpClient(), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  it('filters columns and saves the full selection with its revision', () => {
    const page = TestBed.createComponent(RdwSettingsPage).componentInstance;
    const columns = [{ key: 'merk', name: 'Merk', type: 'text' }, { key: 'eerste_kleur', name: 'Eerste kleur', type: 'text' }];
    http.expectOne('/api/rdw-settings').flush({ columns, settings: { revision: 'one', columns: ['merk'] } });
    page.filter = 'kleur';
    expect(page.filtered().map(c => c.key)).toEqual(['eerste_kleur']);
    page.toggle('eerste_kleur', true);
    page.save();
    http.expectOne('/api/auth/csrf').flush(null);
    const save = http.expectOne('/api/rdw-settings');
    expect(save.request.body).toEqual({ revision: 'one', columns: ['merk', 'eerste_kleur'] });
    save.flush(null);
    http.expectOne('/api/rdw-settings').flush({ columns, settings: { revision: 'two', columns: ['merk', 'eerste_kleur'] } });
    expect(page.revision).toBe('two');
    expect(page.notice()).toContain('opgeslagen');
  });
});
