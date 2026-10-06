import { DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { BehaviorSubject, switchMap } from 'rxjs';
import { DashboardApi, DashboardOrderStatus } from './dashboard.api';
import { loadState } from './catalog/load-state';

@Component({
  imports: [RouterLink, DatePipe],
  template: `
    <div class="page-head">
      <div><div class="eyebrow">Vandaag in MyShop</div><h1>Overzicht</h1></div>
      <button class="secondary" [disabled]="state()?.loading" (click)="refresh()">Vernieuwen</button>
    </div>
    @if (state()?.loading) { <p role="status">Overzicht ophalen…</p> }
    @if (state()?.error) {
      <div class="panel error" role="alert">{{ state()?.error }} <button (click)="refresh()">Opnieuw proberen</button></div>
    }
    @if (state()?.data; as data) {
      <div class="grid">
        <a class="panel card-link" routerLink="/producten">
          <span class="card-number">ASSORTIMENT</span><h2>{{ data.productCount }} producten</h2>
          <p>{{ data.publishedProductCount }} gepubliceerd · {{ data.draftProductCount }} concept</p><span class="arrow">↗</span>
        </a>
        <a class="panel card-link" routerLink="/bestellingen">
          <span class="card-number">ACTIE NODIG</span><h2>{{ count(data.orders, 'awaitingPayment') }} wacht op betaling</h2>
          <p>{{ count(data.orders, 'paid') }} betaald · {{ count(data.orders, 'shipped') }} verzonden</p><span class="arrow">↗</span>
        </a>
        <a class="panel card-link" routerLink="/klanten">
          <span class="card-number">KLANTEN</span><h2>{{ data.customerCount }} accounts</h2>
          <p>Bekijk klantgegevens en toegang.</p><span class="arrow">↗</span>
        </a>
      </div>
      <div class="actions">
        <a class="button secondary" routerLink="/producten" [queryParams]="{ published: true }">Gepubliceerde producten bekijken</a>
        <a class="button secondary" routerLink="/producten" [queryParams]="{ published: false }">Conceptproducten bekijken</a>
      </div>
      <div class="grid">
        <section class="panel"><h2>Actieve omzet</h2>
          <p class="muted">Betaalde en verzonden bestellingen, exclusief terugbetalingen.</p>
          @for (total of data.activeRevenue; track total.currency) { <p class="price">{{ total.currency }} {{ amount(total.amount) }}</p> }
          @empty { <p>Nog geen betaalde omzet.</p> }
        </section>
        <section class="panel"><h2>Lage voorraad</h2><p class="muted">Gepubliceerde varianten met maximaal vijf stuks.</p>
          @for (item of data.lowStock; track item.variantId) {
            <p><a [routerLink]="['/producten', item.productId]">{{ item.productName }} · {{ item.variantName }}</a><br />
            <strong>{{ item.quantity }} op voorraad</strong>@if (item.sku) { <span class="muted"> · {{ item.sku }}</span> }</p>
          } @empty { <p>Geen lage gevolgde voorraad.</p> }
        </section>
      </div>
      <section class="panel"><div class="page-head"><h2>Recente bestellingen</h2><a routerLink="/bestellingen">Alle bestellingen</a></div>
        @if (data.recentOrders.length) {
          <div class="table-wrap"><table><thead><tr><th>Bestelling</th><th>Geplaatst</th><th>Klant</th><th>Status</th><th>Totaal</th></tr></thead>
          <tbody>@for (order of data.recentOrders; track order.id) { <tr>
            <td><a [routerLink]="['/bestellingen', order.id]">{{ order.number }}</a></td>
            <td>{{ order.placedAt | date: 'dd-MM-yyyy HH:mm' }}</td><td>{{ order.customerName }}</td>
            <td><span class="badge">{{ status(order.status) }}</span></td>
            <td>@for (total of order.totals; track total.currency) { <div>{{ total.currency }} {{ amount(total.amount) }}</div> }</td>
          </tr> }</tbody></table></div>
        } @else { <p>Nog geen bestellingen.</p> }
      </section>
    }
  `,
})
export class Home {
  private readonly api = inject(DashboardApi);
  private readonly reload = new BehaviorSubject(0);
  readonly state = toSignal(this.reload.pipe(switchMap(() => loadState(this.api.get()))));
  refresh() { this.reload.next(this.reload.value + 1); }
  count(items: { status: DashboardOrderStatus; count: number }[], status: DashboardOrderStatus) {
    return items.find((item) => item.status === status)?.count ?? 0;
  }
  amount(value: string) { return value.replace('.', ','); }
  status(value: DashboardOrderStatus) {
    return value === 'awaitingPayment' ? 'Wacht op betaling' : value === 'paid' ? 'Betaald'
      : value === 'shipped' ? 'Verzonden' : value === 'cancelled' ? 'Geannuleerd' : 'Terugbetaald';
  }
}
