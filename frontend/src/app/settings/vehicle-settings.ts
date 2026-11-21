import { Component, signal } from '@angular/core';
import { RdwSettingsPage } from './rdw-settings';
import { OverheidIoSettingsPage } from './overheid-io-settings';

@Component({
  imports: [RdwSettingsPage, OverheidIoSettingsPage],
  template: `
    <h1>Voertuiginstellingen</h1>
    <div class="actions" role="tablist" aria-label="Voertuiggegevensbron" (keydown)="switchTab($event)">
      <button type="button" role="tab" id="rdw-tab" aria-controls="rdw-panel" [attr.aria-selected]="tab() === 'rdw'" [attr.tabindex]="tab() === 'rdw' ? 0 : -1" (click)="tab.set('rdw')">RDW-instellingen</button>
      <button type="button" role="tab" id="overheid-tab" aria-controls="overheid-panel" [attr.aria-selected]="tab() === 'overheid'" [attr.tabindex]="tab() === 'overheid' ? 0 : -1" (click)="tab.set('overheid')">Overheid.io</button>
    </div>
    <section role="tabpanel" id="rdw-panel" aria-labelledby="rdw-tab" [hidden]="tab() !== 'rdw'"><app-rdw-settings /></section>
    <section role="tabpanel" id="overheid-panel" aria-labelledby="overheid-tab" [hidden]="tab() !== 'overheid'"><app-overheid-io-settings /></section>
  `,
  styles: `button[aria-selected="false"] { background: transparent; color: inherit; } [hidden] { display: none !important; }`,
})
export class VehicleSettingsPage {
  readonly tab = signal<'rdw' | 'overheid'>('rdw');
  switchTab(event: KeyboardEvent) {
    if (!['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) return;
    event.preventDefault();
    const next = event.key === 'Home' ? 'rdw' : event.key === 'End' ? 'overheid' : this.tab() === 'rdw' ? 'overheid' : 'rdw';
    this.tab.set(next);
    (event.currentTarget as HTMLElement).querySelectorAll<HTMLButtonElement>('[role="tab"]')[next === 'rdw' ? 0 : 1].focus();
  }
}
