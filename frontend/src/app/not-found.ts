import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
@Component({
  imports: [RouterLink],
  template: `<section class="panel empty">
    <div class="eyebrow">Pagina niet gevonden</div>
    <h1>Hier is nog niets te vinden.</h1>
    <p class="muted">Controleer het adres of ga terug naar je overzicht.</p>
    <a class="button" routerLink="/">Naar het overzicht</a>
  </section>`,
})
export class NotFound {}
