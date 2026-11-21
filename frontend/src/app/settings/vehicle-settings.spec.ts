import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { VehicleSettingsPage } from './vehicle-settings';

describe('Vehicle settings tabs', () => {
  it('keeps both drafts when switching source, including by keyboard', () => {
    TestBed.configureTestingModule({ imports: [VehicleSettingsPage], providers: [provideHttpClient(), provideHttpClientTesting()] });
    const http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(VehicleSettingsPage);
    fixture.detectChanges();
    const columns = [{key: 'merk', name: 'Merk', type: 'text'}];
    http.expectOne('/api/rdw-settings').flush({columns, settings: {revision: 'rdw', columns: ['merk']}});
    http.expectOne('/api/overheid-io-settings').flush({columns, settings: {revision: 'ovio', columns: [], hasApiKey: false}});
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    const tabs = element.querySelectorAll<HTMLButtonElement>('[role="tab"]');
    tabs[1].click(); fixture.detectChanges();
    expect(element.querySelector<HTMLElement>('#overheid-panel')!.hidden).toBe(false);
    const input = element.querySelector<HTMLInputElement>('input[name="apiKey"]')!;
    input.value = 'draft-key'; input.dispatchEvent(new Event('input'));
    tabs[1].dispatchEvent(new KeyboardEvent('keydown', {key: 'ArrowLeft', bubbles: true})); fixture.detectChanges();
    expect(element.querySelector<HTMLElement>('#rdw-panel')!.hidden).toBe(false);
    tabs[1].click(); fixture.detectChanges();
    expect(input.value).toBe('draft-key');
    http.verify();
  });
});
