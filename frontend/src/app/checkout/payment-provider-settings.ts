import { Component } from '@angular/core';
import { MollieSettingsEditor } from './mollie-settings';

@Component({
  imports: [MollieSettingsEditor],
  template: `
    <h1>Betaalinstellingen</h1>
    <p>Stel hier de koppeling met je betaalprovider in. De betaalmogelijkheden voor klanten kies je bij Betaalopties.</p>
    <app-mollie-settings />
  `,
})
export class PaymentProviderSettings {}
