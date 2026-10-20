import { Component, effect, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { BehaviorSubject, combineLatest, switchMap } from 'rxjs';
import { loadState } from '../catalog/load-state';
import { ShopList } from './shop-list';
import { CategoryPageInfo } from './category-page';
import { StorefrontSeo } from './storefront-seo';
@Component({
  imports: [ShopList],
  template: `
    @if (state()?.loading) {
      <p role="status">Categorie ophalen…</p>
    }
    @if (state()?.error) {
      <h1>Categorie niet beschikbaar</h1>
      <p>Deze categorie bestaat niet of kon niet worden opgehaald.</p>
      <button type="button" (click)="retry()">Opnieuw proberen</button>
    }
    @if (state()?.data; as category) {
      <app-shop-list [categoryPage]="category" />
    }
  `,
})
export class ShopCategory {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly seo = inject(StorefrontSeo);
  private readonly refresh = new BehaviorSubject(0);
  retry() {
    this.refresh.next(this.refresh.value + 1);
  }
  readonly state = toSignal(
    combineLatest([this.route.paramMap, this.refresh]).pipe(
      switchMap(([params]) =>
        loadState(
          this.http.get<CategoryPageInfo>(
            '/api/shop/category-pages/' + encodeURIComponent(params.get('category') ?? ''),
          ),
        ),
      ),
    ),
  );
  constructor() {
    effect(() => {
      if (this.state()?.error) {
        this.seo.apply(
          'Categorie niet beschikbaar',
          undefined,
          '/assortiment/' + this.route.snapshot.paramMap.get('category'),
        );
        this.seo.reset('/geen-toegang');
      }
    });
  }
}
