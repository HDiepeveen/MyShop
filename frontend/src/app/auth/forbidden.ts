import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
@Component({
  imports: [RouterLink],
  template: `<h1>Geen toegang</h1>
    <p>Dit account heeft geen beheerdersrechten.</p>
    <a routerLink="/inloggen">Naar inloggen</a>`,
})
export class Forbidden {}
