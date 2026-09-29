import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { BehaviorSubject, switchMap } from 'rxjs';
import { CatalogApi } from '../catalog.api';
import { loadState } from '../load-state';

@Component({
  imports: [FormsModule, RouterLink],
  template: ` <div class="eyebrow">Assortiment</div>
    <div class="page-head">
      <div>
        <h1>Producten</h1>
        <p class="muted">Alles wat jouw winkel bijzonder maakt.</p>
      </div>
      <a class="button" routerLink="/producten/nieuw">+ Nieuw product</a>
    </div>
    <section class="panel">
      <form class="toolbar" (ngSubmit)="search()">
        <label
          >Zoek op productnaam<input
            name="search"
            [(ngModel)]="searchText"
            placeholder="Bijvoorbeeld: linnen overhemd"
            type="search" /></label
        ><button type="submit" class="secondary">Zoeken</button>
      </form>
      @if (state()?.loading) {
        <p class="loading" role="status">Producten ophalen…</p>
      }
      @if (state()?.error) {
        <div class="error" role="alert">
          {{ state()?.error }} <button class="secondary" (click)="retry()">Opnieuw proberen</button>
        </div>
      }
      @if (state()?.data; as page) {
        @if (page.items.length) {
          <div class="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Product</th>
                  <th>Varianten</th>
                  <th>Bekijken</th>
                </tr>
              </thead>
              <tbody>
                @for (product of page.items; track product.id) {
                  <tr>
                    <td>
                      <a [routerLink]="['/producten', product.id]">{{ product.name }}</a>
                    </td>
                    <td>
                      <span class="badge">{{ product.variantCount }} varianten</span>
                    </td>
                    <td>
                      <a
                        [routerLink]="['/producten', product.id]"
                        [attr.aria-label]="product.name + ' bekijken'"
                        >Details →</a
                      >
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        } @else {
          <div class="empty">
            <h2>Geen producten gevonden</h2>
            <p class="muted">Pas je zoekopdracht aan of voeg je eerste product toe.</p>
          </div>
        }
        <div class="pager">
          <span
            >{{ page.totalCount }} {{ page.totalCount === 1 ? 'product' : 'producten' }} · Pagina
            {{ offset() / 20 + 1 }}</span
          >
          <div class="actions">
            <button class="secondary" [disabled]="offset() === 0" (click)="changePage(-20)">
              Vorige</button
            ><button
              class="secondary"
              [disabled]="offset() + 20 >= page.totalCount"
              (click)="changePage(20)"
            >
              Volgende
            </button>
          </div>
        </div>
      }
    </section>`,
})
export class ProductList {
  private readonly api = inject(CatalogApi);
  private readonly query = new BehaviorSubject({ offset: 0, search: '' });
  readonly state = toSignal(
    this.query.pipe(switchMap((q) => loadState(this.api.products(q.offset, q.search)))),
  );
  readonly offset = signal(0);
  searchText = '';
  search() {
    this.offset.set(0);
    this.query.next({ offset: 0, search: this.searchText });
  }
  changePage(delta: number) {
    this.offset.update((value) => Math.max(0, value + delta));
    this.query.next({ ...this.query.value, offset: this.offset() });
  }
  retry() {
    this.query.next(this.query.value);
  }
}
