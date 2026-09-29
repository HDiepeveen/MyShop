import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  imports: [RouterLink],
  template: ` <div class="eyebrow">Jouw assortiment, op één plek</div>
    <section class="panel hero">
      <h1>Ruimte voor goede producten.</h1>
      <p class="muted">
        Maak je catalogus overzichtelijk. Beheer producten, breng structuur aan en houd de details
        op orde.
      </p>
      <a class="button" routerLink="/producten"
        >Bekijk producten <span aria-hidden="true">→</span></a
      >
    </section>
    <div class="page-head">
      <h2>Aan de slag</h2>
      <span class="muted">Van structuur naar assortiment</span>
    </div>
    <div class="grid">
      <a class="panel card-link" routerLink="/producttypen"
        ><span class="card-number">01 / DE BASIS</span>
        <h2>Producttypen</h2>
        <p>Groepeer vergelijkbare producten in herkenbare typen.</p>
        <span class="arrow" aria-hidden="true">↗</span></a
      >
      <a class="panel card-link" routerLink="/categorieen"
        ><span class="card-number">02 / STRUCTUUR</span>
        <h2>Categorieën</h2>
        <p>Geef je assortiment een duidelijke indeling.</p>
        <span class="arrow" aria-hidden="true">↗</span></a
      >
      <a class="panel card-link" routerLink="/producten"
        ><span class="card-number">03 / ASSORTIMENT</span>
        <h2>Producten</h2>
        <p>Beheer producten, varianten en de kwaliteit van je gegevens.</p>
        <span class="arrow" aria-hidden="true">↗</span></a
      >
    </div>`,
})
export class Home {}
