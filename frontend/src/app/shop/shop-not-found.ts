import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
@Component({
  imports: [RouterLink],
  template: `<h1>Pagina niet gevonden</h1>
    <p>Deze pagina is niet beschikbaar.</p>
    <a routerLink="/winkel">Naar het assortiment</a>`,
})
export class ShopNotFound {}
