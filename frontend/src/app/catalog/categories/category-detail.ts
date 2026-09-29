import { CategoryEdit } from './category-edit';
import { CategoryManagement } from './category-management';
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { BehaviorSubject, combineLatest, tap, distinctUntilChanged, switchMap } from 'rxjs';
import { CatalogApi } from '../catalog.api';
import { loadState } from '../load-state';

@Component({
  imports: [RouterLink, CategoryEdit, CategoryManagement],
  template: `
    <a class="back" routerLink="/categorieen">← Alle categorieën</a>
    @if (state()?.loading) {
      <p class="loading" role="status">Categorie ophalen…</p>
    }
    @if (state()?.error) {
      <p class="error" role="alert">
        {{ state()?.error }} <button class="secondary" (click)="reload()">Opnieuw proberen</button>
      </p>
    }
    @if (notice()) {
      <p class="success" role="status">{{ notice() }}</p>
    }
    @if (state()?.data; as category) {
      <div class="eyebrow">{{ category.isRoot ? 'Hoofdcategorie' : 'Subcategorie' }}</div>
      <h1>{{ category.name }}</h1>
      <app-category-edit [category]="category" (saved)="onSaved()" />
      <app-category-management
        [category]="category"
        (saved)="onManaged($event)"
        (removed)="onRemoved()"
      />
      <section class="panel">
        <h2>Plek in het assortiment</h2>
        @if (category.parentCategoryId) {
          <a [routerLink]="['/categorieen', category.parentCategoryId]"
            >Bekijk bovenliggende categorie</a
          >
        } @else {
          <p class="muted">Deze categorie staat op het hoogste niveau.</p>
        }
      </section>
    }
  `,
})
export class CategoryDetail {
  private readonly api = inject(CatalogApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly refresh = new BehaviorSubject(0);
  readonly notice = signal('');
  readonly state = toSignal(
    combineLatest([
      this.route.paramMap.pipe(
        distinctUntilChanged((a, b) => a.get('id') === b.get('id')),
        tap(() => this.notice.set('')),
      ),
      this.refresh,
    ]).pipe(switchMap(([params]) => loadState(this.api.category(params.get('id')!)))),
  );
  onSaved() {
    this.notice.set('De categorienaam is bijgewerkt.');
    this.reload();
  }
  onManaged(message: string) {
    this.notice.set(message);
    this.reload();
  }
  onRemoved() {
    void this.router.navigate(['/categorieen']);
  }
  reload() {
    this.refresh.next(this.refresh.value + 1);
  }
}
